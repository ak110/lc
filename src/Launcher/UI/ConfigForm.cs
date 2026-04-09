using Launcher.Core;
using Launcher.Win32;

namespace Launcher.UI;

public partial class ConfigForm : Form
{
    Config config;

    public Config Config
    {
        get { return config; }
    }

    public int ButtonColumns { get; private set; }
    public int ButtonRows { get; private set; }

    public ConfigForm(Config v, ButtonLauncherData btnData)
    {
        InitializeComponent();

        config = v.Clone();
        ButtonColumns = btnData.Columns;
        ButtonRows = btnData.Rows;

        comboBox1.Items.AddRange(
            KeyTable.GetKeyNames(false));

        var hk = KeyTable.GetKeyWithModifiers(config.HotKey);
        checkBox1.Checked = (hk.Modifiers & KeyTable.Modifiers.Ctrl) != 0;
        checkBox2.Checked = (hk.Modifiers & KeyTable.Modifiers.Alt) != 0;
        checkBox3.Checked = (hk.Modifiers & KeyTable.Modifiers.Shift) != 0;
        checkBox4.Checked = (hk.Modifiers & KeyTable.Modifiers.Win) != 0;
        try
        {
            comboBox1.SelectedItem = KeyTable.GetKeyName(hk.Key!.Value);
        }
        catch (ArgumentException)
        {
            comboBox1.SelectedItem = "Space";
        }
        catch (InvalidOperationException)
        {
            comboBox1.SelectedItem = "Space";
        }
        checkBox5.Checked = config.TrayIcon;
        radioButtonList1.SelectedIndex = (int)config.IconDoubleClick;
        radioButtonList2.SelectedIndex = (int)config.ItemDoubleClick;
        checkBox6.Checked = config.HideFirst;
        checkBox7.Checked = config.LargeIcon;
        checkBox8.Checked = config.WindowTopMost;
        checkBox9.Checked = config.WindowHideNoActive;
        checkBox10.Checked = config.WindowNoResize;
        checkBox11.Checked = config.HideOnRun;
        comboBox2.SelectedIndex = (int)config.CloseButton;
        comboBox3.SelectedIndex = (int)config.ButtonLauncherActivation;
        numericUpDown1.Value = ButtonColumns;
        numericUpDown2.Value = ButtonRows;
    }

    private void buttonOk_Click(object? sender, EventArgs e)
    {
        string key = "";
        if (checkBox1.Checked) key += "Ctrl+";
        if (checkBox2.Checked) key += "Alt+";
        if (checkBox3.Checked) key += "Shift+";
        if (checkBox4.Checked) key += "Win+";
        config.HotKey = key + (comboBox1.SelectedItem?.ToString() ?? "");
        config.TrayIcon = checkBox5.Checked;
        config.IconDoubleClick = (TrayIconAction)radioButtonList1.SelectedIndex;
        config.ItemDoubleClick = (ItemAction)radioButtonList2.SelectedIndex;
        config.HideFirst = checkBox6.Checked;
        config.LargeIcon = checkBox7.Checked;
        config.WindowTopMost = checkBox8.Checked;
        config.WindowHideNoActive = checkBox9.Checked;
        config.WindowNoResize = checkBox10.Checked;
        config.HideOnRun = checkBox11.Checked;
        config.CloseButton = (CloseButtonBehavior)comboBox2.SelectedIndex;
        config.ButtonLauncherActivation = (ButtonLauncherActivation)comboBox3.SelectedIndex;
        ButtonColumns = (int)numericUpDown1.Value;
        ButtonRows = (int)numericUpDown2.Value;
    }

    /// <summary>
    /// 置換環境変数の設定ボタン
    /// </summary>
    private void button1_Click(object? sender, EventArgs e)
    {
        using var form = new EnvConfigForm(config.ReplaceEnv);
        if (form.ShowDialogOver(this) == DialogResult.OK)
        {
            config.ReplaceEnv = form.ReplaceEnv;
        }
    }
}
