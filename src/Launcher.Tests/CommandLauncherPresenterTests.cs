using System.Windows.Forms;
using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

public sealed class CommandLauncherPresenterTests
{
    private readonly Config _config = new Config { CommandIgnoreCase = true };

    private static CommandList CreateCommandList(params string[] names)
    {
        var list = new CommandList();
        foreach (var name in names)
        {
            list.Commands.Add(new Command { Name = name, FileName = $"{name}.exe" });
        }
        return list;
    }

    #region ProcessTextChange

    [Fact]
    public void ProcessTextChange_空入力はEmpty()
    {
        var commandList = CreateCommandList("notepad", "calc");
        var presenter = new CommandLauncherPresenter(() => commandList, () => _config);

        var result = presenter.ProcessTextChange("");

        result.State.Should().Be(InputState.Empty);
        result.MatchedCommands.Should().HaveCount(2);
        result.CompletionText.Should().BeNull();
    }

    [Fact]
    public void ProcessTextChange_前方一致はPrefixMatchで補完テキストあり()
    {
        var commandList = CreateCommandList("notepad", "calc");
        var presenter = new CommandLauncherPresenter(() => commandList, () => _config);

        var result = presenter.ProcessTextChange("note");

        result.State.Should().Be(InputState.PrefixMatch);
        result.CompletionText.Should().Be("notepad");
        result.SelectionStart.Should().Be(4);
        result.SelectionLength.Should().Be(3);
    }

    [Fact]
    public void ProcessTextChange_完全一致はPrefixMatchで補完なし()
    {
        var commandList = CreateCommandList("notepad");
        var presenter = new CommandLauncherPresenter(() => commandList, () => _config);

        var result = presenter.ProcessTextChange("notepad");

        result.State.Should().Be(InputState.PrefixMatch);
        result.CompletionText.Should().BeNull();
    }

    [Fact]
    public void ProcessTextChange_部分一致はPartialMatch()
    {
        var commandList = CreateCommandList("notepad");
        var presenter = new CommandLauncherPresenter(() => commandList, () => _config);

        var result = presenter.ProcessTextChange("pad");

        result.State.Should().Be(InputState.PartialMatch);
        result.MatchedCommands.Should().HaveCount(1);
        result.CompletionText.Should().BeNull();
    }

    [Fact]
    public void ProcessTextChange_不一致はNoMatch()
    {
        var commandList = CreateCommandList("notepad");
        var presenter = new CommandLauncherPresenter(() => commandList, () => _config);

        var result = presenter.ProcessTextChange("xyz");

        result.State.Should().Be(InputState.NoMatch);
        result.MatchedCommands.Should().BeEmpty();
    }

    [Fact]
    public void ProcessTextChange_大文字小文字無視で前方一致()
    {
        var commandList = CreateCommandList("Notepad");
        var presenter = new CommandLauncherPresenter(() => commandList, () => _config);

        var result = presenter.ProcessTextChange("note");

        result.State.Should().Be(InputState.PrefixMatch);
        result.CompletionText.Should().Be("notepad"); // 入力+残り
    }

    #endregion

    #region GetButtonTexts

    [Fact]
    public void GetButtonTexts_Empty時に設定と隠す()
    {
        var texts = CommandLauncherPresenter.GetButtonTexts(InputState.Empty, lastFocus: 0, Keys.None);

        texts.Button1Text.Should().Be("設定");
        texts.Button2Text.Should().Be("隠す");
    }

    [Fact]
    public void GetButtonTexts_NoMatch時に追加と削除()
    {
        var texts = CommandLauncherPresenter.GetButtonTexts(InputState.NoMatch, lastFocus: 0, Keys.None);

        texts.Button1Text.Should().Be("追加");
        texts.Button2Text.Should().Be("消去");
    }

    [Fact]
    public void GetButtonTexts_PrefixMatch_Ctrlでｺﾏﾝﾄﾞ()
    {
        var texts = CommandLauncherPresenter.GetButtonTexts(InputState.PrefixMatch, lastFocus: 0, Keys.Control);

        texts.Button1Text.Should().Be("ｺﾏﾝﾄﾞ");
    }

