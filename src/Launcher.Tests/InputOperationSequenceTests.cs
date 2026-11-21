using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

public sealed class InputOperationSequenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ドラッグ終了後は開始位置と開始元を引き継がない(bool restartBeforeReset)
    {
        var drag = new DragDropState();
        var entry = new ButtonEntry();
        var tab = new ButtonTab();
        var start = new Point(100, 100);
        var size = new Size(8, 8);

        for (int i = 0; i < 3; i++)
        {
            drag.IsActive.Should().BeFalse();
            drag.DragEntry.Should().BeNull();
            drag.SourceTab.Should().BeNull();
            drag.ShouldBeginDrag(start, size).Should().BeFalse();

            drag.Start(entry, tab, start);
            drag.IsActive.Should().BeTrue();
            drag.DragEntry.Should().BeSameAs(entry);
            drag.SourceTab.Should().BeSameAs(tab);
            drag.ShouldBeginDrag(new Point(104, 100), size).Should().BeFalse();
            drag.ShouldBeginDrag(new Point(105, 100), size).Should().BeTrue();

            if (restartBeforeReset)
            {
                entry = new ButtonEntry();
                tab = new ButtonTab();
                drag.Start(entry, tab, Point.Empty);
                drag.DragEntry.Should().BeSameAs(entry);
                drag.SourceTab.Should().BeSameAs(tab);
                drag.ShouldBeginDrag(start, size).Should().BeTrue();
            }

            drag.Reset();
            drag.Reset();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void 長押しは現行の押下中に一度だけ発火する(bool cancelBeforeFire)
    {
        var state = new LongPressOperationState();
        state.TryFire(0).Should().BeFalse();
        var previous = state.Begin();

        for (int i = 0; i < 3; i++)
        {
            var current = state.Begin();
            state.TryFire(previous).Should().BeFalse();
            state.ConsumeClickSuppression().Should().BeFalse();
            if (cancelBeforeFire) state.Cancel();
            state.TryFire(current).Should().Be(!cancelBeforeFire);
            state.TryFire(current).Should().BeFalse();
            state.Cancel();
            state.ConsumeClickSuppression().Should().Be(!cancelBeforeFire);
            state.ConsumeClickSuppression().Should().BeFalse();
            state.TryFire(current).Should().BeFalse();
            previous = current;
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void 同時押しとホットキーの抑止は対応する解放またはリセットで終わる(bool firstLeft, bool reset)
    {
        var state = new HookInputState();
        var activation = firstLeft ? ButtonLauncherActivation.LeftThenRight : ButtonLauncherActivation.RightThenLeft;

        for (int i = 0; i < 3; i++)
        {
            state.ProcessMouse(!firstLeft, false, activation).Should().Be((false, false));
            state.ConsumeKeyUp(65).Should().BeFalse();
            state.ProcessMouse(firstLeft, true, activation).Should().Be((false, false));
            state.ProcessMouse(!firstLeft, true, activation).Should().Be((true, true));
            state.ProcessMouse(!firstLeft, true, activation).Should().Be((true, false));
            state.TryBeginHotkey(65).Should().BeTrue();
            state.ConsumeKeyUp(66).Should().BeFalse();
            state.TryBeginHotkey(65).Should().BeFalse();

            if (reset) state.Reset();
            state.ProcessMouse(firstLeft, false, activation).Should().Be((false, false));
            state.ProcessMouse(!firstLeft, false, activation).Should().Be((!reset, false));
            state.ConsumeKeyUp(65).Should().Be(!reset);
        }
    }

    [Fact]
    public void 生成したドラッグ操作列の各辺で開始元と閾値が一致する()
    {
        var random = new Random(3971);
        var state = new DragDropState();
        var entries = new[] { new ButtonEntry(), new ButtonEntry() };
        var tabs = new[] { new ButtonTab(), new ButtonTab() };
        var origins = new[] { new Point(0, 0), new Point(100, 0) };
        var size = new Size(8, 8);
        int? active = null;

        for (int step = 0; step < 500; step++)
        {
            int operation = random.Next(4);
            switch (operation)
            {
                case 0:
                case 1:
                    state.Start(entries[operation], tabs[operation], origins[operation]);
                    active = operation;
                    break;
                case 2:
                    state.Reset();
                    active = null;
                    break;
            }

            state.IsActive.Should().Be(active.HasValue, "手順 {0}", step);
            state.DragEntry.Should().BeSameAs(active is int entry ? entries[entry] : null);
            state.SourceTab.Should().BeSameAs(active is int tab ? tabs[tab] : null);
            for (int probe = 0; probe < origins.Length; probe++)
                state.ShouldBeginDrag(origins[probe], size).Should().Be(active.HasValue && active != probe);
        }
    }

    [Fact]
    public void 生成した長押し操作列では現行の押下だけが発火し抑止を一度返す()
    {
        var random = new Random(3972);
        var state = new LongPressOperationState();
        var ids = new List<long> { 0 };
        long? pressing = null;
        bool clickOwed = false;

        for (int step = 0; step < 500; step++)
        {
            switch (random.Next(5))
            {
                case 0:
                    long id = state.Begin();
                    ids.Should().NotContain(id);
                    ids.Add(id);
                    pressing = id;
                    clickOwed = false;
                    break;
                case 1:
                    state.Cancel();
                    pressing = null;
                    break;
                case 2:
                case 3:
                    long candidate = random.Next(2) == 0 ? ids[^1] : ids[random.Next(ids.Count)];
                    bool allowed = candidate == pressing;
                    state.TryFire(candidate).Should().Be(allowed, "手順 {0}", step);
                    if (allowed)
                    {
                        pressing = null;
                        clickOwed = true;
                    }
                    break;
                case 4:
                    state.ConsumeClickSuppression().Should().Be(clickOwed, "手順 {0}", step);
                    clickOwed = false;
                    break;
            }
        }
        state.ConsumeClickSuppression().Should().Be(clickOwed);
        state.ConsumeClickSuppression().Should().BeFalse();
    }

    [Fact]
    public void 生成した入力操作列では解放の抑止を対応する押下からだけ引き継ぐ()
    {
        var random = new Random(3973);
        var state = new HookInputState();
        var held = new HashSet<bool>();
        var releaseOwed = new HashSet<bool>();
        int? hotkey = null;

        for (int step = 0; step < 1000; step++)
        {
            int operation = random.Next(8);
            bool left = random.Next(2) == 0;
            int key = left ? 65 : 66;
            var order = random.Next(2) == 0 ? ButtonLauncherActivation.LeftThenRight :
                ButtonLauncherActivation.RightThenLeft;

            switch (operation)
            {
                case 0:
                    state.Reset();
                    held.Clear();
                    releaseOwed.Clear();
                    hotkey = null;
                    break;
                case 1:
                    state.ProcessMouse(left, false, ButtonLauncherActivation.Disabled).Should().Be((false, false));
                    held.Clear();
                    releaseOwed.Clear();
                    break;
                case 2:
                case 3:
                    bool alreadyOwed = releaseOwed.Contains(left);
                    bool activate = !alreadyOwed && held.Contains(!left) &&
                        left == (order == ButtonLauncherActivation.RightThenLeft);
                    state.ProcessMouse(left, true, order).Should().Be((alreadyOwed || activate, activate), "手順 {0}", step);
                    held.Add(left);
                    if (activate) releaseOwed.Add(left);
                    break;
                case 4:
                case 5:
                    state.ProcessMouse(left, false, order).Should().Be((releaseOwed.Remove(left), false), "手順 {0}", step);
                    held.Remove(left);
                    break;
                case 6:
                    state.TryBeginHotkey(key).Should().Be(!hotkey.HasValue, "手順 {0}", step);
                    hotkey ??= key;
                    break;
                case 7:
                    state.ConsumeKeyUp(key).Should().Be(hotkey == key, "手順 {0}", step);
                    if (hotkey == key) hotkey = null;
                    break;
            }
        }
    }
}
