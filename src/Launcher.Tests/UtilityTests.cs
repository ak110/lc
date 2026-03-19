using FluentAssertions;
using Launcher.Infrastructure;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// SubStream ユーティリティのテスト
/// </summary>
public class UtilityTests
{
    // --- SubStream ---

    [Fact]
    public void SubStream_部分読み取り()
    {
        // 元データ: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]
        byte[] data = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        using var parent = new MemoryStream(data);
        // offset=3, size=4 → [3, 4, 5, 6]
        using var sub = new SubStream(parent, 3, 4);

        sub.Length.Should().Be(4);
        sub.Position.Should().Be(0);

        byte[] buffer = new byte[4];
        int read = sub.Read(buffer, 0, 4);

        read.Should().Be(4);
        buffer.Should().Equal(3, 4, 5, 6);
    }

    [Fact]
    public void SubStream_Lengthが正しい()
    {
        byte[] data = new byte[100];
        using var parent = new MemoryStream(data);
        using var sub = new SubStream(parent, 10, 50);

        sub.Length.Should().Be(50);
    }

    [Fact]
    public void SubStream_Positionの取得と設定()
    {
        byte[] data = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        using var parent = new MemoryStream(data);
        using var sub = new SubStream(parent, 2, 6);

        sub.Position.Should().Be(0);

        sub.Position = 3;
        sub.Position.Should().Be(3);

        // 1バイト読み取り: offset=2+3=5 → data[5]=5
        byte[] buffer = new byte[1];
        sub.ReadExactly(buffer, 0, 1);
        buffer[0].Should().Be(5);
    }

    [Fact]
    public void SubStream_範囲外のPosition設定で例外()
    {
        byte[] data = new byte[10];
        using var parent = new MemoryStream(data);
        using var sub = new SubStream(parent, 0, 5);

        var act = () => sub.Position = 5;
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SubStream_負のPosition設定で例外()
    {
        byte[] data = new byte[10];
        using var parent = new MemoryStream(data);
        using var sub = new SubStream(parent, 0, 5);

        var act = () => sub.Position = -1;
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SubStream_末尾を超える読み取りはクリップされる()
    {
        byte[] data = { 0, 1, 2, 3, 4 };
        using var parent = new MemoryStream(data);
        using var sub = new SubStream(parent, 2, 3); // [2, 3, 4]

        // 10バイト要求しても3バイトだけ返る
        byte[] buffer = new byte[10];
        int read = sub.Read(buffer, 0, 10);

        read.Should().Be(3);
        buffer[0].Should().Be(2);
        buffer[1].Should().Be(3);
        buffer[2].Should().Be(4);
    }

    [Fact]
    public void SubStream_CanWriteはfalse()
    {
        byte[] data = new byte[10];
        using var parent = new MemoryStream(data);
        using var sub = new SubStream(parent, 0, 5);

        sub.CanWrite.Should().BeFalse();
    }

    [Fact]
    public void SubStream_Writeは例外()
    {
        byte[] data = new byte[10];
        using var parent = new MemoryStream(data);
        using var sub = new SubStream(parent, 0, 5);

        var act = () => sub.Write(new byte[1], 0, 1);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void SubStream_SetLengthは例外()
    {
        byte[] data = new byte[10];
        using var parent = new MemoryStream(data);
        using var sub = new SubStream(parent, 0, 5);

        var act = () => sub.SetLength(10);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void SubStream_Seekで各SeekOriginが動作する()
    {
        byte[] data = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        using var parent = new MemoryStream(data);
        using var sub = new SubStream(parent, 2, 6);

        // SeekOrigin.Begin
        sub.Seek(2, SeekOrigin.Begin);
        sub.Position.Should().Be(2);

        // SeekOrigin.Current
        sub.Seek(1, SeekOrigin.Current);
        sub.Position.Should().Be(3);

        // SeekOrigin.End (Length - offset)
        sub.Seek(1, SeekOrigin.End);
        sub.Position.Should().Be(5);
    }

    [Fact]
    public void SubStream_位置0からの連続読み取り()
    {
        byte[] data = { 10, 20, 30, 40, 50, 60, 70, 80 };
        using var parent = new MemoryStream(data);
        using var sub = new SubStream(parent, 1, 5); // [20, 30, 40, 50, 60]

        // 2バイトずつ読む
        byte[] buf = new byte[2];
        sub.Read(buf, 0, 2).Should().Be(2);
        buf.Should().Equal(20, 30);

        sub.Read(buf, 0, 2).Should().Be(2);
        buf.Should().Equal(40, 50);

        sub.Read(buf, 0, 2).Should().Be(1);
        buf[0].Should().Be(60);

        // もう読めない
        sub.Read(buf, 0, 2).Should().Be(0);
    }
}
