using System.ComponentModel;
using System.Diagnostics;
using FluentAssertions;
using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

public sealed class LaunchRequestTests
{
    [Fact]
    public void 起動元によらず作業フォルダーと環境変数を同じ規則で決める()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string file = Path.Combine(directory, "missing.exe");
            var command = new Command { FileName = file, Param = "%SystemRoot%" };
            var config = new Config { OpenDirByFiler = false };
            var request = command.Execute("", config, IntPtr.Zero, false);
            var scheduled = SchedulerTaskExecutor.ExecuteFileTask(new SchedulerTask
            {
                FileName = file, Param = "%SystemRoot%",
            });
            request.WorkingDirectory.Should().Be(directory);
            scheduled.WorkingDirectory.Should().Be(directory);
            request.Arguments!.Trim().Should().Be(scheduled.Arguments);
            scheduled.Arguments.Should().Be(Environment.GetEnvironmentVariable("SystemRoot"));

            command.FileName = directory;
            command.Execute("", config, IntPtr.Zero, false).WorkingDirectory.Should().Be(directory);
            command.FileName = file;
            var folder = command.OpenDirectory(config, file)!;
            folder.FileName.Should().Be(directory);
            folder.WorkingDirectory.Should().Be(directory);
            folder.ErrorDialog.Should().BeFalse();

            command.WorkDir = "%SystemRoot%";
            command.Execute("", config, IntPtr.Zero, false).WorkingDirectory
                .Should().Be(Environment.GetEnvironmentVariable("SystemRoot"));
        }
        finally
        {
            Directory.Delete(directory);
        }
    }

    [Theory]
    [InlineData(AdminElevation.RunAs, "tool.exe", "runas")]
    [InlineData(AdminElevation.RunAsCommand, "runas", null)]
    [InlineData(AdminElevation.VistaElevator, "elevator.exe", null)]
    public void 管理者起動の要求を生成し既に昇格済みなら再昇格しない(
        AdminElevation method, string fileName, string? verb)
    {
        var command = new Command { FileName = "tool.exe", RunAsAdmin = true };
        var config = new Config { RunAsAdminType = method, VECmdPath = "elevator.exe" };
        var request = command.Execute("", config, IntPtr.Zero, false);
        request.FileName.Should().Be(fileName);
        request.Verb.Should().Be(verb);
        var elevated = command.Execute("", config, IntPtr.Zero, true);
        elevated.FileName.Should().Be("tool.exe");
        elevated.Verb.Should().BeNull();
    }

    [Theory]
    [InlineData(1223, 0)]
    [InlineData(2, 1)]
    [InlineData(5, 1)]
    public void キャンセルを無視し他の起動失敗をログと通知へ渡す(int errorCode, int expected)
    {
        var logged = new List<Exception>();
        var notified = new List<Exception>();
        var error = new Win32Exception(errorCode);
        LaunchFailureHandler.Execute(() => throw error, logged.Add, notified.Add).Should().BeFalse();
        logged.Should().HaveCount(expected);
        notified.Should().Equal(logged);
        LaunchFailureHandler.Execute(() => { }, logged.Add, notified.Add).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void 既存XMLの数値優先度を同じ数値で保存する(int value)
    {
        var config = ConfigStore.DeserializeFromString<Config>(
            $"<Config><ProcessPriority>{value}</ProcessPriority></Config>");
        ((int)config.ProcessPriority).Should().Be(value);
        config.SerializeToString().Should().Contain($"<ProcessPriority>{value}</ProcessPriority>");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void 両ランチャーの親フォルダー起動がShellで成功してプロセスが存続する(bool button)
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Exception? failure = null;
        using var completed = new ManualResetEventSlim();
        try
        {
            var worker = StaThreadRunner.Start(() =>
            {
                try
                {
                    using var owner = new Form();
                    Command command = button ? new ButtonEntry() : new Command();
                    command.FileName = Path.Combine(directory, "missing.exe");
                    // 両フォームのフォルダー操作が使う入口から実際のShellExecuteExまで通す。
                    var result = ShellLaunchService.OpenDirectory(owner, command,
                        new Config { OpenDirByFiler = false }).GetAwaiter().GetResult();
                    result.Should().BeTrue();
                    using var process = Process.GetCurrentProcess();
                    process.HasExited.Should().BeFalse();
                }
#pragma warning disable CA1031 // STAテストの失敗をテストスレッドへ返す
                catch (Exception ex) { failure = ex; }
#pragma warning restore CA1031
                finally { completed.Set(); }
            });
            completed.Wait(TimeSpan.FromSeconds(30)).Should().BeTrue();
            worker.Join();
            failure.Should().BeNull();
        }
        finally
        {
            Directory.Delete(directory);
        }
    }
}
