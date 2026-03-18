using System.Xml.Serialization;

namespace Launcher.Core;

/// <summary>
/// ウィンドウの表示スタイル
/// </summary>
public enum WindowStyle
{
    [XmlEnum("0")] Normal = 0,
    [XmlEnum("1")] Minimized = 1,
    [XmlEnum("2")] Maximized = 2,
    [XmlEnum("3")] NoActivate = 3,
    [XmlEnum("4")] MinimizedNoActivate = 4,
    [XmlEnum("5")] Hidden = 5,
}

/// <summary>
/// プロセスの優先度
/// </summary>
public enum ProcessPriorityLevel
{
    [XmlEnum("0")] RealTime = 0,
    [XmlEnum("1")] High = 1,
    [XmlEnum("2")] AboveNormal = 2,
    [XmlEnum("3")] Normal = 3,
    [XmlEnum("4")] BelowNormal = 4,
    [XmlEnum("5")] Idle = 5,
}

/// <summary>
/// 閉じるボタンの動作
/// </summary>
public enum CloseButtonBehavior
{
    [XmlEnum("0")] Disabled = 0,
    [XmlEnum("1")] Close = 1,
    [XmlEnum("2")] Hide = 2,
}

/// <summary>
/// トレイアイコンのダブルクリック動作
/// </summary>
public enum TrayIconAction
{
    [XmlEnum("0")] ShowHide = 0,
    [XmlEnum("1")] ShowConfig = 1,
}

/// <summary>
/// アイテमのダブルクリック動作
/// </summary>
public enum ItemAction
{
    [XmlEnum("0")] Execute = 0,
    [XmlEnum("1")] EditConfig = 1,
    [XmlEnum("2")] OpenDirectory = 2,
    [XmlEnum("3")] None = 3,
}

/// <summary>
/// 管理者権限での実行方法
/// </summary>
public enum AdminElevation
{
    [XmlEnum("0")] RunAs = 0,
    [XmlEnum("1")] RunAsCommand = 1,
    [XmlEnum("2")] VistaElevator = 2,
}

/// <summary>
/// ボタン型ランチャーの起動方法
/// </summary>
public enum ButtonLauncherActivation
{
    [XmlEnum("0")] Disabled = 0,
    [XmlEnum("1")] LeftThenRight = 1,
    [XmlEnum("2")] RightThenLeft = 2,
}
