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
    enum ItemPhase { Pending, Running, RunningWithPending }
    enum ExecutionPhase { Active, Paused, Shutdown }
    enum IdPersistence { Saved, Unsaved }

    readonly Dictionary<string, ItemPhase> items = [];
    ExecutionPhase execution;
    IdPersistence idPersistence;

    bool CanStart => execution == ExecutionPhase.Active && idPersistence == IdPersistence.Saved;

    /// <summary>実行中の件数と、実行中かどうかにかかわらず保留されている件数。</summary>
    public (int Running, int Pending) GetRunCounts()
    {
        lock (lockObject)
        {
            return (items.Values.Count(phase => phase != ItemPhase.Pending),
                items.Values.Count(phase => phase != ItemPhase.Running));
        }
    }

    /// <summary>指定したアイテムが実行中か、再実行が保留されているかを返す。</summary>
    public (bool Running, bool Pending) GetRunStatus(string id)
    {
        lock (lockObject)
        {
            return items.TryGetValue(id, out var phase)
                ? (phase != ItemPhase.Pending, phase != ItemPhase.Running)
                : (false, false);
        }
    }

    /// <param name="getItems">最新の確定設定のアイテム一覧</param>
    /// <param name="start">アイテムの複製のタスク列を開始する。完了時に第2引数の完了通知をUIスレッドで呼ばせる</param>
    public SchedulerRunCoordinator(Func<IReadOnlyList<SchedulerItem>> getItems, Action<SchedulerItem, Action> start)
    {
        this.getItems = getItems;
        this.start = start;
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
            if (!changed && idPersistence == IdPersistence.Saved)
            {
                return true;
            }
            // 保存失敗の通知中はモーダルループから予定が届くため、保存の前に停止する。
            idPersistence = IdPersistence.Unsaved;
        }
        if (!save())
        {
            DiagnosticLog.Warn("Scheduler.EnsureIds", "識別子を保存できないため予定実行を停止",
                "画面の保存失敗の案内に従って書き込める状態にし、タスクトレイの「スケジューラー設定」でOKを押すか、らんちゃを再起動すると予定実行を再開する");
            return false;
        }
        lock (lockObject)
        {
            idPersistence = IdPersistence.Saved;
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
            if (!CanStart)
            {
                return;
            }
            if (items.TryGetValue(item.Id, out var phase) && phase != ItemPhase.Pending)
            {
                items[item.Id] = ItemPhase.RunningWithPending;
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
            if (!items.TryGetValue(id, out var phase) || phase == ItemPhase.Pending)
            {
                return;
            }
            if (phase == ItemPhase.RunningWithPending)
            {
                items[id] = ItemPhase.Pending;
                StartPending(id);
            }
            else
            {
                items.Remove(id);
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
            items.Remove(id);
        }
    }

    /// <summary>
    /// 一時停止する。実行中のタスク列は続け、保留は再開まで開始しない。
    /// </summary>
    public void Pause()
    {
        lock (lockObject)
        {
            if (execution == ExecutionPhase.Active) execution = ExecutionPhase.Paused;
        }
    }

    /// <summary>
    /// 再開する。実行中でない有効なアイテムの保留を1回ずつ実行する。
    /// </summary>
    public void Resume()
    {
        lock (lockObject)
        {
            if (execution == ExecutionPhase.Shutdown) return;
            execution = ExecutionPhase.Active;
            foreach (var id in items.Keys.ToArray())
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
            foreach (var id in items.Keys.ToArray())
            {
                if (Find(id) is not { Enable: true }) DropPending(id);
            }
        }
    }

    /// <summary>
    /// 終了処理の開始。以後は保留を開始しない。
    /// </summary>
    public void Shutdown()
    {
        lock (lockObject)
        {
            execution = ExecutionPhase.Shutdown;
            foreach (var id in items.Keys.ToArray())
            {
                DropPending(id);
            }
        }
    }

    void DropPending(string id)
    {
        if (items[id] == ItemPhase.Pending) items.Remove(id);
        else if (items[id] == ItemPhase.RunningWithPending) items[id] = ItemPhase.Running;
    }

    void StartPending(string id)
    {
        if (execution != ExecutionPhase.Active ||
            !items.TryGetValue(id, out var phase) || phase != ItemPhase.Pending)
        {
            return;
        }

        items.Remove(id);
        if (CanStart && Find(id) is { Enable: true } item)
        {
            Start(item);
        }
    }

    void Start(SchedulerItem item)
    {
        string id = item.Id;
        items[id] = ItemPhase.Running;
        start(item.Clone(), () => Completed(id));
    }

    SchedulerItem? Find(string id) => getItems().FirstOrDefault(i => i.Id == id);
}
