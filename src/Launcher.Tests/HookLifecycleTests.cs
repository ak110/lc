using System.ComponentModel;
using System.Diagnostics;
using FluentAssertions;
using Launcher.Win32;
using Xunit;
using Xunit.Abstractions;

namespace Launcher.Tests;

[Collection("UI受入")]
public sealed class HookLifecycleTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void 登録スレッド終了でOSが解除したフックを繰り返し回収できる(bool mouse)
    {
        using var trace = new StringWriter();
        using var listener = new TextWriterTraceListener(trace);
        Trace.Listeners.Add(listener);
        try
        {
            // 公開APIで実フックを登録し、所有STAを終了してOS側の解除を発生させる。
            UiAcceptance.Run(() =>
            {
                if (mouse) Hook.SetMouseHook();
                else Hook.SetKeyHook();
            });
            Action unregister = mouse ? Hook.UnsetMouseHook : Hook.UnsetKeyHook;
            var error = Record.Exception(unregister);
            if (error is Win32Exception nativeError) output.WriteLine($"UNREGISTER_ERROR={nativeError.NativeErrorCode}");
            error.Should().BeNull();
            unregister.Should().NotThrow();
            listener.Flush();
            trace.ToString().Should().Contain("alreadyRemoved=true error=1404");
        }
        finally
        {
            Trace.Listeners.Remove(listener);
            listener.Flush();
            output.WriteLine(trace.ToString());
        }
    }
}
