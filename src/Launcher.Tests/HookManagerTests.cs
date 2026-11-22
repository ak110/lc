using System.Reflection;
using FluentAssertions;
using Launcher.Core;
using Launcher.Win32;
using Xunit;

namespace Launcher.Tests;

public sealed class HookManagerTests
{
    [Fact]
    public void 無効中に左ボタンを離すと再有効化後の右押下だけでは起動しない()
    {
        var config = new Config { ButtonLauncherActivation = ButtonLauncherActivation.LeftThenRight };
        int activationRequests = 0;
        var errors = new List<Action>();

        var managerType = typeof(Launcher.UI.ConfigForm).Assembly.GetType("Launcher.UI.HookManager", throwOnError: true)!;
        var manager = Activator.CreateInstance(managerType, (Func<Config>)(() => config),
            (Func<IntPtr>)(() =>
            {
                activationRequests++;
                return new IntPtr(-3);
            }), (Action<Action>)errors.Add)!;
        var onMouseHook = managerType.GetMethod("OnMouseHook", BindingFlags.Instance | BindingFlags.NonPublic)!
            .CreateDelegate<EventHandler<MouseHookEventArgs>>(manager);

        MouseHookEventArgs Dispatch(IntPtr message)
        {
            var args = new MouseHookEventArgs(Hook.HC_ACTION, message);
            onMouseHook(null, args);
            return args;
        }

        Dispatch(Hook.WM_LBUTTONDOWN).Handled.Should().BeFalse();
        config.ButtonLauncherActivation = ButtonLauncherActivation.Disabled;
        Dispatch(Hook.WM_LBUTTONUP).Handled.Should().BeFalse();
        config.ButtonLauncherActivation = ButtonLauncherActivation.LeftThenRight;
        var rightDown = Dispatch(Hook.WM_RBUTTONDOWN);

        errors.Should().BeEmpty();
        activationRequests.Should().Be(0);
        rightDown.Handled.Should().BeFalse();

        Dispatch(Hook.WM_RBUTTONUP);
        Dispatch(Hook.WM_LBUTTONDOWN);
        Dispatch(Hook.WM_RBUTTONDOWN).Handled.Should().BeTrue();
        activationRequests.Should().Be(1);
        errors.Should().BeEmpty();
    }
}
