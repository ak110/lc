using System.ComponentModel;
using Launcher.Core;

namespace Launcher.UI;

/// <summary>コマンドと予定タスクで共通の表示形式・優先度を選択する。</summary>
public sealed class LaunchOptionsControl : UserControl
{
    readonly RadioButtonList windowStyles;
    readonly RadioButtonList priorities;

    public LaunchOptionsControl()
    {
        AutoScaleMode = AutoScaleMode.None;
        Size = new Size(280, 152);
        var showGroup = new GroupBox { Text = "表示(&S)", Location = Point.Empty, Size = new Size(280, 72) };
        var priorityGroup = new GroupBox { Text = "優先度(&P)", Location = new Point(0, 80), Size = new Size(240, 72), TabIndex = 1 };
        windowStyles = new RadioButtonList
        {
            Name = "windowStyles",
            Location = new Point(12, 24),
            ColumnCount = 3,
            StringItems = ["通常", "最小化", "最大化", "非ｱｸﾃｨﾌﾞ", "最小化非ｱｸﾃｨﾌﾞ", "非表示"],
        };
        priorities = new RadioButtonList
        {
            Name = "priorities",
            Location = new Point(12, 24),
            ColumnCount = 3,
            StringItems = ["最高", "高", "通常以上", "通常", "通常以下", "低"],
        };
        showGroup.Controls.Add(windowStyles);
        priorityGroup.Controls.Add(priorities);
        Controls.Add(showGroup);
        Controls.Add(priorityGroup);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public WindowStyle WindowStyle
    {
        get => (WindowStyle)windowStyles.SelectedIndex;
        set => windowStyles.SelectedIndex = (int)value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ProcessPriorityLevel Priority
    {
        get => (ProcessPriorityLevel)priorities.SelectedIndex;
        set => priorities.SelectedIndex = (int)value;
    }
}
