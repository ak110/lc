using FluentAssertions;
using Launcher.Core;
using Launcher.UI;
using Launcher.Win32;
using Xunit;

namespace Launcher.Tests;

[Collection("UI受入")]
public sealed class LauncherDialogTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ボタンからコマンドを割り当てるダイアログは親のTopMostに従う(bool topMost) => UiAcceptance.Run(() =>
    {
        using var data = new UiHostData(new Config
        {
            TrayIcon = false,
            HotKey = "",
            WindowHideNoActive = false,
            ButtonLauncherActivation = ButtonLauncherActivation.LeftThenRight,
        });
        using var host = new ApplicationHostForm(data.BaseName);
        try
        {
            var command = new Command { Name = "割当コマンド", FileName = "cmd.exe" };
            host.CommandList.Add(command);
            host.ButtonLauncherData.IsLocked = true;
            var launcher = host.OwnedForms.OfType<ButtonLauncherForm>().Single();
            launcher.TopMost = topMost;
            launcher.ShowLauncher();
            var tabs = launcher.Controls.OfType<TabControl>().Single();
            var grid = tabs.SelectedTab!.Controls.OfType<TableLayoutPanel>().Single();
            var button = grid.Controls.OfType<Button>().First();
            var menu = button.ContextMenuStrip!;
            bool? observedTopMost = null;
            Form? observedOwner = null;
            using var timer = new System.Windows.Forms.Timer { Interval = 10 };
            timer.Tick += (_, _) =>
            {
                var dialog = Application.OpenForms.Cast<Form>().SingleOrDefault(form => form.Text == "コマンドの選択");
                if (dialog is null) return;
                timer.Stop();
                observedTopMost = dialog.TopMost;
                observedOwner = dialog.Owner;
                dialog.Controls.OfType<ListView>().Single().Items[0].Selected = true;
                dialog.AcceptButton!.PerformClick();
            };
            menu.Show(button, Point.Empty);
            timer.Start();
            menu.Items.Cast<ToolStripItem>().Single(item => item.Text == "コマンドから割り当て(&A)").PerformClick();
            observedTopMost.Should().Be(topMost);
            observedOwner.Should().BeSameAs(launcher);
            button.Text.Should().Be(command.Name);
        }
        finally
        {
            host.Close();
        }
    });

    [Fact]
    public void 設定画面の両ホットキーは全修飾キーの組合せを保存する() => UiAcceptance.Run(() =>
    {
        KeyTable.Modifiers[] modifiers = [KeyTable.Modifiers.Ctrl, KeyTable.Modifiers.Alt, KeyTable.Modifiers.Shift, KeyTable.Modifiers.Win];
        for (int bits = 0; bits < 16; bits++)
        {
            using var form = new ConfigForm(new Config(), new ButtonLauncherData());
            KeyTable.Modifiers expected = 0;
            UiAcceptance.InspectDialog(form, () =>
            {
                ((ComboBox)form.Controls.Find("comboBox1", true).Single()).SelectedItem = "Space";
                ((ComboBox)form.Controls.Find("comboBoxM", true).Single()).SelectedItem = "M";
                for (int index = 0; index < modifiers.Length; index++)
                {
                    bool selected = (bits & (1 << index)) != 0;
                    if (selected) expected |= modifiers[index];
                    ((CheckBox)form.Controls.Find($"checkBox{index + 1}", true).Single()).Checked = selected;
                    ((CheckBox)form.Controls.Find($"checkBoxM{index + 1}", true).Single()).Checked = selected;
                }
                form.AcceptButton!.PerformClick();
            });
            var launcher = KeyTable.GetKeyWithModifiers(form.Config.HotKey);
            var memo = KeyTable.GetKeyWithModifiers(form.Config.MemoHotKey);
            launcher.Key.Should().Be(KeyTable.GetKey("Space"));
            memo.Key.Should().Be(KeyTable.GetKey("M"));
            launcher.Modifiers.Should().Be(expected);
            memo.Modifiers.Should().Be(expected);
        }
    });
}
