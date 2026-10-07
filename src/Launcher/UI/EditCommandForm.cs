using Launcher.Core;
using Launcher.Win32;

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
        launchOptions.WindowStyle = v.Show;
        launchOptions.Priority = v.Priority;
        checkBox1.Checked = v.RunAsAdmin;
    }

    private void buttonOk_Click(object? sender, EventArgs e)
    {
        v.Name = textBox1.Text;
        v.FileName = textBox2.Text;
        v.Param = textBox3.Text;
        v.WorkDir = textBox4.Text;
        v.Show = launchOptions.WindowStyle;
        v.Priority = launchOptions.Priority;
        v.RunAsAdmin = checkBox1.Checked;
    }

    private void button1_Click(object? sender, EventArgs e)
    {
        CommandFileBrowser.Browse(this, textBox2, openFileDialog1);
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
