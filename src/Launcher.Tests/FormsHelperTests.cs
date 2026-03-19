using FluentAssertions;
using Launcher.UI;
using Xunit;

namespace Launcher.Tests;

/// <summary>
/// FormsHelperのテスト（Win32 API非依存の関数のみ）
/// </summary>
public sealed class FormsHelperTests
{
    // --- Find (TreeView) ---

    [Fact]
    public void Find_TreeViewからルートノードを検索できる()
    {
        using var tree = new TreeView();
        tree.Nodes.Add("root1");
        tree.Nodes.Add("target");

        var result = FormsHelper.Find(tree, n => n.Text == "target");

        result.Should().NotBeNull();
        result!.Text.Should().Be("target");
    }

    [Fact]
    public void Find_TreeViewから子孫ノードを再帰的に検索できる()
    {
        using var tree = new TreeView();
        var root = tree.Nodes.Add("root");
        var child = root.Nodes.Add("child");
        child.Nodes.Add("grandchild");

        var result = FormsHelper.Find(tree, n => n.Text == "grandchild");

        result.Should().NotBeNull();
        result!.Text.Should().Be("grandchild");
    }

    [Fact]
    public void Find_TreeViewで該当なしならnull()
    {
        using var tree = new TreeView();
        tree.Nodes.Add("a");

        var result = FormsHelper.Find(tree, n => n.Text == "none");

        result.Should().BeNull();
    }

    // --- Find (ListView) ---

    [Fact]
    public void Find_ListViewからアイテムを検索できる()
    {
        using var lv = new ListView();
        lv.Items.Add("item1");
        lv.Items.Add("target");

        var result = FormsHelper.Find(lv, item => item.Text == "target");

        result.Should().NotBeNull();
        result!.Text.Should().Be("target");
    }

    [Fact]
    public void Find_ListViewで該当なしならnull()
    {
        using var lv = new ListView();
        lv.Items.Add("a");

        var result = FormsHelper.Find(lv, item => item.Text == "none");

        result.Should().BeNull();
    }

    // --- RemoveAll (TreeView) ---

    [Fact]
    public void RemoveAll_TreeViewからルートノードを条件削除できる()
    {
        using var tree = new TreeView();
        tree.Nodes.Add("keep");
        tree.Nodes.Add("remove1");
        tree.Nodes.Add("remove2");

        int count = FormsHelper.RemoveAll(tree, n => n.Text.StartsWith("remove"));

        count.Should().Be(2);
        tree.Nodes.Count.Should().Be(1);
        tree.Nodes[0].Text.Should().Be("keep");
    }

    [Fact]
    public void RemoveAll_TreeViewから子孫ノードも再帰的に削除できる()
    {
        using var tree = new TreeView();
        var root = tree.Nodes.Add("root");
        root.Nodes.Add("keep");
        root.Nodes.Add("remove");

        int count = FormsHelper.RemoveAll(tree, n => n.Text == "remove");

        count.Should().Be(1);
        root.Nodes.Count.Should().Be(1);
    }

    // --- RemoveAll (ListView) ---

    [Fact]
    public void RemoveAll_ListViewからアイテムを条件削除できる()
    {
        using var lv = new ListView();
        lv.Items.Add("keep");
        lv.Items.Add("remove1");
        lv.Items.Add("remove2");

        int count = FormsHelper.RemoveAll(lv, item => item.Text.StartsWith("remove"));

        count.Should().Be(2);
        lv.Items.Count.Should().Be(1);
    }

    // --- SetEnabled ---

    [Fact]
    public void SetEnabled_子コントロールも再帰的にEnabled設定される()
    {
        using var parent = new Panel();
        using var child = new Panel();
        using var grandchild = new Button();
        parent.Controls.Add(child);
        child.Controls.Add(grandchild);

        FormsHelper.SetEnabled(parent, false);

        parent.Enabled.Should().BeFalse();
        child.Enabled.Should().BeFalse();
        grandchild.Enabled.Should().BeFalse();
    }

    [Fact]
    public void SetEnabled_trueで全て有効に戻る()
    {
        using var parent = new Panel();
        using var child = new Button();
        parent.Controls.Add(child);
        parent.Enabled = false;
        child.Enabled = false;

        FormsHelper.SetEnabled(parent, true);

        parent.Enabled.Should().BeTrue();
        child.Enabled.Should().BeTrue();
    }

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

    // --- Sort (ListBox) ---

    [Fact]
    public void Sort_カスタム比較でソートできる()
    {
        using var lb = new ListBox();
        lb.Items.AddRange(new object[] { "c", "a", "b" });
        lb.SelectedIndex = 0; // "c"を選択

        FormsHelper.Sort<string>(lb, (a, b) => string.Compare(a, b, StringComparison.Ordinal));

        lb.Items[0].Should().Be("a");
        lb.Items[1].Should().Be("b");
        lb.Items[2].Should().Be("c");
        // ソート後も元のアイテム("c")が選択されている
        lb.SelectedItem.Should().Be("c");
    }
}
