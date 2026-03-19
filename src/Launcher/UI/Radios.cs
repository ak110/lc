namespace Launcher.UI;

/// <summary>
/// 複数のラジオボタンをまとめて扱うためのクラス。
/// </summary>
public sealed class Radios
{
    RadioButton[] radios;

    /// <summary>
    /// ラジオボタンから作成。
    /// </summary>
    public Radios(params RadioButton[] radios)
    {
        this.radios = radios;
    }

    /// <summary>
    /// GroupBoxやPanelなどから作成。
    /// タブオーダー順になるので、タブオーダーに注意。
    /// </summary>
    /// <param name="parent">RadioButtonの親パネルなど</param>
    /// <param name="radioCount">RadioButtonの数。Assert()するだけ。</param>
    public Radios(Control parent, int radioCount)
    {
        List<RadioButton> list = [];
        // ラジオボタンを(敢えて再帰はせずに1階層だけ)探す
        foreach (Control c in parent.Controls)
        {
            RadioButton? r = c as RadioButton;
            if (r != null)
            {
                list.Add(r);
            }
        }
        System.Diagnostics.Debug.Assert(list.Count == radioCount);
        // タブオーダーでソート
        list.Sort(delegate (RadioButton a, RadioButton b)
        {
            return a.TabIndex.CompareTo(b.TabIndex);
        });
        // 結果を格納
        radios = list.ToArray();
    }

    /// <summary>
    /// 値の取得・設定。
    /// </summary>
    public int Value
    {
        get
        {
            int n = -1;
            for (int i = 0; i < radios.Length; i++)
            {
                if (radios[i].Checked)
                {
                    System.Diagnostics.Debug.Assert(n == -1);
                    n = i;
                }
            }
            return n;
        }
        set
        {
            System.Diagnostics.Debug.Assert(0 <= value);
            System.Diagnostics.Debug.Assert(value < radios.Length);
            for (int i = 0; i < radios.Length; i++)
            {
                radios[i].Checked = i == value;
            }
        }
    }
}
