using System.IO;
using Launcher.Infrastructure;

namespace Launcher.UI;

public partial class EnvConfigForm : Form
{
    List<string> replaceEnv;

    public List<string> ReplaceEnv
    {
        get { return replaceEnv; }
    }

    public EnvConfigForm(List<string> replaceEnv)
    {
        InitializeComponent();

        this.replaceEnv = [.. replaceEnv];
        FormsHelper.SetArray(listBox2, this.replaceEnv);

        foreach (System.Collections.DictionaryEntry p in Environment.GetEnvironmentVariables())
        {
            string? value = p.Value?.ToString();
            //try { value = FileName.GetFullPath(value); } catch { }
            if (value is not null && (Directory.Exists(value) || File.Exists(value)))
            {
                string? name = p.Key?.ToString();
                if (name is not null && this.replaceEnv.Contains(name))
                {
                    //listBox2.Items.Add(name);
                }
                else if (name is not null)
                {
                    listBox1.Items.Add(name);
                }
            }
        }
    }

    private void buttonOk_Click(object? sender, EventArgs e)
    {
        replaceEnv = FormsHelper.GetArray<string>(listBox2);
    }

    /// <summary>
    /// 追加
    /// </summary>
    private void button1_Click(object? sender, EventArgs e)
    {
        string? item = listBox1.SelectedItem as string;
        if (item is not null)
        {
            FormsHelper.RemoveSelected(listBox1);
            listBox2.Items.Add(item);
        }
    }

    /// <summary>
    /// 削除
    /// </summary>
    private void button2_Click(object? sender, EventArgs e)
    {
        string? item = listBox2.SelectedItem as string;
        if (item is not null)
        {
            FormsHelper.RemoveSelected(listBox2);
            listBox1.Items.Add(item);
        }
    }
}
