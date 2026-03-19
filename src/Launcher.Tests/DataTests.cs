using FluentAssertions;
using Launcher.Core;
using Launcher.Infrastructure;
using Launcher.Updater;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// Data のシリアライズ/デシリアライズ ラウンドトリップテスト
/// </summary>
public class DataTests
{
    [Fact]
    public void ラウンドトリップ_デフォルト値が保持される()
    {
        var original = new Data();

        var xml = original.SerializeToString();
        var restored = ConfigStore.DeserializeFromString<Data>(xml);

        restored.WindowHandle.Should().Be(0);
        restored.UpdateRecord.Should().NotBeNull();
        restored.UpdateRecord.LastChecked.Should().Be(original.UpdateRecord.LastChecked);
        restored.UpdateRecord.LastKnownVersion.Should().Be(original.UpdateRecord.LastKnownVersion);
        restored.UpdateRecord.SkippedVersion.Should().Be(original.UpdateRecord.SkippedVersion);
    }

    [Fact]
    public void ラウンドトリップ_全プロパティが保持される()
    {
        var original = new Data
        {
            WindowHandle = 12345,
            UpdateRecord = new UpdateRecord
            {
                LastChecked = new DateTime(2025, 1, 15, 10, 30, 0),
                LastKnownVersion = "v2.0.0",
                SkippedVersion = "v1.9.0",
            },
        };

        var xml = original.SerializeToString();
        var restored = ConfigStore.DeserializeFromString<Data>(xml);

        restored.WindowHandle.Should().Be(12345);
        restored.UpdateRecord.LastChecked.Should().Be(new DateTime(2025, 1, 15, 10, 30, 0));
        restored.UpdateRecord.LastKnownVersion.Should().Be("v2.0.0");
        restored.UpdateRecord.SkippedVersion.Should().Be("v1.9.0");
    }
}
