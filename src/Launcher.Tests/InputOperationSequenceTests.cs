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
}
