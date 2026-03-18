using FluentAssertions;
using Launcher.Win32;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// AsyncIconLoaderのスレッド管理テスト
/// </summary>
public class AsyncIconLoaderTests
{
    [Fact]
    public void Clear後にLoad可能()
    {
        using var loader = new AsyncIconLoader();
        var receivedEvents = new List<IconLoadedEventArgs>();
        loader.IconLoaded += (s, e) => receivedEvents.Add(e);

        // Load → Clear → 再Load のサイクルが例外なく動作すること
        loader.Load("dummy.exe", true, "arg1");
        loader.Clear();
        loader.Load("dummy2.exe", false, "arg2");
        loader.Clear();

        // 例外が発生しなければOK（アイコン取得自体はファイルが存在しないため失敗する可能性あり）
    }

    [Fact]
    public void Clearで世代番号がインクリメントされる()
    {
        using var loader = new AsyncIconLoader();

        int gen0 = loader.Generation;
        loader.Clear();
        int gen1 = loader.Generation;
        loader.Clear();
        int gen2 = loader.Generation;

        gen1.Should().Be(gen0 + 1);
        gen2.Should().Be(gen1 + 1);
    }

    [Fact]
    public void 複数回Clearしても例外が発生しない()
    {
        using var loader = new AsyncIconLoader();

        // 連続Clearが安全であること
        loader.Clear();
        loader.Clear();
        loader.Clear();
    }

    [Fact]
    public void Dispose後のLoadは無視される()
    {
        var loader = new AsyncIconLoader();
        loader.Dispose();

        // Dispose後のLoadは例外を投げずに無視されること
        loader.Load("test.exe", true, null);
    }

    [Fact]
    public void 複数回Disposeしても例外が発生しない()
    {
        var loader = new AsyncIconLoader();
        loader.Dispose();
        loader.Dispose();
    }

    [Fact]
    public void ThreadPriorityの取得と設定()
    {
        using var loader = new AsyncIconLoader();

        loader.ThreadPriority = ThreadPriority.Lowest;
        loader.ThreadPriority.Should().Be(ThreadPriority.Lowest);

        loader.ThreadPriority = ThreadPriority.Highest;
        loader.ThreadPriority.Should().Be(ThreadPriority.Highest);
    }

    [Fact]
    public void IconLoadedイベントに世代番号が含まれる()
    {
        using var loader = new AsyncIconLoader();
        var events = new List<IconLoadedEventArgs>();
        loader.IconLoaded += (s, e) => events.Add(e);

        int gen = loader.Generation;
        // 存在しないファイルをロード（ワーカーがFileLoadExceptionで処理）
        loader.Load("nonexistent_file_12345.exe", true, "test");

        // ワーカースレッドがイベントを発火するまで少し待つ
        Thread.Sleep(500);

        // イベントが発火した場合、世代番号が正しいこと
        foreach (var e in events)
        {
            e.Generation.Should().Be(gen);
        }
    }
}
