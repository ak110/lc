using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Launcher.Tests;

public sealed class ReleaseWorkflowTests
{
    [Theory]
    [InlineData("PATCH", "1.2.4")]
    [InlineData("MINOR", "1.3.0")]
    [InlineData("MAJOR", "2.0.0")]
    public void リリース準備_新規実行で指定した版だけを更新する(string bump, string expected)
    {
        using var env = new ReleaseEnvironment();
        string source = env.Git("rev-parse", "HEAD");
        var result = env.Prepare(bump);
        result["version"].Should().Be(expected);
        result["rerun"].Should().Be("false");
        result["ci_commit"].Should().Be(source);
        env.Version.Should().Be(expected);
        env.Git("tag").Should().BeEmpty();
    }

    [Fact]
    public void リリース準備_元の起動コミットとタグの双方から同じ版を再開する()
    {
        using var env = new ReleaseEnvironment();
        string source = env.Git("rev-parse", "HEAD");
        env.Prepare("PATCH");
        env.Git("add", "Directory.Build.props");
        env.Git("commit", "-m", "release: v1.2.4");
        env.Git("tag", "v1.2.4");
        string release = env.Git("rev-parse", "HEAD");
        env.Git("checkout", "--detach", source);

        foreach (int attempt in new[] { 1, 2 })
        {
            var result = env.Prepare("PATCH");
            result["version"].Should().Be("1.2.4", "再開{0}では版数を増やさない", attempt);
            result["rerun"].Should().Be("true");
            result["ci_commit"].Should().Be(source);
            env.Git("rev-parse", "HEAD").Should().Be(release);
            env.Version.Should().Be("1.2.4", "配布物をビルドする作業ツリーもタグの版にする");
            env.Git("status", "--porcelain").Should().BeEmpty();
        }
    }

