namespace Launcher.Core;

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

    public SchedulerRunCoordinator(Func<IReadOnlyList<SchedulerItem>> getItems, Action<SchedulerItem, Action> start)
    {
        this.getItems = getItems;
        this.start = start;
    }

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
            return false;
        }
        lock (lockObject)
        {
            idPersistence = IdPersistence.Saved;
        }
        return true;
    }

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

    public void Release(string id)
    {
        lock (lockObject)
        {
            items.Remove(id);
        }
    }

    public void Pause()
    {
        lock (lockObject)
        {
            if (execution == ExecutionPhase.Active) execution = ExecutionPhase.Paused;
        }
    }

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
