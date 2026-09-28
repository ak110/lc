using FluentAssertions;
using Launcher.Core;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// 保存データの読込失敗時の保存停止・バックアップ・復元を、隔離した保存先で検証する。
/// </summary>
public sealed class ConfigRecoveryTests : IDisposable
{
    const string Broken = "<?xml version=\"1.0\"?><CommandList><Commands><Command>";

    readonly string dir;
    readonly string baseName;

    public ConfigRecoveryTests()
    {
        dir = Path.Combine(Path.GetTempPath(), "lc_cfg_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        baseName = Path.Combine(dir, "らんちゃ");
    }

    public void Dispose()
    {
        try { Directory.Delete(dir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    static CommandList Commands(params string[] names)
    {
        var list = new CommandList();
        foreach (var name in names)
        {
            list.Add(new Command { Name = name, FileName = name + ".exe" });
        }
        return list;
    }

    static IEnumerable<string> Names(CommandList list) => list.Commands.Select(c => c.Name);

    string CommandFile => CommandList.Store(".cmd.cfg").FileName(baseName);

    [Fact]
    public void 読込失敗したファイルへは保存せず原本を変えない()
    {
        File.WriteAllText(CommandFile, Broken);
        byte[] original = File.ReadAllBytes(CommandFile);

        var result = CommandList.Load(".cmd.cfg", baseName);

        result.Status.Should().Be(ConfigLoadStatus.Failed);
        result.Value.Commands.Should().BeEmpty();
        var save = () => Commands("new").SerializeTo(".cmd.cfg", baseName);
        save.Should().Throw<ConfigSaveBlockedException>().Which.Message.Should().NotContain(dir);
        File.ReadAllBytes(CommandFile).Should().Equal(original);
    }

    [Fact]
    public void 復元で原本を保全してバックアップの内容へ戻し保存を再開する()
    {
        Commands("a", "b").SerializeTo(".cmd.cfg", baseName);
        CommandList.Load(".cmd.cfg", baseName).Status.Should().Be(ConfigLoadStatus.Loaded);
        File.WriteAllText(CommandFile, Broken);

        var failed = CommandList.Load(".cmd.cfg", baseName);
        failed.Status.Should().Be(ConfigLoadStatus.Failed);
        failed.BackupAvailable.Should().BeTrue();

        var restored = CommandList.Store(".cmd.cfg").RestoreFromBackup(baseName);
        Names(restored).Should().Equal("a", "b");
        var brokenFiles = Directory.GetFiles(dir, "らんちゃ.cmd.cfg.broken-*");
        brokenFiles.Should().ContainSingle();
        File.ReadAllText(brokenFiles[0]).Should().Be(Broken);

        // 復元後は編集・保存・再読込ができる
        restored.Add(new Command { Name = "c", FileName = "c.exe" });
        restored.SerializeTo(".cmd.cfg", baseName);
        var reloaded = CommandList.Load(".cmd.cfg", baseName);
        reloaded.Status.Should().Be(ConfigLoadStatus.Loaded);
        Names(reloaded.Value).Should().Equal("a", "b", "c");
    }

    [Fact]
    public void 保存時に直前の正常な内容をバックアップへ写す()
    {
        Commands("a").SerializeTo(".cmd.cfg", baseName);
        var loaded = CommandList.Load(".cmd.cfg", baseName).Value;
        loaded.Add(new Command { Name = "b", FileName = "b.exe" });
        loaded.SerializeTo(".cmd.cfg", baseName);
        loaded.Add(new Command { Name = "c", FileName = "c.exe" });
        loaded.SerializeTo(".cmd.cfg", baseName);

        var backup = ConfigStore.DeserializeFromFile<CommandList>(ConfigFileState.BackupName(CommandFile));
        Names(backup).Should().Equal("a", "b");
        Names(CommandList.Load(".cmd.cfg", baseName).Value).Should().Equal("a", "b", "c");
    }

    [Fact]
    public void 外部で壊れた本体はバックアップへ写さず別名で保全する()
    {
        Commands("a").SerializeTo(".cmd.cfg", baseName);
        var loaded = CommandList.Load(".cmd.cfg", baseName).Value;
        File.WriteAllText(CommandFile, Broken);

        loaded.Add(new Command { Name = "b", FileName = "b.exe" });
        loaded.SerializeTo(".cmd.cfg", baseName);

        var backup = ConfigStore.DeserializeFromFile<CommandList>(ConfigFileState.BackupName(CommandFile));
        Names(backup).Should().Equal("a");
        var brokenFiles = Directory.GetFiles(dir, "らんちゃ.cmd.cfg.broken-*");
        brokenFiles.Should().ContainSingle();
        File.ReadAllText(brokenFiles[0]).Should().Be(Broken);
        Names(CommandList.Load(".cmd.cfg", baseName).Value).Should().Equal("a", "b");
    }

    [Fact]
    public void バックアップを作成できない場合は本体を置換しない()
    {
        Commands("a").SerializeTo(".cmd.cfg", baseName);
        var loaded = CommandList.Load(".cmd.cfg", baseName).Value;
        byte[] original = File.ReadAllBytes(CommandFile);
        // バックアップの位置をフォルダーでふさぎ、作成を失敗させる
        File.Delete(ConfigFileState.BackupName(CommandFile));
        Directory.CreateDirectory(ConfigFileState.BackupName(CommandFile));

        loaded.Add(new Command { Name = "b", FileName = "b.exe" });
        var save = () => loaded.SerializeTo(".cmd.cfg", baseName);

        save.Should().Throw<Exception>().Which.Should().Match(e => e is IOException || e is UnauthorizedAccessException);
        File.ReadAllBytes(CommandFile).Should().Equal(original);
    }

    [Fact]
    public void 本体もバックアップも無い初回は初期値で保存できる()
    {
        var result = CommandList.Load(".cmd.cfg", baseName);

        result.Status.Should().Be(ConfigLoadStatus.NotFound);
        result.Value.Commands.Should().BeEmpty();
        result.Value.Add(new Command { Name = "a", FileName = "a.exe" });
        result.Value.SerializeTo(".cmd.cfg", baseName);
        Names(CommandList.Load(".cmd.cfg", baseName).Value).Should().Equal("a");
    }

    [Fact]
    public void バックアップだけがある場合は読込失敗として扱う()
    {
        Commands("a").SerializeTo(".cmd.cfg", baseName);
        CommandList.Load(".cmd.cfg", baseName);
        File.Delete(CommandFile);

        var result = CommandList.Load(".cmd.cfg", baseName);

        result.Status.Should().Be(ConfigLoadStatus.Failed);
        result.BackupAvailable.Should().BeTrue();
        var save = () => Commands("x").SerializeTo(".cmd.cfg", baseName);
        save.Should().Throw<ConfigSaveBlockedException>();
        Names(CommandList.Store(".cmd.cfg").RestoreFromBackup(baseName)).Should().Equal("a");
    }

    [Fact]
    public void 破損したXMLを旧形式の空データとして扱わない()
    {
        File.WriteAllText(CommandFile, Broken);
        CommandList.Load(".cmd.cfg", baseName).Status.Should().Be(ConfigLoadStatus.Failed);

        // 旧形式の行（キー = 値）に見える部分を含む破損XMLも旧形式として受理しない
        File.WriteAllText(CommandFile, "<?xml version=\"1.0\"?>\r\n<CommandList>\r\n  <Commands>\r\n    <Command>x = a.exe\\n\\n\\n0\\n3\r\n");
        CommandList.Load(".cmd.cfg", baseName).Status.Should().Be(ConfigLoadStatus.Failed);

        string configFile = Config.Store.FileName(baseName);
        File.WriteAllText(configFile, "<?xml version=\"1.0\"?><Config><TrayIcon>");
        Config.Load(baseName).Status.Should().Be(ConfigLoadStatus.Failed);
    }

    [Fact]
    public void 旧形式の設定は読込に成功する()
    {
        File.WriteAllText(CommandFile, "mycmd = test.exe\\n-param\\n\\n0\\n3\r\n");

        var result = CommandList.Load(".cmd.cfg", baseName);

        result.Status.Should().Be(ConfigLoadStatus.Loaded);
        var command = result.Value.Commands.Should().ContainSingle().Subject;
        command.Name.Should().Be("mycmd");
        command.FileName.Should().Be("test.exe");
        command.Param.Should().Be("-param");
    }

    [Fact]
    public void 各データ型で読込失敗から復元まで成立する()
    {
        var config = new Config { TrayIcon = false };
        AssertRecovery(Config.Store, config, c => c.TrayIcon.ToString());
        AssertRecovery(CommandList.Store(".cmd.cfg"), Commands("a"), c => string.Join(",", Names(c)));
        AssertRecovery(ButtonLauncherData.Store, new ButtonLauncherData { Columns = 3 }, b => b.Columns.ToString());
        AssertRecovery(SchedulerData.Store, new SchedulerData { Items = [new SchedulerItem { Name = "s" }] },
            s => string.Join(",", s.Items.Select(i => i.Name)));
        AssertRecovery(MemoData.Store, new MemoData { Tabs = [new MemoTab { Name = "t", Text = "本文" }] },
            m => string.Join(",", m.Tabs.Select(t => t.Name + ":" + t.Text)));
    }

    void AssertRecovery<T>(ConfigFile<T> store, T sample, Func<T, string> probe)
        where T : ConfigStore, new()
    {
        string fileName = store.FileName(baseName);
        sample.SerializeToFile(fileName);
        store.Load(baseName).Status.Should().Be(ConfigLoadStatus.Loaded, typeof(T).Name);
        File.WriteAllText(fileName, "<?xml version=\"1.0\"?><Broken>");
        byte[] broken = File.ReadAllBytes(fileName);

        var failed = store.Load(baseName);
        failed.Status.Should().Be(ConfigLoadStatus.Failed, typeof(T).Name);
        var save = () => new T().SerializeToFile(fileName);
        save.Should().Throw<ConfigSaveBlockedException>(typeof(T).Name);
        File.ReadAllBytes(fileName).Should().Equal(broken, typeof(T).Name);

        var restored = store.RestoreFromBackup(baseName);
        probe(restored).Should().Be(probe(sample), typeof(T).Name);
        restored.SerializeToFile(fileName);
        probe(store.Load(baseName).Value).Should().Be(probe(sample), typeof(T).Name);
    }

    [Fact]
    public void コマンド一覧を読めない場合は送るからの追加をしない()
    {
        File.WriteAllText(CommandFile, Broken);
        byte[] original = File.ReadAllBytes(CommandFile);

        var result = CommandList.AddAndSave(".cmd.cfg", new Command { Name = "added", FileName = "added.exe" }, baseName);

        result.Status.Should().Be(ConfigLoadStatus.Failed);
        File.ReadAllBytes(CommandFile).Should().Equal(original);
    }

    [Fact]
    public void 送るからの追加は既存のコマンドを保つ()
    {
        Commands("a").SerializeTo(".cmd.cfg", baseName);

        var result = CommandList.AddAndSave(".cmd.cfg", new Command { Name = "b", FileName = "b.exe" }, baseName);

        result.Status.Should().Be(ConfigLoadStatus.Loaded);
        Names(CommandList.Load(".cmd.cfg", baseName).Value).Should().Equal("a", "b");
    }

    [Fact]
    public void 読込失敗したメモへ自動保存しない()
    {
        string memoFile = MemoData.Store.FileName(baseName);
        File.WriteAllText(memoFile, "<?xml version=\"1.0\"?><MemoData><Tabs>");
        byte[] original = File.ReadAllBytes(memoFile);

        var result = MemoData.Load(baseName);
        result.Status.Should().Be(ConfigLoadStatus.Failed);
        result.Value.Tabs.Add(new MemoTab { Name = "t", Text = "入力" });
        var save = () => result.Value.SerializeToFile(memoFile);

        // MemoFormの自動保存はIOExceptionを記録して続行する。保存停止もIOExceptionとして届く
        save.Should().Throw<IOException>();
        File.ReadAllBytes(memoFile).Should().Equal(original);
    }
}
