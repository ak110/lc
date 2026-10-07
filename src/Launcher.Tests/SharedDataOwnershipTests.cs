using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

public sealed class SharedDataOwnershipTests
{
    [Fact]
    public void 再読込は一覧実体と同じ定義の編集参照を保持する()
    {
        var retained = new Command { Name = "保持", FileName = "a.exe" };
        var removed = new Command { Name = "削除", FileName = "b.exe" };
        var list = new CommandList { Commands = [retained, removed] };
        var original = list.Commands;

        list.ReplaceContents(new CommandList
        {
            Commands = [new Command { Name = "保持", FileName = "a.exe" },
                new Command { Name = "追加", FileName = "c.exe" }],
        });

        list.Commands.Should().BeSameAs(original);
        list.Commands[0].Should().BeSameAs(retained);
        list.Commands.Should().NotContain(removed);
        retained.Param = "編集結果";
        list.Commands[0].Param.Should().Be("編集結果");
    }

    [Fact]
    public void 再読込で定義が変わったコマンドの旧参照を残さない()
    {
        var old = new Command { Name = "対象", FileName = "old.exe" };
        var list = new CommandList { Commands = [old] };
        list.ReplaceContents(new CommandList
        {
            Commands = [new Command { Name = "対象", FileName = "new.exe" }],
        });
        list.Commands.Should().NotContain(old);
        list.Commands[0].FileName.Should().Be("new.exe");
    }

    [Fact]
    public void 背景計算の取得後に編集した値を上書きせず追加を次回へ送る()
    {
        var command = new Command { FileName = "before.exe", WorkDir = "before-dir" };
        var task = new SchedulerTask { FileName = "before-task.exe" };
        var commands = new CommandList { Commands = [command] };
        var scheduler = new SchedulerData { Items = [new SchedulerItem { Tasks = [task] }] };
        var batch = EnvironmentReplacementBatch.Capture(commands, scheduler);

        command.FileName = "edited.exe";
        task.FileName = "edited-task.exe";
        var added = new Command { FileName = "added.exe" };
        commands.Commands.Add(added);
        batch.Apply(commands, scheduler, ["%OLD%", "%DIR%", "%TASK%"])
            .Should().BeTrue();

        command.FileName.Should().Be("edited.exe");
        command.WorkDir.Should().Be("%DIR%");
        task.FileName.Should().Be("edited-task.exe");
        added.FileName.Should().Be("added.exe");
        batch.Values.Should().Equal("before.exe", "before-dir", "before-task.exe");

        var next = EnvironmentReplacementBatch.Capture(commands, scheduler);
        next.Apply(commands, scheduler, ["%EDITED%", "%DIR%", "%ADDED%", null, "%EDITED_TASK%"])
            .Should().BeFalse();
        added.FileName.Should().Be("%ADDED%");
        task.FileName.Should().Be("%EDITED_TASK%");
    }

    [Fact]
    public void 計算中に削除した対象へ結果を適用しない()
    {
        var removed = new Command { FileName = "removed.exe" };
        var commands = new CommandList { Commands = [removed] };
        var scheduler = new SchedulerData();
        var batch = EnvironmentReplacementBatch.Capture(commands, scheduler);
        commands.Commands.Clear();

        batch.Apply(commands, scheduler, ["%REMOVED%", null]).Should().BeTrue();
        removed.FileName.Should().Be("removed.exe");
    }
}
