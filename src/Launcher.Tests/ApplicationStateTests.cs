using System.Runtime.ExceptionServices;
using FluentAssertions;
using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

[CollectionDefinition("ApplicationState", DisableParallelization = true)]
public sealed class ApplicationStateCollection { }

[Collection("ApplicationState")]
public sealed class ApplicationStateTests
{
    [Fact]
    public void 再読込後のボタン画面が持つタブへの編集と移動を保存できる()
    {
        RunWithHost((host, baseName) =>
        {
            host.ButtonLauncherData.Tabs.Add(new ButtonTab { Name = "移動元" });
            host.ButtonLauncherData.Tabs.Add(new ButtonTab { Name = "移動先" });
            host.ButtonLauncherData.Save(host.ReportSaveFailure, baseName).Should().BeTrue();
            using var menu = new ContextMenuStrip();
            using var form = new ButtonLauncherForm(host, menu);
            var tabs = form.Controls.OfType<TabControl>().Single();
            var source = (ButtonTab)tabs.TabPages[0].Tag!;
            var destination = (ButtonTab)tabs.TabPages[1].Tag!;
            host.Reload();

            source.SetButton(0, 0, new ButtonEntry { Name = "編集", FileName = "a.exe" });
            source.GetButton(0, 0)!.Param = "編集内容";
            ButtonLauncherPresenter.SwapButtons(source, 0, 0, destination, 0, 0, source.GetButton(0, 0)!);
            var deleted = ButtonLauncherPresenter.DeleteTab(host.ButtonLauncherData,
                host.ButtonLauncherData.Tabs.IndexOf(source));
            deleted.Success.Should().BeTrue();
            var toolbar = form.Controls.OfType<ToolStrip>().Single();
            toolbar.Items["lockButton"]!.PerformClick();

            var saved = ButtonLauncherData.Load(baseName).Value;
            saved.Tabs.Should().ContainSingle().Which.Name.Should().Be("移動先");
            saved.Tabs[0].GetButton(0, 0)!.Param.Should().Be("編集内容");
        });
    }

    [Fact]
    public void 再読込で両コマンド画面を更新し保持した編集参照を保存できる()
    {
        RunWithHost((host, baseName) =>
        {
            var commands = host.CommandList;
            var buttons = host.ButtonLauncherData;
            var retained = new Command { Name = "保持", FileName = "a.exe" };
            commands.Add(retained);
            commands.Save(host.ReportSaveFailure, baseName).Should().BeTrue();
            host.RefreshCommandLauncherFormCommandList();
            using var management = new CommandManagementForm(host);
            var launcher = host.OwnedForms.OfType<CommandLauncherForm>().Single();

            CommandList.AddAndSave(new Command { Name = "追加", FileName = "b.exe" },
                _ => Assert.Fail("登録の保存失敗"), _ => Assert.Fail("登録の読込失敗"), baseName)
                .Should().BeTrue();
            host.Reload();

            host.CommandList.Should().BeSameAs(commands);
            host.ButtonLauncherData.Should().BeSameAs(buttons);
            foreach (var form in new Form[] { launcher, management })
            {
                var list = form.Controls.Find("listView1", true).OfType<ListView>().Single();
                list.Items.Cast<ListViewItem>().Select(item => ((Command)item.Tag!).Name)
                    .Should().BeEquivalentTo("保持", "追加");
            }
            retained.Param = "編集後";
            host.SaveEditedCommand(retained).Should().BeTrue();
            CommandList.Load(baseName).Value.Commands.Single(command => command.Name == "保持")
                .Param.Should().Be("編集後");
        });
    }

    [Fact]
    public void 終了準備でメモと設定の遅延保存を確定しウィンドウハンドルを削除する()
    {
        RunWithHost((host, baseName) =>
        {
            var launcher = host.OwnedForms.OfType<CommandLauncherForm>().Single();
            launcher.Show();
            launcher.Location = new Point(80, 90);
            host.ShowHideMemo();
            var memo = host.OwnedForms.OfType<MemoForm>().Single();
            var tabs = memo.Controls.OfType<TabControl>().Single();
            var textBox = tabs.SelectedTab!.Controls.OfType<RichTextBox>().Single();
            textBox.Text = "終了直前の本文";

            host.PrepareShutdown();

            MemoData.Load(baseName).Value.Tabs[0].Text.Should().Be("終了直前の本文");
            Config.Load(baseName).Value.WindowPos.Should().Be(new Point(80, 90));
            Data.Load(baseName).Value.WindowHandle.Should().Be(0);
            // 更新終了と通常終了が続けて呼んでも二重に終了処理をしない。
            host.PrepareShutdown();
            Data.Load(baseName).Value.WindowHandle.Should().Be(0);
        });
    }

    static void RunWithHost(Action<ApplicationHostForm, string> action)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = StaThreadRunner.Start(() =>
        {
            string directory = Path.Combine(Path.GetTempPath(), "lc_host_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string baseName = Path.Combine(directory, "data");
                new Config
                {
                    HideFirst = true,
                    TrayIcon = false,
                    HotKey = "",
                    MemoHotKey = "",
                    ReplaceEnv = [],
                    WindowHideNoActive = false,
                }.Save(_ => Assert.Fail("初期設定の保存失敗"), baseName).Should().BeTrue();
                using var host = new ApplicationHostForm(baseName);
                try
                {
                    action(host, baseName);
                }
                finally
                {
                    host.PrepareShutdown();
                    host.Close();
                }
            }
#pragma warning disable CA1031 // STAで発生したテスト失敗を呼び出し元へ再送出する
            catch (Exception error)
#pragma warning restore CA1031
            {
                failure = ExceptionDispatchInfo.Capture(error);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        });
        thread.Join(TimeSpan.FromSeconds(30)).Should().BeTrue("STAでのフォーム操作を完了する");
        failure?.Throw();
    }
}
