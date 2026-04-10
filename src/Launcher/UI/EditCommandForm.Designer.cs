#nullable disable
namespace Launcher.UI {
	partial class EditCommandForm {
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
			this.buttonOk = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.textBox2 = new System.Windows.Forms.TextBox();
			this.textBox3 = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.textBox4 = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.radioButton12 = new System.Windows.Forms.RadioButton();
			this.radioButton11 = new System.Windows.Forms.RadioButton();
			this.radioButton10 = new System.Windows.Forms.RadioButton();
			this.radioButton9 = new System.Windows.Forms.RadioButton();
			this.radioButton8 = new System.Windows.Forms.RadioButton();
			this.radioButton7 = new System.Windows.Forms.RadioButton();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.radioButton6 = new System.Windows.Forms.RadioButton();
			this.radioButton5 = new System.Windows.Forms.RadioButton();
			this.radioButton4 = new System.Windows.Forms.RadioButton();
			this.radioButton3 = new System.Windows.Forms.RadioButton();
			this.radioButton2 = new System.Windows.Forms.RadioButton();
			this.radioButton1 = new System.Windows.Forms.RadioButton();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
			this.checkBox1 = new System.Windows.Forms.CheckBox();
			this.groupBox2.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			//
			// buttonOk
			//
			this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOk.Location = new System.Drawing.Point(245, 240);
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
			this.buttonCancel.Location = new System.Drawing.Point(325, 240);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 13;
			this.buttonCancel.Text = "キャンセル";
			this.buttonCancel.UseVisualStyleBackColor = true;
			//
			// label1
			//
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(16, 16);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(52, 12);
			this.label1.TabIndex = 0;
			this.label1.Text = "コマンド名";
			//
			// textBox1
			//
			this.textBox1.Location = new System.Drawing.Point(72, 8);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(100, 19);
			this.textBox1.TabIndex = 1;
			//
			// label2
			//
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(16, 40);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(41, 12);
			this.label2.TabIndex = 2;
			this.label2.Text = "リンク先";
			//
			// textBox2
			//
			this.textBox2.Location = new System.Drawing.Point(72, 32);
			this.textBox2.Name = "textBox2";
			this.textBox2.Size = new System.Drawing.Size(256, 19);
			this.textBox2.TabIndex = 3;
			//
			// textBox3
			//
			this.textBox3.Location = new System.Drawing.Point(72, 56);
			this.textBox3.Name = "textBox3";
			this.textBox3.Size = new System.Drawing.Size(256, 19);
			this.textBox3.TabIndex = 6;
			//
			// label3
			//
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(16, 64);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(49, 12);
			this.label3.TabIndex = 5;
			this.label3.Text = "パラメータ";
			//
			// textBox4
			//
			this.textBox4.Location = new System.Drawing.Point(72, 80);
			this.textBox4.Name = "textBox4";
			this.textBox4.Size = new System.Drawing.Size(256, 19);
			this.textBox4.TabIndex = 8;
			//
			// label4
			//
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(8, 88);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(64, 12);
			this.label4.TabIndex = 7;
			this.label4.Text = "作業フォルダ";
			//
			// groupBox2
			//
			this.groupBox2.Controls.Add(this.radioButton12);
			this.groupBox2.Controls.Add(this.radioButton11);
			this.groupBox2.Controls.Add(this.radioButton10);
			this.groupBox2.Controls.Add(this.radioButton9);
			this.groupBox2.Controls.Add(this.radioButton8);
			this.groupBox2.Controls.Add(this.radioButton7);
			this.groupBox2.Location = new System.Drawing.Point(8, 192);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(216, 72);
			this.groupBox2.TabIndex = 11;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "優先度(&P)";
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
			// groupBox1
			//
			this.groupBox1.Controls.Add(this.radioButton6);
			this.groupBox1.Controls.Add(this.radioButton5);
			this.groupBox1.Controls.Add(this.radioButton4);
			this.groupBox1.Controls.Add(this.radioButton3);
			this.groupBox1.Controls.Add(this.radioButton2);
			this.groupBox1.Controls.Add(this.radioButton1);
			this.groupBox1.Location = new System.Drawing.Point(8, 112);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(272, 72);
			this.groupBox1.TabIndex = 10;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "表示(&S)";
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
			// button1
			//
			this.button1.Location = new System.Drawing.Point(328, 32);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(32, 23);
			this.button1.TabIndex = 4;
			this.button1.Text = "...";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			//
			// button2
			//
			this.button2.Location = new System.Drawing.Point(328, 80);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(32, 23);
			this.button2.TabIndex = 9;
			this.button2.Text = "...";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			//
			// checkBox1
			//
			this.checkBox1.AutoSize = true;
			this.checkBox1.Location = new System.Drawing.Point(296, 120);
			this.checkBox1.Name = "checkBox1";
			this.checkBox1.Size = new System.Drawing.Size(118, 16);
			this.checkBox1.TabIndex = 14;
			this.checkBox1.Text = "管理者権限で実行";
			this.checkBox1.UseVisualStyleBackColor = true;
			//
			// EditCommandForm
			//
			this.AcceptButton = this.buttonOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(417, 281);
			this.Controls.Add(this.checkBox1);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.textBox4);
			this.Controls.Add(this.textBox3);
			this.Controls.Add(this.textBox2);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.Name = "EditCommandForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "らんちゃ : コマンドの編集";
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonOk;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBox2;
		private System.Windows.Forms.TextBox textBox3;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox textBox4;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.RadioButton radioButton12;
		private System.Windows.Forms.RadioButton radioButton11;
		private System.Windows.Forms.RadioButton radioButton10;
		private System.Windows.Forms.RadioButton radioButton9;
		private System.Windows.Forms.RadioButton radioButton8;
		private System.Windows.Forms.RadioButton radioButton7;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton radioButton6;
		private System.Windows.Forms.RadioButton radioButton5;
		private System.Windows.Forms.RadioButton radioButton4;
		private System.Windows.Forms.RadioButton radioButton3;
		private System.Windows.Forms.RadioButton radioButton2;
		private System.Windows.Forms.RadioButton radioButton1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
		private System.Windows.Forms.CheckBox checkBox1;
	}
}
