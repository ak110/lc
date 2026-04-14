using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;

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
    /// GetHicon()はαチャネルを失うため、ICO形式でラップして<see cref="System.Drawing.Icon"/>で読み込む。
    /// </summary>
    static System.Drawing.Icon? BitmapToIcon(byte[] pngBytes, bool small)
    {
        int size = small ? 16 : 32;

        using var srcMs = new MemoryStream(pngBytes);
        using var src = new Bitmap(srcMs);

        // srcが既に目的サイズであれば再割り当てを避ける
        if (src.Width == size && src.Height == size)
        {
            return WrapPngInIco(pngBytes, size, size);
        }

        // Format32bppArgbを明示してリサイズし、αチャネルを保持する
        using var resized = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(resized))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(src, 0, 0, size, size);
        }
        using var resizedMs = new MemoryStream();
        resized.Save(resizedMs, ImageFormat.Png);
        return WrapPngInIco(resizedMs.ToArray(), size, size);
    }

    /// <summary>
    /// PNGバイト列をICOファイル形式でラップして<see cref="System.Drawing.Icon"/>を生成する。
    /// PNG圧縮ICOはWindows Vista以降でサポートされており、αチャネルが正しく保持される。
    /// </summary>
    static System.Drawing.Icon WrapPngInIco(byte[] pngBytes, int width, int height)
    {
        // ICOファイル形式: ICONDIR(6バイト) + ICONDIRENTRY(16バイト) + 画像データ
        using var icoStream = new MemoryStream();
        using var writer = new BinaryWriter(icoStream, System.Text.Encoding.UTF8, leaveOpen: true);

        // ICONDIR
        writer.Write((short)0);    // reserved
        writer.Write((short)1);    // type = icon
        writer.Write((short)1);    // count = 1

        // ICONDIRENTRY
        writer.Write((byte)(width >= 256 ? 0 : width));
        writer.Write((byte)(height >= 256 ? 0 : height));
        writer.Write((byte)0);     // color count
        writer.Write((byte)0);     // reserved
        writer.Write((short)1);    // planes
        writer.Write((short)32);   // bit count
        writer.Write((int)pngBytes.Length);
        writer.Write((int)22);     // image offset = 6(ICONDIR) + 16(ICONDIRENTRY)

        // 画像データ(PNG形式のまま埋め込む)
        writer.Write(pngBytes);
        writer.Flush();

        icoStream.Seek(0, SeekOrigin.Begin);
        return new System.Drawing.Icon(icoStream);
    }
}
