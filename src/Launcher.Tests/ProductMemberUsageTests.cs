using FluentAssertions;
using Launcher.Core;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

public sealed class ProductMemberUsageTests
{
    [Fact]
    public void 製品アセンブリは本体から使われない非privateのメンバーを持たない()
    {
        // テストからの参照は使用に数えない。走査するのは、テストの出力先へ複製された2つの製品アセンブリのメソッド本体だけである。
        var unused = ProductMemberUsage.FindUnusedMembers(
            new[] { typeof(ApplicationHostForm), typeof(Command) }
                .Select(type => Path.Combine(AppContext.BaseDirectory, type.Assembly.GetName().Name + ".dll")));

        unused.Should().BeEmpty(
            "本体から使われない定義は削除する。ILに参照が現れない利用の区分が増えた場合は、ProductMemberUsageへ理由とともに加える");
    }
}
