#nullable disable
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Launcher.Infrastructure;
using Launcher.Win32;

namespace Launcher.UI;

/// <summary>
/// スタートアップに登録等のボタン群。
/// </summary>
[DefaultProperty("BaseName")]
public partial class RegisterStartup : UserControl
{
    string baseName;
    string sendToName;

    public RegisterStartup()
    {
        InitializeComponent();
    }

    private void RegisterStartup_Load(object sender, EventArgs e)
    {
        UpdateButtonValid();
    }

    #region IDEプロパティ

    [DefaultValue(null)]
    [Description("作成するショートカットのファイル名(拡張子を除く)")]
    public string BaseName
    {
        get { return baseName; }
        set { baseName = value; }
    }

    [DefaultValue(true)]
    [Description("送るへの登録ボタンを使用する")]
    public bool UseSendTo
    {
        get
        {
            return button5.Visible && button6.Visible;
        }
        set
        {
            button5.Visible = value;
            button6.Visible = value;
        }
    }

    [DefaultValue(null)]
    [Description("送るに登録するファイル名(拡張子を除く＋省略可)。デフォルトならBaseNameを使用する。")]
    public string SendToName
    {
        get { return sendToName; }
        set { sendToName = value; }
    }

    #endregion

    private string InnerGetBaseName()
    {
        if (string.IsNullOrEmpty(baseName))
        {
            return Path.GetFileNameWithoutExtension(
                Environment.ProcessPath);
        }
        return baseName;
    }

    private void button1_Click(object sender, EventArgs e)
    {
        CreateShortcut(GetCommonStartupLinkName());
        UpdateButtonValid();
    }

    private void button2_Click(object sender, EventArgs e)
    {
        File.Delete(GetCommonStartupLinkName());
        UpdateButtonValid();
    }

    private void button3_Click(object sender, EventArgs e)
    {
        CreateShortcut(GetStartupLinkName());
        UpdateButtonValid();
    }

    private void button4_Click(object sender, EventArgs e)
    {
        File.Delete(GetStartupLinkName());
        UpdateButtonValid();
    }

    private void button5_Click(object sender, EventArgs e)
    {
        CreateShortcut(GetSendToName());
        UpdateButtonValid();
    }

    private void button6_Click(object sender, EventArgs e)
    {
        File.Delete(GetSendToName());
        UpdateButtonValid();
    }

    /// <summary>
    /// ショートカットの作成
    /// </summary>
    private static void CreateShortcut(string file)
    {
        using (ShellLink link = new ShellLink())
        {
            link.TargetPath = Environment.ProcessPath;
            link.Arguments = "";
            //link.WorkingDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            link.Description = "";
            //link.IconFile = Process.GetCurrentProcess().MainModule.FileName;
            //link.IconIndex = 0;
            link.Save(file);
        }
    }

    /// <summary>
    /// ボタンの有効・無効を設定
    /// </summary>
    private void UpdateButtonValid()
    {
        int common = -1, user = -1;
        try
        {
            string path = GetCommonStartupLinkName();
            common = File.Exists(path) ? 1 : 0;
        }
        catch (IOException)
        {
        }
        try
        {
            if (!IsCommonStartupWritable())
            {
                common = -1;
            }
        }
        catch (IOException)
        {
        }
        try
        {
            string path = GetStartupLinkName();
            user = File.Exists(path) ? 1 : 0;
        }
        catch (IOException)
        {
        }
        button1.Enabled = common == 0 && user <= 0;
        button2.Enabled = common == 1;
        button3.Enabled = common <= 0 && user == 0;
        button4.Enabled = user == 1;
        if (UseSendTo)
        {
            int sendto = -1;
            try
            {
                sendto = File.Exists(GetSendToName()) ? 1 : 0;
            }
            catch
            {
            }
            button5.Enabled = sendto == 0;
            button6.Enabled = sendto == 1;
        }
    }

    private static bool IsCommonStartupWritable()
    {
        return PathHelper.IsWritable(ShellEnvironment.GetFolderPath(
            ShellEnvironment.SpecialFolder.CommonStartup));
    }

    private string GetCommonStartupLinkName()
    {
        string startupDir = ShellEnvironment.GetFolderPath(
            ShellEnvironment.SpecialFolder.CommonStartup);
        return Path.Combine(startupDir, InnerGetBaseName() + ".lnk");
    }

    private string GetStartupLinkName()
    {
        string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        return Path.Combine(startupDir, InnerGetBaseName() + ".lnk");
    }

    private string GetSendToName()
    {
        string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
        string baseName = string.IsNullOrEmpty(sendToName) ? InnerGetBaseName() : sendToName;
        return Path.Combine(startupDir, baseName + ".lnk");
    }
}
