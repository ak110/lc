using System.Diagnostics;
using FluentAssertions;
using Launcher.Updater;
using Xunit;

namespace Launcher.Tests;

public sealed class UpdatePerformerTests
{
    [Fact]
    public void GenerateBatchScript_親プロセス待機コードが含まれる()
    {
        List<string> files = ["test.exe", "test.dll"];
        var result = UpdatePerformer.GenerateBatchScript(
            1234, @"C:\app", @"C:\temp\update", @"C:\app\test.exe", @"C:\temp\_update.bat", files);

        result.Should().Contain("tasklist /FI \"PID eq 1234\"");
        result.Should().Contain("find \"1234\"");
        result.Should().Contain("goto WAIT_DONE");
    }

    [Fact]
    public void GenerateBatchScript_ZIP内ファイルだけを退避してコピーする()
    {
        List<string> files = ["app.exe", @"sub\lib.dll"];
        var result = UpdatePerformer.GenerateBatchScript(
            1000, @"C:\app", @"C:\temp\update", @"C:\app\app.exe", @"C:\temp\_update_ab12.bat", files);

        result.Should().Contain(@"move /Y ""C:\app\app.exe"" ""C:\app\_update_backup_update_ab12\app.exe""");
        result.Should().Contain(@"move /Y ""C:\app\sub\lib.dll"" ""C:\app\_update_backup_update_ab12\sub\lib.dll""");
        result.Should().Contain(@"copy /Y ""C:\temp\update\app.exe"" ""C:\app\app.exe""");
        result.Should().Contain(@"copy /Y ""C:\temp\update\sub\lib.dll"" ""C:\app\sub\lib.dll""");
        result.Should().NotContain(".cfg");
        result.Should().NotContain("xcopy");
        result.Should().NotContain(".old");
    }

    [Fact]
    public void GenerateBatchScript_各コピーの結果を判定して復旧へ進む()
    {
        List<string> files = ["app.exe"];
        var result = UpdatePerformer.GenerateBatchScript(
            1000, @"C:\app", @"C:\temp\update", @"C:\app\app.exe", @"C:\temp\_update.bat", files);

        var lines = result.Split(Environment.NewLine);
        int copy = Array.FindIndex(lines, l => l.StartsWith("copy /Y", StringComparison.Ordinal));
        lines[copy - 1].Should().Be("ver >NUL");
        lines[copy + 1].Should().Be("if errorlevel 1 goto RESTORE");
        result.Should().Contain(":RESTORE");
        result.Should().Contain(":RESTORE_FAILED");
    }

    [Fact]
    public void GenerateBatchScript_アプリ起動コマンドが含まれる()
    {
        List<string> files = ["app.exe"];
        var result = UpdatePerformer.GenerateBatchScript(
            1000, @"C:\app", @"C:\temp\update", @"C:\app\app.exe", @"C:\temp\_update.bat", files);

        result.Should().Contain(@"start """" ""C:\app\app.exe""");
    }

    [Fact]
    public void GenerateBatchScript_echoオフとexit()
    {
        List<string> files = ["app.exe"];
        var result = UpdatePerformer.GenerateBatchScript(
            1000, @"C:\app", @"C:\temp\update", @"C:\app\app.exe", @"C:\temp\_update.bat", files);

        result.Should().StartWith("@echo off");
        result.Should().Contain("exit");
    }

    [Fact]
    public void 更新バッチ_正常更新で配布ファイルだけを置換して退避物を残さない()
    {
        using var env = new BatchEnvironment();
        env.WriteApp("app.exe", "old-app");
        env.WriteApp(@"sub\lib.dll", "old-lib");
        env.WriteApp("user.cfg", "user-data");
        env.WriteNew("app.exe", "new-app");
        env.WriteNew(@"sub\lib.dll", "new-lib");
        env.WriteNew("added.txt", "new-added");
        File.WriteAllText(env.TempDir + ".zip", "downloaded");

        env.Run(["app.exe", @"sub\lib.dll", "added.txt"], []).Should().Be("success");

        env.ReadApp("app.exe").Should().Be("new-app");
        env.ReadApp(@"sub\lib.dll").Should().Be("new-lib");
        env.ReadApp("added.txt").Should().Be("new-added");
        env.ReadApp("user.cfg").Should().Be("user-data");
        Directory.Exists(env.BackupDir).Should().BeFalse();
        Directory.Exists(env.TempDir).Should().BeFalse();
        File.Exists(env.TempDir + ".zip").Should().BeFalse();
        File.Exists(env.BatchPath).Should().BeFalse();
    }

