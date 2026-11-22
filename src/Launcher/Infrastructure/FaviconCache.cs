using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Launcher.Infrastructure;

public sealed class FaviconCache
{
    readonly string _cacheDir;
    readonly Action<string, string, string> warn;

    public FaviconCache(string cacheDir, Action<string, string, string>? warningLogger = null)
    {
        _cacheDir = cacheDir;
        warn = warningLogger ?? DiagnosticLog.Warn;
        Directory.CreateDirectory(cacheDir);
    }

    public System.Drawing.Icon? Get(string url, bool small)
    {
        try
        {
            var host = new Uri(url).Host;
            if (string.IsNullOrEmpty(host)) return null;

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
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            warn("Favicon", $"取得失敗: {e.GetType().Name}", "対応不要（アイコンなしで表示を継続した）");
            return null;
        }
    }

    /// <summary>
    /// Google Favicon APIからPNGバイト列を同期取得する。
    /// STAワーカースレッド上での呼び出しを前提とするため同期APIを使用する。
    /// </summary>
    static byte[] FetchFromApi(string host)
    {
        var apiUrl = $"https://www.google.com/s2/favicons?domain={host}&sz=32";
        using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        using var response = SharedHttpClient.Instance.Send(request);
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
