using System.Runtime.ExceptionServices;
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
}