    [Fact]
    public void 更新バッチ_コピー失敗で旧版へ戻す()
    {
        using var env = new BatchEnvironment();
        env.WriteApp("app.exe", "old-app");
        env.WriteApp(@"sub\lib.dll", "old-lib");
        env.WriteApp("user.cfg", "user-data");
        env.WriteNew("added.txt", "new-added");
        env.WriteNew("app.exe", "new-app");
        env.WriteNew(@"sub\lib.dll", "new-lib");

        env.Run(["added.txt", "app.exe", @"sub\lib.dll"], ["copy:2"]).Should().Be("restored");

        env.ReadApp("app.exe").Should().Be("old-app");
        env.ReadApp(@"sub\lib.dll").Should().Be("old-lib");
        env.ReadApp("user.cfg").Should().Be("user-data");
        File.Exists(env.AppPath("added.txt")).Should().BeFalse("更新前に無かったファイルは削除する");
        Directory.Exists(env.BackupDir).Should().BeFalse();
    }

    [Fact]
    public void 更新バッチ_退避失敗でコピーせず元へ戻す()
    {
        using var env = new BatchEnvironment();
        env.WriteApp("app.exe", "old-app");
        env.WriteApp(@"sub\lib.dll", "old-lib");
        env.WriteNew("app.exe", "new-app");
        env.WriteNew(@"sub\lib.dll", "new-lib");
        env.WriteNew("added.txt", "new-added");

        env.Run(["app.exe", @"sub\lib.dll", "added.txt"], ["backup:1"]).Should().Be("restored");

        env.ReadApp("app.exe").Should().Be("old-app");
        env.ReadApp(@"sub\lib.dll").Should().Be("old-lib");
        File.Exists(env.AppPath("added.txt")).Should().BeFalse("コピーへ進まない");
        Directory.Exists(env.BackupDir).Should().BeFalse();
    }

    [Fact]
    public void 更新バッチ_復旧失敗で退避物を残し手動復旧を案内する()
    {
        using var env = new BatchEnvironment();
        env.WriteApp("app.exe", "old-app");
        env.WriteApp(@"sub\lib.dll", "old-lib");
        env.WriteNew("app.exe", "new-app");
        env.WriteNew(@"sub\lib.dll", "new-lib");

        env.Run(["app.exe", @"sub\lib.dll"], ["copy:1", "restore:0"]).Should().Be("restore_failed");

        File.ReadAllText(Path.Combine(env.BackupDir, "app.exe")).Should().Be("old-app");
        env.ReadApp(@"sub\lib.dll").Should().Be("old-lib");

        var message = UpdatePerformer.BuildRestoreFailedMessage(env.AppDir, env.BackupDir);
        message.Should().Contain(env.BackupDir);
        message.Should().Contain(env.AppDir);
        message.Should().Contain("上書きコピー");
    }

