using System.Diagnostics;
using FluentAssertions;
using Launcher.Core;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

[Collection("UI受入")]
public sealed class SchedulerNotificationTests
{
    [Fact]
    public void 予定の通知は閉じるまで次へ進まず表示中のホットキーでランチャーを隠さない() => UiAcceptance.Run(() =>
    {
        using var data = new UiHostData(new Config
        {
            TrayIcon = false,
            HotKey = "",
            WindowTopMost = true,
            WindowHideNoActive = true,
        });
        data.Write(".dat", new Data { SchedulerLastCheckTime = DateTime.Now.AddMinutes(-2) });
        data.Write(".sch.cfg", new SchedulerData
        {
            Items = [new SchedulerItem
            {
                Id = "同期通知受入",
                SleepTimeMs = 0,
                Schedules = [new Schedule
                {
                    TimeType = ScheduleTimeType.Interval,
                    TimeIntervalStart = new HourMinute(0, 0),
                    TimeIntervalEnd = new HourMinute(23, 59),
                    TimeIntervalMinutes = 1,
                }],
                Tasks = [
                    new SchedulerTask { Type = SchedulerTaskType.MessageBox, Message = "先の通知" },
                    new SchedulerTask { Type = SchedulerTaskType.MessageBox, Message = "後の通知" },
                ],
            }],
        });
        using var host = new ApplicationHostForm(data.BaseName);
        using var context = new ApplicationContext();
        using var timer = new System.Windows.Forms.Timer { Interval = 10 };
        var elapsed = Stopwatch.StartNew();
        var observed = new List<string>();
        bool overlapping = false;
        bool ownerMatches = true;
        bool topMost = true;
        bool tracking = true;
        bool launcherVisible = true;
        try
        {
            host.Show();
            var launcher = host.OwnedForms.OfType<CommandLauncherForm>().Single();
            launcher.ShowWindow();
            timer.Tick += (_, _) =>
            {
                var notifications = Application.OpenForms.OfType<NotificationForm>().ToArray();
                overlapping |= notifications.Length > 1;
                foreach (var notification in notifications)
                {
                    tracking &= host.HasActiveNotifications;
                    ownerMatches &= ReferenceEquals(notification.Owner, launcher);
                    topMost &= notification.TopMost;
                    host.ShowHide();
                    launcherVisible &= launcher.Visible;
                    observed.Add(notification.Controls.Find("labelMessage", true).Single().Text);
                    notification.DialogResult = DialogResult.OK;
                    notification.Close();
                }
                if (observed.Count >= 2 && !host.HasActiveNotifications)
                    context.ExitThread();
                if (elapsed.Elapsed > TimeSpan.FromSeconds(120))
                    context.ExitThread();
            };
            timer.Start();
            Application.Run(context);
            observed.Should().Equal("先の通知", "後の通知");
            overlapping.Should().BeFalse();
            ownerMatches.Should().BeTrue();
            topMost.Should().BeTrue();
            tracking.Should().BeTrue();
            launcherVisible.Should().BeTrue();
            host.HasActiveNotifications.Should().BeFalse();
        }
        finally
        {
            host.Close();
        }
    });
}
