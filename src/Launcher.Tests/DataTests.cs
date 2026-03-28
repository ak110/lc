using FluentAssertions;
using Launcher.Core;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// Data のシリアライズ/デシリアライズ ラウンドトリップテスト
/// </summary>
public sealed class DataTests
{
    [Fact]
    public void ラウンドトリップ_デフォルト値が保持される()
    {
        var original = new Data();

        var xml = original.SerializeToString();
        var restored = ConfigStore.DeserializeFromString<Data>(xml);

        restored.WindowHandle.Should().Be(0);
        restored.SchedulerLastCheckTime.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void ラウンドトリップ_全プロパティが保持される()
    {
        var original = new Data
        {
            WindowHandle = 12345,
            SchedulerLastCheckTime = new DateTime(2025, 6, 15, 12, 0, 0),
        };

        var xml = original.SerializeToString();
        var restored = ConfigStore.DeserializeFromString<Data>(xml);

        restored.WindowHandle.Should().Be(12345);
        restored.SchedulerLastCheckTime.Should().Be(new DateTime(2025, 6, 15, 12, 0, 0));
    }
}