    [UpdateEnumerationFact]
    public void 更新バッチ_処理境界で完全な新版か復旧可能な状態を保つ()
    {
        string[] paths = ["app.exe", @"sub\lib.dll"];
        foreach (bool reverse in new[] { false, true })
        {
            List<string> order = reverse ? [paths[1], paths[0]] : [.. paths];
            for (int oldMask = 0; oldMask < 4; oldMask++)
            {
                bool HadOld(int index) => (oldMask & (1 << index)) != 0;

                Check([], [], "success");
                for (int failed = 0; failed < order.Count; failed++)
                {
                    if (HadOld(failed))
                    {
                        CheckRestoreMasks($"backup:{failed}", oldMask & ((1 << failed) - 1), 0);
                    }
                    CheckRestoreMasks($"copy:{failed}", oldMask, ~oldMask & ((1 << failed) - 1));
                }

                void CheckRestoreMasks(string initialFailure, int backupMask, int addedMask)
                {
                    for (int restoreMask = 0; restoreMask < 4; restoreMask++)
                    {
                        if ((restoreMask & ~(backupMask | addedMask)) != 0) continue;
                        List<string> failures = [initialFailure];
                        List<string> blockedDeletions = [];
                        for (int index = 0; index < order.Count; index++)
                        {
                            if ((restoreMask & (1 << index)) == 0) continue;
                            if (HadOld(index)) failures.Add($"restore:{index}");
                            else blockedDeletions.Add(order[index]);
                        }
                        Check(failures, blockedDeletions, restoreMask == 0 ? "restored" : "restore_failed");
                    }
                }

                void Check(List<string> failures, List<string> blockedDeletions, string expected)
                {
                    const string settings = "launcher.cfg";
                    const string memo = "launcher.memo.cfg";
                    var scenario = $"order={string.Join(',', order)}, old={oldMask}, failures={string.Join(',', failures)}, delete={string.Join(',', blockedDeletions)}";
                    int? checkpoint = null;
                    int checkpointCount;
                    int stopsReached = 0;
                    do
                    {
                        using var env = new BatchEnvironment();
                        env.WriteApp(settings, "settings");
                        env.WriteApp(memo, "memo");
                        for (int index = 0; index < order.Count; index++)
                        {
                            if (HadOld(index)) env.WriteApp(order[index], $"old-{index}");
                            env.WriteNew(order[index], $"new-{index}");
                        }

                        var (result, count) = env.RunWithCheckpoint(order, failures, checkpoint, blockedDeletions);
                        checkpointCount = count;
                        if (checkpoint is null || result != "interrupted") result.Should().Be(expected, scenario);
                        else stopsReached++;
                        checkpoint = (checkpoint ?? -1) + 1;

                        var reason = $"{scenario}, boundary={checkpoint - 1}, result={result}";
                        env.ReadApp(settings).Should().Be("settings", reason);
                        env.ReadApp(memo).Should().Be("memo", reason);
                        string?[] installed = order.Select(path => ReadIfPresent(env.AppPath(path))).ToArray();
                        bool completeNew = installed.Select((content, i) => content == $"new-{i}").All(match => match);
                        bool completeOld = installed.Select((content, i) => content == (HadOld(i) ? $"old-{i}" : null)).All(match => match);

                        if (result == "success")
                        {
                            completeNew.Should().BeTrue(reason);
                        }
                        else if (result == "restored")
                        {
                            completeOld.Should().BeTrue(reason);
                        }
                        else if (!completeNew || result == "restore_failed")
                        {
                            for (int index = 0; index < order.Count; index++)
                            {
                                if (!HadOld(index)) continue;
                                var backup = ReadIfPresent(Path.Combine(env.BackupDir, order[index]));
                                (installed[index] == $"old-{index}" || backup == $"old-{index}").Should().BeTrue(reason);
                            }
                        }

                    }
                    while (checkpoint < checkpointCount);
                    stopsReached.Should().BePositive(scenario);
                }
            }
        }
    }

