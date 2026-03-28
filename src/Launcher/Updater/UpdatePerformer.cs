using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Text;

namespace Launcher.Updater;

/// <summary>
/// ZIPダウンロード・展開・バッチスクリプトによるファイル置換で自動更新を実行する
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
    /// 更新を実行する。ZIPダウンロード→展開→バッチスクリプト生成・起動→アプリ終了。
    /// </summary>
    public static async Task PerformUpdateAsync(GitHubRelease release, IProgress<string>? progress = null)
    {
        // ZIPアセットを検索
        var zipAsset = release.Assets?.Find(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
        if (zipAsset == null)
        {
            throw new InvalidOperationException("ZIPアセットが見つかりません。");
        }

        string appDir = Path.GetDirectoryName(Application.ExecutablePath)!;
        string tempDir = Path.Combine(Path.GetTempPath(), "launcher_update_" + Guid.NewGuid().ToString("N")[..8]);
        string zipPath = tempDir + ".zip";

        try
        {
            // ZIPダウンロード
            progress?.Report("ダウンロード中...");
            using (var response = await _httpClient.GetAsync(zipAsset.BrowserDownloadUrl))
            {
                response.EnsureSuccessStatusCode();
                using var fs = new FileStream(zipPath, FileMode.Create);
                await response.Content.CopyToAsync(fs);
            }

            // ZIP展開
            progress?.Report("展開中...");
            ZipFile.ExtractToDirectory(zipPath, tempDir);

            // ZIP内のファイル一覧を取得（サブディレクトリも含む相対パス）
            var extractedFiles = GetRelativeFiles(tempDir);

            // バッチスクリプトはtempDirの外に作成する (xcopyでアプリディレクトリにコピーされないようにするため)
            progress?.Report("更新を適用しています...");
            string batchPath = Path.Combine(Path.GetTempPath(), "_launcher_update_" + Guid.NewGuid().ToString("N")[..8] + ".bat");
            string batchContent = GenerateBatchScript(
                Environment.ProcessId,
                appDir,
                tempDir,
                Application.ExecutablePath,
                batchPath,
                extractedFiles
            );

            // CP932(Shift_JIS)で書き出す（アセンブリ名が日本語のため）
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var cp932 = Encoding.GetEncoding(932);
            File.WriteAllText(batchPath, batchContent, cp932);

            // バッチ起動
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
            // 失敗時はクリーンアップ
            try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            throw;
        }
    }

    /// <summary>
    /// ディレクトリ内のファイルを相対パスで取得（バッチスクリプト自身は除外）
    /// </summary>
    private static List<string> GetRelativeFiles(string baseDir)
    {
        List<string> files = [];
        foreach (var fullPath in Directory.GetFiles(baseDir, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(baseDir, fullPath);
            // バッチスクリプト自身は除外
            if (relativePath.Equals("_update.bat", StringComparison.OrdinalIgnoreCase)) continue;
            files.Add(relativePath);
        }
        return files;
    }

    /// <summary>
    /// 更新用バッチスクリプトを生成する。
    /// DBCSトレイルバイト問題を避けるため、if()ブロック内に日本語を入れずgotoで制御する。
    /// </summary>
    public static string GenerateBatchScript(
        int pid, string appDir, string tempDir, string appExe, string batchPath, List<string> files)
    {
        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("title Update");
        sb.AppendLine();

        // 親プロセスの終了を待機
        sb.AppendLine(":WAIT_LOOP");
        sb.AppendLine($"tasklist /FI \"PID eq {pid}\" 2>NUL | find \"{pid}\" >NUL");
        sb.AppendLine("if errorlevel 1 goto WAIT_DONE");
        sb.AppendLine("timeout /t 1 /nobreak >NUL");
        sb.AppendLine("goto WAIT_LOOP");
        sb.AppendLine(":WAIT_DONE");
        sb.AppendLine();

        // ZIP内に存在するファイルのみを.oldにリネーム（ユーザーデータは触らない）
        foreach (var file in files)
        {
            string targetPath = Path.Combine(appDir, file);
            sb.AppendLine($"if exist \"{targetPath}\" rename \"{targetPath}\" \"{Path.GetFileName(file)}.old\"");
        }
        sb.AppendLine();

        // 展開したZIP内の全ファイルをアプリディレクトリへコピー
        sb.AppendLine($"xcopy /E /Y /I \"{tempDir}\\*\" \"{appDir}\\\"");
        sb.AppendLine();

        // アプリを起動
        sb.AppendLine($"start \"\" \"{appExe}\"");
        sb.AppendLine();

        // .oldファイルを削除
        foreach (var file in files)
        {
            string oldPath = Path.Combine(appDir, file + ".old");
            sb.AppendLine($"if exist \"{oldPath}\" del /F /Q \"{oldPath}\"");
        }
        sb.AppendLine();

        // 一時ファイルのクリーンアップ
        string zipPath = tempDir + ".zip";
        sb.AppendLine($"if exist \"{zipPath}\" del /F /Q \"{zipPath}\"");
        sb.AppendLine();

        // 一時ディレクトリ削除
        sb.AppendLine($"rd /S /Q \"{tempDir}\"");
        sb.AppendLine();

        // バッチ自身を削除して終了
        sb.AppendLine($"del /F /Q \"{batchPath}\"");
        sb.AppendLine("exit");

        return sb.ToString();
    }
}
