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
    public IconLoadedEventArgs(System.Drawing.Icon icon, string fileName, bool small, object arg)
    {
        Icon = icon;
        FileName = fileName;
        Small = small;
        Arg = arg;
    }
}

/// <summary>
/// アイコン読み込みスレッド。
/// </summary>
public class AsyncIconLoader
{
    object lockObject = new object();
    volatile bool valid = false;
    List<Thread> threads = new List<Thread>();
    Queue<Request> queue = new Queue<Request>();
    struct Request
    {
        public string FileName;
        public bool Small;
        public object Arg;
        public Request(string fileName, bool small, object arg)
        {
            FileName = fileName;
            Small = small;
            Arg = arg;
        }
    }

    /// <summary>
    /// アイコン読み込んだぞイベント
    /// </summary>
    public event EventHandler<IconLoadedEventArgs> IconLoaded;

    /// <summary>
    /// キューをクリアしてスレッド停止
    /// </summary>
    public void Clear()
    {
        lock (lockObject)
        {
            valid = false;
            Monitor.PulseAll(lockObject);
        }
        foreach (var thread in threads)
        {
            thread.Join(3000); // .NET 8 では Thread.Abort() が使えないため、タイムアウト付き Join で待機
        }
        threads.Clear();
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
                thread.Start();
                threads.Add(thread);
            }
            queue.Enqueue(new Request(fileName, small, arg));
            Monitor.Pulse(lockObject);
        }
    }

    /// <summary>
    /// アイコン読み込みスレッド
    /// </summary>
    void OnThread()
    {
        while (valid)
        {
            // Dequeue
            Request r;
            lock (lockObject)
            {
                if (queue.Count <= 0)
                {
                    Monitor.Wait(lockObject);
                    continue;
                }
                else
                {
                    r = queue.Dequeue();
                }
            }

            // アイコン読み込み
            try
            {
                using (var icon = IconExtractor.ExtractAssociatedIcon(
                     PathHelper.PathNormalize(r.FileName), r.Small))
                {

                    if (!valid)
                    {
                        break;
                    }

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
                icon, r.FileName, r.Small, r.Arg));
        }
    }
}