    private sealed class UpdateEnumerationFactAttribute : FactAttribute
    {
        public UpdateEnumerationFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("LC_CHECK_UPDATE_BOUNDARIES") != "1")
            {
                Skip = "LC_CHECK_UPDATE_BOUNDARIES=1";
            }
        }
    }

    private static string? ReadIfPresent(string path) => File.Exists(path) ? File.ReadAllText(path) : null;

    /// <summary>
    /// 生成した更新バッチを隔離フォルダで実行する環境。
    /// 更新バッチはCP932で書くため、コードページが932の環境 (日本語版Windows) ではフォルダ名に日本語を含め、
    /// CP932での書き込みも検証する。それ以外の環境 (CIのランナーなど) ではcmdが日本語のパスを解釈できないため、ASCIIのフォルダ名で動作を検証する。
    /// </summary>
    private sealed class BatchEnvironment : IDisposable
    {
        private static readonly bool JapaneseCodePage =
            System.Globalization.CultureInfo.InstalledUICulture.TextInfo.OEMCodePage == 932;

        private readonly string _root;

        public BatchEnvironment()
        {
            _root = Path.Combine(Path.GetTempPath(),
                (JapaneseCodePage ? "らんちゃ更新テスト_" : "lc_update_test_") + Guid.NewGuid().ToString("N")[..8]);
            AppDir = Path.Combine(_root, JapaneseCodePage ? "アプリ" : "app");
            TempDir = Path.Combine(_root, "update");
            BatchPath = Path.Combine(_root, "_launcher_update_test.bat");
            Directory.CreateDirectory(AppDir);
            Directory.CreateDirectory(TempDir);
        }

        public string AppDir { get; }

        public string TempDir { get; }

        public string BatchPath { get; }

        public string BackupDir => UpdatePerformer.GetBackupDir(AppDir, BatchPath);

        public string AppPath(string relative) => Path.Combine(AppDir, relative);

        public void WriteApp(string relative, string content) => Write(AppPath(relative), content);

        public void WriteNew(string relative, string content) => Write(Path.Combine(TempDir, relative), content);

        public string ReadApp(string relative) => File.ReadAllText(AppPath(relative));

        public string Run(List<string> files, IReadOnlyCollection<string> failSteps) =>
            RunWithCheckpoint(files, failSteps, null).Result;

        public (string Result, int CheckpointCount) RunWithCheckpoint(
            List<string> files, IReadOnlyCollection<string> failSteps, int? checkpoint,
            IReadOnlyCollection<string>? blockedDeletions = null)
        {
            string resultPath = Path.Combine(_root, "result.txt");
            var options = new UpdateBatchOptions { ResultFilePath = resultPath, FailSteps = failSteps };
            string script = UpdatePerformer.GenerateBatchScript(
                blockedDeletions is null ? FinishedProcessId() : 0, AppDir, TempDir, AppPath("app.exe"), BatchPath, files, options);
            string[] lines = script.Split("\r\n", StringSplitOptions.None);
            if (blockedDeletions is not null)
            {
                int wait = Array.IndexOf(lines, ":WAIT_LOOP");
                wait.Should().BeGreaterThanOrEqualTo(0);
                lines[wait] = "goto WAIT_DONE\r\n" + lines[wait];
            }
            int[] boundaries = Enumerable.Range(0, lines.Length).Where(index =>
                lines[index].StartsWith("ver >NUL", StringComparison.Ordinal)
                || lines[index].Contains(" mkdir ", StringComparison.Ordinal)
                || lines[index].Contains(" del /F /Q ", StringComparison.Ordinal)
                || lines[index].StartsWith("del /F /Q ", StringComparison.Ordinal)
                || lines[index].StartsWith("rd /S /Q ", StringComparison.Ordinal)).ToArray();
            foreach (string path in blockedDeletions ?? [])
            {
                string target = AppPath(path);
                string prefix = $"if exist \"{target}\" del /F /Q \"{target}\"";
                int index = Array.FindIndex(lines, line => line.StartsWith(prefix, StringComparison.Ordinal));
                index.Should().BeGreaterThanOrEqualTo(0);
                lines[index] = "rem leave the file in place";
            }
            if (checkpoint is int stop)
            {
                lines[boundaries[stop]] = $">\"{resultPath}\" echo interrupted\r\nexit /b 99\r\n" + lines[boundaries[stop]];
            }
            script = string.Join("\r\n", lines);
            UpdatePerformer.WriteBatchFile(BatchPath, script);

            var psi = new ProcessStartInfo("cmd.exe", $"/d /c \"\"{BatchPath}\"\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.Environment.Remove("COPY_STARTED");
            using var process = Process.Start(psi)!;
            if (!process.WaitForExit(60_000))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
                throw new TimeoutException("更新バッチが時間内に終わらなかった");
            }
            return (File.ReadAllText(resultPath).Trim(), boundaries.Length);
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        private static void Write(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }

        private static int FinishedProcessId()
        {
            using var process = Process.Start(new ProcessStartInfo("cmd.exe", "/c exit")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            })!;
            process.WaitForExit();
            return process.Id;
        }
    }
}
