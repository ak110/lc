using FluentAssertions;
using Launcher.Core;
using Xunit;

namespace Launcher.Tests;

public sealed class HookInputStateTests
{
    [Fact]
    public void 自注入のキー解放は物理状態を変えず左右両方の解放で修飾を解除する()
    {
        var state = new HookInputState();
        state.UpdateModifier(PhysicalModifierKey.LeftControl, true, false);
        state.UpdateModifier(PhysicalModifierKey.RightControl, true, false);
        state.UpdateModifier(PhysicalModifierKey.LeftControl, false, true);
        state.MatchesHotkey(77, 77, InputModifiers.Control).Should().BeTrue();
        state.UpdateModifier(PhysicalModifierKey.LeftControl, false, false);
        state.Modifiers.Should().Be(InputModifiers.Control);
        state.UpdateModifier(PhysicalModifierKey.RightControl, false, false);
        state.Modifiers.Should().Be(InputModifiers.None);
    }

    [Fact]
    public void ホットキーは完全一致で発火しリピートを抑え解放後に再発火する()
    {
        var state = new HookInputState();
        state.UpdateModifier(PhysicalModifierKey.LeftAlt, true, false);
        state.MatchesHotkey(77, 78, InputModifiers.Alt).Should().BeFalse();
        state.MatchesHotkey(77, 77, InputModifiers.Control).Should().BeFalse();
        state.MatchesHotkey(77, 77, InputModifiers.Alt).Should().BeTrue();
        state.TryBeginHotkey(77).Should().BeTrue();
        state.TryBeginHotkey(77).Should().BeFalse();
        state.ConsumeKeyUp(78).Should().BeFalse();
        state.ConsumeKeyUp(77).Should().BeTrue();
        state.ConsumeKeyUp(77).Should().BeFalse();
        state.TryBeginHotkey(77).Should().BeTrue();
        state.Reset();
        state.Modifiers.Should().Be(InputModifiers.None);
        state.ConsumeKeyUp(77).Should().BeFalse();
    }

    [Theory]
    [InlineData(ButtonLauncherActivation.LeftThenRight, true)]
    [InlineData(ButtonLauncherActivation.RightThenLeft, false)]
    public void 指定順の押下で起動しトリガーの解放だけを一度抑止する(ButtonLauncherActivation activation, bool firstLeft)
    {
        var state = new HookInputState();
        state.ProcessMouse(firstLeft, true, activation).Should().Be((false, false));
        state.ProcessMouse(!firstLeft, true, activation).Should().Be((true, true));
        state.ProcessMouse(!firstLeft, false, activation).Should().Be((true, false));
        state.ProcessMouse(!firstLeft, false, activation).Should().Be((false, false));
        state.ProcessMouse(firstLeft, false, activation).Should().Be((false, false));
    }

    [Fact]
    public void 無効化とリセット後は押下状態を引き継がない()
    {
        var state = new HookInputState();
        state.ProcessMouse(true, true, ButtonLauncherActivation.Disabled).Should().Be((false, false));
        state.ProcessMouse(false, true, ButtonLauncherActivation.LeftThenRight).Should().Be((false, false));
        state.ProcessMouse(true, true, ButtonLauncherActivation.LeftThenRight);
        state.Reset();
        state.ProcessMouse(false, true, ButtonLauncherActivation.LeftThenRight).Should().Be((false, false));
    }
}
