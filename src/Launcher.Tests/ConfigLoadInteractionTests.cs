using FluentAssertions;
using Launcher.Core;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

public sealed class ConfigLoadInteractionTests : IDisposable
{
    readonly string directory = Path.Combine(Path.GetTempPath(), "lc_load_" + Guid.NewGuid().ToString("N"));

    public ConfigLoadInteractionTests() => Directory.CreateDirectory(directory);

    public void Dispose() => Directory.Delete(directory, true);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void バックアップ復元の回答に応じて内容と保存停止を選ぶ(bool restore)
    {
        var store = new ConfigFile<Config>(".cfg");
        string baseName = Path.Combine(directory, "data");
        store.Save(new Config { TrayIcon = false }, _ => Assert.Fail("初期保存の失敗"), baseName);
        store.Load(baseName);
        File.WriteAllText(store.FileName(baseName), "<Config>");
        var result = store.Load(baseName);
        var fallback = new Config { TrayIcon = true };
        List<ConfigLoadNotice> notices = [];

        var accepted = ConfigLoadInteraction.Accept(result, store, fallback, notice =>
        {
            notices.Add(notice);
            return restore;
        }, baseName);

        notices.Should().ContainSingle().Which.ConfirmRestore.Should().BeTrue();
        accepted.TrayIcon.Should().Be(!restore);
        store.Save(accepted, _ => { }, baseName).Should().Be(restore);
        if (!restore) accepted.Should().BeSameAs(fallback);
    }

    [Fact]
    public void 復元を選んだ後にバックアップを失った場合は保持中の値を返す()
    {
        var store = new ConfigFile<Config>(".cfg");
        string baseName = Path.Combine(directory, "data");
        store.Save(new Config(), _ => Assert.Fail("初期保存の失敗"), baseName);
        store.Load(baseName);
        File.WriteAllText(store.FileName(baseName), "<Config>");
        var result = store.Load(baseName);
        var fallback = new Config();
        List<ConfigLoadNotice> notices = [];

        ConfigLoadInteraction.Accept(result, store, fallback, notice =>
        {
            notices.Add(notice);
            if (notice.ConfirmRestore) File.Delete(store.FileName(baseName) + ".bak");
            return true;
        }, baseName).Should().BeSameAs(fallback);

        notices.Should().HaveCount(2);
        notices[1].ConfirmRestore.Should().BeFalse();
        store.Save(fallback, _ => { }, baseName).Should().BeFalse();
        File.ReadAllText(store.FileName(baseName)).Should().Be("<Config>");
    }

    [Fact]
    public void datの破損は復元を質問せず初期値で続行する()
    {
        var store = new ConfigFile<Data>(".dat", protectOriginal: false);
        string baseName = Path.Combine(directory, "data");
        File.WriteAllText(store.FileName(baseName), "<Data>");
        var fallback = new Data();
        List<ConfigLoadNotice> notices = [];

        ConfigLoadInteraction.Accept(store.Load(baseName), store, fallback, notice =>
        {
            notices.Add(notice);
            return false;
        }, baseName).Should().BeSameAs(fallback);

        notices.Should().ContainSingle().Which.ConfirmRestore.Should().BeFalse();
        store.Save(fallback, _ => Assert.Fail("datの保存を停止した"), baseName).Should().BeTrue();
    }
}
