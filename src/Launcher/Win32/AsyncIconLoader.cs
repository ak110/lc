#nullable disable
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
/// アイコン読み込みスレッド。
/// </summary>
public class AsyncIconLoader
{
    object lockObject = new object();
    volatile bool valid;
    int generation;
    List<Thread> threads = new List<Thread>();
    Queue<Request> queue = new Queue<Request>();
    ThreadPriority threadPriority = ThreadPriority.Normal;
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
    /// キューをクリアする（ノンブロッキング）。
    /// 世代をインクリメントし、古いリクエストの結果はUIスレッドで破棄される。
    /// </summary>
    public void Clear()
    {
        lock (lockObject)
        {
            generation++;
            valid = false;
            queue.Clear();
            // 終了済みスレッドがカウントに残ると次のLoad()で新スレッドが起動しないためクリア
            threads.Clear();
            Monitor.PulseAll(lockObject);
        }
    }

    /// <summary>
    /// 読み込み。
    /// </summary>
    public void Load(string fileName, bool small, object arg)
    {
        lock (lockObject)
        {
            valid = true;
            if (threads.Count < System.Environment.ProcessorCount)
            {
                var thread = new Thread(new ThreadStart(OnThread));
                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Priority = threadPriority;
                thread.Start();
                threads.Add(thread);
            }
            queue.Enqueue(new Request(fileName, small, arg, generation));
            Monitor.Pulse(lockObject);
        }
    }

    /// <summary>
    /// アイコン読み込みスレッド
    /// </summary>
    void OnThread()
    {
        while (true)
        {
            Request r;
            lock (lockObject)
            {
                // キューが空なら待機
                while (queue.Count <= 0)
                {
                    if (!valid) return; // Clear()後でキューも空ならスレッド終了
                    Monitor.Wait(lockObject);
                }
                r = queue.Dequeue();
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

    private void CallEvent(Request r, System.Drawing.Icon icon)
    {
        EventHandler<IconLoadedEventArgs> IconLoaded = this.IconLoaded;
        if (IconLoaded != null)
        {
            IconLoaded(this, new IconLoadedEventArgs(
                icon, r.FileName, r.Small, r.Arg, r.Generation));
        }
    }
}
