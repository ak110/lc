#nullable disable
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
    public readonly System.Drawing.Icon Icon;
    public string FileName;
    public bool Small;
    public readonly object Arg;
    /// <summary>リクエスト時の世代番号</summary>
    public readonly int Generation;
    public IconLoadedEventArgs(System.Drawing.Icon icon, string fileName, bool small, object arg, int generation)
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
/// BlockingCollection + STAワーカースレッドで構成。
/// Shell APIのSTA制約のためTask.Run(ThreadPool/MTA)は使用不可。
/// </summary>
public sealed class AsyncIconLoader : IDisposable
{
    readonly object lockObject = new object();
    int generation;
    int activeWorkerCount;
    BlockingCollection<Request> queue = new BlockingCollection<Request>();
    CancellationTokenSource cts = new CancellationTokenSource();
    ThreadPriority threadPriority = ThreadPriority.Normal;
    bool disposed;

    struct Request
    {
        public string FileName;
        public bool Small;
        public object Arg;
        public int Generation;
        public Request(string fileName, bool small, object arg, int generation)
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
    public int Generation
    {
        get { lock (lockObject) { return generation; } }
    }

    /// <summary>
    /// ワーカースレッドの優先度。Load()で新規スレッド生成時に適用される。
    /// </summary>
    public ThreadPriority ThreadPriority
    {
        get { lock (lockObject) { return threadPriority; } }
        set { lock (lockObject) { threadPriority = value; } }
    }

    /// <summary>
    /// アイコン読み込んだぞイベント
    /// </summary>
    public event EventHandler<IconLoadedEventArgs> IconLoaded;

    /// <summary>
    /// キューをクリアし、全ワーカーを停止する。
    /// 世代をインクリメントし、古いリクエストの結果はUIスレッドで破棄される。
    /// </summary>
    public void Clear()
    {
        lock (lockObject)
        {
            generation++;

            // 既存のワーカーを停止
            cts.Cancel();
            cts.Dispose();
            queue.Dispose();

            // 新しいキューとトークンを作成
            cts = new CancellationTokenSource();
            queue = new BlockingCollection<Request>();
            activeWorkerCount = 0;
        }
    }

    /// <summary>
    /// アイコン読み込みリクエストをキューに追加する。
    /// 必要に応じてワーカースレッドを新規起動する。
    /// </summary>
    public void Load(string fileName, bool small, object arg)
    {
        lock (lockObject)
        {
            if (disposed) return;

            // ワーカー数がプロセッサ数未満なら新規スレッド起動
            if (activeWorkerCount < Environment.ProcessorCount)
            {
                activeWorkerCount++;
                var currentCts = cts;
                var currentQueue = queue;
                var thread = new Thread(() => OnThread(currentQueue, currentCts.Token));
                thread.SetApartmentState(ApartmentState.STA); // Shell API (SHGetFileInfo) にはSTAが必須
                thread.IsBackground = true;
                thread.Priority = threadPriority;
                thread.Start();
            }
            queue.Add(new Request(fileName, small, arg, generation));
        }
    }

    /// <summary>
    /// STAワーカースレッドのメインループ。
    /// BlockingCollection.Take()で同期的にブロック待機する。
    /// （STAスレッド内でasync/awaitを使うとThreadPoolに継続が流れSTA制約を破る危険がある）
    /// </summary>
    void OnThread(BlockingCollection<Request> workerQueue, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                Request r;
                try
                {
                    r = workerQueue.Take(token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                // 世代が古ければスキップ
                if (r.Generation != generation) continue;

                // アイコン読み込み
                try
                {
                    using (var icon = IconExtractor.ExtractAssociatedIcon(
                         PathHelper.PathNormalize(r.FileName), r.Small))
                    {
                        // 世代が変わっていたらスキップ
                        if (r.Generation != generation) continue;

                        CallEvent(r, icon);
                    }
                }
                catch (FileLoadException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                    CallEvent(r, null);
                }
            }
        }
        finally
        {
            // スレッド終了時にカウントをデクリメント
            Interlocked.Decrement(ref activeWorkerCount);
        }
    }

    private void CallEvent(Request r, System.Drawing.Icon icon)
    {
        EventHandler<IconLoadedEventArgs> handler = IconLoaded;
        handler?.Invoke(this, new IconLoadedEventArgs(
            icon, r.FileName, r.Small, r.Arg, r.Generation));
    }

    public void Dispose()
    {
        lock (lockObject)
        {
            if (disposed) return;
            disposed = true;
            cts.Cancel();
            cts.Dispose();
            queue.Dispose();
        }
    }
}
