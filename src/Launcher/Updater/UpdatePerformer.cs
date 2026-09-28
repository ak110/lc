using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Text;

namespace Launcher.Updater;

/// <summary>
/// 更新用バッチの試験用の生成オプション。既定値では通常の更新バッチを生成する。
/// </summary>
public sealed record UpdateBatchOptions
{
    /// <summary>
    /// 指定すると、アプリの起動と通知の代わりに結果 (success / restored / restore_failed) をこのファイルへ書く。
    /// </summary>
    public string? ResultFilePath { get; init; }

    /// <summary>
    /// 失敗させる処理段階 (<c>backup:N</c>・<c>copy:N</c>・<c>restore:N</c>、Nはファイル一覧の位置)。
    /// </summary>
    public IReadOnlyCollection<string> FailSteps { get; init; } = [];
}

/// <summary>
/// ZIP のダウンロード・展開・バッチスクリプトによるファイル置換で自動更新を実行する。
/// </summary>
public static class UpdatePerformer
{
    private static readonly HttpClient _httpClient = new()
    {
        DefaultRequestHeaders = {
            { "User-Agent", "Launcher-UpdateClient" },
        },
    };

    /// <summary>
    /// 更新を実行する。ZIP のダウンロード、展開、バッチスクリプトの生成・起動の順に処理する。
    /// この処理の完了はバッチの開始であり、更新の完了ではない。置換の成否の通知と失敗時の復旧はバッチが担う。
    /// 例外を送出した場合、インストール済みのアプリのファイルには触れていない。
    /// </summary>
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
            // ZIP をダウンロードする。
            progress?.Report("ダウンロード中...");
            using (var response = await _httpClient.GetAsync(release.DownloadUrl).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                using var fs = new FileStream(zipPath, FileMode.Create);
                await response.Content.CopyToAsync(fs).ConfigureAwait(false);
            }

            // ZIP を展開する。
            progress?.Report("展開中...");
            ZipFile.ExtractToDirectory(zipPath, tempDir);

            // ZIP 内のファイル一覧を取得する (サブディレクトリを含む相対パス)。
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

