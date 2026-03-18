#nullable disable
namespace Launcher.UI
{
    partial class ButtonLauncherForm
    {
        private System.ComponentModel.IContainer components = null;

        // Disposeはコードビハインド側で定義

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.lockButton = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            //
            // tabControl1
            //
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 25);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(500, 475);
            this.tabControl1.TabIndex = 0;
            this.tabControl1.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.tabControl1_MouseWheel);
            //
            // toolStrip1
            //
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lockButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(500, 25);
            this.toolStrip1.TabIndex = 1;
            //
            // lockButton
            //
            this.lockButton.CheckOnClick = true;
            this.lockButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.lockButton.Name = "lockButton";
            this.lockButton.Size = new System.Drawing.Size(35, 22);
            this.lockButton.Text = "Lock";
            this.lockButton.Click += new System.EventHandler(this.lockButton_Click);
            //
            // ButtonLauncherForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 500);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.toolStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "ButtonLauncherForm";
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton lockButton;
    }
}
