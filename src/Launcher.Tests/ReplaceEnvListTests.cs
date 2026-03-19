using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// ReplaceEnvListのテスト。
/// ReplaceEnvListは環境変数のパス値↔%変数名%を相互置換する。
/// ただしInnerReplaceがFile.Exists/Directory.Existsチェックを行うため、
/// 実在しないパスは置換されない。実在パスを使ったテストも含む。
/// </summary>
public class ReplaceEnvListTests
{
    // --- コンストラクタ: 環境変数が正しく収集される ---

    [Fact]
    public void コンストラクタ_既知の環境変数が収集される()
    {
        // SystemRootは通常 C:\Windows のような値を持つ
        var envList = new ReplaceEnvList(["SystemRoot"]);

        // Replace(Command)を呼んで、実在するパスが置換されることを確認
        string systemRoot = Environment.GetEnvironmentVariable("SystemRoot")!;
        var cmd = new Command
        {
            FileName = systemRoot,
            WorkDir = null,
        };

        envList.Replace(cmd);

        // SystemRootは実在するディレクトリなので置換される
        cmd.FileName.Should().Be("%SystemRoot%");
    }

    [Fact]
    public void コンストラクタ_存在しない環境変数は無視される()
    {
        // 存在しない環境変数名を指定しても例外が出ない
        var envList = new ReplaceEnvList(["THIS_ENV_VAR_SHOULD_NOT_EXIST_12345"]);

        var cmd = new Command
        {
            FileName = @"C:\dummy\path",
            WorkDir = null,
        };

        envList.Replace(cmd);

        // 存在しない環境変数なので置換は起きない
        cmd.FileName.Should().Be(@"C:\dummy\path");
    }

    // --- Replace: 実在しないパスは置換されない ---

    [Fact]
    public void Replace_実在しないパスは置換されない()
    {
        var envList = new ReplaceEnvList(["SystemRoot"]);

        var cmd = new Command
        {
            FileName = @"Z:\nonexistent\path\that\does\not\exist",
            WorkDir = null,
        };

        envList.Replace(cmd);

        cmd.FileName.Should().Be(@"Z:\nonexistent\path\that\does\not\exist");
    }

    // --- Replace: 実在するパスが正しく置換される ---

    [Fact]
    public void Replace_実在するディレクトリパスが環境変数に置換される()
    {
        string systemRoot = Environment.GetEnvironmentVariable("SystemRoot")!;
        // SystemRoot配下の実在するディレクトリ
        string system32 = Path.Combine(systemRoot, "System32");
        if (!Directory.Exists(system32))
        {
            // テスト環境にSystem32がない場合はスキップ
            return;
        }

        var envList = new ReplaceEnvList(["SystemRoot"]);
        var cmd = new Command
        {
            FileName = system32,
            WorkDir = null,
        };

        envList.Replace(cmd);

        cmd.FileName.Should().Be(@"%SystemRoot%\System32");
    }

    // --- Replace: WorkDirも置換される ---

    [Fact]
    public void Replace_WorkDirも置換される()
    {
        string systemRoot = Environment.GetEnvironmentVariable("SystemRoot")!;

        var envList = new ReplaceEnvList(["SystemRoot"]);
        var cmd = new Command
        {
            FileName = @"Z:\nonexistent",
            WorkDir = systemRoot,
        };

        envList.Replace(cmd);

        // FileNameは実在しないので置換されない
        cmd.FileName.Should().Be(@"Z:\nonexistent");
        // WorkDirは実在するので置換される
        cmd.WorkDir.Should().Be("%SystemRoot%");
    }

    // --- Replace(CommandList): 複数コマンドの一括置換 ---

    [Fact]
    public void Replace_CommandListの全コマンドが処理される()
    {
        string systemRoot = Environment.GetEnvironmentVariable("SystemRoot")!;

        var envList = new ReplaceEnvList(["SystemRoot"]);
        var list = new CommandList();
        list.Commands.Add(new Command { Name = "cmd1", FileName = systemRoot });
        list.Commands.Add(new Command { Name = "cmd2", FileName = @"Z:\nonexistent" });

        envList.Replace(list);

        list.Commands[0].FileName.Should().Be("%SystemRoot%");
        list.Commands[1].FileName.Should().Be(@"Z:\nonexistent");
    }

    // --- 空リスト ---

    [Fact]
    public void コンストラクタ_空リストでも例外が出ない()
    {
        var envList = new ReplaceEnvList([]);

        var cmd = new Command { FileName = @"C:\test" };
        envList.Replace(cmd);

        cmd.FileName.Should().Be(@"C:\test");
    }
}
