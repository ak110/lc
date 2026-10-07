using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FluentAssertions;
using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

[Collection("UI受入")]
public sealed class LaunchRequestTests
{
    [Theory]
    [InlineData("https://github.com/ak110/lc")]
    [InlineData("https://example.com/path?q=a/b#part")]
    [InlineData("mailto:example@example.com")]
    public void URIの起動要求はURLを保持し作業フォルダーを推定しない(string uri)
    {
        var request = LaunchRequestBuilder.Create(uri);
        request.FileName.Should().Be(uri);
        request.WorkingDirectory.Should().BeNull();
    }

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
                FileName = file,
                Param = "%SystemRoot%",
            });
            request.WorkingDirectory.Should().Be(directory);
            scheduled.WorkingDirectory.Should().Be(directory);
            request.Arguments!.Trim().Should().Be(scheduled.Arguments);
            scheduled.Arguments.Should().Be(Environment.GetEnvironmentVariable("SystemRoot"));

            command.FileName = directory;
            command.Execute("", config, IntPtr.Zero, false).WorkingDirectory.Should().Be(directory);
            command.FileName = file;
            var folder = Command.OpenDirectory(config, file)!;
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
    [InlineData(false)]
    [InlineData(true)]
    public void 両ランチャーの親フォルダー起動がShellで成功してプロセスが存続する(bool button) => UiAcceptance.Run(() =>
    {
        using var data = new UiHostData(new Config
        {
            TrayIcon = false,
            HotKey = "",
            WindowHideNoActive = false,
            ButtonLauncherActivation = ButtonLauncherActivation.LeftThenRight,
            OpenDirByFiler = false,
        });
        string directory = Path.GetDirectoryName(data.BaseName)!;
        using var host = new ApplicationHostForm(data.BaseName);
        object? folderWindow = null;
        try
        {
            var command = new Command { Name = "親フォルダー受入", FileName = Path.Combine(directory, "missing.exe") };
            Control target;
            ContextMenuStrip menu;
            if (button)
            {
                host.ButtonLauncherData.Tabs[0].SetButton(0, 0, ButtonEntry.FromCommand(command, 0, 0));
                var launcher = host.OwnedForms.OfType<ButtonLauncherForm>().Single();
                launcher.ShowLauncher();
                var tabs = launcher.Controls.OfType<TabControl>().Single();
                target = tabs.SelectedTab!.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<Button>().First();
                menu = target.ContextMenuStrip!;
            }
            else
            {
                host.CommandList.Add(command);
                host.RefreshCommandLauncherFormCommandList();
                var launcher = host.OwnedForms.OfType<CommandLauncherForm>().Single();
                launcher.ShowWindow();
                var list = (ListView)launcher.Controls.Find("listView1", true).Single();
                list.Items.Cast<ListViewItem>().Single(item => ReferenceEquals(item.Tag, command)).Selected = true;
                target = list;
                menu = list.ContextMenuStrip!;
            }
            using var context = new ApplicationContext();
            using var timer = new System.Windows.Forms.Timer { Interval = 10 };
            var elapsed = Stopwatch.StartNew();
            timer.Tick += (_, _) =>
            {
                folderWindow = FindFolderWindow(directory);
                if (folderWindow is not null || elapsed.Elapsed > TimeSpan.FromSeconds(30)) context.ExitThread();
            };
            menu.Show(target, Point.Empty);
            menu.Items.Cast<ToolStripItem>().Single(item => item.Text?.StartsWith("フォルダを開く", StringComparison.Ordinal) == true).PerformClick();
            menu.Close();
            timer.Start();
            Application.Run(context);
            folderWindow.Should().NotBeNull("ファイラー設定を無効にした両フォームから実際に親フォルダーを開く");
            using var process = Process.GetCurrentProcess();
            process.HasExited.Should().BeFalse();
        }
        finally
        {
            // 一意な一時フォルダーを表示する、自分の起動結果だけを閉じる。
            folderWindow ??= FindFolderWindow(directory);
            if (folderWindow is not null)
            {
                try { ((dynamic)folderWindow).Quit(); }
                finally { Marshal.ReleaseComObject(folderWindow); }
            }
            host.Close();
        }
    });

    static object? FindFolderWindow(string directory)
    {
        // Shellの公開COM APIで、今回作成したフォルダーのウィンドウだけを識別する。
        dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application")!)!;
        try
        {
            dynamic windows = shell.Windows();
            try
            {
                for (int index = 0; index < (int)windows.Count; index++)
                {
                    object? window = windows.Item(index);
                    if (window is null) continue;
                    try
                    {
                        string location = (string)((dynamic)window).LocationURL;
                        if (Uri.TryCreate(location, UriKind.Absolute, out var uri) && uri.IsFile &&
                            PathHelper.EqualsPath(uri.LocalPath, directory))
                        {
                            object ownedWindow = window;
                            window = null;
                            return ownedWindow;
                        }
                    }
                    finally
                    {
                        if (window is not null) Marshal.ReleaseComObject(window);
                    }
                }
                return null;
            }
            finally { Marshal.ReleaseComObject(windows); }
        }
        finally { Marshal.ReleaseComObject(shell); }
    }
}
