using Launcher.Core;

namespace Launcher.UI;

public partial class EditCommandForm : Form
{
    Command v;

#if CLONE
    public Command Value {
        get { return v; }
    }
#endif

    public EditCommandForm(Command vv)
    {
        InitializeComponent();

#if CLONE
        v = vv.Clone();
#else
        v = vv;
#endif

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
        if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
        {
            textBox2.Text = openFileDialog1.FileName;
        }
    }

    private void button2_Click(object? sender, EventArgs e)
    {
        if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
        {
            textBox4.Text = folderBrowserDialog1.SelectedPath;
        }
    }
}