            // バッチを起動する。
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
            // 失敗時はクリーンアップする。
            try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            foreach (var path in new[] { batchPath, RestoredMessagePath(batchPath), RestoreFailedMessagePath(batchPath) })
            {
                try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
            throw;
        }
    }

    /// <summary>
    /// ディレクトリ内のファイルを相対パスで取得する (バッチスクリプト自身は除外)。
    /// </summary>
    private static List<string> GetRelativeFiles(string baseDir)
    {
        List<string> files = [];
        foreach (var fullPath in Directory.GetFiles(baseDir, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(baseDir, fullPath);
            // バッチスクリプト自身は除外する。
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

    /// <summary>
    /// この更新の退避フォルダ。更新ごとに一意で、この更新のバッチだけが作成・削除する。
    /// </summary>
    public static string GetBackupDir(string appDir, string batchPath)
    {
        string id = Path.GetFileNameWithoutExtension(batchPath).TrimStart('_');
        return Path.Combine(appDir, "_update_backup_" + id);
    }

    /// <summary>旧版へ戻せたときの通知文のファイル</summary>
    public static string RestoredMessagePath(string batchPath) => batchPath + ".restored.txt";

    /// <summary>旧版へ戻せなかったときの通知文のファイル</summary>
    public static string RestoreFailedMessagePath(string batchPath) => batchPath + ".failed.txt";

    /// <summary>
    /// 旧版へ戻せたときの通知文。
    /// </summary>
    public static string BuildRestoredMessage() =>
        "らんちゃの更新に失敗したため、更新前のバージョンに戻して起動しました。\r\n\r\n" +
        "時間をおいて、もう一度更新してください。";

    /// <summary>
    /// 旧版へ戻せなかったときの通知文。退避フォルダの場所と手動復旧の手順を含める。
    /// </summary>
    public static string BuildRestoreFailedMessage(string appDir, string backupDir) =>
        "らんちゃの更新に失敗し、更新前のバージョンへ戻すこともできませんでした。\r\n\r\n" +
        "更新前のファイルは次のフォルダーに残っています。\r\n" +
        backupDir + "\r\n\r\n" +
        "手動で戻すには、らんちゃが起動していないことを確認してから、" +
        "上のフォルダーの中身をすべて次のフォルダーへ上書きコピーしてください。\r\n" +
        appDir + "\r\n\r\n" +
        "設定・コマンド・メモなどのファイルは更新の対象外のため、変更していません。";

    /// <summary>
    /// バッチが表示する通知文を UTF-8 で書き込む。CP932 のバッチの行へ文章を埋め込まずに済ませる。
    /// </summary>
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

        // 親プロセスの終了を待機する。
        sb.AppendLine(":WAIT_LOOP");
        sb.AppendLine($"tasklist /FI \"PID eq {pid}\" 2>NUL | find \"{pid}\" >NUL");
        sb.AppendLine("if errorlevel 1 goto WAIT_DONE");
        sb.AppendLine("timeout /t 1 /nobreak >NUL");
        sb.AppendLine("goto WAIT_LOOP");
        sb.AppendLine(":WAIT_DONE");
        sb.AppendLine();

        // 退避: ZIP 内に存在するファイルだけを退避フォルダへ移動する (ユーザーデータには触れない)。
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

        // コピー: 1件ずつコピーし、失敗したら復旧へ進む。
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

        // 成功: 新版を起動し、この更新の退避物を削除する。
        AppendResult(sb, options, "success");
        if (options.ResultFilePath is null)
        {
            sb.AppendLine($"start \"\" \"{appExe}\"");
        }
        sb.AppendLine($"rd /S /Q \"{backupDir}\"");
        sb.AppendLine("goto CLEANUP");
        sb.AppendLine();

        // 復旧: 逆順に、退避物があれば元へ戻し、更新前に無かったファイルは削除する。
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

        // 復旧失敗: 退避フォルダを残し、場所と手動復旧の手順を通知する。新旧が混在したアプリは起動しない。
        sb.AppendLine(":RESTORE_FAILED");
        AppendResult(sb, options, "restore_failed");
        if (options.ResultFilePath is null)
        {
            AppendNotify(sb, RestoreFailedMessagePath(batchPath), "Error");
        }
        sb.AppendLine();

        // 一時ファイルをクリーンアップする。
        sb.AppendLine(":CLEANUP");
        string zipPath = tempDir + ".zip";
        sb.AppendLine($"if exist \"{zipPath}\" del /F /Q \"{zipPath}\"");
        sb.AppendLine($"rd /S /Q \"{tempDir}\"");
        sb.AppendLine($"if exist \"{RestoredMessagePath(batchPath)}\" del /F /Q \"{RestoredMessagePath(batchPath)}\"");
        sb.AppendLine($"if exist \"{RestoreFailedMessagePath(batchPath)}\" del /F /Q \"{RestoreFailedMessagePath(batchPath)}\"");
        sb.AppendLine();

        // バッチ自身を削除して終了する。
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

    /// <summary>
    /// 通知文のファイルをメッセージボックスで表示する行を追加する。閉じるまでバッチは待機する。
    /// </summary>
    private static void AppendNotify(StringBuilder sb, string messagePath, string icon)
    {
        string literal = messagePath.Replace("'", "''");
        sb.AppendLine(
            "powershell -NoProfile -ExecutionPolicy Bypass -Command \"Add-Type -AssemblyName System.Windows.Forms; " +
            $"[void][System.Windows.Forms.MessageBox]::Show((Get-Content -Raw -Encoding UTF8 -LiteralPath '{literal}'), " +
            $"'らんちゃの更新', 'OK', '{icon}')\"");
    }
}
