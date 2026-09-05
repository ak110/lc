#nullable disable
namespace Launcher.UI
{
    partial class MemoForm
    {
        private System.ComponentModel.IContainer components = null;

        // Disposeはコードビハインド側で定義

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.newTabMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renameTabMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeTabMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.restoreClosedTabMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.closeWindowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.undoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.redoMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.cutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.findMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.findNextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.findPrevMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.replaceMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editSep3 = new System.Windows.Forms.ToolStripSeparator();
            this.selectAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.formatMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.fontMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            //
            // menuStrip1
            //
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.fileMenu,
                this.editMenu,
                this.formatMenu});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(500, 24);
            this.menuStrip1.TabIndex = 1;
            //
            // fileMenu
            //
            this.fileMenu.Name = "fileMenu";
            this.fileMenu.Text = "ファイル(&F)";
            this.fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.newTabMenuItem,
                this.renameTabMenuItem,
                this.closeTabMenuItem,
                this.restoreClosedTabMenuItem,
                this.fileSep1,
                this.closeWindowMenuItem});
            //
            // newTabMenuItem
            //
            this.newTabMenuItem.Name = "newTabMenuItem";
            this.newTabMenuItem.Text = "新しいタブ(&N)";
            this.newTabMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T;
            //
            // renameTabMenuItem
            //
            this.renameTabMenuItem.Name = "renameTabMenuItem";
            this.renameTabMenuItem.Text = "タブ名の変更(&R)";
            //
            // closeTabMenuItem
            //
            this.closeTabMenuItem.Name = "closeTabMenuItem";
            this.closeTabMenuItem.Text = "タブを閉じる(&C)";
            this.closeTabMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W;
            //
            // restoreClosedTabMenuItem
            //
            this.restoreClosedTabMenuItem.Name = "restoreClosedTabMenuItem";
            this.restoreClosedTabMenuItem.Text = "閉じたタブを戻す(&U)";
            //
            // fileSep1
            //
            this.fileSep1.Name = "fileSep1";
            //
            // closeWindowMenuItem
            //
            this.closeWindowMenuItem.Name = "closeWindowMenuItem";
            this.closeWindowMenuItem.Text = "閉じる(&X)";
            //
            // editMenu
            //
            this.editMenu.Name = "editMenu";
            this.editMenu.Text = "編集(&E)";
            this.editMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.undoMenuItem,
                this.redoMenuItem,
                this.editSep1,
                this.cutMenuItem,
                this.copyMenuItem,
                this.pasteMenuItem,
                this.editSep2,
                this.findMenuItem,
                this.findNextMenuItem,
                this.findPrevMenuItem,
                this.replaceMenuItem,
                this.editSep3,
                this.selectAllMenuItem});
            //
            // undoMenuItem
            //
            this.undoMenuItem.Name = "undoMenuItem";
            this.undoMenuItem.Text = "元に戻す(&U)";
            this.undoMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z;
            //
            // redoMenuItem
            //
            this.redoMenuItem.Name = "redoMenuItem";
            this.redoMenuItem.Text = "やり直し(&R)";
            this.redoMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y;
            //
            // editSep1
            //
            this.editSep1.Name = "editSep1";
            //
            // cutMenuItem
            //
            this.cutMenuItem.Name = "cutMenuItem";
            this.cutMenuItem.Text = "切り取り(&T)";
            this.cutMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X;
            //
            // copyMenuItem
            //
            this.copyMenuItem.Name = "copyMenuItem";
            this.copyMenuItem.Text = "コピー(&C)";
            this.copyMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C;
            //
            // pasteMenuItem
            //
            this.pasteMenuItem.Name = "pasteMenuItem";
            this.pasteMenuItem.Text = "貼り付け(&P)";
            this.pasteMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V;
            //
            // editSep2
            //
            this.editSep2.Name = "editSep2";
            //
            // findMenuItem
            //
            this.findMenuItem.Name = "findMenuItem";
            this.findMenuItem.Text = "検索(&F)...";
            this.findMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F;
            //
            // findNextMenuItem
            //
            this.findNextMenuItem.Name = "findNextMenuItem";
            this.findNextMenuItem.Text = "次を検索(&N)";
            this.findNextMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F3;
            //
            // findPrevMenuItem
            //
            this.findPrevMenuItem.Name = "findPrevMenuItem";
            this.findPrevMenuItem.Text = "前を検索(&V)";
            this.findPrevMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F3;
            //
            // replaceMenuItem
            //
            this.replaceMenuItem.Name = "replaceMenuItem";
            this.replaceMenuItem.Text = "置換(&H)...";
            this.replaceMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.H;
            //
            // editSep3
            //
            this.editSep3.Name = "editSep3";
            //
            // selectAllMenuItem
            //
            this.selectAllMenuItem.Name = "selectAllMenuItem";
            this.selectAllMenuItem.Text = "すべて選択(&A)";
            this.selectAllMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A;
            //
            // formatMenu
            //
            this.formatMenu.Name = "formatMenu";
            this.formatMenu.Text = "書式(&O)";
            this.formatMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.fontMenuItem});
            //
            // fontMenuItem
            //
            this.fontMenuItem.Name = "fontMenuItem";
            this.fontMenuItem.Text = "フォント(&F)...";
            //
            // tabControl1
            //
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 24);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(500, 376);
            this.tabControl1.TabIndex = 0;
            //
            // MemoForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 400);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MemoForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileMenu;
        private System.Windows.Forms.ToolStripMenuItem newTabMenuItem;
        private System.Windows.Forms.ToolStripMenuItem renameTabMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeTabMenuItem;
        private System.Windows.Forms.ToolStripMenuItem restoreClosedTabMenuItem;
        private System.Windows.Forms.ToolStripSeparator fileSep1;
        private System.Windows.Forms.ToolStripMenuItem closeWindowMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editMenu;
        private System.Windows.Forms.ToolStripMenuItem undoMenuItem;
        private System.Windows.Forms.ToolStripMenuItem redoMenuItem;
        private System.Windows.Forms.ToolStripSeparator editSep1;
        private System.Windows.Forms.ToolStripMenuItem cutMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteMenuItem;
        private System.Windows.Forms.ToolStripSeparator editSep2;
        private System.Windows.Forms.ToolStripMenuItem findMenuItem;
        private System.Windows.Forms.ToolStripMenuItem findNextMenuItem;
        private System.Windows.Forms.ToolStripMenuItem findPrevMenuItem;
        private System.Windows.Forms.ToolStripMenuItem replaceMenuItem;
        private System.Windows.Forms.ToolStripSeparator editSep3;
        private System.Windows.Forms.ToolStripMenuItem selectAllMenuItem;
        private System.Windows.Forms.ToolStripMenuItem formatMenu;
        private System.Windows.Forms.ToolStripMenuItem fontMenuItem;
    }
}
