using System.Xml.Serialization;

namespace Launcher.Core;

public enum WindowStyle
{
    [XmlEnum("0")] Normal = 0,
    [XmlEnum("1")] Minimized = 1,
    [XmlEnum("2")] Maximized = 2,
    [XmlEnum("3")] NoActivate = 3,
    [XmlEnum("4")] MinimizedNoActivate = 4,
    [XmlEnum("5")] Hidden = 5,
}

public enum ProcessPriorityLevel
{
    [XmlEnum("0")] RealTime = 0,
    [XmlEnum("1")] High = 1,
    [XmlEnum("2")] AboveNormal = 2,
    [XmlEnum("3")] Normal = 3,
    [XmlEnum("4")] BelowNormal = 4,
    [XmlEnum("5")] Idle = 5,
}

public enum CloseButtonBehavior
{
    [XmlEnum("0")] Disabled = 0,
    [XmlEnum("1")] Hide = 1,
    [XmlEnum("2")] Exit = 2,
}

public enum TrayIconAction
{
    [XmlEnum("0")] ShowHide = 0,
    [XmlEnum("1")] ShowConfig = 1,
}

public enum ItemAction
{
    [XmlEnum("0")] Execute = 0,
    [XmlEnum("1")] OpenDirectory = 1,
    [XmlEnum("2")] EditCommand = 2,
    [XmlEnum("3")] Delete = 3,
}

public enum AdminElevation
{
    [XmlEnum("0")] RunAs = 0,
    [XmlEnum("1")] RunAsCommand = 1,
    [XmlEnum("2")] VistaElevator = 2,
}

public enum ButtonLauncherActivation
{
    [XmlEnum("0")] Disabled = 0,
    [XmlEnum("1")] LeftThenRight = 1,
    [XmlEnum("2")] RightThenLeft = 2,
}
