using FluentAssertions;
using Launcher.Core;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

public sealed class ConfigSaveTests : IDisposable
{
    readonly string directory = Path.Combine(Path.GetTempPath(), "lc_save_" + Guid.NewGuid().ToString("N"));

    public ConfigSaveTests() => Directory.CreateDirectory(directory);

    public void Dispose() => Directory.Delete(directory, true);

    [Fact]
    public void 六種類とも書込失敗は成功まで一回だけ通知する()
    {
        CheckWriteFailure(new ConfigFile<Config>(".cfg"));
        CheckWriteFailure(new ConfigFile<CommandList>(".cmd.cfg"));
        CheckWriteFailure(new ConfigFile<ButtonLauncherData>(".btns.cfg"));
        CheckWriteFailure(new ConfigFile<MemoData>(".memo.cfg"));
        CheckWriteFailure(new ConfigFile<SchedulerData>(".sch.cfg"));
        CheckWriteFailure(new ConfigFile<Data>(".dat", protectOriginal: false));
    }

    void CheckWriteFailure<T>(ConfigFile<T> store) where T : ConfigStore, new()
    {
        string baseName = Path.Combine(directory, "data");
        string path = store.FileName(baseName);
        List<ConfigSaveFailure> failures = [];
        store.Load(baseName);
        Directory.CreateDirectory(path + ".tmp");
        store.Save(new T(), failures.Add, baseName).Should().BeFalse();
        store.Save(new T(), failures.Add, baseName).Should().BeFalse();
        failures.Should().ContainSingle().Which.Kind.Should().Be(ConfigSaveFailureKind.Write);
        Directory.Delete(path + ".tmp");
        store.Save(new T(), failures.Add, baseName).Should().BeTrue();
        Directory.CreateDirectory(path + ".tmp");
        store.Save(new T(), failures.Add, baseName).Should().BeFalse();
        failures.Should().HaveCount(2);
        Directory.Delete(path + ".tmp");
    }

    [Fact]
    public void 保存停止と書込失敗を別々に通知して読込状態を窓口ごとに持つ()
    {
        string baseName = Path.Combine(directory, "data");
        var store = new ConfigFile<Config>(".cfg");
        string path = store.FileName(baseName);
        File.WriteAllText(path, "<Config>");
        store.Load(baseName).Status.Should().Be(ConfigLoadStatus.Failed);
        List<ConfigSaveFailure> failures = [];
        store.Save(new Config(), failures.Add, baseName).Should().BeFalse();
        store.Save(new Config(), failures.Add, baseName).Should().BeFalse();
        failures.Should().ContainSingle().Which.Kind.Should().Be(ConfigSaveFailureKind.Blocked);

        File.Delete(path);
        var independent = new ConfigFile<Config>(".cfg");
        independent.Save(new Config(), failures.Add, baseName).Should().BeTrue();
        store.Save(new Config(), failures.Add, baseName).Should().BeFalse();
        failures.Should().ContainSingle();

        store.Load(baseName).Status.Should().Be(ConfigLoadStatus.Loaded);
        Directory.CreateDirectory(path + ".tmp");
        store.Save(new Config(), failures.Add, baseName).Should().BeFalse();
        failures.Select(f => f.Kind).Should().Equal(ConfigSaveFailureKind.Blocked, ConfigSaveFailureKind.Write);
    }

    [Fact]
    public void 実行状態は破損読込後も保存できバックアップを作らない()
    {
        string baseName = Path.Combine(directory, "data");
        var store = new ConfigFile<Data>(".dat", protectOriginal: false);
        string path = store.FileName(baseName);
        File.WriteAllText(path, "<Data>");
        store.Load(baseName).Status.Should().Be(ConfigLoadStatus.Failed);
        store.Save(new Data { WindowHandle = 42 }, _ => Assert.Fail("実行状態を保存できない"), baseName)
            .Should().BeTrue();
        store.Load(baseName).Value.WindowHandle.Should().Be(42);
        File.Exists(path + ".bak").Should().BeFalse();
    }

    [Fact]
    public void 送る登録の保存失敗を成功として返さない()
    {
        string baseName = Path.Combine(directory, "data");
        Directory.CreateDirectory(CommandList.Store.FileName(baseName) + ".tmp");
        List<ConfigSaveFailure> failures = [];
        CommandList.AddAndSave(new Command { Name = "追加", FileName = "a.exe" }, failures.Add,
            _ => Assert.Fail("初回読込は成功する"), baseName).Should().BeFalse();
        failures.Should().ContainSingle().Which.Kind.Should().Be(ConfigSaveFailureKind.Write);
        File.Exists(CommandList.Store.FileName(baseName)).Should().BeFalse();
    }
}
