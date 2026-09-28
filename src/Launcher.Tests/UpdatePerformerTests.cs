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
        // ユーザーデータ(.cfgなど)は対象に含まれない
        result.Should().NotContain(".cfg");
        // 旧方式の一括コピーと.oldの削除は使わない
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
    public void GenerateBatchScript_一時ディレクトリ削除が含まれる()
    {
        List<string> files = ["app.exe"];
        var result = UpdatePerformer.GenerateBatchScript(
            1000, @"C:\app", @"C:\temp\update", @"C:\app\app.exe", @"C:\temp\_update.bat", files);

        result.Should().Contain(@"rd /S /Q ""C:\temp\update""");
        result.Should().Contain(@"del /F /Q ""C:\temp\update.zip""");
    }

    [Fact]
    public void GenerateBatchScript_バッチ自身の削除が含まれる()
    {
        List<string> files = ["app.exe"];
        var result = UpdatePerformer.GenerateBatchScript(
            1000, @"C:\app", @"C:\temp\update", @"C:\app\app.exe", @"C:\temp\_update.bat", files);

        // バッチ自身が最後に自分を削除
        result.Should().Contain(@"del /F /Q ""C:\temp\_update.bat""");
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

        env.Run(["app.exe", @"sub\lib.dll", "added.txt"], []).Should().Be("success");

        env.ReadApp("app.exe").Should().Be("new-app");
        env.ReadApp(@"sub\lib.dll").Should().Be("new-lib");
        env.ReadApp("added.txt").Should().Be("new-added");
        env.ReadApp("user.cfg").Should().Be("user-data");
        Directory.Exists(env.BackupDir).Should().BeFalse();
        Directory.Exists(env.TempDir).Should().BeFalse();
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

        // 追加ファイルと app.exe のコピー後、lib.dll のコピーで失敗させる
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

        // app.exe の退避後、lib.dll の退避で失敗させる
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

        // lib.dll のコピーで失敗させ、app.exe の復旧も失敗させる
        env.Run(["app.exe", @"sub\lib.dll"], ["copy:1", "restore:0"]).Should().Be("restore_failed");

        File.ReadAllText(Path.Combine(env.BackupDir, "app.exe")).Should().Be("old-app");
        env.ReadApp(@"sub\lib.dll").Should().Be("old-lib");

        var message = UpdatePerformer.BuildRestoreFailedMessage(env.AppDir, env.BackupDir);
        message.Should().Contain(env.BackupDir);
        message.Should().Contain(env.AppDir);
        message.Should().Contain("上書きコピー");
    }

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

        /// <summary>
        /// バッチを生成して実行し、結果ファイルの内容を返す。
        /// </summary>
        public string Run(List<string> files, IReadOnlyCollection<string> failSteps)
        {
            string resultPath = Path.Combine(_root, "result.txt");
            var options = new UpdateBatchOptions { ResultFilePath = resultPath, FailSteps = failSteps };
            string script = UpdatePerformer.GenerateBatchScript(
                FinishedProcessId(), AppDir, TempDir, AppPath("app.exe"), BatchPath, files, options);
            UpdatePerformer.WriteBatchFile(BatchPath, script);

            var psi = new ProcessStartInfo("cmd.exe", $"/c \"\"{BatchPath}\"\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(psi)!;
            process.WaitForExit(60_000).Should().BeTrue("更新バッチが時間内に終わる");
            return File.ReadAllText(resultPath).Trim();
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

        /// <summary>
        /// 終了済みプロセスのID。バッチの親プロセス待機をすぐに抜けさせる。
        /// </summary>
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
