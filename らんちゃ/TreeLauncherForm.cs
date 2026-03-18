using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace らんちゃ {
    /// <summary>
    /// ツリーランチャー
    /// </summary>
    public partial class TreeLauncherForm : Form {
        DummyForm owner;

        public TreeLauncherForm(DummyForm owner) {
            InitializeComponent();
            this.owner = owner;
            Show(owner);
            Hide();

            int build = System.Diagnostics.Process.GetCurrentProcess()
                .MainModule.FileVersionInfo.FileBuildPart;
            Text = "らんちゃ build-" + build;
            
            UpdateTree();
        }

        /// <summary>
        /// ツリーランチャーを表示する
        /// </summary>
        public void ShowLauncher() {
            Hide();
            Show();
            Activate();
            BringToFront();
            listView1.SelectedIndices.Clear();
            foreach (var s in Screen.AllScreens) {
                if (s.WorkingArea.Contains(Cursor.Position)) {
                    Location = new Point(
                        s.WorkingArea.X + (s.WorkingArea.Width - Width) / 2,
                        s.WorkingArea.Y + (s.WorkingArea.Height - Height) / 2);
                    break;
                }
            }
        }

        private void listView1_Click(object sender, EventArgs e) {

        }

        private void listView1_DragOver(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effect = DragDropEffects.Link;
            }
        }

        private void listView1_DragDrop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                foreach (string file in (string[])e.Data.GetData(DataFormats.FileDrop)) {
                    owner.TreeCommandList.Add(new Command() { FileName = file });
                }
                owner.TreeCommandList.Serialize(".treecmd.cfg");
                UpdateTree();
            }
        }

        /// <summary>
        /// ツリーを更新する
        /// </summary>
        private void UpdateTree() {
        }
    }
}
