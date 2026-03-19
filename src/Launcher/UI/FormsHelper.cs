using System.ComponentModel;
using System.Runtime.InteropServices;
using Launcher.Win32;

namespace Launcher.UI;

/// <summary>
/// System.Windows.Forms関係のヘルパー関数など。
/// </summary>
public static class FormsHelper
{
    /// <summary>
    /// フォームをカーソルがあるモニターの中央に配置する。
    /// ShowDialog()の前に呼ぶとStartPositionをManualに変更して位置を設定する。
    /// </summary>
    public static void CenterOnCursorScreen(Form form)
    {
        Screen screen = Screen.FromPoint(Cursor.Position);
        Rectangle wa = screen.WorkingArea;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(
            wa.Left + (wa.Width - form.Width) / 2,
            wa.Top + (wa.Height - form.Height) / 2);
    }

    /// <summary>
    /// クリッピングしてフォームの位置をセット
    /// </summary>
    public static void SetLocationWithClip(Control form, Point pos)
    {
        int posR = pos.X + form.Width;
        int posB = pos.Y + form.Height;
        int dist = int.MaxValue;
        var result = new Point();
        foreach (Screen screen in Screen.AllScreens)
        {
            Rectangle wa = screen.WorkingArea;
            var clipped = new Point();
            if (pos.X < wa.Left)
            {
                clipped.X = wa.Left;
            }
            else if (wa.Right < posR)
            {
                clipped.X = pos.X - (posR - wa.Right);
            }
            else
            {
                clipped.X = pos.X;
            }
            if (pos.Y < wa.Top)
            {
                clipped.Y = wa.Top;
            }
            else if (wa.Bottom < posB)
            {
                clipped.Y = pos.Y - (posB - wa.Bottom);
            }
            else
            {
                clipped.Y = pos.Y;
            }
            int clippedDist =
                (clipped.X - pos.X) * (clipped.X - pos.X) +
                (clipped.Y - pos.Y) * (clipped.Y - pos.Y);
            // 元の位置に近ければ採用
            if (clippedDist < dist)
            {
                dist = clippedDist;
                result = clipped;
            }
        }
        // 移動
        form.Location = result;
    }

