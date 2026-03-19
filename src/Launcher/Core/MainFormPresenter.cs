using System.Windows.Forms;

namespace Launcher.Core;

/// <summary>入力欄とコマンドリストの状態</summary>
public enum InputState
{
    Empty,        // 入力欄が空
    NoMatch,      // 該当コマンド無し
    PartialMatch, // 部分一致のみ有り
    PrefixMatch,  // 前方一致有り
}

/// <summary>テキスト変更時の処理結果</summary>
public record TextChangeResult(
    InputState State,
    IReadOnlyList<Command> MatchedCommands,
    string? CompletionText,  // null = 補完なし
    int SelectionStart,      // 補完時のカーソル位置
    int SelectionLength      // 補完時の選択範囲長
);

/// <summary>ボタンテキストの表示内容</summary>
public record ButtonTexts(string Button1Text, string Button2Text);

/// <summary>ボタン1クリック時のアクション種別</summary>
public enum MainAction { ShowConfig, AddCommand, Execute, EditCommand, OpenDirectory }

/// <summary>ボタン1クリック時の判定結果</summary>
public record ButtonClickResult(MainAction Action, Command? TargetCommand);

/// <summary>
/// MainFormのビジネスロジックを担当するPresenter。
/// UI操作を伴わない判定・計算ロジックを集約する。
/// </summary>
public class MainFormPresenter(Func<CommandList> getCommandList, Func<Config> getConfig)
{
    /// <summary>
    /// テキスト入力変更時のロジック。
    /// コマンドマッチングと補完テキストの決定を行う。
    /// </summary>
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
                inputText.Length, config.CommandIgnoreCase) == 0)
            {
                // 前方一致 → 補完処理
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

    /// <summary>
    /// ボタンのテキストを決定する。
    /// </summary>
    /// <param name="state">現在の入力状態</param>
    /// <param name="lastFocus">0=エディットボックス, 1=リストビュー</param>
    /// <param name="modifierKeys">現在の修飾キー状態</param>
    public static ButtonTexts GetButtonTexts(InputState state, int lastFocus, Keys modifierKeys)
    {
        // リストビューにフォーカスがある場合はコマンド有無に関わらず実行系ボタン表示
        InputState? effectiveState = lastFocus == 1 ? null : state;

        // OKボタン
        string button1Text = effectiveState switch
        {
            InputState.Empty => "設定",
            InputState.NoMatch => "追加",
            _ => modifierKeys switch
            {
                Keys.Control => "ｺﾏﾝﾄﾞ",
                Keys.Shift => "ﾌｫﾙﾀﾞ",
                _ => "実行",
            },
        };

        // キャンセルボタン
        string button2Text = state == InputState.Empty ? "隠す" : "消す";

        return new ButtonTexts(button1Text, button2Text);
    }

    /// <summary>
    /// ボタン1クリック時のアクションを判定する。
    /// </summary>
    /// <param name="state">現在の入力状態</param>
    /// <param name="lastFocus">0=エディットボックス, 1=リストビュー</param>
    /// <param name="firstCommand">リストビュー先頭のコマンド（存在する場合）</param>
    /// <param name="selectedCommand">リストビューで選択中のコマンド（存在する場合）</param>
    /// <param name="modifierKeys">現在の修飾キー状態</param>
    public static ButtonClickResult DetermineAction(
        InputState state, int lastFocus,
        Command? firstCommand, Command? selectedCommand,
        Keys modifierKeys)
    {
        // フォーカス位置に応じてコマンドを決定
        Command? command = lastFocus == 0 ? firstCommand : selectedCommand;

        if (state == InputState.Empty && lastFocus == 0)
        {
            return new ButtonClickResult(MainAction.ShowConfig, null);
        }

        if (command == null)
        {
            return new ButtonClickResult(MainAction.AddCommand, null);
        }

        // コマンドありの場合、修飾キーで分岐
        MainAction action = modifierKeys switch
        {
            Keys.Control => MainAction.EditCommand,
            Keys.Shift => MainAction.OpenDirectory,
            _ => MainAction.Execute,
        };

        return new ButtonClickResult(action, command);
    }
}
