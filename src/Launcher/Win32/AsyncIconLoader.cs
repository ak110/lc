using System.Collections.Concurrent;
using System.IO;
using Launcher.Infrastructure;

namespace Launcher.Win32;

/// <summary>
/// アイコン読み込んだぞイベントの引数。
/// 失敗時は Icon == null
/// </summary>
public sealed class IconLoadedEventArgs : EventArgs
{
    public System.Drawing.Icon? Icon { get; }
    public string FileName { get; set; }
    public bool Small { get; set; }
    public object? Arg { get; }
    /// <summary>リクエスト時の世代番号</summary>
    public int Generation { get; }
    public IconLoadedEventArgs(System.Drawing.Icon? icon, string fileName, bool small, object? arg, int generation)
    {
        Icon = icon;
        FileName = fileName;
        Small = small;
        Arg = arg;
        Generation = generation;
    }
}

/// <summary>
/// アイコン非同期読み込み。
/// BlockingCollection + 固定数STAワーカースレッドで構成。
/// Shell APIのSTA制約のためTask.Run(ThreadPool/MTA)は使用不可。
///
/// ワーカースレッドはコンストラクタで一括起動され、Disposeまで長寿命で生存する。
/// Clear()はキュードレイン+世代インクリメントのみでスレッドの停止・再作成は行わない。
///
/// Shell API (SHGetFileInfo) は高並行度で不安定になるため、ワーカー数は
/// ProcessorCount等の動的な値ではなく固定値(デフォルト8本)を使用する。
/// </summary>
public sealed class AsyncIconLoader : IDisposable
{
    const int DefaultWorkerCount = 8;
    const int MaxRetries = 2; // 合計最大3回試行

    readonly BlockingCollection<Request> queue = new();
    readonly CancellationTokenSource cts = new();
    readonly Func<string, bool, System.Drawing.Icon?> extractIcon;
    volatile int generation;
    bool disposed;

    struct Request
    {
        public string FileName;
        public bool Small;
        public object? Arg;
        public int Generation;
        public int RetryCount;
        public Request(string fileName, bool small, object? arg, int generation)
        {
            FileName = fileName;
            Small = small;
            Arg = arg;
            Generation = generation;
            RetryCount = 0;
        }
    }

    /// <summary>
    /// 現在の世代番号。UIスレッドから参照して古い結果を破棄するために使用。
    /// </summary>
    public int Generation => generation;

    /// <summary>
    /// ワーカースレッドの優先度（読み取り専用）。コンストラクタで設定。
    /// </summary>
    public ThreadPriority ThreadPriority { get; }

    /// <summary>
    /// アイコン読み込んだぞイベント
    /// </summary>
    public event EventHandler<IconLoadedEventArgs>? IconLoaded;

    /// <summary>
    /// コンストラクタ。固定数のSTAワーカースレッドを一括起動する。
    /// </summary>
    /// <param name="workerCount">ワーカースレッド数（デフォルト8）</param>
    /// <param name="threadPriority">ワーカースレッドの優先度</param>
    /// <param name="extractIcon">アイコン抽出関数（テスト時にモック差し替え可能）</param>
    public AsyncIconLoader(
        int workerCount = DefaultWorkerCount,
        ThreadPriority threadPriority = ThreadPriority.Normal,
        Func<string, bool, System.Drawing.Icon?>? extractIcon = null)
    {
        ThreadPriority = threadPriority;
        this.extractIcon = extractIcon ?? DefaultExtractIcon;
        // Dispose()とのレース回避: スレッド開始前にトークンを取得しておく
        var token = cts.Token;
        for (int i = 0; i < workerCount; i++)
        {
            var thread = new Thread(() => OnThread(token));
            thread.SetApartmentState(ApartmentState.STA); // Shell API (SHGetFileInfo) にはSTAが必須
            thread.IsBackground = true;
            thread.Priority = threadPriority;
            thread.Start();
        }
    }

    static System.Drawing.Icon? DefaultExtractIcon(string fileName, bool small)
    {
        string normalized = PathHelper.PathNormalize(fileName);

        // シェル名前空間パス（"::{CLSID}"や"shell:xxx"形式）はPIDL経由で取得
        if (normalized.StartsWith("::", StringComparison.Ordinal)
            || normalized.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
        {
            return IconExtractor.ExtractIconByShellNamespace(normalized, small);
        }

        // bare name（"control"等）をパス解決してからアイコンを取得する
        string resolved = FileHelper.ResolveExecutable(normalized);
        return IconExtractor.ExtractAssociatedIcon(resolved, small);
    }

    /// <summary>
    /// キューをクリアし、世代をインクリメントする。
    /// ワーカースレッドは停止せず、古いリクエストの結果は世代チェックで破棄される。
    /// </summary>
    public void Clear()
    {
        Interlocked.Increment(ref generation);
        // キューを空にして古いリクエストの処理をスキップ
        while (queue.TryTake(out _)) { }
    }

    /// <summary>
    /// アイコン読み込みリクエストをキューに追加する。
    /// </summary>
    public void Load(string fileName, bool small, object? arg)
    {
        if (disposed) return;

        queue.Add(new Request(fileName, small, arg, generation));
    }

    /// <summary>
    /// STAワーカースレッドのメインループ。
    /// GetConsumingEnumerable()でDispose/キャンセルまでブロック待機する。
    /// </summary>
    void OnThread(CancellationToken token)
    {
        try
        {
            foreach (var r in queue.GetConsumingEnumerable(token))
            {
                ProcessRequest(r);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    void ProcessRequest(Request r)
    {
        // 世代が古ければスキップ
        if (r.Generation != generation) return;

        // アイコン抽出とイベント通知を分離し、どちらの例外でもワーカースレッドが死なないようにする
        System.Drawing.Icon? icon = null;
        try
        {
            icon = extractIcon(r.FileName, r.Small);

            // アイコン抽出中にClear()された場合はスキップ
            if (r.Generation != generation)
            {
                icon?.Dispose();
                return;
            }
        }
        catch (Exception e) when (e is not OperationCanceledException and not ObjectDisposedException)
        {
            System.Diagnostics.Debug.WriteLine($"アイコン読み込みエラー ({r.FileName}, retry={r.RetryCount}): {e}");
            icon?.Dispose();

            // リトライ: 世代がまだ有効で、リトライ回数上限未満なら再キュー
            if (r.RetryCount < MaxRetries && r.Generation == generation)
            {
                Thread.Sleep(50);
                r.RetryCount++;
                queue.TryAdd(r);
                return;
            }
            icon = null;
        }

        // CallEventは別のtry-catchで保護（フォームDispose済み等のrace conditionに対応）
        try
        {
            CallEvent(r, icon);
        }
#pragma warning disable CA1031 // ワーカースレッド保護: イベントハンドラ側の任意例外でスレッドが死なないようにする
        catch (Exception e)
#pragma warning restore CA1031
        {
            System.Diagnostics.Debug.WriteLine($"IconLoadedイベント通知エラー: {e}");
            icon?.Dispose();
        }
    }

    private void CallEvent(Request r, System.Drawing.Icon? icon)
    {
        EventHandler<IconLoadedEventArgs>? handler = IconLoaded;
        handler?.Invoke(this, new IconLoadedEventArgs(
            icon, r.FileName, r.Small, r.Arg, r.Generation));
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        queue.CompleteAdding();
        cts.Cancel();
        cts.Dispose();
        queue.Dispose();
    }
}
