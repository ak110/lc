using FluentAssertions;
using Launcher.Win32;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// AsyncIconLoaderのスレッド管理・リトライ機構テスト
/// </summary>
public sealed class AsyncIconLoaderTests
{
    [Fact]
    public void Clear後にLoad可能()
    {
        using var loader = new AsyncIconLoader(workerCount: 1);
        List<IconLoadedEventArgs> receivedEvents = [];
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
        using var loader = new AsyncIconLoader(workerCount: 1);

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
        using var loader = new AsyncIconLoader(workerCount: 1);

        // 連続Clearが安全であること
        loader.Clear();
        loader.Clear();
        loader.Clear();
    }

    [Fact]
    public void Dispose後のLoadは無視される()
    {
        var loader = new AsyncIconLoader(workerCount: 1);
        loader.Dispose();

        // Dispose後のLoadは例外を投げずに無視されること
        loader.Load("test.exe", true, null);
    }

    [Fact]
    public void 複数回Disposeしても例外が発生しない()
    {
        var loader = new AsyncIconLoader(workerCount: 1);
        loader.Dispose();
        loader.Dispose();
    }

    [Fact]
    public void ThreadPriorityがコンストラクタで設定される()
    {
        using var loader = new AsyncIconLoader(workerCount: 1, threadPriority: ThreadPriority.Lowest);
        loader.ThreadPriority.Should().Be(ThreadPriority.Lowest);

        using var loader2 = new AsyncIconLoader(workerCount: 1, threadPriority: ThreadPriority.Highest);
        loader2.ThreadPriority.Should().Be(ThreadPriority.Highest);
    }

    [Fact]
    public void IconLoadedイベントに世代番号が含まれる()
    {
        using var loader = new AsyncIconLoader(workerCount: 1, extractIcon: (_, _) => null);
        List<IconLoadedEventArgs> events = [];
        loader.IconLoaded += (s, e) => events.Add(e);

        int gen = loader.Generation;
        loader.Load("test.exe", true, "test");

        // ワーカースレッドがイベントを発火するまで待つ
        Thread.Sleep(500);

        events.Should().ContainSingle();
        events[0].Generation.Should().Be(gen);
    }

    [Fact]
    public void Clear後の古い世代のリクエストは新しい世代と一致しない()
    {
        using var loader = new AsyncIconLoader(workerCount: 1);

        int gen0 = loader.Generation;
        loader.Load("dummy.exe", true, "arg1");
        loader.Clear();
        int gen1 = loader.Generation;

        // Clear後は世代が変わっているため、古いリクエストの世代と一致しない
        gen0.Should().NotBe(gen1);
    }

    [Fact]
    public void 大量のLoadとClearを繰り返しても例外が発生しない()
    {
        using var loader = new AsyncIconLoader(workerCount: 1);

        for (int i = 0; i < 100; i++)
        {
            loader.Load($"dummy{i}.exe", true, $"arg{i}");
        }
        loader.Clear();
        for (int i = 0; i < 100; i++)
        {
            loader.Load($"dummy{i}.exe", false, $"arg{i}");
        }
    }

    [Fact]
    public void 正常時はリトライなしで即座にイベント発火()
    {
        int callCount = 0;
        using var loader = new AsyncIconLoader(
            workerCount: 1,
            extractIcon: (_, _) =>
            {
                Interlocked.Increment(ref callCount);
                return null; // nullだがアイコン取得自体は成功扱い
            });
        List<IconLoadedEventArgs> events = [];
        loader.IconLoaded += (s, e) => events.Add(e);

        loader.Load("test.exe", true, "arg");
        Thread.Sleep(500);

        callCount.Should().Be(1);
        events.Should().ContainSingle();
    }

    [Fact]
    public void リトライにより最終的にアイコンが取得される()
    {
        int callCount = 0;
        using var loader = new AsyncIconLoader(
            workerCount: 1,
            extractIcon: (_, _) =>
            {
                int count = Interlocked.Increment(ref callCount);
                if (count == 1) throw new FileLoadException("1回目失敗");
                return null; // 2回目は成功（nullアイコン = 成功扱い）
            });
        using var done = new ManualResetEventSlim();
        List<IconLoadedEventArgs> events = [];
        loader.IconLoaded += (s, e) =>
        {
            events.Add(e);
            done.Set();
        };

        loader.Load("test.exe", true, "arg");
        done.Wait(TimeSpan.FromSeconds(3)).Should().BeTrue("イベントが発火されるべき");

        callCount.Should().Be(2);
        events.Should().ContainSingle();
    }

    [Fact]
    public void 最大リトライ回数超過でnullイベントが発火()
    {
        int callCount = 0;
        using var loader = new AsyncIconLoader(
            workerCount: 1,
            extractIcon: (_, _) =>
            {
                Interlocked.Increment(ref callCount);
                throw new FileLoadException("常に失敗");
            });
        using var done = new ManualResetEventSlim();
        List<IconLoadedEventArgs> events = [];
        loader.IconLoaded += (s, e) =>
        {
            events.Add(e);
            done.Set();
        };

        loader.Load("test.exe", true, "arg");
        done.Wait(TimeSpan.FromSeconds(3)).Should().BeTrue("リトライ上限後にイベントが発火されるべき");

        // 合計3回試行（初回 + リトライ2回）
        callCount.Should().Be(3);
        events.Should().ContainSingle();
        events[0].Icon.Should().BeNull();
    }

    [Fact]
    public void 大量Load時に全件イベント発火される()
    {
        const int count = 50;
        using var loader = new AsyncIconLoader(
            workerCount: 4,
            extractIcon: (_, _) => null);
        using var allDone = new CountdownEvent(count);
        List<IconLoadedEventArgs> events = [];
        loader.IconLoaded += (s, e) =>
        {
            lock (events)
            {
                events.Add(e);
            }
            allDone.Signal();
        };

        for (int i = 0; i < count; i++)
        {
            loader.Load($"file{i}.exe", true, i);
        }

        allDone.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue("全件イベントが発火されるべき");
        events.Should().HaveCount(count);
    }

    [Fact]
    public void ワーカー数のカスタマイズ()
    {
        // workerCount: 2 で正常動作すること
        using var loader = new AsyncIconLoader(
            workerCount: 2,
            extractIcon: (_, _) => null);
        using var done = new ManualResetEventSlim();
        loader.IconLoaded += (s, e) => done.Set();

        loader.Load("test.exe", true, null);
        done.Wait(TimeSpan.FromSeconds(3)).Should().BeTrue("イベントが発火されるべき");
    }
}
