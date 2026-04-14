using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;

namespace Launcher.Infrastructure;

/// <summary>
/// URLのファビコンを取得してディスクにキャッシュする。
/// Google Favicon APIを使用し、キャッシュミス時のみHTTPリクエストを実行する。
/// </summary>
public sealed class FaviconCache
{
    // ソケット枯渇防止のため静的シングルトンで保持する
    static readonly HttpClient _httpClient = new();

    readonly string _cacheDir;

    public FaviconCache(string cacheDir)
    {
        _cacheDir = cacheDir;
        Directory.CreateDirectory(cacheDir);
    }

    /// <summary>
    /// 指定URLのファビコンを取得する。ディスクキャッシュを確認し、キャッシュミス時はHTTPで取得する。
    /// 失敗時はnullを返す。
    /// </summary>
    /// <param name="url">ファビコンを取得するURL</param>
    /// <param name="small">true=16px、false=32px</param>
    public System.Drawing.Icon? Get(string url, bool small)
    {
        try
        {
            var host = new Uri(url).Host;
            if (string.IsNullOrEmpty(host)) return null;

            // ホスト名にファイル名として使えない文字が含まれる場合は除去する
            var safeHost = string.Concat(host.Split(Path.GetInvalidFileNameChars()));
            var cachePath = Path.Combine(_cacheDir, $"{safeHost}.png");

            byte[] pngBytes;
            if (File.Exists(cachePath))
            {
                pngBytes = File.ReadAllBytes(cachePath);
            }
            else
            {
                pngBytes = FetchFromApi(host);
                // 競合書き込みに備えてIOExceptionは無視し、既存ファイルがあれば読み直す
                try
                {
                    File.WriteAllBytes(cachePath, pngBytes);
                }
                catch (IOException)
                {
                    if (File.Exists(cachePath))
                    {
                        pngBytes = File.ReadAllBytes(cachePath);
                    }
                }
            }

            return BitmapToIcon(pngBytes, small);
        }
#pragma warning disable CA1031 // 呼び出し元スレッド保護: ネットワーク・IO・画像デコードなど任意の例外でnullを返す
        catch (Exception e)
#pragma warning restore CA1031
        {
            System.Diagnostics.Debug.WriteLine($"ファビコン取得エラー ({url}): {e}");
            return null;
        }
    }

    /// <summary>
    /// Google Favicon APIからPNGバイト列を同期取得する。
    /// STAワーカースレッド上での呼び出しを前提とするため同期APIを使用する。
    /// </summary>
    static byte[] FetchFromApi(string host)
    {
        // 常に32pxで取得し、small要求時は呼び出し元でリサイズする
        var apiUrl = $"https://www.google.com/s2/favicons?domain={host}&sz=32";
        using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        using var response = _httpClient.Send(request);
        response.EnsureSuccessStatusCode();
        using var stream = response.Content.ReadAsStream();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// PNGバイト列をIconに変換する。
    /// GetHicon()で取得したGDIハンドルはClone()後にDestroyIcon()で明示解放する。
    /// </summary>
    static System.Drawing.Icon? BitmapToIcon(byte[] pngBytes, bool small)
    {
        using var ms = new MemoryStream(pngBytes);
        using var src = new Bitmap(ms);

        int size = small ? 16 : 32;
        // srcが既に目的サイズであれば再割り当てを避ける
        if (src.Width == size && src.Height == size)
        {
            return BitmapHandleToIcon(src);
        }
        using var resized = new Bitmap(src, new Size(size, size));
        return BitmapHandleToIcon(resized);
    }

    /// <summary>
    /// BitmapのGDIハンドルからIconを生成する。
    /// GetHicon()で取得したハンドルはClone()後にDestroyIcon()で明示解放する。
    /// </summary>
    static System.Drawing.Icon BitmapHandleToIcon(Bitmap bitmap)
    {
        var hIcon = bitmap.GetHicon();
        try
        {
            using var tempIcon = System.Drawing.Icon.FromHandle(hIcon);
            return (System.Drawing.Icon)tempIcon.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool DestroyIcon(IntPtr handle);
}
