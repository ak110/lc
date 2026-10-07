namespace Launcher.UI;

/// <summary>
/// System.Windows.Forms関係のヘルパー関数など。
/// </summary>
public static class FormsHelper
{
    /// <summary>
    /// owner が TopMost の Form の場合、子ダイアログも TopMost に揃えてから
    /// ShowDialog する。
    /// WinForms では親が TopMost、子が非 TopMost のとき、z-order の再評価時に
    /// 子が親の裏に回ることがあるため、その対処として使用する。
    /// </summary>
    public static DialogResult ShowDialogOver(this Form dialog, IWin32Window? owner)
    {
        if (owner is Form { TopMost: true })
        {
            dialog.TopMost = true;
        }
        return dialog.ShowDialog(owner);
    }

    /// <summary>
    /// クリッピングしてフォームの位置をセット
    /// </summary>
    public static void SetLocationWithClip(this Control form, Point pos)
    {
        double distance = double.MaxValue;
        Rectangle result = form.Bounds;
        foreach (Screen screen in Screen.AllScreens)
        {
            var candidate = ClipBounds(pos, form.Size, screen.WorkingArea);
            double dx = (double)candidate.X - pos.X;
            double dy = (double)candidate.Y - pos.Y;
            double candidateDistance = dx * dx + dy * dy;
            if (candidateDistance < distance)
            {
                distance = candidateDistance;
                result = candidate;
            }
        }
        form.Bounds = result;
    }

    /// <summary>カーソルのある画面内で、カーソルを中心にフォームを配置する。</summary>
    public static void CenterOnCursor(Control form)
    {
        Point cursor = Cursor.Position;
        var position = new Point(cursor.X - form.Width / 2, cursor.Y - form.Height / 2);
        form.Bounds = ClipBounds(position, form.Size, Screen.FromPoint(cursor).WorkingArea);
    }

    private static Rectangle ClipBounds(Point position, Size size, Rectangle area)
    {
        size = new Size(Math.Min(size.Width, area.Width), Math.Min(size.Height, area.Height));
        return new Rectangle(
            Math.Clamp(position.X, area.Left, area.Right - size.Width),
            Math.Clamp(position.Y, area.Top, area.Bottom - size.Height),
            size.Width, size.Height);
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
    /// コントロールから配列を取得する
    /// </summary>
    public static List<T> GetArray<T>(ListBox c)
    {
        List<T> array = [];
        foreach (object item in c.Items)
        {
            array.Add((T)item);
        }
        return array;
    }

    /// <summary>
    /// 選択中のアイテムの次の位置に追加する。
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
    /// 選択中のアイテムを上に移動する。
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
    /// 選択中のアイテムを下に移動する。
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
    /// 選択中のアイテムを削除する。
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

    #endregion
}
