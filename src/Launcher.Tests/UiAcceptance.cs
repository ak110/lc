using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using FluentAssertions;
using Xunit;

namespace Launcher.Tests;

[CollectionDefinition("UI受入", DisableParallelization = true)]
public sealed class UiAcceptanceCollection;

internal static class UiAcceptance
{
    public static void Run(Action action)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex) when ((failure = ExceptionDispatchInfo.Capture(ex)) is not null)
            {
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }

    public static void InspectDialog(Form dialog, Action inspect, Form? owner = null)
    {
        ExceptionDispatchInfo? failure = null;
        dialog.Shown += (_, _) =>
        {
            try
            {
                inspect();
            }
            catch (Exception ex) when ((failure = ExceptionDispatchInfo.Capture(ex)) is not null)
            {
            }
            finally
            {
                dialog.Close();
            }
        };
        Launcher.UI.FormsHelper.ShowDialogOver(dialog, owner);
        failure?.Throw();
    }

    /// <summary>
    /// キー入力を対象コントロールの通常の入力前処理へ送る。キー状態はこのSTAだけで変更して元へ戻す。
    /// SendKeysは前景の別アプリへ届きうるため、受入対象のコントロールへ限定する。
    /// </summary>
    public static void PreProcessShortcut(Control control, Keys keys)
    {
        var original = new byte[256];
        GetKeyboardState(original).Should().BeTrue();
        var state = new byte[256];
        if ((keys & Keys.Control) != 0) state[(int)Keys.ControlKey] = 0x80;
        if ((keys & Keys.Shift) != 0) state[(int)Keys.ShiftKey] = 0x80;
        try
        {
            SetKeyboardState(state).Should().BeTrue();
            var message = Message.Create(control.Handle, 0x0100, (nint)(keys & Keys.KeyCode), nint.Zero);
            control.PreProcessMessage(ref message).Should().BeTrue();
        }
        finally
        {
            SetKeyboardState(original).Should().BeTrue();
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetKeyboardState([Out] byte[] state);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetKeyboardState(byte[] state);
}
