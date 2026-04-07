using System.Collections;
using System.ComponentModel;

namespace Launcher.UI;

/// <summary>
/// ラジオボタンのリストを管理するコントロール。
/// </summary>
[DefaultProperty("Items")]
[DefaultEvent("SelectedIndexChanged")]
public partial class RadioButtonList : UserControl
{
    object lockObject = new();
    int selectedIndex;
    List<RadioButton> items = [];

    public RadioButtonList()
    {
        InitializeComponent();
    }

    void radioButton_CheckedChanged(object? sender, EventArgs e)
    {
        RadioButton r = (RadioButton)sender!;
        if (r.Checked)
        {
            int index = items.IndexOf(r);
            if (selectedIndex != index)
            {
                selectedIndex = index;
                // イベントのコールバック
                if (SelectedIndexChanged is not null)
                {
                    SelectedIndexChanged(this, EventArgs.Empty);
                }
            }
        }
    }

    /// <summary>
    /// 項目のインデックス
    /// </summary>
    [DefaultValue(0)]
    public int SelectedIndex
    {
        get { return selectedIndex; }
        set
        {
            lock (lockObject)
            {
                if (selectedIndex != value)
                {
                    if (value == -1)
                    {
                        items[selectedIndex].Checked = false;
                    }
                    else
                    {
                        items[value].Checked = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// SelectedIndex変更されたイベント
    /// </summary>
    public event EventHandler? SelectedIndexChanged;

    /// <summary>
    /// 選択された項目
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public object? SelectedItem
    {
        get
        {
            lock (lockObject)
            {
                if (0 <= selectedIndex && selectedIndex < items.Count)
                {
                    return items[selectedIndex].Tag;
                }
            }
            return null;
        }
        set
        {
            lock (lockObject)
            {
                SelectedIndex = items.FindIndex(delegate (RadioButton r) { return r.Tag?.Equals(value) == true; });
            }
        }
    }

    /// <summary>
    /// 項目のリスト
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public object?[] Items
    {
        get
        {
            object?[] array = new object?[items.Count];
            new ObjectCollection(this).CopyTo(array, 0);
            return array;
        }
        set
        {
            lock (lockObject)
            {
                items.Clear();
                new ObjectCollection(this).AddRange(value);
            }
        }
    }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string?[] StringItems
    {
        get { return Array.ConvertAll(Items, x => x?.ToString()); }
        set { Items = value!; }
    }

    /// <summary>
    /// ラジオボタンのレイアウトを更新
    /// </summary>
    void UpdateLayout()
    {
        lock (lockObject)
        {
            Controls.Clear();
            int maxWidth = 8, lastBottom = 0;
            foreach (RadioButton r in items)
            {
                r.TabIndex = Controls.Count;
                r.Location = new Point(0, lastBottom);
                Controls.Add(r);
                lastBottom = r.Bottom;
                if (maxWidth < r.Right) maxWidth = r.Right;
            }
            if (lastBottom <= 0) lastBottom = 8;
            ClientSize = new Size(maxWidth, lastBottom);
            PerformLayout();
        }
    }

    /// <summary>
    /// Itemsの型
    /// </summary>
    [ListBindable(false)]
    public class ObjectCollection : IList, ICollection, IEnumerable
    {
        RadioButtonList owner;

        public ObjectCollection(RadioButtonList owner)
        {
            this.owner = owner;
        }

        public int Count
        {
            get { return owner.items.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        [DesignerSerializationVisibility(0)]
        [Browsable(false)]
        public virtual object? this[int index]
        {
            get
            {
                lock (owner.lockObject)
                {
                    if (index < 0 || owner.items.Count <= index)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    return owner.items[index].Tag;
                }
            }
            set
            {
                lock (owner.lockObject)
                {
                    if (index < 0 || owner.items.Count <= index)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    owner.items[index].Tag = value!;
                    owner.items[index].Text = value?.ToString() ?? "";
                }
            }
        }

        public int Add(object? item)
        {
            ArgumentNullException.ThrowIfNull(item);
            lock (owner.lockObject)
            {
                owner.items.Add(CreateRadioButton(item));
                owner.UpdateLayout();
                return owner.items.Count - 1;
            }
        }

        public void AddRange(ObjectCollection items)
        {
            foreach (object v in items)
            {
                Add(v);
            }
        }

        public void AddRange(object?[] items)
        {
            foreach (object? v in items)
            {
                Add(v);
            }
        }

        public virtual void Clear()
        {
            lock (owner.lockObject)
            {
                foreach (RadioButton r in owner.items) r.Dispose();
                owner.items.Clear();
            }
        }

        public bool Contains(object? value)
        {
            return owner.items.Exists(delegate (RadioButton r) { return r.Tag?.Equals(value) == true; });
        }

        public void CopyTo(object?[] destination, int arrayIndex)
        {
            for (int i = 0; i < owner.items.Count; i++)
            {
                destination[arrayIndex + i] = owner.items[i].Tag;
            }
        }

        public IEnumerator GetEnumerator()
        {
            foreach (RadioButton item in owner.items)
            {
                yield return item.Tag;
            }
        }

        public int IndexOf(object? value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return owner.items.FindIndex(delegate (RadioButton r) { return r.Tag?.Equals(value) == true; });
        }

        public void Insert(int index, object? item)
        {
            ArgumentNullException.ThrowIfNull(item);
            lock (owner.lockObject)
            {
#pragma warning disable CA2000 // Controls コレクションがRadioButtonのライフサイクルを管理
                owner.items.Insert(index, CreateRadioButton(item));
#pragma warning restore CA2000
                owner.UpdateLayout();
            }
        }

        public void Remove(object? value)
        {
            Remove(IndexOf(value));
        }

        public void RemoveAt(int index)
        {
            lock (owner.lockObject)
            {
                owner.items.RemoveAt(index);
                owner.UpdateLayout();
            }
        }

        #region IList メンバ

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        #endregion

        #region ICollection メンバ

        void ICollection.CopyTo(Array array, int index)
        {
            for (int i = 0; i < owner.items.Count; i++)
            {
                array.SetValue(owner.items[i].Tag, index + i);
            }
        }

        bool ICollection.IsSynchronized
        {
            get { return true; }
        }

        object ICollection.SyncRoot
        {
            get { return owner.lockObject; }
        }

        #endregion

        private RadioButton CreateRadioButton(object item)
        {
            var r = new RadioButton();
            r.Tag = item;
            r.Text = item.ToString() ?? "";
            r.Checked = owner.items.Count == owner.selectedIndex;
            r.AutoSize = true;
            r.UseVisualStyleBackColor = true;
            r.TabStop = true;
            r.CheckedChanged += new EventHandler(owner.radioButton_CheckedChanged);
            return r;
        }
    }
}