    /// <summary>
    /// フォームの閉じるボタンを無効にする
    /// </summary>
    public static void DisableCloseButton(Control form)
    {
        form.Resize += new EventHandler(form_Resize);
        EnableMenuItem(GetSystemMenu(form.Handle, false), SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
    }

    /// <summary>
    /// フォームの閉じるボタンを有効に戻す
    /// </summary>
    public static void EnableCloseButton(Control form)
    {
        form.Resize -= new EventHandler(form_Resize);
        EnableMenuItem(GetSystemMenu(form.Handle, false), SC_CLOSE, MF_BYCOMMAND | MF_ENABLED);
    }

    #region 閉じるボタンを無効にするためのWinAPIなど。

    static void form_Resize(object? sender, EventArgs e)
    {
        // 最大化とかした時に戻っちゃう気がするので再設定
        Control? form = sender as Control;
        if (form != null)
            EnableMenuItem(GetSystemMenu(form.Handle, false), SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
    }

    [DllImport("user32.dll")]
    static extern IntPtr GetSystemMenu(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bRevert);

    const uint SC_CLOSE = 0x0000F060u;
    const uint MF_BYCOMMAND = 0x00000000u;
    const uint MF_BYPOSITION = 0x00000400u;
    const uint MF_SEPARATOR = 0x00000800u;
    const uint MF_ENABLED = 0x00000000u;
    const uint MF_GRAYED = 0x00000001u;
    const uint MF_DISABLED = 0x00000002u;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

    #endregion

    /// <summary>
    /// 強制的にアクティブにする
    /// </summary>
    public static void ActivateForce(Form form)
    {
        using (var ati = new AttachThreadInput())
        {
            int time = 0;
            try
            {
                SystemParametersInfo(SPI_GETFOREGROUNDLOCKTIMEOUT, 0, ref time, 0);
                SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, IntPtr.Zero, 0);
            }
            catch (Win32Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            Application.DoEvents();
            form.Visible = true;
            bool topMost = form.TopMost;
            form.TopMost = true;
            form.BringToFront();
            Application.DoEvents();
            form.Focus();
            form.Activate();
            Application.DoEvents();

            if (time != 0)
            {
                try
                {
                    SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, ref time, 0);
                }
                catch (Win32Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                }
            }
        }
    }

    #region WinAPI for ActivateForce

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SystemParametersInfo(int uiAction, int uiParam, IntPtr pvParam, int fWinIni);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SystemParametersInfo(int uiAction, int uiParam, ref int pvParam, int fWinIni);

    const int SPI_GETFOREGROUNDLOCKTIMEOUT = 0x2000;
    const int SPI_SETFOREGROUNDLOCKTIMEOUT = 0x2001;

    #endregion

    #region Find

    /// <summary>
    /// ノードを再帰的に探す。
    /// </summary>
    public static TreeNode? Find(TreeView tree, Predicate<TreeNode> predict)
    {
        foreach (TreeNode node in tree.Nodes)
        {
            TreeNode? ret = Find(node, predict);
            if (ret != null) return ret;
        }
        return null;
    }
    /// <summary>
    /// ノードを再帰的に探す。
    /// </summary>
    public static TreeNode? Find(TreeNode node, Predicate<TreeNode> predict)
    {
        if (predict(node)) return node;
        foreach (TreeNode child in node.Nodes)
        {
            TreeNode? posterity = Find(child, predict);
            if (posterity != null) return posterity;
        }
        return null;
    }

    /// <summary>
    /// アイテムを探す。
    /// </summary>
    public static ListViewItem? Find(ListView listView, Predicate<ListViewItem> predict)
    {
        foreach (ListViewItem item in listView.Items)
        {
            if (predict(item)) return item;
        }
        return null;
    }

    #endregion

    #region RemoveAll

    /// <summary>
    /// ノードを再帰的に探して全て削除。
    /// </summary>
    public static int RemoveAll(TreeView tree, Predicate<TreeNode> predict)
    {
        int count = 0;
        for (int i = 0; i < tree.Nodes.Count;)
        {
            if (predict(tree.Nodes[i]))
            {
                tree.Nodes.RemoveAt(i);
                count++;
            }
            else
            {
                i++;
            }
        }
        foreach (TreeNode n in tree.Nodes)
        {
            count += RemoveAll(n, predict);
        }
        return count;
    }
    /// <summary>
    /// ノードを再帰的に探して全て削除。
    /// </summary>
    public static int RemoveAll(TreeNode node, Predicate<TreeNode> predict)
    {
        int count = 0;
        for (int i = 0; i < node.Nodes.Count;)
        {
            if (predict(node.Nodes[i]))
            {
                node.Nodes.RemoveAt(i);
                count++;
            }
            else
            {
                i++;
            }
        }
        foreach (TreeNode n in node.Nodes)
        {
            count += RemoveAll(n, predict);
        }
        return count;
    }

    /// <summary>
    /// アイテムを探して全て削除。
    /// </summary>
    public static int RemoveAll(ListView listView, Predicate<ListViewItem> predict)
    {
        int count = 0;
        for (int i = 0; i < listView.Items.Count;)
        {
            if (predict(listView.Items[i]))
            {
                listView.Items.RemoveAt(i);
                count++;
            }
            else
            {
                i++;
            }
        }
        return count;
    }

    #endregion

    #region ドラッグ＆ドロップ関連用ヘルパ

    /// <summary>
    /// DragEnter, DragOverでe.Effectを設定する処理。
    /// </summary>
    public static void SetDropEffect(DragEventArgs e, DragDropEffects defaultEffect)
    {
        if ((e.AllowedEffect & DragDropEffects.Copy) != 0 && (e.KeyState & 0x8) != 0)
        { // ctrlキー
            e.Effect = DragDropEffects.Copy;
        }
        else if ((e.AllowedEffect & DragDropEffects.Move) != 0 && (e.KeyState & 0x4) != 0)
        { // shiftキー
            e.Effect = DragDropEffects.Move;
        }
        else if ((e.AllowedEffect & defaultEffect) != 0)
        { // デフォルト
            e.Effect = defaultEffect;
        }
        else
        { // 該当なし
            e.Effect = DragDropEffects.None;
        }
    }

    /// <summary>
    /// DragDropイベントの処理時に、ドロップ先のノードを取得する処理。
    /// </summary>
    public static TreeNode? GetDropTarget(TreeView treeView, DragEventArgs e)
    {
        Point p = treeView.PointToClient(new Point(e.X, e.Y));
        return treeView.GetNodeAt(p.X, p.Y);
    }
    /// <summary>
    /// DragDropイベントの処理時に、ドロップ先のノードを取得する処理。
    /// </summary>
    public static ListViewItem? GetDropTarget(ListView listView, DragEventArgs e)
    {
        Point p = listView.PointToClient(new Point(e.X, e.Y));
        return listView.GetItemAt(p.X, p.Y);
    }
    /// <summary>
    /// DragDropイベントの処理時に、ドロップ先のノードを取得する処理。
    /// </summary>
    public static object? GetDropTarget(ListBox listBox, DragEventArgs e)
    {
        int? n = GetDropTargetIndex(listBox, e);
        return n.HasValue ? listBox.Items[n.Value] : null;
    }
    /// <summary>
    /// DragDropイベントの処理時に、ドロップ先のノードを取得する処理。
    /// </summary>
    public static int? GetDropTargetIndex(ListBox listBox, DragEventArgs e)
    {
        Point p = listBox.PointToClient(new Point(e.X, e.Y));
        int n = listBox.IndexFromPoint(p);
        return n == ListBox.NoMatches ? (int?)null : n;
    }

    #endregion

    /// <summary>
    /// 再帰的にEnabledプロパティを設定
    /// </summary>
    public static void SetEnabled(Control control, bool enabled)
    {
        control.Enabled = enabled;
        foreach (Control c in control.Controls)
        {
            SetEnabled(c, enabled);
        }
    }

    #region リストボックス・コンボボックスなど

    /// <summary>
    /// 配列をコントロールにセットする
    /// </summary>
    public static void SetArray<T>(ListBox c, List<T> a) where T : ICloneable
    {
        InnerSetArray(c, a.ConvertAll(x => x.Clone()).ToArray());
    }
    static void InnerSetArray(ListBox c, object[] a)
    {
        int n = c.SelectedIndex;
        c.BeginUpdate();
        c.Items.Clear();
        c.Items.AddRange(a);
        if (0 < a.Length) c.SelectedIndex = Math.Min(Math.Max(n, 0), a.Length - 1);
        c.EndUpdate();
    }
    /// <summary>
    /// 配列をコントロールにセットする
    /// </summary>
    public static void SetArray<T>(ComboBox c, List<T> a) where T : ICloneable
    {
        InnerSetArray(c, a.ConvertAll(x => x.Clone()).ToArray());
    }
    static void InnerSetArray(ComboBox c, object[] a)
    {
        int n = c.SelectedIndex;
        c.BeginUpdate();
        c.Items.Clear();
        c.Items.AddRange(a);
        if (0 < a.Length) c.SelectedIndex = Math.Min(Math.Max(n, 0), a.Length - 1);
        c.EndUpdate();
    }

    /// <summary>
    /// コントロールから配列を取得する
    /// </summary>
    public static List<T> GetArray<T>(ListBox c)
    {
        List<T> array = new List<T>();
        foreach (object item in c.Items)
        {
            array.Add((T)item);
        }
        return array;
    }

    /// <summary>
    /// コントロールから配列を取得する
    /// </summary>
    public static List<T> GetArray<T>(ComboBox c)
    {
        List<T> array = new List<T>();
        foreach (object item in c.Items)
        {
            array.Add((T)item);
        }
        return array;
    }

    /// <summary>
    /// 選択してる次の位置に追加
    /// </summary>
    public static void Insert(ListBox listBox, object item)
    {
        System.Diagnostics.Debug.Assert(listBox.SelectionMode == SelectionMode.One);
        if (0 < listBox.Items.Count)
        {
            int i = Math.Min(Math.Max(listBox.SelectedIndex + 1, 0),
                listBox.Items.Count);
            listBox.Items.Insert(i, item);
        }
        else
        {
            listBox.Items.Add(item);
        }
        listBox.SelectedItem = item;
    }

    /// <summary>
    /// 選択してるアイテムを上に移動
    /// </summary>
    public static void UpSelected(ListBox listBox)
    {
        System.Diagnostics.Debug.Assert(listBox.SelectionMode == SelectionMode.One);
        int i = listBox.SelectedIndex;
        if (0 <= i - 1 && i < listBox.Items.Count)
        {
            object item = listBox.Items[i];
            listBox.Items[i] = listBox.Items[i - 1];
            listBox.Items[i - 1] = item;
            listBox.SelectedItem = item;
        }
    }

    /// <summary>
    /// 選択してるアイテムを下に移動
    /// </summary>
    public static void DownSelected(ListBox listBox)
    {
        System.Diagnostics.Debug.Assert(listBox.SelectionMode == SelectionMode.One);
        int i = listBox.SelectedIndex;
        if (0 <= i && i + 1 < listBox.Items.Count)
        {
            object item = listBox.Items[i];
            listBox.Items[i] = listBox.Items[i + 1];
            listBox.Items[i + 1] = item;
            listBox.SelectedItem = item;
        }
    }

    /// <summary>
    /// 選択してるアイテムを削除
    /// </summary>
    public static void RemoveSelected(ListBox listBox)
    {
        System.Diagnostics.Debug.Assert(listBox.SelectionMode == SelectionMode.One);
        int i = listBox.SelectedIndex;
        if (0 <= i && i < listBox.Items.Count)
        {
            listBox.Items.RemoveAt(i);
            int n = Math.Min(Math.Max(i, 0), listBox.Items.Count - 1);
            if (0 <= n && n < listBox.Items.Count)
            {
                listBox.SelectedIndex = n;
            }
        }
    }

    /// <summary>
    /// リストボックスをカスタムな方法でソート。
    /// </summary>
    public static void Sort<T>(ListBox listBox, Comparison<T> cmp)
    {
        System.Diagnostics.Debug.Assert(listBox.SelectionMode == SelectionMode.One);
        listBox.BeginUpdate();
        try
        {
            object? selected = listBox.SelectedItem;
            object[] objs = new object[listBox.Items.Count];
            listBox.Items.CopyTo(objs, 0);
            Array.Sort(objs, delegate (object a, object b)
            {
                return cmp((T)a, (T)b);
            });
            listBox.Items.Clear();
            listBox.Items.AddRange(objs);
            listBox.SelectedItem = selected;
        }
        finally
        {
            listBox.EndUpdate();
        }
    }

    #endregion
}
