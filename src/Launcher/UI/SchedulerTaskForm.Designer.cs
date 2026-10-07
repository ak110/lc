#nullable disable
namespace Launcher.UI {
	partial class SchedulerTaskForm {
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
			this.launchOptions = new LaunchOptionsControl();
			this.checkBoxEnable = new System.Windows.Forms.CheckBox();
			this.labelType = new System.Windows.Forms.Label();
			this.comboBoxType = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxFileName = new System.Windows.Forms.TextBox();
			this.buttonBrowse = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.textBoxParam = new System.Windows.Forms.TextBox();
			this.labelMessage = new System.Windows.Forms.Label();
			this.textBoxMessage = new System.Windows.Forms.TextBox();
			this.buttonOk = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.SuspendLayout();
			this.launchOptions.Location = new System.Drawing.Point(8, 124);
			this.launchOptions.Name = "launchOptions";
			this.launchOptions.TabIndex = 10;
			this.Controls.Add(this.launchOptions);
			//
			// checkBoxEnable
			//
			this.checkBoxEnable.AutoSize = true;
			this.checkBoxEnable.Location = new System.Drawing.Point(16, 16);
			this.checkBoxEnable.Name = "checkBoxEnable";
			this.checkBoxEnable.Size = new System.Drawing.Size(68, 16);
			this.checkBoxEnable.TabIndex = 0;
			this.checkBoxEnable.Text = "有効(&E)";
			this.checkBoxEnable.UseVisualStyleBackColor = true;
			//
			// labelType
			//
			this.labelType.AutoSize = true;
			this.labelType.Location = new System.Drawing.Point(16, 44);
			this.labelType.Name = "labelType";
			this.labelType.Size = new System.Drawing.Size(29, 12);
			this.labelType.TabIndex = 1;
			this.labelType.Text = "種類";
			//
			// comboBoxType
			//
			this.comboBoxType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxType.FormattingEnabled = true;
			this.comboBoxType.Items.AddRange(new object[] {
				"ファイル実行",
				"バルーン通知",
				"メッセージボックス"});
			this.comboBoxType.Location = new System.Drawing.Point(72, 40);
			this.comboBoxType.Name = "comboBoxType";
			this.comboBoxType.Size = new System.Drawing.Size(160, 20);
			this.comboBoxType.TabIndex = 2;
			this.comboBoxType.SelectedIndexChanged += new System.EventHandler(this.comboBoxType_SelectedIndexChanged);
			//
			// label1
			//
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(16, 72);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(52, 12);
			this.label1.TabIndex = 3;
			this.label1.Text = "ファイル名";
			//
			// textBoxFileName
			//
			this.textBoxFileName.Location = new System.Drawing.Point(72, 68);
			this.textBoxFileName.Name = "textBoxFileName";
			this.textBoxFileName.Size = new System.Drawing.Size(256, 19);
			this.textBoxFileName.TabIndex = 4;
			//
			// buttonBrowse
			//
			this.buttonBrowse.Location = new System.Drawing.Point(328, 68);
			this.buttonBrowse.Name = "buttonBrowse";
			this.buttonBrowse.Size = new System.Drawing.Size(32, 23);
			this.buttonBrowse.TabIndex = 5;
			this.buttonBrowse.Text = "...";
			this.buttonBrowse.UseVisualStyleBackColor = true;
			this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
			//
			// label2
			//
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(16, 96);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(49, 12);
			this.label2.TabIndex = 6;
			this.label2.Text = "パラメータ";
			//
			// textBoxParam
			//
			this.textBoxParam.Location = new System.Drawing.Point(72, 92);
			this.textBoxParam.Name = "textBoxParam";
			this.textBoxParam.Size = new System.Drawing.Size(256, 19);
			this.textBoxParam.TabIndex = 7;
			//
			// labelMessage
			//
			this.labelMessage.AutoSize = true;
			this.labelMessage.Location = new System.Drawing.Point(16, 72);
			this.labelMessage.Name = "labelMessage";
			this.labelMessage.Size = new System.Drawing.Size(52, 12);
			this.labelMessage.TabIndex = 8;
			this.labelMessage.Text = "メッセージ";
			//
			// textBoxMessage
			//
			this.textBoxMessage.Location = new System.Drawing.Point(72, 68);
			this.textBoxMessage.Multiline = true;
			this.textBoxMessage.Name = "textBoxMessage";
			this.textBoxMessage.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textBoxMessage.Size = new System.Drawing.Size(288, 80);
			this.textBoxMessage.TabIndex = 9;
			//
			// buttonOk
			//
			this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOk.Location = new System.Drawing.Point(213, 292);
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.Size = new System.Drawing.Size(75, 23);
			this.buttonOk.TabIndex = 12;
			this.buttonOk.Text = "OK";
			this.buttonOk.UseVisualStyleBackColor = true;
			this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(293, 292);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 13;
			this.buttonCancel.Text = "キャンセル";
			this.buttonCancel.UseVisualStyleBackColor = true;
			//
			// SchedulerTaskForm
			//
			this.AcceptButton = this.buttonOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(377, 329);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOk);
			this.Controls.Add(this.textBoxMessage);
			this.Controls.Add(this.labelMessage);
			this.Controls.Add(this.textBoxParam);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.buttonBrowse);
			this.Controls.Add(this.textBoxFileName);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.comboBoxType);
			this.Controls.Add(this.labelType);
			this.Controls.Add(this.checkBoxEnable);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.Name = "SchedulerTaskForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "らんちゃ : タスクの編集";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox checkBoxEnable;
		private System.Windows.Forms.Label labelType;
		private System.Windows.Forms.ComboBox comboBoxType;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxFileName;
		private System.Windows.Forms.Button buttonBrowse;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBoxParam;
		private System.Windows.Forms.Label labelMessage;
		private System.Windows.Forms.TextBox textBoxMessage;
		private LaunchOptionsControl launchOptions;
		private System.Windows.Forms.Button buttonOk;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
	}
}
