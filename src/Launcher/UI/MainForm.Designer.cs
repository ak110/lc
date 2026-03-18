#nullable disable
namespace Launcher.UI {
	partial class MainForm {
		/// <summary>
		/// 必要なデザイナ変数です。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 使用中のリソースをすべてクリーンアップします。
		/// </summary>
		/// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows フォーム デザイナで生成されたコード

		/// <summary>
		/// デザイナ サポートに必要なメソッドです。このメソッドの内容を
		/// コード エディタで変更しないでください。
		/// </summary>
		private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStrip2 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.実行RToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.フォルダを開くFToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.設定CToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.削除DToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.複製の作成LToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip2.SuspendLayout();
            this.SuspendLayout();
            //
            // textBox1
            //
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.textBox1.Location = new System.Drawing.Point(8, 8);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(272, 19);
            this.textBox1.TabIndex = 0;
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            this.textBox1.Enter += new System.EventHandler(this.textBox1_Enter);
            this.textBox1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox1_KeyDown);
            this.textBox1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBox1_KeyPress);
            this.textBox1.KeyUp += new System.Windows.Forms.KeyEventHandler(this.textBox1_KeyUp);
            //
            // button1
            //
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button1.Location = new System.Drawing.Point(280, 8);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(47, 23);
            this.button1.TabIndex = 1;
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            //
            // button2
            //
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button2.Location = new System.Drawing.Point(328, 8);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(56, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "ｷｬﾝｾﾙ";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            //
            // listView1
            //
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.listView1.ContextMenuStrip = this.contextMenuStrip2;
            this.listView1.FullRowSelect = true;
            this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listView1.Location = new System.Drawing.Point(8, 40);
            this.listView1.MultiSelect = false;
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(375, 271);
            this.listView1.SmallImageList = this.imageList1;
            this.listView1.TabIndex = 3;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.Enter += new System.EventHandler(this.listView1_Enter);
            this.listView1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.listView1_KeyDown);
            this.listView1.KeyUp += new System.Windows.Forms.KeyEventHandler(this.listView1_KeyUp);
            this.listView1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listView1_MouseDoubleClick);
            //
            // columnHeader1
            //
            this.columnHeader1.Text = "コマンド";
            this.columnHeader1.Width = 128;
            //
            // columnHeader2
            //
            this.columnHeader2.Text = "パス";
            this.columnHeader2.Width = 192;
            //
            // contextMenuStrip2
            //
            this.contextMenuStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.実行RToolStripMenuItem,
            this.フォルダを開くFToolStripMenuItem,
            this.設定CToolStripMenuItem1,
            this.削除DToolStripMenuItem,
            this.複製の作成LToolStripMenuItem});
            this.contextMenuStrip2.Name = "contextMenuStrip2";
            this.contextMenuStrip2.Size = new System.Drawing.Size(178, 136);
            //
            // 実行RToolStripMenuItem
            //
            this.実行RToolStripMenuItem.Name = "実行RToolStripMenuItem";
            this.実行RToolStripMenuItem.Size = new System.Drawing.Size(177, 22);
            this.実行RToolStripMenuItem.Text = "実行(&R)";
            this.実行RToolStripMenuItem.Click += new System.EventHandler(this.実行RToolStripMenuItem_Click);
            //
            // フォルダを開くFToolStripMenuItem
            //
            this.フォルダを開くFToolStripMenuItem.Name = "フォルダを開くFToolStripMenuItem";
            this.フォルダを開くFToolStripMenuItem.Size = new System.Drawing.Size(177, 22);
            this.フォルダを開くFToolStripMenuItem.Text = "フォルダを開く(&F)";
            this.フォルダを開くFToolStripMenuItem.Click += new System.EventHandler(this.フォルダを開くFToolStripMenuItem_Click);
            //
            // 設定CToolStripMenuItem1
            //
            this.設定CToolStripMenuItem1.Name = "設定CToolStripMenuItem1";
            this.設定CToolStripMenuItem1.Size = new System.Drawing.Size(177, 22);
            this.設定CToolStripMenuItem1.Text = "設定(&C)";
            this.設定CToolStripMenuItem1.Click += new System.EventHandler(this.設定CToolStripMenuItem1_Click);
            //
            // 削除DToolStripMenuItem
            //
            this.削除DToolStripMenuItem.Name = "削除DToolStripMenuItem";
            this.削除DToolStripMenuItem.Size = new System.Drawing.Size(177, 22);
            this.削除DToolStripMenuItem.Text = "削除(&D)";
            this.削除DToolStripMenuItem.Click += new System.EventHandler(this.削除DToolStripMenuItem_Click);
            //
            // imageList1
            //
            this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imageList1.ImageSize = new System.Drawing.Size(32, 32);
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            //
            // 複製の作成LToolStripMenuItem
            //
            this.複製の作成LToolStripMenuItem.Name = "複製の作成LToolStripMenuItem";
            this.複製の作成LToolStripMenuItem.Size = new System.Drawing.Size(177, 22);
            this.複製の作成LToolStripMenuItem.Text = "複製の作成(&L)";
            this.複製の作成LToolStripMenuItem.Click += new System.EventHandler(this.複製の作成LToolStripMenuItem_Click);
            //
            // MainForm
            //
            this.AcceptButton = this.button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.button2;
            this.ClientSize = new System.Drawing.Size(392, 323);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.textBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "らんちゃ";
            this.Activated += new System.EventHandler(this.MainForm_Activated);
            this.Deactivate += new System.EventHandler(this.MainForm_Deactivate);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.LocationChanged += new System.EventHandler(this.MainForm_LocationChanged);
            this.SizeChanged += new System.EventHandler(this.MainForm_SizeChanged);
            this.Leave += new System.EventHandler(this.MainForm_Leave);
            this.contextMenuStrip2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.ListView listView1;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ImageList imageList1;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip2;
		private System.Windows.Forms.ToolStripMenuItem フォルダを開くFToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem 実行RToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem 設定CToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem 削除DToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 複製の作成LToolStripMenuItem;
	}
}