    [Fact]
    public void GetButtonTexts_PrefixMatch_Shiftでﾌｫﾙﾀﾞ()
    {
        var texts = CommandLauncherPresenter.GetButtonTexts(InputState.PrefixMatch, lastFocus: 0, Keys.Shift);

        texts.Button1Text.Should().Be("ﾌｫﾙﾀﾞ");
    }

    [Fact]
    public void GetButtonTexts_PrefixMatch_デフォルトで実行()
    {
        var texts = CommandLauncherPresenter.GetButtonTexts(InputState.PrefixMatch, lastFocus: 0, Keys.None);

        texts.Button1Text.Should().Be("実行");
    }

    [Fact]
    public void GetButtonTexts_リストビューフォーカス時は常に実行系()
    {
        // lastFocus=1のとき、stateがEmptyでも実行系ボタンになる
        var texts = CommandLauncherPresenter.GetButtonTexts(InputState.Empty, lastFocus: 1, Keys.None);

        texts.Button1Text.Should().Be("実行");
        // button2はstateに依存 (Emptyなので「隠す」)
        texts.Button2Text.Should().Be("隠す");
    }

    [Fact]
    public void GetButtonTexts_PartialMatch時は実行()
    {
        var texts = CommandLauncherPresenter.GetButtonTexts(InputState.PartialMatch, lastFocus: 0, Keys.None);

        texts.Button1Text.Should().Be("実行");
        texts.Button2Text.Should().Be("消去");
    }

    #endregion

    #region DetermineAction

    [Fact]
    public void DetermineAction_Empty_Focus0はShowConfig()
    {
        var result = CommandLauncherPresenter.DetermineAction(
            InputState.Empty, lastFocus: 0, null, null, Keys.None);

        result.Action.Should().Be(MainAction.ShowConfig);
        result.TargetCommand.Should().BeNull();
    }

    [Fact]
    public void DetermineAction_コマンドなしはAddCommand()
    {
        var result = CommandLauncherPresenter.DetermineAction(
            InputState.NoMatch, lastFocus: 0, null, null, Keys.None);

        result.Action.Should().Be(MainAction.AddCommand);
    }

    [Fact]
    public void DetermineAction_コマンドあり_デフォルトはExecute()
    {
        var cmd = new Command { Name = "test" };
        var result = CommandLauncherPresenter.DetermineAction(
            InputState.PrefixMatch, lastFocus: 0, cmd, null, Keys.None);

        result.Action.Should().Be(MainAction.Execute);
        result.TargetCommand.Should().Be(cmd);
    }

    [Fact]
    public void DetermineAction_コマンドあり_CtrlはEditCommand()
    {
        var cmd = new Command { Name = "test" };
        var result = CommandLauncherPresenter.DetermineAction(
            InputState.PrefixMatch, lastFocus: 0, cmd, null, Keys.Control);

        result.Action.Should().Be(MainAction.EditCommand);
        result.TargetCommand.Should().Be(cmd);
    }

    [Fact]
    public void DetermineAction_コマンドあり_ShiftはOpenDirectory()
    {
        var cmd = new Command { Name = "test" };
        var result = CommandLauncherPresenter.DetermineAction(
            InputState.PrefixMatch, lastFocus: 0, cmd, null, Keys.Shift);

        result.Action.Should().Be(MainAction.OpenDirectory);
        result.TargetCommand.Should().Be(cmd);
    }

    [Fact]
    public void DetermineAction_リストビューフォーカス時はselectedCommandを使用()
    {
        var first = new Command { Name = "first" };
        var selected = new Command { Name = "selected" };
        var result = CommandLauncherPresenter.DetermineAction(
            InputState.PrefixMatch, lastFocus: 1, first, selected, Keys.None);

        result.Action.Should().Be(MainAction.Execute);
        result.TargetCommand.Should().Be(selected);
    }

    [Fact]
    public void DetermineAction_リストビューフォーカス_選択なしはAddCommand()
    {
        var result = CommandLauncherPresenter.DetermineAction(
            InputState.PrefixMatch, lastFocus: 1, null, null, Keys.None);

        result.Action.Should().Be(MainAction.AddCommand);
    }

    #endregion
}
