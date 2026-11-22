using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Launcher.Infrastructure;

namespace Launcher.Updater;

public sealed record UpdateBatchOptions
{
    public string? ResultFilePath { get; init; }

    public IReadOnlyCollection<string> FailSteps { get; init; } = [];
}

public static class UpdatePerformer
{
    public static async Task PerformUpdateAsync(GitHubRelease release, IProgress<string>? progress = null)
    {
        if (string.IsNullOrEmpty(release.DownloadUrl))
        {
            throw new InvalidOperationException("ダウンロード URL が見つからない");
        }

        string appDir = Path.GetDirectoryName(Application.ExecutablePath)!;
        string updateId = Guid.NewGuid().ToString("N")[..8];
        string tempDir = Path.Combine(Path.GetTempPath(), "launcher_update_" + updateId);
        string zipPath = tempDir + ".zip";
        // xcopy 等でアプリディレクトリへコピーされないよう、バッチスクリプトは tempDir の外に作成する。
        string batchPath = Path.Combine(Path.GetTempPath(), "_launcher_update_" + updateId + ".bat");

        try
        {
            progress?.Report("ダウンロード中...");
            using var request = SharedHttpClient.CreateUpdateRequest(release.DownloadUrl);
            using (var response = await SharedHttpClient.Instance.SendAsync(request).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                using var fs = new FileStream(zipPath, FileMode.Create);
                await response.Content.CopyToAsync(fs).ConfigureAwait(false);
            }

            progress?.Report("展開中...");
            ZipFile.ExtractToDirectory(zipPath, tempDir);

            var extractedFiles = GetRelativeFiles(tempDir);

            progress?.Report("更新を適用しています...");
            string batchContent = GenerateBatchScript(
                Environment.ProcessId,
                appDir,
                tempDir,
                Application.ExecutablePath,
                batchPath,
                extractedFiles
            );
            WriteMessageFiles(batchPath, appDir, GetBackupDir(appDir, batchPath));
            WriteBatchFile(batchPath, batchContent);

            var psi = new ProcessStartInfo
            {
                FileName = batchPath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            Process.Start(psi);
        }
        catch
        {
            try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            foreach (var path in new[] { batchPath, RestoredMessagePath(batchPath), RestoreFailedMessagePath(batchPath) })
            {
                try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
            throw;
        }
    }

    private static List<string> GetRelativeFiles(string baseDir)
    {
        List<string> files = [];
        foreach (var fullPath in Directory.GetFiles(baseDir, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(baseDir, fullPath);
            if (relativePath.Equals("_update.bat", StringComparison.OrdinalIgnoreCase)) continue;
            files.Add(relativePath);
        }
        return files;
    }

    /// <summary>
    /// 更新バッチをファイルへ書き込む。アセンブリ名が日本語のため CP932 (Shift_JIS) で書き込む。
    /// </summary>
    public static void WriteBatchFile(string batchPath, string batchContent)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        File.WriteAllText(batchPath, batchContent, Encoding.GetEncoding(932));
    }

    public static string GetBackupDir(string appDir, string batchPath)
    {
        string id = Path.GetFileNameWithoutExtension(batchPath).TrimStart('_');
        return Path.Combine(appDir, "_update_backup_" + id);
    }

    public static string RestoredMessagePath(string batchPath) => batchPath + ".restored.txt";

    public static string RestoreFailedMessagePath(string batchPath) => batchPath + ".failed.txt";

    public static string BuildRestoredMessage() =>
        "らんちゃの更新に失敗したため、更新前のバージョンに戻して起動しました。\r\n\r\n" +
        "時間をおいて、もう一度更新してください。";

    public static string BuildRestoreFailedMessage(string appDir, string backupDir) =>
        "らんちゃの更新に失敗し、更新前のバージョンへ戻すこともできませんでした。\r\n\r\n" +
        "更新前のファイルは次のフォルダーに残っています。\r\n" +
        backupDir + "\r\n\r\n" +
        "手動で戻すには、らんちゃが起動していないことを確認してから、" +
        "上のフォルダーの中身をすべて次のフォルダーへ上書きコピーしてください。\r\n" +
        appDir + "\r\n\r\n" +
        "設定・コマンド・メモなどのファイルは更新の対象外のため、変更していません。";

    public static void WriteMessageFiles(string batchPath, string appDir, string backupDir)
    {
        File.WriteAllText(RestoredMessagePath(batchPath), BuildRestoredMessage(), Encoding.UTF8);
        File.WriteAllText(RestoreFailedMessagePath(batchPath), BuildRestoreFailedMessage(appDir, backupDir), Encoding.UTF8);
    }

    /// <summary>
    /// 更新用バッチスクリプトを生成する。
    /// 親プロセスの終了待機の後、配布ファイルの退避と、1件ずつのコピーと結果判定を行う。
    /// 全件成功した場合だけ新版を起動して退避物を削除する。途中で失敗した場合は変更したファイルだけを逆順に戻し、
    /// 旧版を起動して通知する。戻せなかった場合は退避フォルダを残し、場所と手動復旧の手順を通知する。
    /// DBCS トレイルバイト問題を避けるため、if() ブロック内に日本語を含めず goto で制御する。
    /// 終了コードを判定するコマンドの直前には <c>ver</c> を置き、前のコマンドの ERRORLEVEL を持ち越さない。
    /// </summary>
    public static string GenerateBatchScript(
        int pid, string appDir, string tempDir, string appExe, string batchPath, List<string> files,
        UpdateBatchOptions? options = null)
    {
        options ??= new UpdateBatchOptions();
        string backupDir = GetBackupDir(appDir, batchPath);
        string Target(int i) => Path.Combine(appDir, files[i]);
        string Backup(int i) => Path.Combine(backupDir, files[i]);
        string Source(int i) => Path.Combine(tempDir, files[i]);
        string Checked(string step, int i, string command) =>
            options.FailSteps.Contains($"{step}:{i}") ? "cmd /c exit 1" : command;

        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("setlocal");
        sb.AppendLine("title Update");
        sb.AppendLine();

        sb.AppendLine(":WAIT_LOOP");
        sb.AppendLine($"tasklist /FI \"PID eq {pid}\" 2>NUL | find \"{pid}\" >NUL");
        sb.AppendLine("if errorlevel 1 goto WAIT_DONE");
        sb.AppendLine("timeout /t 1 /nobreak >NUL");
        sb.AppendLine("goto WAIT_LOOP");
        sb.AppendLine(":WAIT_DONE");
        sb.AppendLine();

        sb.AppendLine($"if not exist \"{backupDir}\\\" mkdir \"{backupDir}\"");
        sb.AppendLine($"if not exist \"{backupDir}\\\" goto RESTORE");
        for (int i = 0; i < files.Count; i++)
        {
            sb.AppendLine($"if not exist \"{Target(i)}\" goto B{i}_DONE");
            string backupParent = Path.GetDirectoryName(Backup(i))!;
            if (!string.Equals(backupParent, backupDir, StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"if not exist \"{backupParent}\\\" mkdir \"{backupParent}\"");
            }
            sb.AppendLine("ver >NUL");
            sb.AppendLine(Checked("backup", i, $"move /Y \"{Target(i)}\" \"{Backup(i)}\" >NUL"));
            sb.AppendLine("if errorlevel 1 goto RESTORE");
            sb.AppendLine($":B{i}_DONE");
        }
        sb.AppendLine();

        sb.AppendLine("set \"COPY_STARTED=1\"");
        for (int i = 0; i < files.Count; i++)
        {
            string targetParent = Path.GetDirectoryName(Target(i))!;
            if (!string.Equals(targetParent, appDir.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"if not exist \"{targetParent}\\\" mkdir \"{targetParent}\"");
            }
            sb.AppendLine("ver >NUL");
            sb.AppendLine(Checked("copy", i, $"copy /Y \"{Source(i)}\" \"{Target(i)}\" >NUL"));
            sb.AppendLine("if errorlevel 1 goto RESTORE");
        }
        sb.AppendLine();

        AppendResult(sb, options, "success");
        if (options.ResultFilePath is null)
        {
            sb.AppendLine($"start \"\" \"{appExe}\"");
        }
        sb.AppendLine($"rd /S /Q \"{backupDir}\"");
        sb.AppendLine("goto CLEANUP");
        sb.AppendLine();

        sb.AppendLine(":RESTORE");
        sb.AppendLine("set \"RESTORE_FAILED=0\"");
        for (int i = files.Count - 1; i >= 0; i--)
        {
            sb.AppendLine($"if not exist \"{Backup(i)}\" goto R{i}_NEW");
            sb.AppendLine("ver >NUL");
            sb.AppendLine(Checked("restore", i, $"move /Y \"{Backup(i)}\" \"{Target(i)}\" >NUL"));
            sb.AppendLine("if errorlevel 1 set \"RESTORE_FAILED=1\"");
            sb.AppendLine($"goto R{i}_DONE");
            sb.AppendLine($":R{i}_NEW");
            sb.AppendLine($"if not defined COPY_STARTED goto R{i}_DONE");
            sb.AppendLine($"if exist \"{Target(i)}\" del /F /Q \"{Target(i)}\" >NUL 2>&1");
            sb.AppendLine($"if exist \"{Target(i)}\" set \"RESTORE_FAILED=1\"");
            sb.AppendLine($":R{i}_DONE");
        }
        sb.AppendLine("if \"%RESTORE_FAILED%\"==\"1\" goto RESTORE_FAILED");
        AppendResult(sb, options, "restored");
        if (options.ResultFilePath is null)
        {
            sb.AppendLine($"start \"\" \"{appExe}\"");
            AppendNotify(sb, RestoredMessagePath(batchPath), "Warning");
        }
        sb.AppendLine($"rd /S /Q \"{backupDir}\"");
        sb.AppendLine("goto CLEANUP");
        sb.AppendLine();

        sb.AppendLine(":RESTORE_FAILED");
        AppendResult(sb, options, "restore_failed");
        if (options.ResultFilePath is null)
        {
            AppendNotify(sb, RestoreFailedMessagePath(batchPath), "Error");
        }
        sb.AppendLine();

        sb.AppendLine(":CLEANUP");
        string zipPath = tempDir + ".zip";
        sb.AppendLine($"if exist \"{zipPath}\" del /F /Q \"{zipPath}\"");
        sb.AppendLine($"rd /S /Q \"{tempDir}\"");
        sb.AppendLine($"if exist \"{RestoredMessagePath(batchPath)}\" del /F /Q \"{RestoredMessagePath(batchPath)}\"");
        sb.AppendLine($"if exist \"{RestoreFailedMessagePath(batchPath)}\" del /F /Q \"{RestoreFailedMessagePath(batchPath)}\"");
        sb.AppendLine();

        sb.AppendLine($"del /F /Q \"{batchPath}\"");
        sb.AppendLine("exit");

        return sb.ToString();
    }

    private static void AppendResult(StringBuilder sb, UpdateBatchOptions options, string result)
    {
        if (options.ResultFilePath is not null)
        {
            sb.AppendLine($">\"{options.ResultFilePath}\" echo {result}");
        }
    }

    private static void AppendNotify(StringBuilder sb, string messagePath, string icon)
    {
        string literal = messagePath.Replace("'", "''");
        sb.AppendLine(
            "powershell -NoProfile -ExecutionPolicy Bypass -Command \"Add-Type -AssemblyName System.Windows.Forms; " +
            $"[void][System.Windows.Forms.MessageBox]::Show((Get-Content -Raw -Encoding UTF8 -LiteralPath '{literal}'), " +
            $"'らんちゃの更新', 'OK', '{icon}')\"");
    }
}
