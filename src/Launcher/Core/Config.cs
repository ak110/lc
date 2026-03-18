#nullable disable
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Serialization;
using Launcher.Infrastructure;

namespace Launcher.Core;

public class Config : ConfigStore, ICloneable
{
    public bool Debug { get; set; } = false;

    public TrayIconAction IconDoubleClick { get; set; } = TrayIconAction.ShowHide;
    public ItemAction ItemDoubleClick { get; set; } = ItemAction.Execute;
    public int ProcessPriority { get; set; } = 3;
    public bool HideFirst { get; set; } = false;

    public string HotKey { get; set; } = "Win+Space";

    public bool OpenDirByFiler { get; set; } = true;
    public string Filer { get; set; } = "Explorer.exe";
    public string OpenParentFiler { get; set; } = "Explorer.exe";
    public string OpenParentFilerParam1 { get; set; } = "/select,";
    public string OpenParentFilerParam2 { get; set; } = "";

    public bool LargeIcon { get; set; } = true;
    public bool TrayIcon { get; set; } = true;
    public List<string> ReplaceEnv { get; set; } = new List<string>();

    public CloseButtonBehavior CloseButton { get; set; } = CloseButtonBehavior.Hide;
    public bool WindowNoResize { get; set; } = false;
    public bool WindowTopMost { get; set; } = false;
    public bool WindowHideNoActive { get; set; } = true;
    public bool HideOnRun { get; set; } = false;
    public bool CommandIgnoreCase { get; set; } = true;

    public Point WindowPos { get; set; } = new Point(200, 125);
    public Size WindowSize { get; set; } = new Size(400, 350);

    public AdminElevation RunAsAdminType { get; set; } = AdminElevation.RunAs;
    public string RunAsCommandLine { get; set; } = "/user:Administrator /savecred";
    public string VECmdPath { get; set; } = @"%ProgramFiles%\Vistaのエレベータ\VECmd.exe";

    public ButtonLauncherActivation ButtonLauncherActivation { get; set; } = ButtonLauncherActivation.Disabled;

    /// <summary>
    /// 後方互換: 旧UseTreeLauncher=true → LeftThenRight にマッピング
    /// </summary>
    [XmlElement("UseTreeLauncher")]
    public bool UseTreeLauncherCompat
    {
        get => ButtonLauncherActivation != ButtonLauncherActivation.Disabled;
        set
        {
            if (value && ButtonLauncherActivation == ButtonLauncherActivation.Disabled)
            {
                ButtonLauncherActivation = ButtonLauncherActivation.LeftThenRight;
            }
        }
    }


    /// <summary>
    /// 複製の作成
    /// </summary>
    public Config Clone()
    {
        Config copy = (Config)MemberwiseClone();
        return copy;
    }

    #region ICloneable メンバ

    object ICloneable.Clone()
    {
        return Clone();
    }

    #endregion

    #region Serialize/Deserialize

    /// <summary>
    /// 書き込み
    /// </summary>
    public void Serialize()
    {
        Serialize(".cfg");
    }

    /// <summary>
    /// 読み込み
    /// </summary>
    public static Config Deserialize()
    {
        try
        {
            return Deserialize<Config>(".cfg");
        }
        catch
        {
            string name = DefaultBaseName + ".cfg";
            if (File.Exists(name))
            {
                LegacyConfigReader reader = new LegacyConfigReader(name);
                try
                {
                    return Config.LoadFrom(reader);
                }
                catch
                {
                }
            }
            return new Config();
        }
    }

    #endregion

    /// <summary>
    /// 後方互換性のための処理
    /// </summary>
    public static Config LoadFrom(LegacyConfigReader reader)
    {
        Config data = new Config();

        data.IconDoubleClick = (TrayIconAction)reader.Num("IconDoubleClick");
        int itemDblClick = reader.Num("ItemDoubleClick");
        if (itemDblClick == 0)
        {
            itemDblClick = 1;
        }
        else if (itemDblClick == 1)
        {
            itemDblClick = 0;
        }
        data.ItemDoubleClick = (ItemAction)itemDblClick;
        data.ProcessPriority = reader.Num("ProcessPriority");
        data.HideFirst = reader.Bool("HideFirst");

        data.HotKey = reader.Indirect("HotKey");

        data.OpenDirByFiler = reader.Bool("OpenDirByFiler");
        data.Filer = reader.Indirect("Filer");
        data.OpenParentFiler = reader.Indirect("OpenParentFiler");
        data.OpenParentFilerParam1 = reader.Indirect("OpenParentFilerParam1");
        data.OpenParentFilerParam2 = reader.Indirect("OpenParentFilerParam2");

        data.LargeIcon = reader.Bool("LargeIcon");
        data.TrayIcon = reader.Bool("TrayIcon");
        data.ReplaceEnv = new List<string>(
            reader.Indirect("ReplaceEnv").Split('%'));

        data.CloseButton = reader.Bool("WindowNoClose") ? CloseButtonBehavior.Disabled : CloseButtonBehavior.Hide;
        data.WindowNoResize = reader.Bool("WindowNoResize");
        data.WindowTopMost = reader.Bool("WindowTopMost");
        data.WindowHideNoActive = reader.Bool("WindowHideNoActive");
        data.CommandIgnoreCase = !reader.Bool("CommandCharDistinct");

        Regex regex = new Regex(@"(-?\d+), *(-?\d+)");
        Match m = regex.Match(reader.Indirect("WindowPos"));
        if (m.Success)
        {
            data.WindowPos = new Point(
                int.Parse(m.Groups[1].Value),
                int.Parse(m.Groups[2].Value));
        }

        m = regex.Match(reader.Indirect("WindowSize"));
        if (m.Success)
        {
            data.WindowSize = new Size(
                int.Parse(m.Groups[1].Value),
                int.Parse(m.Groups[2].Value));
        }

        return data;
    }
}