    [Fact]
    public void リリース準備_他の履歴の同名タグを流用しない()
    {
        using var env = new ReleaseEnvironment();
        string source = env.Git("rev-parse", "HEAD");
        env.Git("commit", "--allow-empty", "-m", "別の変更");
        env.Prepare("PATCH");
        env.Git("add", "Directory.Build.props");
        env.Git("commit", "-m", "release: v1.2.4");
        env.Git("tag", "v1.2.4");
        string release = env.Git("rev-parse", "HEAD");
        env.Git("checkout", "--detach", source);

        var result = env.RunPrepare("PATCH");
        result.ExitCode.Should().NotBe(0);
        result.Error.Should().Contain("別の変更");
        env.Git("rev-parse", "v1.2.4").Should().Be(release);
        env.Git("rev-parse", "HEAD").Should().Be(source);
        env.Version.Should().Be("1.2.3");
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public void リリース公開_新規と既存の双方で配布ZIPの公開を完了する(bool exists, bool draft, bool hasAsset)
    {
        using var env = new ReleaseEnvironment();
        var result = env.Publish(exists, draft, hasAsset, failUpload: false);
        result.ExitCode.Should().Be(0, "{0}", result.Error);
        using var state = JsonDocument.Parse(File.ReadAllText(env.PublishResultPath));
        state.RootElement.GetProperty("isDraft").GetBoolean().Should().BeFalse();
        state.RootElement.GetProperty("assets").EnumerateArray()
            .Select(asset => asset.GetProperty("name").GetString()).Should().Contain("lc-v1.2.4.zip");
    }

    [Fact]
    public void リリース公開_ZIPを追加できなければ成功として扱わない()
    {
        using var env = new ReleaseEnvironment();
        env.Publish(exists: true, draft: false, hasAsset: false, failUpload: true).ExitCode.Should().NotBe(0);
        File.Exists(env.PublishResultPath).Should().BeFalse();
    }

    private sealed class ReleaseEnvironment : IDisposable
    {
        readonly string root = Path.Combine(Path.GetTempPath(), "lc_release_test_" + Guid.NewGuid().ToString("N"));
        readonly string workflow;

        public ReleaseEnvironment()
        {
            Directory.CreateDirectory(root);
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Launcher.sln")))
                directory = directory.Parent;
            workflow = File.ReadAllText(Path.Combine(directory!.FullName, ".github", "workflows", "release.yaml"));
            Git("init", "--quiet");
            Git("config", "user.name", "Release Test");
            Git("config", "user.email", "release-test@example.invalid");
            Git("config", "commit.gpgsign", "false");
            Git("config", "core.hooksPath", Path.Combine(root, "no-hooks"));
            File.WriteAllText(Path.Combine(root, ".gitignore"), "*.ps1\ngithub-output.txt\npublished.json\n");
            File.WriteAllText(Path.Combine(root, "Directory.Build.props"), "<Project><PropertyGroup><Version>1.2.3</Version></PropertyGroup></Project>\n");
            Git("add", ".gitignore");
            Git("add", "Directory.Build.props");
            Git("commit", "--quiet", "-m", "初期状態");
        }

        public string Version => System.Xml.Linq.XDocument.Load(Path.Combine(root, "Directory.Build.props"))
            .Descendants("Version").Single().Value;

        public string PublishResultPath => Path.Combine(root, "published.json");

        public string Git(params string[] arguments)
        {
            var result = Run("git", arguments);
            result.ExitCode.Should().Be(0, "git {0}: {1}", string.Join(' ', arguments), result.Error);
            return result.Output.Trim();
        }

        public Dictionary<string, string> Prepare(string bump)
        {
            var result = RunPrepare(bump);
            result.ExitCode.Should().Be(0, "{0}", result.Error);
            return File.ReadAllLines(Path.Combine(root, "github-output.txt"))
                .Select(line => line.Split('=', 2)).ToDictionary(parts => parts[0], parts => parts[1]);
        }

        public ProcessResult RunPrepare(string bump)
        {
            string script = Path.Combine(root, "prepare.ps1");
            string outputPath = Path.Combine(root, "github-output.txt");
            File.WriteAllText(script, "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8\n"
                + StepScript("Prepare release"), new UTF8Encoding(true));
            File.WriteAllText(outputPath, string.Empty);
            return Run("powershell.exe", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script], new Dictionary<string, string>
            {
                ["RELEASE_BUMP"] = bump,
                ["GITHUB_OUTPUT"] = outputPath,
            });
        }

        public ProcessResult Publish(bool exists, bool draft, bool hasAsset, bool failUpload)
        {
            string script = Path.Combine(root, "publish.ps1");
            string prefix = $$"""
                $script:exists = ${{exists.ToString().ToLowerInvariant()}}
                $script:failUpload = ${{failUpload.ToString().ToLowerInvariant()}}
                $script:releaseState = [pscustomobject]@{ isDraft = ${{draft.ToString().ToLowerInvariant()}}; assets = @() }
                if (${{hasAsset.ToString().ToLowerInvariant()}}) { $script:releaseState.assets = @([pscustomobject]@{ name = 'lc-v1.2.4.zip' }) }
                function gh {
                    param([Parameter(ValueFromRemainingArguments)][string[]]$arguments)
                    $global:LASTEXITCODE = 0
                    switch ($arguments[1]) {
                        'view' {
                            if (-not $script:exists) { $global:LASTEXITCODE = 1; return }
                            $script:releaseState | ConvertTo-Json -Depth 5
                        }
                        'create' {
                            $script:exists = $true
                            $script:releaseState.isDraft = $false
                            $script:releaseState.assets = @([pscustomobject]@{ name = $arguments[3] })
                        }
                        'upload' {
                            if ($script:failUpload) { $global:LASTEXITCODE = 1; return }
                            $script:releaseState.assets += [pscustomobject]@{ name = $arguments[3] }
                        }
                        'edit' { $script:releaseState.isDraft = $false }
                        default { throw '未対応のGitHub CLI操作' }
                    }
                }
                """;
            string body = StepScript("Create GitHub Release").Replace("${{ steps.version.outputs.version }}", "1.2.4", StringComparison.Ordinal);
            File.WriteAllText(script, prefix + Environment.NewLine + body + Environment.NewLine
                + "$script:releaseState | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 -LiteralPath 'published.json'", new UTF8Encoding(true));
            return Run("powershell.exe", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script]);
        }

        string StepScript(string name)
        {
            // ワークフローが実際に実行するPowerShellをローカルGitとGitHub CLIの代替応答で検証する。
            var lines = workflow.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            int start = Array.FindIndex(lines, line => line == "      - name: " + name);
            start.Should().BeGreaterThanOrEqualTo(0);
            int run = Array.FindIndex(lines, start + 1, line => line == "        run: |");
            run.Should().BeGreaterThan(start);
            return string.Join(Environment.NewLine, lines.Skip(run + 1)
                .TakeWhile(line => line.Length == 0 || line.StartsWith("          ", StringComparison.Ordinal))
                .Select(line => line.Length == 0 ? string.Empty : line[10..]));
        }

        ProcessResult Run(string executable, string[] arguments, Dictionary<string, string>? environment = null)
        {
            var info = new ProcessStartInfo(executable)
            {
                WorkingDirectory = root,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            foreach (string argument in arguments) info.ArgumentList.Add(argument);
            if (environment is not null)
                foreach (var (key, value) in environment) info.Environment[key] = value;
            using var process = Process.Start(info)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30_000))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
                throw new TimeoutException("リリース検証が時間内に終了しなかった");
            }
            return new ProcessResult(process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
        }

        public void Dispose()
        {
            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(root, true);
        }
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
