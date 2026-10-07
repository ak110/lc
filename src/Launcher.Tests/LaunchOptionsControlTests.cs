using System.Xml.Serialization;
using FluentAssertions;
using Launcher.Core;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

[Collection("UI受入")]
public sealed class LaunchOptionsControlTests
{
    [Fact]
    public void コマンド編集で全表示形式と優先度を保存して再編集できる() => UiAcceptance.Run(() =>
    {
        foreach (var style in Enum.GetValues<WindowStyle>())
            foreach (var priority in Enum.GetValues<ProcessPriorityLevel>())
            {
                var command = new Command();
                using (var form = new EditCommandForm(command))
                {
                    UiAcceptance.InspectDialog(form, () =>
                    {
                        var options = form.Controls.OfType<LaunchOptionsControl>().Single();
                        options.WindowStyle = style;
                        options.Priority = priority;
                        form.AcceptButton!.PerformClick();
                    });
                }
                command.Show.Should().Be(style);
                command.Priority.Should().Be(priority);
                var restored = SaveAndLoad(command);
                using var reopened = new EditCommandForm(restored);
                UiAcceptance.InspectDialog(reopened, () =>
                {
                    var options = reopened.Controls.OfType<LaunchOptionsControl>().Single();
                    options.WindowStyle.Should().Be(style);
                    options.Priority.Should().Be(priority);
                });
            }
    });

    [Fact]
    public void 予定タスク編集で全表示形式と優先度を保存して再編集できる() => UiAcceptance.Run(() =>
    {
        foreach (var style in Enum.GetValues<WindowStyle>())
            foreach (var priority in Enum.GetValues<ProcessPriorityLevel>())
            {
                var task = new SchedulerTask { Type = SchedulerTaskType.Execute };
                using (var form = new SchedulerTaskForm(task))
                {
                    UiAcceptance.InspectDialog(form, () =>
                    {
                        var options = form.Controls.OfType<LaunchOptionsControl>().Single();
                        options.WindowStyle = style;
                        options.Priority = priority;
                        form.AcceptButton!.PerformClick();
                    });
                }
                task.Show.Should().Be(style);
                task.Priority.Should().Be(priority);
                var restored = SaveAndLoad(task);
                using var reopened = new SchedulerTaskForm(restored);
                UiAcceptance.InspectDialog(reopened, () =>
                {
                    var options = reopened.Controls.OfType<LaunchOptionsControl>().Single();
                    options.WindowStyle.Should().Be(style);
                    options.Priority.Should().Be(priority);
                });
            }
    });

    static T SaveAndLoad<T>(T value)
    {
        string path = Path.GetTempFileName();
        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var output = File.Create(path)) serializer.Serialize(output, value);
            using var input = File.OpenRead(path);
            return (T)serializer.Deserialize(input)!;
        }
        finally
        {
            File.Delete(path);
        }
    }
}
