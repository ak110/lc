using Launcher.Core;
using Launcher.Infrastructure;

namespace Launcher.UI;

public partial class EditCommandForm : Form
{
    Command v;

    public EditCommandForm(Command vv)
    {
        InitializeComponent();

        v = vv;

        textBox1.Text = v.Name;
        textBox2.Text = v.FileName;
        textBox3.Text = v.Param;
        textBox4.Text = v.WorkDir ?? "";
        new Radios(groupBox1, 6).Value = (int)v.Show;
        new Radios(groupBox2, 6).Value = (int)v.Priority;
        checkBox1.Checked = v.RunAsAdmin;
    }

    private void buttonOk_Click(object? sender, EventArgs e)
    {
        v.Name = textBox1.Text;
        v.FileName = textBox2.Text;
        v.Param = textBox3.Text;
        v.WorkDir = textBox4.Text;
        v.Show = (WindowStyle)new Radios(groupBox1, 6).Value;
        v.Priority = (ProcessPriorityLevel)new Radios(groupBox2, 6).Value;
        v.RunAsAdmin = checkBox1.Checked;
    }

    private void button1_Click(object? sender, EventArgs e)
    {
        // PATH解決済みパスをダイアログの起点に設定する。
        // bare name (例: "notepad.exe") をアイコン表示と同じ解決順で扱い、操作時の挙動を揃える。
        string resolved = FileHelper.ResolveCommandPath(textBox2.Text);
        if (File.Exists(resolved))
        {
            openFileDialog1.FileName = resolved;
            string? dir = Path.GetDirectoryName(resolved);
            if (!string.IsNullOrEmpty(dir))
            {
                openFileDialog1.InitialDirectory = dir;
            }
        }
        if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
        {
            textBox2.Text = openFileDialog1.FileName;
        }
    }

    private void button2_Click(object? sender, EventArgs e)
    {
        string resolved = FileHelper.ResolveCommandPath(textBox4.Text);
        if (Directory.Exists(resolved))
        {
            folderBrowserDialog1.SelectedPath = resolved;
        }
        if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
        {
            textBox4.Text = folderBrowserDialog1.SelectedPath;
        }
    }
}
