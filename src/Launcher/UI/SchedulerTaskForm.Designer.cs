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
			this.checkBoxEnable = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxFileName = new System.Windows.Forms.TextBox();
			this.buttonBrowse = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.textBoxParam = new System.Windows.Forms.TextBox();
			this.groupBoxShow = new System.Windows.Forms.GroupBox();
			this.radioButton6 = new System.Windows.Forms.RadioButton();
			this.radioButton5 = new System.Windows.Forms.RadioButton();
			this.radioButton4 = new System.Windows.Forms.RadioButton();
			this.radioButton3 = new System.Windows.Forms.RadioButton();
			this.radioButton2 = new System.Windows.Forms.RadioButton();
			this.radioButton1 = new System.Windows.Forms.RadioButton();
			this.groupBoxPriority = new System.Windows.Forms.GroupBox();
			this.radioButton12 = new System.Windows.Forms.RadioButton();
			this.radioButton11 = new System.Windows.Forms.RadioButton();
			this.radioButton10 = new System.Windows.Forms.RadioButton();
			this.radioButton9 = new System.Windows.Forms.RadioButton();
			this.radioButton8 = new System.Windows.Forms.RadioButton();
			this.radioButton7 = new System.Windows.Forms.RadioButton();
			this.buttonOk = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.groupBoxShow.SuspendLayout();
			this.groupBoxPriority.SuspendLayout();
			this.SuspendLayout();
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
			// label1
			//
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(16, 44);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(52, 12);
			this.label1.TabIndex = 1;
			this.label1.Text = "ファイル名";
			//
			// textBoxFileName
			//
			this.textBoxFileName.Location = new System.Drawing.Point(72, 40);
			this.textBoxFileName.Name = "textBoxFileName";
			this.textBoxFileName.Size = new System.Drawing.Size(256, 19);
			this.textBoxFileName.TabIndex = 2;
			//
			// buttonBrowse
			//
			this.buttonBrowse.Location = new System.Drawing.Point(328, 40);
			this.buttonBrowse.Name = "buttonBrowse";
			this.buttonBrowse.Size = new System.Drawing.Size(32, 23);
			this.buttonBrowse.TabIndex = 3;
			this.buttonBrowse.Text = "...";
			this.buttonBrowse.UseVisualStyleBackColor = true;
			this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
			//
			// label2
			//
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(16, 68);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(49, 12);
			this.label2.TabIndex = 4;
			this.label2.Text = "パラメータ";
			//
			// textBoxParam
			//
			this.textBoxParam.Location = new System.Drawing.Point(72, 64);
			this.textBoxParam.Name = "textBoxParam";
			this.textBoxParam.Size = new System.Drawing.Size(256, 19);
			this.textBoxParam.TabIndex = 5;
			//
			// groupBoxShow
			//
			this.groupBoxShow.Controls.Add(this.radioButton6);
			this.groupBoxShow.Controls.Add(this.radioButton5);
			this.groupBoxShow.Controls.Add(this.radioButton4);
			this.groupBoxShow.Controls.Add(this.radioButton3);
			this.groupBoxShow.Controls.Add(this.radioButton2);
			this.groupBoxShow.Controls.Add(this.radioButton1);
			this.groupBoxShow.Location = new System.Drawing.Point(8, 96);
			this.groupBoxShow.Name = "groupBoxShow";
			this.groupBoxShow.Size = new System.Drawing.Size(272, 72);
			this.groupBoxShow.TabIndex = 6;
			this.groupBoxShow.TabStop = false;
			this.groupBoxShow.Text = "表示(&S)";
			//
			// radioButton6
			//
			this.radioButton6.AutoSize = true;
			this.radioButton6.Location = new System.Drawing.Point(200, 40);
			this.radioButton6.Name = "radioButton6";
			this.radioButton6.Size = new System.Drawing.Size(59, 16);
			this.radioButton6.TabIndex = 5;
			this.radioButton6.TabStop = true;
			this.radioButton6.Text = "非表示";
			this.radioButton6.UseVisualStyleBackColor = true;
			//
			// radioButton5
			//
			this.radioButton5.AutoSize = true;
			this.radioButton5.Location = new System.Drawing.Point(88, 40);
			this.radioButton5.Name = "radioButton5";
			this.radioButton5.Size = new System.Drawing.Size(106, 16);
			this.radioButton5.TabIndex = 4;
			this.radioButton5.TabStop = true;
			this.radioButton5.Text = "最小化非ｱｸﾃｨﾌﾞ";
			this.radioButton5.UseVisualStyleBackColor = true;
			//
			// radioButton4
			//
			this.radioButton4.AutoSize = true;
			this.radioButton4.Location = new System.Drawing.Point(16, 40);
			this.radioButton4.Name = "radioButton4";
			this.radioButton4.Size = new System.Drawing.Size(70, 16);
			this.radioButton4.TabIndex = 3;
			this.radioButton4.TabStop = true;
			this.radioButton4.Text = "非ｱｸﾃｨﾌﾞ";
			this.radioButton4.UseVisualStyleBackColor = true;
			//
			// radioButton3
			//
			this.radioButton3.AutoSize = true;
			this.radioButton3.Location = new System.Drawing.Point(200, 24);
			this.radioButton3.Name = "radioButton3";
			this.radioButton3.Size = new System.Drawing.Size(59, 16);
			this.radioButton3.TabIndex = 2;
			this.radioButton3.TabStop = true;
			this.radioButton3.Text = "最大化";
			this.radioButton3.UseVisualStyleBackColor = true;
			//
			// radioButton2
			//
			this.radioButton2.AutoSize = true;
			this.radioButton2.Location = new System.Drawing.Point(88, 24);
			this.radioButton2.Name = "radioButton2";
			this.radioButton2.Size = new System.Drawing.Size(59, 16);
			this.radioButton2.TabIndex = 1;
			this.radioButton2.TabStop = true;
			this.radioButton2.Text = "最小化";
			this.radioButton2.UseVisualStyleBackColor = true;
			//
			// radioButton1
			//
			this.radioButton1.AutoSize = true;
			this.radioButton1.Location = new System.Drawing.Point(16, 24);
			this.radioButton1.Name = "radioButton1";
			this.radioButton1.Size = new System.Drawing.Size(47, 16);
			this.radioButton1.TabIndex = 0;
			this.radioButton1.TabStop = true;
			this.radioButton1.Text = "通常";
			this.radioButton1.UseVisualStyleBackColor = true;
			//
			// groupBoxPriority
			//
			this.groupBoxPriority.Controls.Add(this.radioButton12);
			this.groupBoxPriority.Controls.Add(this.radioButton11);
			this.groupBoxPriority.Controls.Add(this.radioButton10);
			this.groupBoxPriority.Controls.Add(this.radioButton9);
			this.groupBoxPriority.Controls.Add(this.radioButton8);
			this.groupBoxPriority.Controls.Add(this.radioButton7);
			this.groupBoxPriority.Location = new System.Drawing.Point(8, 176);
			this.groupBoxPriority.Name = "groupBoxPriority";
			this.groupBoxPriority.Size = new System.Drawing.Size(216, 72);
			this.groupBoxPriority.TabIndex = 7;
			this.groupBoxPriority.TabStop = false;
			this.groupBoxPriority.Text = "優先度(&P)";
			//
			// radioButton12
			//
			this.radioButton12.AutoSize = true;
			this.radioButton12.Location = new System.Drawing.Point(136, 40);
			this.radioButton12.Name = "radioButton12";
			this.radioButton12.Size = new System.Drawing.Size(35, 16);
			this.radioButton12.TabIndex = 5;
			this.radioButton12.TabStop = true;
			this.radioButton12.Text = "低";
			this.radioButton12.UseVisualStyleBackColor = true;
			//
			// radioButton11
			//
			this.radioButton11.AutoSize = true;
			this.radioButton11.Location = new System.Drawing.Point(64, 40);
			this.radioButton11.Name = "radioButton11";
			this.radioButton11.Size = new System.Drawing.Size(71, 16);
			this.radioButton11.TabIndex = 4;
			this.radioButton11.TabStop = true;
			this.radioButton11.Text = "通常以下";
			this.radioButton11.UseVisualStyleBackColor = true;
			//
			// radioButton10
			//
			this.radioButton10.AutoSize = true;
			this.radioButton10.Location = new System.Drawing.Point(16, 40);
			this.radioButton10.Name = "radioButton10";
			this.radioButton10.Size = new System.Drawing.Size(47, 16);
			this.radioButton10.TabIndex = 3;
			this.radioButton10.TabStop = true;
			this.radioButton10.Text = "通常";
			this.radioButton10.UseVisualStyleBackColor = true;
			//
			// radioButton9
			//
			this.radioButton9.AutoSize = true;
			this.radioButton9.Location = new System.Drawing.Point(136, 24);
			this.radioButton9.Name = "radioButton9";
			this.radioButton9.Size = new System.Drawing.Size(71, 16);
			this.radioButton9.TabIndex = 2;
			this.radioButton9.TabStop = true;
			this.radioButton9.Text = "通常以上";
			this.radioButton9.UseVisualStyleBackColor = true;
			//
			// radioButton8
			//
			this.radioButton8.AutoSize = true;
			this.radioButton8.Location = new System.Drawing.Point(64, 24);
			this.radioButton8.Name = "radioButton8";
			this.radioButton8.Size = new System.Drawing.Size(35, 16);
			this.radioButton8.TabIndex = 1;
			this.radioButton8.TabStop = true;
			this.radioButton8.Text = "高";
			this.radioButton8.UseVisualStyleBackColor = true;
			//
			// radioButton7
			//
			this.radioButton7.AutoSize = true;
			this.radioButton7.Location = new System.Drawing.Point(16, 24);
			this.radioButton7.Name = "radioButton7";
			this.radioButton7.Size = new System.Drawing.Size(47, 16);
			this.radioButton7.TabIndex = 0;
			this.radioButton7.TabStop = true;
			this.radioButton7.Text = "最高";
			this.radioButton7.UseVisualStyleBackColor = true;
			//
			// buttonOk
			//
			this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOk.Location = new System.Drawing.Point(213, 264);
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.Size = new System.Drawing.Size(75, 23);
			this.buttonOk.TabIndex = 8;
			this.buttonOk.Text = "OK";
			this.buttonOk.UseVisualStyleBackColor = true;
			this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(293, 264);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 9;
			this.buttonCancel.Text = "キャンセル";
			this.buttonCancel.UseVisualStyleBackColor = true;
			//
			// SchedulerTaskForm
			//
			this.AcceptButton = this.buttonOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(377, 301);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOk);
			this.Controls.Add(this.groupBoxPriority);
			this.Controls.Add(this.groupBoxShow);
			this.Controls.Add(this.textBoxParam);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.buttonBrowse);
			this.Controls.Add(this.textBoxFileName);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.checkBoxEnable);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.Name = "SchedulerTaskForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "らんちゃ : タスクの編集";
			this.groupBoxShow.ResumeLayout(false);
			this.groupBoxShow.PerformLayout();
			this.groupBoxPriority.ResumeLayout(false);
			this.groupBoxPriority.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox checkBoxEnable;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxFileName;
		private System.Windows.Forms.Button buttonBrowse;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBoxParam;
		private System.Windows.Forms.GroupBox groupBoxShow;
		private System.Windows.Forms.RadioButton radioButton6;
		private System.Windows.Forms.RadioButton radioButton5;
		private System.Windows.Forms.RadioButton radioButton4;
		private System.Windows.Forms.RadioButton radioButton3;
		private System.Windows.Forms.RadioButton radioButton2;
		private System.Windows.Forms.RadioButton radioButton1;
		private System.Windows.Forms.GroupBox groupBoxPriority;
		private System.Windows.Forms.RadioButton radioButton12;
		private System.Windows.Forms.RadioButton radioButton11;
		private System.Windows.Forms.RadioButton radioButton10;
		private System.Windows.Forms.RadioButton radioButton9;
		private System.Windows.Forms.RadioButton radioButton8;
		private System.Windows.Forms.RadioButton radioButton7;
		private System.Windows.Forms.Button buttonOk;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
	}
}
