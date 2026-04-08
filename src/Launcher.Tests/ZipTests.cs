using System.IO.Compression;
using FluentAssertions;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// ZipReader / ZipEntry のテスト
/// </summary>
public sealed class ZipTests
{
    /// <summary>
    /// テスト用ZIPバイト列を作成するヘルパー
    /// </summary>
    private static byte[] CreateTestZip(params (string Name, byte[] Content)[] files)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var (name, content) in files)
            {
                var entry = archive.CreateEntry(name);
                using var es = entry.Open();
                es.Write(content, 0, content.Length);
            }
        }
        return ms.ToArray();
    }

    // --- ZipEntry ---

    [Fact]
    public void ZipEntry_ファイル名のスラッシュがバックスラッシュに変換される()
    {
        var entry = new ZipEntry("dir/subdir/file.txt");

        entry.Name.Should().Be(@"dir\subdir\file.txt");
    }

    [Fact]
    public void ZipEntry_ディレクトリ判定_末尾スラッシュ()
    {
        var dirEntry = new ZipEntry("folder/");
        dirEntry.IsDirectory.Should().BeTrue();
        dirEntry.IsFile.Should().BeFalse();
    }

    [Fact]
    public void ZipEntry_ディレクトリ判定_末尾バックスラッシュ()
    {
        var dirEntry = new ZipEntry("folder\\");
        dirEntry.IsDirectory.Should().BeTrue();
    }

    [Fact]
    public void ZipEntry_ファイル判定()
    {
        var fileEntry = new ZipEntry("file.txt");
        fileEntry.IsFile.Should().BeTrue();
        fileEntry.IsDirectory.Should().BeFalse();
    }

    [Fact]
    public void ZipEntry_SizeとCompressedSizeを設定取得できる()
    {
        var entry = new ZipEntry("test.txt")
        {
            Size = 12345,
            CompressedSize = 6789,
        };

        entry.Size.Should().Be(12345);
        entry.CompressedSize.Should().Be(6789);
    }

    [Fact]
    public void ZipEntry_DosTime日時変換_往復()
    {
        var entry = new ZipEntry("test.txt");
        var dt = new DateTime(2024, 6, 15, 10, 30, 20);
        entry.DateTime = dt;

        // DOS時刻は2秒単位なので偶数秒に丸められる
        entry.DateTime.Year.Should().Be(2024);
        entry.DateTime.Month.Should().Be(6);
        entry.DateTime.Day.Should().Be(15);
        entry.DateTime.Hour.Should().Be(10);
        entry.DateTime.Minute.Should().Be(30);
        entry.DateTime.Second.Should().Be(20);
    }

    [Fact]
    public void ZipEntry_DosTimeゼロはDateTime_Nowに近い値を返す()
    {
        var entry = new ZipEntry("test.txt") { DosTime = 0 };

        // DosTime == 0 の場合は DateTime.Now を返す
        entry.DateTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ZipEntry_Clone_独立したコピーを作成する()
    {
        var original = new ZipEntry("file.txt") { Size = 100 };
        var clone = original.Clone();

        clone.Name.Should().Be(original.Name);
        clone.Size.Should().Be(100);

        // 変更が独立していること
        clone.Size = 999;
        original.Size.Should().Be(100);
    }

    [Fact]
    public void ZipEntry_ToString_名前とサイズを含む()
    {
        var entry = new ZipEntry("test.txt") { Size = 42 };

        entry.ToString().Should().Be("test.txt (42)");
    }

    [Fact]
    public void ZipEntry_DosTime奇数秒は切り捨てられる()
    {
        var entry = new ZipEntry("test.txt");
        // DOS時刻は2秒単位なので奇数秒は切り捨て
        entry.DateTime = new DateTime(2020, 1, 1, 0, 0, 13);
        entry.DateTime.Second.Should().Be(12);
    }

    // --- ZipReader ---

    [Fact]
    public void ZipReader_バイト配列からエントリ一覧を読み取れる()
    {
        byte[] zip = CreateTestZip(
            ("file1.txt", "hello"u8.ToArray()),
            ("file2.txt", "world"u8.ToArray()));

        using var reader = new ZipReader(zip);

        reader.Count.Should().Be(2);
        reader.Entries.Should().HaveCount(2);
    }

    [Fact]
    public void ZipReader_エントリのファイル名を正しく読み取れる()
    {
        byte[] zip = CreateTestZip(("data.bin", new byte[] { 1, 2, 3 }));

        using var reader = new ZipReader(zip);

        reader.Entries[0].Name.Should().Be("data.bin");
    }

    [Fact]
    public void ZipReader_ファイルサイズを正しく読み取れる()
    {
        var content = new byte[256];
        for (int i = 0; i < content.Length; i++)
            content[i] = (byte)(i & 0xFF);
        byte[] zip = CreateTestZip(("test.bin", content));

        using var reader = new ZipReader(zip);

        reader.Entries[0].Size.Should().Be(256);
    }

    [Fact]
    public void ZipReader_ReadAllBytesでファイル内容を読み取れる()
    {
        var content = "Hello, ZIP!"u8.ToArray();
        byte[] zip = CreateTestZip(("msg.txt", content));

        using var reader = new ZipReader(zip);
        byte[] data = reader.ReadAllBytes(0);

        data.Should().Equal(content);
    }

    [Fact]
    public void ZipReader_複数ファイルの内容をそれぞれ読み取れる()
    {
        var c1 = "first"u8.ToArray();
        var c2 = "second"u8.ToArray();
        byte[] zip = CreateTestZip(("a.txt", c1), ("b.txt", c2));

        using var reader = new ZipReader(zip);

        reader.ReadAllBytes(0).Should().Equal(c1);
        reader.ReadAllBytes(1).Should().Equal(c2);
    }

    [Fact]
    public void ZipReader_Openでストリーム経由で読み取れる()
    {
        var content = "stream test"u8.ToArray();
        byte[] zip = CreateTestZip(("s.txt", content));

        using var reader = new ZipReader(zip);
        using var stream = reader.Open(0);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);

        ms.ToArray().Should().Equal(content);
    }

    [Fact]
    public void ZipReader_Streamコンストラクタで読み取れる()
    {
        var content = "from stream"u8.ToArray();
        byte[] zip = CreateTestZip(("test.txt", content));

        using var ms = new MemoryStream(zip);
        using var reader = new ZipReader(ms);

        reader.Count.Should().Be(1);
        reader.ReadAllBytes(0).Should().Equal(content);
    }

    [Fact]
    public void ZipReader_leaveOpenがtrueならストリームを閉じない()
    {
        byte[] zip = CreateTestZip(("test.txt", "x"u8.ToArray()));
        var ms = new MemoryStream(zip);

        using (var reader = new ZipReader(ms, true))
        {
            reader.Count.Should().Be(1);
        }

        // leaveOpen=true のためストリームはまだ利用可能である。
        ms.CanRead.Should().BeTrue();
        ms.Dispose();
    }

    [Fact]
    public void ZipReader_不正なデータでIOExceptionをスロー()
    {
        byte[] invalidData = [0x00, 0x01, 0x02, 0x03];

        var act = () => new ZipReader(invalidData);

        act.Should().Throw<IOException>();
    }

    [Fact]
    public void ZipReader_Extractでファイルに解凍できる()
    {
        var content = "extracted content"u8.ToArray();
        byte[] zip = CreateTestZip(("sub/file.txt", content));
        string tempDir = Path.Combine(Path.GetTempPath(), "ZipTests_" + Guid.NewGuid().ToString("N"));

        try
        {
            using var reader = new ZipReader(zip);
            string outPath = Path.Combine(tempDir, "output.txt");
            reader.Extract(outPath, 0);

            File.Exists(outPath).Should().BeTrue();
            File.ReadAllBytes(outPath).Should().Equal(content);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ZipReader_空のZIPを読み取れる()
    {
        byte[] zip = CreateTestZip();

        using var reader = new ZipReader(zip);

        reader.Count.Should().Be(0);
        reader.Entries.Should().BeEmpty();
    }
}
