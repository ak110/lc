#nullable disable
namespace Launcher.UI {
	partial class DummyForm {
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DummyForm));
			this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.設定CToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.コマンドの管理LToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.メインウィンドウを表示非表示VToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.実行ファイルのあるフォルダを開くMToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.ネットワーク更新NToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.再起動RToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.終了XToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.スケジューラ設定SToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.schedulerTimer = new System.Windows.Forms.Timer(this.components);
			this.contextMenuStrip1.SuspendLayout();
			this.SuspendLayout();
			//
			// notifyIcon1
			//
			this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
			this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
			this.notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
			//
			// contextMenuStrip1
			//
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.設定CToolStripMenuItem,
            this.コマンドの管理LToolStripMenuItem,
            this.スケジューラ設定SToolStripMenuItem,
            this.メインウィンドウを表示非表示VToolStripMenuItem,
            this.toolStripSeparator1,
            this.実行ファイルのあるフォルダを開くMToolStripMenuItem,
            this.toolStripSeparator3,
            this.ネットワーク更新NToolStripMenuItem,
            this.toolStripSeparator2,
            this.再起動RToolStripMenuItem,
            this.終了XToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(289, 242);
			//
			// 設定CToolStripMenuItem
			//
			this.設定CToolStripMenuItem.Name = "設定CToolStripMenuItem";
			this.設定CToolStripMenuItem.Size = new System.Drawing.Size(288, 22);
			this.設定CToolStripMenuItem.Text = "設定(&C)";
			this.設定CToolStripMenuItem.Click += new System.EventHandler(this.設定CToolStripMenuItem_Click);
			//
			// コマンドの管理LToolStripMenuItem
			//
			this.コマンドの管理LToolStripMenuItem.Name = "コマンドの管理LToolStripMenuItem";
			this.コマンドの管理LToolStripMenuItem.Size = new System.Drawing.Size(288, 22);
			this.コマンドの管理LToolStripMenuItem.Text = "コマンドの管理(&L)";
			this.コマンドの管理LToolStripMenuItem.Click += new System.EventHandler(this.コマンドの管理LToolStripMenuItem_Click);
			//
			// スケジューラ設定SToolStripMenuItem
			//
			this.スケジューラ設定SToolStripMenuItem.Name = "スケジューラ設定SToolStripMenuItem";
			this.スケジューラ設定SToolStripMenuItem.Size = new System.Drawing.Size(288, 22);
			this.スケジューラ設定SToolStripMenuItem.Text = "スケジューラ設定(&S)";
			this.スケジューラ設定SToolStripMenuItem.Click += new System.EventHandler(this.スケジューラ設定SToolStripMenuItem_Click);
			//
			// schedulerTimer
			//
			this.schedulerTimer.Interval = 30000;
			this.schedulerTimer.Tick += new System.EventHandler(this.schedulerTimer_Tick);
			//
			// メインウィンドウを表示非表示VToolStripMenuItem
			//
			this.メインウィンドウを表示非表示VToolStripMenuItem.Name = "メインウィンドウを表示非表示VToolStripMenuItem";
			this.メインウィンドウを表示非表示VToolStripMenuItem.Size = new System.Drawing.Size(288, 22);
			this.メインウィンドウを表示非表示VToolStripMenuItem.Text = "メインウィンドウを表示/非表示(&V)";
			this.メインウィンドウを表示非表示VToolStripMenuItem.Click += new System.EventHandler(this.メインウィンドウを表示非表示VToolStripMenuItem_Click);
			//
			// toolStripSeparator1
			//
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(285, 6);
			//
			// 実行ファイルのあるフォルダを開くMToolStripMenuItem
			//
			this.実行ファイルのあるフォルダを開くMToolStripMenuItem.Name = "実行ファイルのあるフォルダを開くMToolStripMenuItem";
			this.実行ファイルのあるフォルダを開くMToolStripMenuItem.Size = new System.Drawing.Size(288, 22);
			this.実行ファイルのあるフォルダを開くMToolStripMenuItem.Text = "実行ファイルのあるフォルダを開く(&M)";
			this.実行ファイルのあるフォルダを開くMToolStripMenuItem.Click += new System.EventHandler(this.実行ファイルのあるフォルダを開くMToolStripMenuItem_Click);
			//
			// toolStripSeparator3
			//
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(285, 6);
			//
			// ネットワーク更新NToolStripMenuItem
			//
			this.ネットワーク更新NToolStripMenuItem.Name = "ネットワーク更新NToolStripMenuItem";
			this.ネットワーク更新NToolStripMenuItem.Size = new System.Drawing.Size(288, 22);
			this.ネットワーク更新NToolStripMenuItem.Text = "ネットワーク更新(&N)";
			this.ネットワーク更新NToolStripMenuItem.Click += new System.EventHandler(this.ネットワーク更新NToolStripMenuItem_Click);
			//
			// toolStripSeparator2
			//
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(285, 6);
			//
			// 再起動RToolStripMenuItem
			//
			this.再起動RToolStripMenuItem.Name = "再起動RToolStripMenuItem";
			this.再起動RToolStripMenuItem.Size = new System.Drawing.Size(288, 22);
			this.再起動RToolStripMenuItem.Text = "再起動(&R)";
			this.再起動RToolStripMenuItem.Click += new System.EventHandler(this.再起動RToolStripMenuItem_Click);
			//
			// 終了XToolStripMenuItem
			//
			this.終了XToolStripMenuItem.Name = "終了XToolStripMenuItem";
			this.終了XToolStripMenuItem.Size = new System.Drawing.Size(288, 22);
			this.終了XToolStripMenuItem.Text = "終了(&X)";
			this.終了XToolStripMenuItem.Click += new System.EventHandler(this.終了XToolStripMenuItem_Click);
			//
			// DummyForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(24, 24);
			this.ControlBox = false;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DummyForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Shown += new System.EventHandler(this.DummyForm_Shown);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DummyForm_FormClosing);
			this.Load += new System.EventHandler(this.DummyForm_Load);
			this.contextMenuStrip1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.NotifyIcon notifyIcon1;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolStripMenuItem 設定CToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem コマンドの管理LToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem 実行ファイルのあるフォルダを開くMToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem ネットワーク更新NToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem 終了XToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem 再起動RToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem メインウィンドウを表示非表示VToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem スケジューラ設定SToolStripMenuItem;
		private System.Windows.Forms.Timer schedulerTimer;
	}
}
