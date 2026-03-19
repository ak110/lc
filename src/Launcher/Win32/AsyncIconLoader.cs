using System.Collections.Concurrent;
using System.IO;
using Launcher.Infrastructure;

namespace Launcher.Win32;

/// <summary>
/// アイコン読み込んだぞイベントの引数。
/// 失敗時は Icon == null
/// </summary>
public class IconLoadedEventArgs : EventArgs
{
    public readonly System.Drawing.Icon? Icon;
    public string FileName;
    public bool Small;
    public readonly object? Arg;
    /// <summary>リクエスト時の世代番号</summary>
    public readonly int Generation;
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
/// BlockingCollection + 複数STAワーカースレッドで構成。
/// Shell APIのSTA制約のためTask.Run(ThreadPool/MTA)は使用不可。
///
/// ワーカースレッドはLoad()初回〜N回目で必要に応じて起動され、
/// Disposeまで長寿命で生存する。Clear()はキュードレイン+世代インクリメントのみで
/// スレッドの停止・再作成は行わない。
/// </summary>
public sealed class AsyncIconLoader : IDisposable
{
    readonly BlockingCollection<Request> queue = new BlockingCollection<Request>();
    readonly CancellationTokenSource cts = new CancellationTokenSource();
    volatile int generation;
    int workerCount;
    ThreadPriority threadPriority = ThreadPriority.Normal;
    bool disposed;

    struct Request
    {
        public string FileName;
        public bool Small;
        public object? Arg;
        public int Generation;
        public Request(string fileName, bool small, object? arg, int generation)
        {
            FileName = fileName;
            Small = small;
            Arg = arg;
            Generation = generation;
        }
    }

    /// <summary>
    /// 現在の世代番号。UIスレッドから参照して古い結果を破棄するために使用。
    /// </summary>
    public int Generation => generation;

    /// <summary>
    /// ワーカースレッドの優先度。Load()で新規スレッド生成時に適用される。
    /// 既に起動済みのスレッドには影響しない。
    /// </summary>
    public ThreadPriority ThreadPriority
    {
        get => threadPriority;
        set => threadPriority = value;
    }

    /// <summary>
    /// アイコン読み込んだぞイベント
    /// </summary>
    public event EventHandler<IconLoadedEventArgs>? IconLoaded;

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
    /// 必要に応じてワーカースレッドを新規起動する（最大ProcessorCount本）。
    /// </summary>
    public void Load(string fileName, bool small, object? arg)
    {
        if (disposed) return;

        queue.Add(new Request(fileName, small, arg, generation));

        // ワーカー数がプロセッサ数未満なら新規スレッド起動
        if (workerCount < Environment.ProcessorCount)
        {
            // lock内で再チェックして重複起動を防止
            lock (queue)
            {
                if (workerCount < Environment.ProcessorCount && !disposed)
                {
                    workerCount++;
                    var token = cts.Token;
                    var thread = new Thread(() => OnThread(token));
                    thread.SetApartmentState(ApartmentState.STA); // Shell API (SHGetFileInfo) にはSTAが必須
                    thread.IsBackground = true;
                    thread.Priority = threadPriority;
                    thread.Start();
                }
            }
        }
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
                // 世代が古ければスキップ
                if (r.Generation != generation) continue;

                // アイコン抽出とイベント通知を分離し、どちらの例外でもワーカースレッドが死なないようにする
                System.Drawing.Icon? icon = null;
                try
                {
                    icon = IconExtractor.ExtractAssociatedIcon(
                        PathHelper.PathNormalize(r.FileName), r.Small);

                    // アイコン抽出中にClear()された場合はスキップ
                    if (r.Generation != generation)
                    {
                        icon.Dispose();
                        continue;
                    }
                }
                catch (Exception e) when (e is not OperationCanceledException and not ObjectDisposedException)
                {
                    System.Diagnostics.Debug.WriteLine($"アイコン読み込みエラー ({r.FileName}): {e}");
                    icon?.Dispose();
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
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
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
