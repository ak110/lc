using FluentAssertions;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// FormsHelperのテスト (Win32 API非依存の関数のみ)
/// </summary>
public sealed class FormsHelperTests
{
    // --- Insert (ListBox) ---

    [Fact]
    public void Insert_空のリストボックスに追加できる()
    {
        using var lb = new ListBox();

        FormsHelper.Insert(lb, "item1");

        lb.Items.Count.Should().Be(1);
        lb.Items[0].Should().Be("item1");
        lb.SelectedItem.Should().Be("item1");
    }

    [Fact]
    public void Insert_選択位置の次に挿入される()
    {
        using var lb = new ListBox();
        lb.Items.Add("a");
        lb.Items.Add("c");
        lb.SelectedIndex = 0;

        FormsHelper.Insert(lb, "b");

        lb.Items.Count.Should().Be(3);
        lb.Items[1].Should().Be("b");
        lb.SelectedItem.Should().Be("b");
    }

    // --- UpSelected (ListBox) ---

    [Fact]
    public void UpSelected_選択アイテムを一つ上に移動する()
    {
        using var lb = new ListBox();
        lb.Items.AddRange(new object[] { "a", "b", "c" });
        lb.SelectedIndex = 1;

        FormsHelper.UpSelected(lb);

        lb.Items[0].Should().Be("b");
        lb.Items[1].Should().Be("a");
        lb.Items[2].Should().Be("c");
        lb.SelectedItem.Should().Be("b");
    }

    [Fact]
    public void UpSelected_先頭の場合は何も変わらない()
    {
        using var lb = new ListBox();
        lb.Items.AddRange(new object[] { "a", "b" });
        lb.SelectedIndex = 0;

        FormsHelper.UpSelected(lb);

        lb.Items[0].Should().Be("a");
        lb.Items[1].Should().Be("b");
    }

    // --- DownSelected (ListBox) ---

    [Fact]
    public void DownSelected_選択アイテムを一つ下に移動する()
    {
        using var lb = new ListBox();
        lb.Items.AddRange(new object[] { "a", "b", "c" });
        lb.SelectedIndex = 1;

        FormsHelper.DownSelected(lb);

        lb.Items[0].Should().Be("a");
        lb.Items[1].Should().Be("c");
        lb.Items[2].Should().Be("b");
        lb.SelectedItem.Should().Be("b");
    }

    [Fact]
    public void DownSelected_末尾の場合は何も変わらない()
    {
        using var lb = new ListBox();
        lb.Items.AddRange(new object[] { "a", "b" });
        lb.SelectedIndex = 1;

        FormsHelper.DownSelected(lb);

        lb.Items[0].Should().Be("a");
        lb.Items[1].Should().Be("b");
    }

    // --- RemoveSelected (ListBox) ---

    [Fact]
    public void RemoveSelected_選択アイテムを削除する()
    {
        using var lb = new ListBox();
        lb.Items.AddRange(new object[] { "a", "b", "c" });
        lb.SelectedIndex = 1;

        FormsHelper.RemoveSelected(lb);

        lb.Items.Count.Should().Be(2);
        lb.Items[0].Should().Be("a");
        lb.Items[1].Should().Be("c");
    }

    [Fact]
    public void RemoveSelected_末尾削除後に前のアイテムが選択される()
    {
        using var lb = new ListBox();
        lb.Items.AddRange(new object[] { "a", "b" });
        lb.SelectedIndex = 1;

        FormsHelper.RemoveSelected(lb);

        lb.Items.Count.Should().Be(1);
        lb.SelectedIndex.Should().Be(0);
    }

}
