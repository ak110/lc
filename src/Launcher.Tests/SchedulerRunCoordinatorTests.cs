using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

public sealed class SchedulerRunCoordinatorTests
{
    readonly SchedulerData data = new();
    readonly List<(SchedulerItem Item, Action Completed)> started = [];
    readonly SchedulerRunCoordinator coordinator;

    public SchedulerRunCoordinatorTests()
    {
        coordinator = new SchedulerRunCoordinator(() => data.Items, (item, completed) => started.Add((item, completed)));
    }

    SchedulerItem AddItem(string name, int sleep = 0)
    {
        var item = new SchedulerItem { Id = SchedulerItem.NewId(), Name = name, SleepTimeMs = sleep };
        data.Items.Add(item);
        return item;
    }

    [Fact]
    public void 実行中に到来した予定は完了後に1回だけ実行する()
    {
        var item = AddItem("a");

        coordinator.Request(item);
        coordinator.Request(item);
        coordinator.Request(item);
        started.Should().HaveCount(1, "実行中は同じアイテムを開始しない");

        started[0].Completed();
        started.Should().HaveCount(2, "保留は1件にまとめて完了後に実行する");

        started[1].Completed();
        started.Should().HaveCount(2);
        coordinator.Request(item);
        started.Should().HaveCount(3, "完了で実行状態を解放し、次の予定を開始する");
    }

    [Fact]
    public void 別アイテムと同名の別アイテムは並行して実行する()
    {
        var a = AddItem("同名");
        var b = AddItem("同名");
        var c = AddItem("別");

        coordinator.Request(a);
        coordinator.Request(b);
        coordinator.Request(c);

        started.Select(s => s.Item.Id).Should().Equal(a.Id, b.Id, c.Id);
    }

    [Fact]
    public void 保留は最新の確定設定で実行し無効化と削除で破棄する()
    {
        var edited = AddItem("編集前", sleep: 1);
        var disabled = AddItem("無効化");
        var removed = AddItem("削除");
        foreach (var item in new[] { edited, disabled, removed })
        {
            coordinator.Request(item);
            coordinator.Request(item);
        }

        var newEdited = edited.Clone();
        newEdited.Name = "編集後";
        newEdited.SleepTimeMs = 2;
        data.Items[0] = newEdited;
        data.Items[1].Enable = false;
        data.Items.RemoveAt(2);
        coordinator.ItemsChanged();

        started[0].Completed();
        started[1].Completed();
        started[2].Completed();

        started.Should().HaveCount(4);
        started[3].Item.Name.Should().Be("編集後");
        started[3].Item.SleepTimeMs.Should().Be(2);
    }

    [Fact]
    public void 無効化で破棄した保留は再度有効化しても実行しない()
    {
        var item = AddItem("a");
        coordinator.Request(item);
        coordinator.Request(item);

        item.Enable = false;
        coordinator.ItemsChanged();
        item.Enable = true;
        coordinator.ItemsChanged();
        started[0].Completed();

        started.Should().HaveCount(1);
    }

    [Fact]
    public void 実行中のタスク列は設定変更で書き換わらない()
    {
        var item = AddItem("a");
        item.Tasks.Add(new SchedulerTask { Message = "変更前" });

        coordinator.Request(item);
        item.Tasks[0].Message = "変更後";
        item.Tasks.Add(new SchedulerTask());

        var running = started[0].Item;
        running.Should().NotBeSameAs(item);
        running.Tasks.Should().ContainSingle().Which.Message.Should().Be("変更前");
    }

    [Fact]
    public void 一時停止中は保留を開始せず再開後に1回実行する()
    {
        var item = AddItem("a");
        coordinator.Request(item);
        coordinator.Request(item);

        coordinator.Pause();
        started[0].Completed();
        started.Should().HaveCount(1, "一時停止中は保留を開始しない");
        coordinator.Request(item);
        started.Should().HaveCount(1, "一時停止中は新しい予定も開始しない");

        coordinator.Resume();
        started.Should().HaveCount(2);
        started[1].Completed();
        coordinator.Resume();
        started.Should().HaveCount(2, "保留は1回だけ実行する");
    }

    [Fact]
    public void 終了処理中と配送不能時は保留を開始しない()
    {
        var a = AddItem("a");
        var b = AddItem("b");
        coordinator.Request(a);
        coordinator.Request(a);
        coordinator.Request(b);
        coordinator.Request(b);

        coordinator.Release(a.Id);
        started.Should().HaveCount(2);
        coordinator.Request(a);
        started.Should().HaveCount(3, "解放した実行状態は次の予定の開始を妨げない");

        coordinator.Shutdown();
        started[1].Completed();
        coordinator.Request(a);
        started.Should().HaveCount(3);
    }

