using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// 予定実行の直列化と保留の集約 (<see cref="SchedulerRunCoordinator"/>) のテスト。
/// 開始処理を記録に差し替え、完了通知はテストから呼ぶ。
/// </summary>
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
        coordinator.IsRunning(item.Id).Should().BeTrue();

        started[0].Completed();
        started.Should().HaveCount(2, "保留は1件にまとめて完了後に実行する");

        started[1].Completed();
        started.Should().HaveCount(2);
        coordinator.IsRunning(item.Id).Should().BeFalse();
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

        // 設定変更: 編集は同じ識別子の新しいインスタンス、無効化、削除
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

        // 完了通知を配送できない: 実行状態を解放し、保留は開始しない
        coordinator.Release(a.Id);
        coordinator.IsRunning(a.Id).Should().BeFalse();
        started.Should().HaveCount(2);

        // 終了処理中: 完了しても保留を開始しない
        coordinator.Shutdown();
        started[1].Completed();
        coordinator.IsRunning(b.Id).Should().BeFalse();
        coordinator.Request(a);
        started.Should().HaveCount(2);
    }

    [Fact]
    public void 識別子を保存できない場合は予定実行を開始しない()
    {
        var item = new SchedulerItem { Name = "旧版の設定" };
        data.Items.Add(item);

        coordinator.EnsureIds(data, () => throw new IOException("保存失敗")).Should().BeFalse();
        coordinator.Request(item);
        started.Should().BeEmpty();

        // 書き込める状態で再試行すると予定実行を再開する
        int saved = 0;
        coordinator.EnsureIds(data, () => saved++).Should().BeTrue();
        saved.Should().Be(1);
        coordinator.Request(item);
        started.Should().ContainSingle();
    }

    [Fact]
    public void 識別子を補ったときだけ保存する()
    {
        AddItem("a");
        int saved = 0;
        coordinator.EnsureIds(data, () => saved++).Should().BeTrue();
        saved.Should().Be(0);

        data.Items.Add(new SchedulerItem { Name = "旧版" });
        coordinator.EnsureIds(data, () => saved++).Should().BeTrue();
        saved.Should().Be(1);
    }
}
