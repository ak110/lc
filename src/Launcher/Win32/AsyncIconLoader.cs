using System.Collections.Concurrent;
using Launcher.Infrastructure;

namespace Launcher.Win32;

public sealed class IconLoadedEventArgs : EventArgs
{
    public System.Drawing.Icon? Icon { get; }
    public string FileName { get; set; }
    public bool Small { get; set; }
    public object? Arg { get; }
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
/// ProcessorCount等の動的な値ではなく用途別の固定値を使用する。
/// グリッド全体用は8本固定、フォルダポップアップメニュー用のper-menu生成インスタンスは4本とする。
/// 詳細は .claude/rules/threading.md「アイコンローダーの並行度」節を参照。
/// </summary>
public sealed class AsyncIconLoader : IDisposable
{
    const int DefaultWorkerCount = 8;
    const int MaxRetries = 2;

    readonly BlockingCollection<Request> queue = new();
    readonly CancellationTokenSource cts = new();
    readonly Func<string, bool, System.Drawing.Icon?> extractIcon;
    readonly FaviconCache _faviconCache;
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

    public int Generation => generation;

    public bool IsDisposed => disposed;

    public event EventHandler<IconLoadedEventArgs>? IconLoaded;

    public AsyncIconLoader(
        int workerCount = DefaultWorkerCount,
        ThreadPriority threadPriority = ThreadPriority.Normal,
        Func<string, bool, System.Drawing.Icon?>? extractIcon = null)
    {
        _faviconCache = new FaviconCache(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "favicons"));
        this.extractIcon = extractIcon ?? DefaultExtractIcon;
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

    System.Drawing.Icon? DefaultExtractIcon(string fileName, bool small)
    {
        // PathNormalize はURLのスキーム区切り文字を変形するため、URL判定は先に行う
        if (fileName.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return _faviconCache.Get(fileName, small);
        }

        string normalized = PathHelper.PathNormalize(fileName);

        // シェル名前空間パス ("::{CLSID}"や"shell:xxx"形式) はPIDL経由で取得
        if (normalized.StartsWith("::", StringComparison.Ordinal)
            || normalized.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
        {
            return IconExtractor.ExtractIconByShellNamespace(normalized, small);
        }

        string resolved = FileHelper.ResolveExecutable(normalized);
        return IconExtractor.ExtractAssociatedIcon(resolved, small);
    }

    public void Clear()
    {
        Interlocked.Increment(ref generation);
        while (queue.TryTake(out _)) { }
    }

    public void Load(string fileName, bool small, object? arg)
    {
        if (disposed) return;

        queue.Add(new Request(fileName, small, arg, generation));
    }

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
        if (r.Generation != generation) return;

        System.Drawing.Icon? icon = null;
        try
        {
            icon = extractIcon(r.FileName, r.Small);

            if (r.Generation != generation)
            {
                icon?.Dispose();
                return;
            }
        }
        catch (Exception e) when (e is not OperationCanceledException and not ObjectDisposedException)
        {
            var ext = Path.GetExtension(r.FileName);
            DiagnosticLog.Warn(
                "IconLoader.Load",
                $"アイコン読み込み失敗 (ext={ext} retry={r.RetryCount}): {e.GetType().Name}",
                $"対応不要（{MaxRetries}回まで再試行し、読み込めなければアイコンなしで表示を継続する）");
            icon?.Dispose();

            if (r.RetryCount < MaxRetries && r.Generation == generation)
            {
                Thread.Sleep(50);
                r.RetryCount++;
                queue.TryAdd(r);
                return;
            }
            icon = null;
        }

        try
        {
            CallEvent(r, icon);
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            DiagnosticLog.Error("IconLoader.Load", e, "対応不要（このアイコンの反映を中止し、読み込みを継続した）");
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
