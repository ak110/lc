using Launcher.Infrastructure;

namespace Launcher.Core;

/// <summary>
/// スケジューラーの予定実行をアイテム識別子ごとに管理する。
/// 同じアイテムは同時に1つだけ実行し、実行中に到来した予定は1件の保留にまとめて完了後に1回実行する。
/// 実行はアイテムの複製で行い、実行中の設定変更で書き換えない。
/// 完了通知 (<see cref="Completed"/>) を含む各操作はUIスレッドから呼ぶ。
/// 完了通知を配送できない場合の<see cref="Release"/>だけは任意のスレッドから呼べる。
/// </summary>
public sealed class SchedulerRunCoordinator
{
    readonly object lockObject = new();
    readonly Func<IReadOnlyList<SchedulerItem>> getItems;
    readonly Action<SchedulerItem, Action> start;
    readonly HashSet<string> running = [];
    readonly HashSet<string> pending = [];
    bool paused;
    bool shuttingDown;
    bool idsUnsaved;

    /// <param name="getItems">最新の確定設定のアイテム一覧</param>
    /// <param name="start">アイテムの複製のタスク列を開始する。完了時に第2引数の完了通知をUIスレッドで呼ばせる</param>
    public SchedulerRunCoordinator(Func<IReadOnlyList<SchedulerItem>> getItems, Action<SchedulerItem, Action> start)
    {
        this.getItems = getItems;
        this.start = start;
    }

    /// <summary>実行中のアイテムがあるか</summary>
    public bool IsRunning(string id)
    {
        lock (lockObject)
        {
            return running.Contains(id);
        }
    }

    /// <summary>
    /// 識別子の無いアイテムへ識別子を補い、補った場合と前回の保存に失敗していた場合は保存する。
    /// 保存できなければ、識別子を設定変更をまたいで追跡できないため予定実行を止めて false を返す。
    /// </summary>
    public bool EnsureIds(SchedulerData data, Func<bool> save)
    {
        bool changed = data.EnsureIds();
        lock (lockObject)
        {
            if (!changed && !idsUnsaved)
            {
                return true;
            }
            // 保存失敗の通知中はモーダルループから予定が届くため、保存の前に停止する。
            idsUnsaved = true;
        }
        if (!save())
        {
            DiagnosticLog.Warn("Scheduler.EnsureIds", "識別子を保存できないため予定実行を停止");
            return false;
        }
        lock (lockObject)
        {
            idsUnsaved = false;
        }
        return true;
    }

    /// <summary>
    /// 予定の到来。実行中なら保留にまとめ、そうでなければ開始する。
    /// </summary>
    public void Request(SchedulerItem item)
    {
        lock (lockObject)
        {
            if (shuttingDown || paused || idsUnsaved)
            {
                return;
            }
            if (running.Contains(item.Id))
            {
                pending.Add(item.Id);
                return;
            }
            Start(item);
        }
    }

    /// <summary>
    /// タスク列の完了。保留があれば最新の確定設定で1回実行する。
    /// </summary>
    public void Completed(string id)
    {
        lock (lockObject)
        {
            running.Remove(id);
            if (shuttingDown)
            {
                pending.Remove(id);
                return;
            }
            if (!paused)
            {
                StartPending(id);
            }
        }
    }

    /// <summary>
    /// 完了通知を配送できない場合 (ウィンドウ破棄後など) に、保留を開始せず実行状態を解放する。
    /// </summary>
    public void Release(string id)
    {
        lock (lockObject)
        {
            running.Remove(id);
            pending.Remove(id);
        }
    }

    /// <summary>
    /// 一時停止する。実行中のタスク列は続け、保留は再開まで開始しない。
    /// </summary>
    public void Pause()
    {
        lock (lockObject)
        {
            paused = true;
        }
    }

    /// <summary>
    /// 再開する。実行中でない有効なアイテムの保留を1回ずつ実行する。
    /// </summary>
    public void Resume()
    {
        lock (lockObject)
        {
            paused = false;
            foreach (var id in pending.Where(id => !running.Contains(id)).ToList())
            {
                StartPending(id);
            }
        }
    }

    /// <summary>
    /// 設定変更・再読込の後に呼ぶ。無効化・削除されたアイテムの保留を破棄する。
    /// </summary>
    public void ItemsChanged()
    {
        lock (lockObject)
        {
            pending.RemoveWhere(id => Find(id) is not { Enable: true });
        }
    }

    /// <summary>
    /// 終了処理の開始。以後は保留を開始しない。
    /// </summary>
    public void Shutdown()
    {
        lock (lockObject)
        {
            shuttingDown = true;
            pending.Clear();
        }
    }

    void StartPending(string id)
    {
        if (!pending.Remove(id))
        {
            return;
        }
        if (Find(id) is { Enable: true } item && !idsUnsaved)
        {
            Start(item);
        }
    }

    void Start(SchedulerItem item)
    {
        string id = item.Id;
        running.Add(id);
        start(item.Clone(), () => Completed(id));
    }

    SchedulerItem? Find(string id) => getItems().FirstOrDefault(i => i.Id == id);
}