    [Fact]
    public void 識別子を保存できない場合は予定実行を開始しない()
    {
        var item = new SchedulerItem { Name = "旧版の設定" };
        data.Items.Add(item);

        coordinator.EnsureIds(data, () =>
        {
            coordinator.Request(item);
            started.Should().BeEmpty();
            return false;
        }).Should().BeFalse();
        coordinator.Request(item);
        started.Should().BeEmpty();

        int saved = 0;
        coordinator.EnsureIds(data, () => { saved++; return true; }).Should().BeTrue();
        saved.Should().Be(1);
        coordinator.Request(item);
        started.Should().ContainSingle();
    }

    [Fact]
    public void 識別子を補ったときだけ保存する()
    {
        AddItem("a");
        int saved = 0;
        coordinator.EnsureIds(data, () => { saved++; return true; }).Should().BeTrue();
        saved.Should().Be(0);

        data.Items.Add(new SchedulerItem { Name = "旧版" });
        coordinator.EnsureIds(data, () => { saved++; return true; }).Should().BeTrue();
        saved.Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void 保存保護と停止をまたぐ保留と実行の遷移(bool pauseBeforeCompletion)
    {
        var a = AddItem("a");
        var b = AddItem("b");
        coordinator.Request(a);
        coordinator.Request(a);
        coordinator.Request(b);
        coordinator.Request(b);

        var newItem = new SchedulerItem { Name = "識別子なし" };
        data.Items.Add(newItem);
        coordinator.EnsureIds(data, () =>
        {
            coordinator.Request(newItem);
            started.Should().HaveCount(2);
            return false;
        }).Should().BeFalse();

        if (pauseBeforeCompletion) coordinator.Pause();
        started[0].Completed();

        a.Enable = false;
        coordinator.ItemsChanged();
        coordinator.Resume();
        coordinator.Request(newItem);
        started.Should().HaveCount(2);

        coordinator.Pause();
        coordinator.EnsureIds(data, () => true).Should().BeTrue();
        coordinator.Request(newItem);
        started.Should().HaveCount(2);
        coordinator.Resume();
        coordinator.Request(newItem);
        coordinator.Request(newItem);

        coordinator.Shutdown();
        coordinator.Resume();
        coordinator.Request(newItem);
        started.Should().HaveCount(3);

        coordinator.Release(b.Id);
        started[2].Completed();
        started.Should().HaveCount(3);
    }

    [Fact]
    public void 生成した操作列で開始するアイテムがモデルと一致する()
    {
        var random = new Random(3974);
        for (int trial = 0; trial < 32; trial++)
        {
            started.Clear();
            data.Items.Clear();
            var items = new[] { AddItem("a"), AddItem("b") };
            var subject = new SchedulerRunCoordinator(() => data.Items, (item, done) => started.Add((item, done)));
            var running = new bool[items.Length];
            var pending = new bool[items.Length];
            bool paused = false;
            bool unsaved = false;
            bool shutdown = false;
            int shutdownStep = random.Next(30, 55);

            for (int step = 0; step < 64; step++)
            {
                int operation = step == shutdownStep ? 8 : random.Next(8);
                int index = random.Next(items.Length);
                var item = items[index];
                int before = started.Count;
                var expectedStarts = new List<string>();
                switch (operation)
                {
                    case 0:
                        subject.Request(item);
                        if (!paused && !unsaved && !shutdown)
                        {
                            if (running[index]) pending[index] = true;
                            else ExpectStart(index);
                        }
                        break;
                    case 1:
                        if (running[index])
                        {
                            started.Last(run => run.Item.Id == item.Id).Completed();
                            running[index] = false;
                            if (!paused) Flush(index);
                        }
                        else subject.Completed(item.Id);
                        break;
                    case 2:
                        subject.Release(item.Id);
                        running[index] = pending[index] = false;
                        break;
                    case 3:
                        subject.Pause();
                        paused = true;
                        break;
                    case 4:
                        subject.Resume();
                        paused = false;
                        for (int i = 0; i < items.Length; i++) Flush(i);
                        break;
                    case 5:
                        item.Enable = !item.Enable;
                        subject.ItemsChanged();
                        for (int i = 0; i < items.Length; i++)
                            if (!items[i].Enable) pending[i] = false;
                        break;
                    case 6:
                    case 7:
                        bool saveSucceeds = operation == 7;
                        data.Items.Add(new SchedulerItem());
                        subject.EnsureIds(data, () =>
                        {
                            subject.Request(item);
                            started.Should().HaveCount(before);
                            return saveSucceeds;
                        }).Should().Be(saveSucceeds);
                        unsaved = !saveSucceeds;
                        break;
                    case 8:
                        subject.Shutdown();
                        shutdown = true;
                        Array.Clear(pending);
                        break;
                }

                started.Skip(before).Select(run => run.Item.Id).Should().BeEquivalentTo(expectedStarts,
                    "試行 {0} 手順 {1} 操作 {2}", trial, step, operation);

                void ExpectStart(int i)
                {
                    expectedStarts.Add(items[i].Id);
                    running[i] = true;
                    pending[i] = false;
                }

                void Flush(int i)
                {
                    if (!pending[i] || running[i] || shutdown) return;
                    pending[i] = false;
                    if (!unsaved && items[i].Enable) ExpectStart(i);
                }
            }
        }
    }
}
