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
        env.Git("rev-parse", "HEAD").Should().Be(source);
        env.Version.Should().Be(expected);
        env.Git("tag").Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void リリース準備_起動コミットを保って同じ版を再開する(bool fromTag)
    {
        using var env = new ReleaseEnvironment();
        string source = env.Git("rev-parse", "HEAD");
        string tagCommit = env.CreateReleaseTag();
        string launchCommit = fromTag ? tagCommit : source;
        env.Git("checkout", "--detach", launchCommit);

        var result = env.Prepare("PATCH");
        result["version"].Should().Be("1.2.4");
        result["rerun"].Should().Be("true");
        result["ci_commit"].Should().Be(source);
        env.Git("rev-parse", "HEAD").Should().Be(launchCommit);
        env.Version.Should().Be("1.2.4");
        env.Git("diff", "--name-only").Should().Be(fromTag ? string.Empty : "Directory.Build.props");
    }

    [Fact]
    public void リリース準備_他の履歴の同名タグを流用しない()
    {
        using var env = new ReleaseEnvironment();
        string source = env.Git("rev-parse", "HEAD");
        env.Git("commit", "--allow-empty", "-m", "別の変更");
        string release = env.CreateReleaseTag();
        env.Git("checkout", "--detach", source);

        var result = env.RunPrepare("PATCH");
        result.ExitCode.Should().NotBe(0);
        result.Error.Should().Contain("別の変更");
        env.Git("rev-parse", "v1.2.4^{commit}").Should().Be(release);
        env.Git("rev-parse", "HEAD").Should().Be(source);
        env.Version.Should().Be("1.2.3");
    }

    [Fact]
    public void リリース準備_証明の起動コミットと異なるソースでは停止する()
    {
        using var env = new ReleaseEnvironment();
        string source = env.Git("rev-parse", "HEAD");
        string tagCommit = env.CreateReleaseTag();
        env.Git("checkout", "--detach", source);

        var result = env.RunPrepare("PATCH", launchCommit: tagCommit);
        result.ExitCode.Should().NotBe(0);
        result.Error.Should().Contain("起動コミットと一致しません");
        env.Git("rev-parse", "HEAD").Should().Be(source);
        env.Version.Should().Be("1.2.3");
    }

    [Theory]
    [InlineData(false, false, false, null, "create")]
    [InlineData(true, false, false, null, "upload")]
    [InlineData(true, true, false, null, "upload")]
    [InlineData(true, false, true, "parent", "verify")]
    [InlineData(true, true, true, "tag", "verify")]
    public void リリース公開_新規と既存の双方で配布ZIPの公開を完了する(
        bool exists, bool draft, bool hasAsset, string? signedAt, string expectedCommand)
    {
        using var env = new ReleaseEnvironment();
        var result = env.Publish(exists, draft, hasAsset, signedAt);
        result.ExitCode.Should().Be(0, "{0}", result.Error);
        env.PublishCommands.Should().Contain(expectedCommand);
        if (hasAsset)
        {
            env.PublishCommands.Should().NotContain("upload").And.NotContain("create");
        }
        using var state = JsonDocument.Parse(File.ReadAllText(env.PublishResultPath));
        state.RootElement.GetProperty("isDraft").GetBoolean().Should().BeFalse();
        state.RootElement.GetProperty("assets").EnumerateArray()
            .Select(asset => asset.GetProperty("name").GetString()).Should().Equal("lc-v1.2.4.zip");
        state.RootElement.GetProperty("body").GetString().Should().StartWith(exists ? "既存の変更履歴" : "新規の変更履歴")
            .And.Contain("gh attestation verify lc-v1.2.4.zip --repo ak110/lc --signer-workflow ak110/lc/.github/workflows/release.yaml")
            .And.Contain("https://github.com/ak110/lc/attestations");
    }

    [Fact]
    public void リリース公開_ZIPを追加できなければ成功として扱わない()
    {
        using var env = new ReleaseEnvironment();
        env.Publish(exists: true, draft: true, hasAsset: false, failUpload: true).ExitCode.Should().NotBe(0);
        env.PublishCommands.Should().Contain("upload").And.NotContain("edit");
        using var state = JsonDocument.Parse(File.ReadAllText(env.PublishResultPath));
        state.RootElement.GetProperty("isDraft").GetBoolean().Should().BeTrue();
        state.RootElement.GetProperty("assets").GetArrayLength().Should().Be(0);
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, "unrelated")]
    public void リリース公開_許可されたソースの証明がない既存ZIPは置き換えない(bool draft, string? signedAt)
    {
        using var env = new ReleaseEnvironment();
        var result = env.Publish(exists: true, draft, hasAsset: true, signedAt);
        result.ExitCode.Should().NotBe(0);
        result.Error.Should().Contain("来歴証明を確認できません").And.Contain("証明一覧");
        env.PublishCommands.Should().Contain("download").And.Contain("verify")
            .And.NotContain("create").And.NotContain("upload").And.NotContain("edit");
        using var state = JsonDocument.Parse(File.ReadAllText(env.PublishResultPath));
        state.RootElement.GetProperty("isDraft").GetBoolean().Should().Be(draft);
        state.RootElement.GetProperty("body").GetString().Should().Be("既存の変更履歴");
        state.RootElement.GetProperty("assets").EnumerateArray()
            .Select(asset => asset.GetProperty("name").GetString()).Should().Equal("lc-v1.2.4.zip");
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

        public string[] PublishCommands => File.ReadAllLines(Path.Combine(root, "publish-commands.txt"));

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

        public string CreateReleaseTag()
        {
            string propsPath = Path.Combine(root, "Directory.Build.props");
            File.WriteAllText(propsPath, File.ReadAllText(propsPath)
                .Replace("<Version>1.2.3</Version>", "<Version>1.2.4</Version>", StringComparison.Ordinal));
            Git("add", "Directory.Build.props");
            Git("commit", "-m", "release: v1.2.4");
            Git("tag", "-a", "v1.2.4", "-m", "v1.2.4");
            return Git("rev-parse", "HEAD");
        }

        public ProcessResult RunPrepare(string bump, string? launchCommit = null)
        {
            string outputPath = Path.Combine(root, "github-output.txt");
            File.WriteAllText(outputPath, string.Empty);
            return RunWorkflowScript("prepare.ps1", StepScript("Prepare release"), new Dictionary<string, string>
            {
                ["RELEASE_BUMP"] = bump,
                ["GITHUB_OUTPUT"] = outputPath,
                ["GITHUB_SHA"] = launchCommit ?? Git("rev-parse", "HEAD"),
            });
        }

        public ProcessResult Publish(bool exists, bool draft, bool hasAsset, string? signedAt = null, bool failUpload = false)
        {
            string parent = Git("rev-parse", "HEAD");
            string tagCommit = CreateReleaseTag();
            string signedCommit = signedAt switch
            {
                "parent" => parent,
                "tag" => tagCommit,
                "unrelated" => new string('1', 40),
                null => string.Empty,
                _ => throw new ArgumentException("不明な証明の起点", nameof(signedAt)),
            };
            string runnerTemp = Directory.CreateDirectory(Path.Combine(root, "runner temp")).FullName;
            File.WriteAllText(Path.Combine(root, "lc-v1.2.4.zip"), "新しく署名したZIP");
            File.WriteAllText(Path.Combine(root, "CHANGES.md"), "新規の変更履歴");
            string prefix = $$"""
                $script:exists = ${{exists.ToString().ToLowerInvariant()}}
                $script:failUpload = ${{failUpload.ToString().ToLowerInvariant()}}
                $script:releaseState = [pscustomobject]@{ isDraft = ${{draft.ToString().ToLowerInvariant()}}; assets = @(); body = '既存の変更履歴' }
                if (${{hasAsset.ToString().ToLowerInvariant()}}) { $script:releaseState.assets = @([pscustomobject]@{ name = 'lc-v1.2.4.zip' }) }
                function Save-ReleaseState {
                    $script:releaseState | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 -LiteralPath 'published.json'
                }
                Save-ReleaseState
                function gh {
                    param([Parameter(ValueFromRemainingArguments)][string[]]$arguments)
                    Add-Content -LiteralPath 'publish-commands.txt' -Value $arguments[1]
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
                            $script:releaseState.body = Get-Content -Raw -LiteralPath 'CHANGES.md'
                            Save-ReleaseState
                        }
                        'upload' {
                            if ($script:failUpload) { $global:LASTEXITCODE = 1; return }
                            $script:releaseState.assets += [pscustomobject]@{ name = $arguments[3] }
                            Save-ReleaseState
                        }
                        'download' {
                            if ([IO.Path]::GetFullPath($arguments[-1]) -ceq [IO.Path]::GetFullPath('lc-v1.2.4.zip')) {
                                throw '既存ZIPを再ビルドしたZIPとは別のファイルに取得していない'
                            }
                            Set-Content -LiteralPath $arguments[-1] -Value '既存のZIP' -NoNewline
                        }
                        'verify' {
                            if (($arguments[0] -cne 'attestation') -or
                                ((Get-Content -Raw -LiteralPath $arguments[2]) -cne '既存のZIP') -or
                                (($arguments[3..7] -join ' ') -cne '--repo ak110/lc --signer-workflow ak110/lc/.github/workflows/release.yaml --source-digest')) {
                                throw '既存ZIPを想定した署名ワークフローで検証していない'
                            }
                            if ($arguments[8] -cne $env:FIXTURE_SIGNED_COMMIT) { $global:LASTEXITCODE = 1 }
                        }
                        'edit' {
                            if ($arguments[3] -ceq '--notes-file') {
                                $script:releaseState.body = Get-Content -Raw -LiteralPath $arguments[4]
                            } elseif ($arguments[3] -ceq '--draft=false') {
                                if (-not $script:releaseState.assets -or $script:releaseState.body -notlike '*gh attestation verify*') {
                                    throw 'ZIPと検証案内を用意する前にdraftを公開した'
                                }
                                $script:releaseState.isDraft = $false
                            } else {
                                throw '未対応のRelease編集操作'
                            }
                            Save-ReleaseState
                        }
                        default { throw '未対応のGitHub CLI操作' }
                    }
                }
                """;
            return RunWorkflowScript("publish.ps1", prefix + Environment.NewLine + StepScript("Create GitHub Release"), new Dictionary<string, string>
            {
                ["GITHUB_REPOSITORY"] = "ak110/lc",
                ["RELEASE_VERSION"] = "1.2.4",
                ["PRE_RELEASE_COMMIT"] = parent,
                ["RUNNER_TEMP"] = runnerTemp,
                ["FIXTURE_SIGNED_COMMIT"] = signedCommit,
            });
        }

        ProcessResult RunWorkflowScript(string name, string body, Dictionary<string, string>? environment = null)
        {
            // Actionsのpwsh実行器と同じ前後処理で、本文が消費した非0終了も検出する。
            string script = Path.Combine(root, name);
            File.WriteAllText(script, "$ErrorActionPreference = 'Stop'\n"
                + "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8\n" + body
                + "\nif ((Test-Path -LiteralPath variable:\\LASTEXITCODE)) { exit $LASTEXITCODE }\n", new UTF8Encoding(true));
            return Run("pwsh", ["-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", script], environment);
        }

        string StepScript(string name)
        {
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
