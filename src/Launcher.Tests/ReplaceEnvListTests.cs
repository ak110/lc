using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

public sealed class ReplaceEnvListTests
{
    [Fact]
    public void コンストラクタ_既知の環境変数が収集される()
    {
        var envList = new ReplaceEnvList(["SystemRoot"]);

        string systemRoot = Environment.GetEnvironmentVariable("SystemRoot")!;
        var cmd = new Command
        {
            FileName = systemRoot,
            WorkDir = null,
        };

        envList.Replace(cmd);

        cmd.FileName.Should().Be("%SystemRoot%");
    }

    [Fact]
    public void コンストラクタ_存在しない環境変数は無視される()
    {
        var envList = new ReplaceEnvList(["THIS_ENV_VAR_SHOULD_NOT_EXIST_12345"]);

        var cmd = new Command
        {
            FileName = @"C:\dummy\path",
            WorkDir = null,
        };

        envList.Replace(cmd);

        cmd.FileName.Should().Be(@"C:\dummy\path");
    }

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

    [Fact]
    public void Replace_実在するディレクトリパスが環境変数に置換される()
    {
        string systemRoot = Environment.GetEnvironmentVariable("SystemRoot")!;
        string system32 = Path.Combine(systemRoot, "System32");
        if (!Directory.Exists(system32))
        {
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

        cmd.FileName.Should().Be(@"Z:\nonexistent");
        cmd.WorkDir.Should().Be("%SystemRoot%");
    }

    [Fact]
    public void Replace_CommandListの全コマンドが処理される()
    {
        string systemRoot = Environment.GetEnvironmentVariable("SystemRoot")!;

        var envList = new ReplaceEnvList(["SystemRoot"]);
        var list = new CommandList();
        list.Commands.Add(new Command { Name = "cmd1", FileName = systemRoot });
        list.Commands.Add(new Command { Name = "cmd2", FileName = @"Z:\nonexistent" });

        var scheduler = new SchedulerData();
        var batch = EnvironmentReplacementBatch.Capture(list, scheduler);
        batch.Apply(list, scheduler, envList.Calculate(batch.Values)).Should().BeFalse();

        list.Commands[0].FileName.Should().Be("%SystemRoot%");
        list.Commands[1].FileName.Should().Be(@"Z:\nonexistent");
    }

    [Fact]
    public void コンストラクタ_空リストでも例外が出ない()
    {
        var envList = new ReplaceEnvList([]);

        var cmd = new Command { FileName = @"C:\test" };
        envList.Replace(cmd);

        cmd.FileName.Should().Be(@"C:\test");
    }

    [Theory]
    [InlineData(@"C:\WINDOWS", @"c:\windows", "%LC_TEST_REPLACE_PREFIX%")]
    [InlineData(@".\", @".\.\", @"%LC_TEST_REPLACE_PREFIX%.\")]
    public void Replace_一致した先頭だけを置換する(string prefix, string path, string expected)
    {
        const string name = "LC_TEST_REPLACE_PREFIX";
        var previous = Environment.GetEnvironmentVariable(name);
        try
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            Environment.SetEnvironmentVariable(name, prefix == @"C:\WINDOWS" ? root.ToUpperInvariant() : prefix);
            var cmd = new Command { FileName = path == @"c:\windows" ? root.ToLowerInvariant() : path };
            new ReplaceEnvList([name]).Replace(cmd);

            cmd.FileName.Should().Be(expected);
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, previous);
        }
    }
}
