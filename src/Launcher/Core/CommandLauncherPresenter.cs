namespace Launcher.Core;

public enum InputState
{
    Empty,
    NoMatch,
    PartialMatch,
    PrefixMatch,
}

public record TextChangeResult(
    InputState State,
    IReadOnlyList<Command> MatchedCommands,
    string? CompletionText,
    int SelectionStart,
    int SelectionLength
);

public record ButtonTexts(string Button1Text, string Button2Text);

public enum MainAction { ShowConfig, AddCommand, Execute, EditCommand, OpenDirectory }

public record ButtonClickResult(MainAction Action, Command? TargetCommand);

public sealed class CommandLauncherPresenter(Func<CommandList> getCommandList, Func<Config> getConfig)
{
    public TextChangeResult ProcessTextChange(string inputText)
    {
        var config = getConfig();
        var commands = getCommandList().FindMatch(inputText, config).ToList();

        InputState state;
        string? completionText = null;
        int selectionStart = 0;
        int selectionLength = 0;

        if (commands.Count > 0)
        {
            var firstCommand = commands[0];
            if (string.IsNullOrEmpty(inputText))
            {
                state = InputState.Empty;
            }
            else if (inputText.Length <= firstCommand.Name.Length &&
                string.Compare(inputText, 0, firstCommand.Name, 0,
                inputText.Length, config.CommandIgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) == 0)
            {
                if (inputText.Length < firstCommand.Name.Length)
                {
                    completionText = string.Concat(inputText, firstCommand.Name.AsSpan(inputText.Length));
                    selectionStart = inputText.Length;
                    selectionLength = completionText.Length - inputText.Length;
                }
                state = InputState.PrefixMatch;
            }
            else
            {
                state = InputState.PartialMatch;
            }
        }
        else
        {
            state = InputState.NoMatch;
        }

        return new TextChangeResult(state, commands, completionText, selectionStart, selectionLength);
    }

    public static ButtonTexts GetButtonTexts(InputState state, int lastFocus, InputModifiers modifiers)
    {
        InputState? effectiveState = lastFocus == 1 ? null : state;

        string button1Text = effectiveState switch
        {
            InputState.Empty => "設定",
            InputState.NoMatch => "追加",
            _ => modifiers switch
            {
                InputModifiers.Control => "ｺﾏﾝﾄﾞ",
                InputModifiers.Shift => "ﾌｫﾙﾀﾞ",
                _ => "実行",
            },
        };

        string button2Text = state == InputState.Empty ? "隠す" : "消去";

        return new ButtonTexts(button1Text, button2Text);
    }

    public static ButtonClickResult DetermineAction(
        InputState state, int lastFocus,
        Command? firstCommand, Command? selectedCommand,
        InputModifiers modifiers)
    {
        Command? command = lastFocus == 0 ? firstCommand : selectedCommand;

        if (state == InputState.Empty && lastFocus == 0)
        {
            return new ButtonClickResult(MainAction.ShowConfig, null);
        }

        if (command is null)
        {
            return new ButtonClickResult(MainAction.AddCommand, null);
        }

        MainAction action = modifiers switch
        {
            InputModifiers.Control => MainAction.EditCommand,
            InputModifiers.Shift => MainAction.OpenDirectory,
            _ => MainAction.Execute,
        };

        return new ButtonClickResult(action, command);
    }
}
