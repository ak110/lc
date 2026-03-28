#nullable disable
namespace Launcher.UI {
	partial class SchedulerItemForm {
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows フォーム デザイナで生成されたコード

		private void InitializeComponent() {
			this.checkBoxEnable = new System.Windows.Forms.CheckBox();
			this.labelName = new System.Windows.Forms.Label();
			this.textBoxName = new System.Windows.Forms.TextBox();
			this.labelSleepTime = new System.Windows.Forms.Label();
			this.numSleepTime = new System.Windows.Forms.NumericUpDown();
			this.labelSleepTimeUnit = new System.Windows.Forms.Label();
			this.groupBoxSchedules = new System.Windows.Forms.GroupBox();
			this.listBoxSchedules = new System.Windows.Forms.ListBox();
			this.buttonScheduleAdd = new System.Windows.Forms.Button();
			this.buttonScheduleClone = new System.Windows.Forms.Button();
			this.buttonScheduleEdit = new System.Windows.Forms.Button();
			this.buttonScheduleDelete = new System.Windows.Forms.Button();
			this.groupBoxTasks = new System.Windows.Forms.GroupBox();
			this.listBoxTasks = new System.Windows.Forms.ListBox();
			this.buttonTaskAdd = new System.Windows.Forms.Button();
			this.buttonTaskClone = new System.Windows.Forms.Button();
			this.buttonTaskEdit = new System.Windows.Forms.Button();
			this.buttonTaskUp = new System.Windows.Forms.Button();
			this.buttonTaskDown = new System.Windows.Forms.Button();
			this.buttonTaskDelete = new System.Windows.Forms.Button();
			this.buttonTaskTest = new System.Windows.Forms.Button();
			this.buttonOk = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.numSleepTime)).BeginInit();
			this.groupBoxSchedules.SuspendLayout();
			this.groupBoxTasks.SuspendLayout();
			this.SuspendLayout();
			//
			// checkBoxEnable
			//
			this.checkBoxEnable.AutoSize = true;
			this.checkBoxEnable.Location = new System.Drawing.Point(16, 16);
			this.checkBoxEnable.Name = "checkBoxEnable";
			this.checkBoxEnable.Size = new System.Drawing.Size(62, 16);
			this.checkBoxEnable.TabIndex = 0;
			this.checkBoxEnable.Text = "有効(&E)";
			this.checkBoxEnable.UseVisualStyleBackColor = true;
			//
			// labelName
			//
			this.labelName.AutoSize = true;
			this.labelName.Location = new System.Drawing.Point(16, 40);
			this.labelName.Name = "labelName";
			this.labelName.Size = new System.Drawing.Size(29, 12);
			this.labelName.TabIndex = 1;
			this.labelName.Text = "名前";
			//
			// textBoxName
			//
			this.textBoxName.Location = new System.Drawing.Point(56, 37);
			this.textBoxName.Name = "textBoxName";
			this.textBoxName.Size = new System.Drawing.Size(200, 19);
			this.textBoxName.TabIndex = 2;
			//
			// labelSleepTime
			//
			this.labelSleepTime.AutoSize = true;
			this.labelSleepTime.Location = new System.Drawing.Point(272, 40);
			this.labelSleepTime.Name = "labelSleepTime";
			this.labelSleepTime.Size = new System.Drawing.Size(59, 12);
			this.labelSleepTime.TabIndex = 3;
			this.labelSleepTime.Text = "タスク間隔";
			//
			// numSleepTime
			//
			this.numSleepTime.Location = new System.Drawing.Point(336, 37);
			this.numSleepTime.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
			this.numSleepTime.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
			this.numSleepTime.Name = "numSleepTime";
			this.numSleepTime.Size = new System.Drawing.Size(72, 19);
			this.numSleepTime.TabIndex = 4;
			this.numSleepTime.Value = new decimal(new int[] { 2000, 0, 0, 0 });
			//
			// labelSleepTimeUnit
			//
			this.labelSleepTimeUnit.AutoSize = true;
			this.labelSleepTimeUnit.Location = new System.Drawing.Point(412, 40);
			this.labelSleepTimeUnit.Name = "labelSleepTimeUnit";
			this.labelSleepTimeUnit.Size = new System.Drawing.Size(20, 12);
			this.labelSleepTimeUnit.TabIndex = 5;
			this.labelSleepTimeUnit.Text = "ms";
			//
			// groupBoxSchedules
			//
			this.groupBoxSchedules.Controls.Add(this.listBoxSchedules);
			this.groupBoxSchedules.Controls.Add(this.buttonScheduleAdd);
			this.groupBoxSchedules.Controls.Add(this.buttonScheduleClone);
			this.groupBoxSchedules.Controls.Add(this.buttonScheduleEdit);
			this.groupBoxSchedules.Controls.Add(this.buttonScheduleDelete);
			this.groupBoxSchedules.Location = new System.Drawing.Point(8, 64);
			this.groupBoxSchedules.Name = "groupBoxSchedules";
			this.groupBoxSchedules.Size = new System.Drawing.Size(440, 136);
			this.groupBoxSchedules.TabIndex = 6;
			this.groupBoxSchedules.TabStop = false;
			this.groupBoxSchedules.Text = "スケジュール";
			//
			// listBoxSchedules
			//
			this.listBoxSchedules.Font = new System.Drawing.Font("MS ゴシック", 11F);
			this.listBoxSchedules.HorizontalScrollbar = true;
			this.listBoxSchedules.Location = new System.Drawing.Point(8, 18);
			this.listBoxSchedules.Name = "listBoxSchedules";
			this.listBoxSchedules.Size = new System.Drawing.Size(352, 108);
			this.listBoxSchedules.TabIndex = 0;
			this.listBoxSchedules.DoubleClick += new System.EventHandler(this.listBoxSchedules_DoubleClick);
			//
			// buttonScheduleAdd
			//
			this.buttonScheduleAdd.Location = new System.Drawing.Point(368, 18);
			this.buttonScheduleAdd.Name = "buttonScheduleAdd";
			this.buttonScheduleAdd.Size = new System.Drawing.Size(64, 23);
			this.buttonScheduleAdd.TabIndex = 1;
			this.buttonScheduleAdd.Text = "追加";
			this.buttonScheduleAdd.UseVisualStyleBackColor = true;
			this.buttonScheduleAdd.Click += new System.EventHandler(this.buttonScheduleAdd_Click);
			//
			// buttonScheduleClone
			//
			this.buttonScheduleClone.Location = new System.Drawing.Point(368, 46);
			this.buttonScheduleClone.Name = "buttonScheduleClone";
			this.buttonScheduleClone.Size = new System.Drawing.Size(64, 23);
			this.buttonScheduleClone.TabIndex = 2;
			this.buttonScheduleClone.Text = "複製";
			this.buttonScheduleClone.UseVisualStyleBackColor = true;
			this.buttonScheduleClone.Click += new System.EventHandler(this.buttonScheduleClone_Click);
			//
			// buttonScheduleEdit
			//
			this.buttonScheduleEdit.Location = new System.Drawing.Point(368, 74);
			this.buttonScheduleEdit.Name = "buttonScheduleEdit";
			this.buttonScheduleEdit.Size = new System.Drawing.Size(64, 23);
			this.buttonScheduleEdit.TabIndex = 3;
			this.buttonScheduleEdit.Text = "編集";
			this.buttonScheduleEdit.UseVisualStyleBackColor = true;
			this.buttonScheduleEdit.Click += new System.EventHandler(this.buttonScheduleEdit_Click);
			//
			// buttonScheduleDelete
			//
			this.buttonScheduleDelete.Location = new System.Drawing.Point(368, 102);
			this.buttonScheduleDelete.Name = "buttonScheduleDelete";
			this.buttonScheduleDelete.Size = new System.Drawing.Size(64, 23);
			this.buttonScheduleDelete.TabIndex = 4;
			this.buttonScheduleDelete.Text = "削除";
			this.buttonScheduleDelete.UseVisualStyleBackColor = true;
			this.buttonScheduleDelete.Click += new System.EventHandler(this.buttonScheduleDelete_Click);
			//
			// groupBoxTasks
			//
			this.groupBoxTasks.Controls.Add(this.listBoxTasks);
			this.groupBoxTasks.Controls.Add(this.buttonTaskAdd);
			this.groupBoxTasks.Controls.Add(this.buttonTaskClone);
			this.groupBoxTasks.Controls.Add(this.buttonTaskEdit);
			this.groupBoxTasks.Controls.Add(this.buttonTaskUp);
			this.groupBoxTasks.Controls.Add(this.buttonTaskDown);
			this.groupBoxTasks.Controls.Add(this.buttonTaskDelete);
			this.groupBoxTasks.Controls.Add(this.buttonTaskTest);
			this.groupBoxTasks.Location = new System.Drawing.Point(8, 208);
			this.groupBoxTasks.Name = "groupBoxTasks";
			this.groupBoxTasks.Size = new System.Drawing.Size(440, 160);
			this.groupBoxTasks.TabIndex = 7;
			this.groupBoxTasks.TabStop = false;
			this.groupBoxTasks.Text = "タスク";
			//
			// listBoxTasks
			//
			this.listBoxTasks.Font = new System.Drawing.Font("MS ゴシック", 11F);
			this.listBoxTasks.HorizontalScrollbar = true;
			this.listBoxTasks.Location = new System.Drawing.Point(8, 18);
			this.listBoxTasks.Name = "listBoxTasks";
			this.listBoxTasks.Size = new System.Drawing.Size(352, 134);
			this.listBoxTasks.TabIndex = 0;
			this.listBoxTasks.DoubleClick += new System.EventHandler(this.listBoxTasks_DoubleClick);
			//
			// buttonTaskAdd
			//
			this.buttonTaskAdd.Location = new System.Drawing.Point(368, 18);
			this.buttonTaskAdd.Name = "buttonTaskAdd";
			this.buttonTaskAdd.Size = new System.Drawing.Size(64, 23);
			this.buttonTaskAdd.TabIndex = 1;
			this.buttonTaskAdd.Text = "追加";
			this.buttonTaskAdd.UseVisualStyleBackColor = true;
			this.buttonTaskAdd.Click += new System.EventHandler(this.buttonTaskAdd_Click);
			//
			// buttonTaskClone
			//
			this.buttonTaskClone.Location = new System.Drawing.Point(368, 46);
			this.buttonTaskClone.Name = "buttonTaskClone";
			this.buttonTaskClone.Size = new System.Drawing.Size(64, 23);
			this.buttonTaskClone.TabIndex = 2;
			this.buttonTaskClone.Text = "複製";
			this.buttonTaskClone.UseVisualStyleBackColor = true;
			this.buttonTaskClone.Click += new System.EventHandler(this.buttonTaskClone_Click);
			//
			// buttonTaskEdit
			//
			this.buttonTaskEdit.Location = new System.Drawing.Point(368, 74);
			this.buttonTaskEdit.Name = "buttonTaskEdit";
			this.buttonTaskEdit.Size = new System.Drawing.Size(64, 23);
			this.buttonTaskEdit.TabIndex = 3;
			this.buttonTaskEdit.Text = "編集";
			this.buttonTaskEdit.UseVisualStyleBackColor = true;
			this.buttonTaskEdit.Click += new System.EventHandler(this.buttonTaskEdit_Click);
			//
			// buttonTaskUp
			//
			this.buttonTaskUp.Location = new System.Drawing.Point(368, 102);
			this.buttonTaskUp.Name = "buttonTaskUp";
			this.buttonTaskUp.Size = new System.Drawing.Size(30, 23);
			this.buttonTaskUp.TabIndex = 4;
			this.buttonTaskUp.Text = "↑";
			this.buttonTaskUp.UseVisualStyleBackColor = true;
			this.buttonTaskUp.Click += new System.EventHandler(this.buttonTaskUp_Click);
			//
			// buttonTaskDown
			//
			this.buttonTaskDown.Location = new System.Drawing.Point(402, 102);
			this.buttonTaskDown.Name = "buttonTaskDown";
			this.buttonTaskDown.Size = new System.Drawing.Size(30, 23);
			this.buttonTaskDown.TabIndex = 5;
			this.buttonTaskDown.Text = "↓";
			this.buttonTaskDown.UseVisualStyleBackColor = true;
			this.buttonTaskDown.Click += new System.EventHandler(this.buttonTaskDown_Click);
			//
			// buttonTaskDelete
			//
			this.buttonTaskDelete.Location = new System.Drawing.Point(368, 130);
			this.buttonTaskDelete.Name = "buttonTaskDelete";
			this.buttonTaskDelete.Size = new System.Drawing.Size(64, 23);
			this.buttonTaskDelete.TabIndex = 6;
			this.buttonTaskDelete.Text = "削除";
			this.buttonTaskDelete.UseVisualStyleBackColor = true;
			this.buttonTaskDelete.Click += new System.EventHandler(this.buttonTaskDelete_Click);
			//
			// buttonTaskTest
			//
			this.buttonTaskTest.Location = new System.Drawing.Point(8, 156);
			this.buttonTaskTest.Name = "buttonTaskTest";
			this.buttonTaskTest.Size = new System.Drawing.Size(96, 23);
			this.buttonTaskTest.TabIndex = 7;
			this.buttonTaskTest.Text = "テスト実行(&T)";
			this.buttonTaskTest.UseVisualStyleBackColor = true;
			this.buttonTaskTest.Click += new System.EventHandler(this.buttonTaskTest_Click);
			//
			// buttonOk
			//
			this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOk.Location = new System.Drawing.Point(296, 392);
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
			this.buttonCancel.Location = new System.Drawing.Point(376, 392);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 9;
			this.buttonCancel.Text = "キャンセル";
			this.buttonCancel.UseVisualStyleBackColor = true;
			//
			// SchedulerItemForm
			//
			this.AcceptButton = this.buttonOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(457, 425);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOk);
			this.Controls.Add(this.groupBoxTasks);
			this.Controls.Add(this.groupBoxSchedules);
			this.Controls.Add(this.labelSleepTimeUnit);
			this.Controls.Add(this.numSleepTime);
			this.Controls.Add(this.labelSleepTime);
			this.Controls.Add(this.textBoxName);
			this.Controls.Add(this.labelName);
			this.Controls.Add(this.checkBoxEnable);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.Name = "SchedulerItemForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "らんちゃ : アイテムの編集";
			((System.ComponentModel.ISupportInitialize)(this.numSleepTime)).EndInit();
			this.groupBoxSchedules.ResumeLayout(false);
			this.groupBoxTasks.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion

		private System.Windows.Forms.CheckBox checkBoxEnable;
		private System.Windows.Forms.Label labelName;
		private System.Windows.Forms.TextBox textBoxName;
		private System.Windows.Forms.Label labelSleepTime;
		private System.Windows.Forms.NumericUpDown numSleepTime;
		private System.Windows.Forms.Label labelSleepTimeUnit;
		private System.Windows.Forms.GroupBox groupBoxSchedules;
		private System.Windows.Forms.ListBox listBoxSchedules;
		private System.Windows.Forms.Button buttonScheduleAdd;
		private System.Windows.Forms.Button buttonScheduleClone;
		private System.Windows.Forms.Button buttonScheduleEdit;
		private System.Windows.Forms.Button buttonScheduleDelete;
		private System.Windows.Forms.GroupBox groupBoxTasks;
		private System.Windows.Forms.ListBox listBoxTasks;
		private System.Windows.Forms.Button buttonTaskAdd;
		private System.Windows.Forms.Button buttonTaskClone;
		private System.Windows.Forms.Button buttonTaskEdit;
		private System.Windows.Forms.Button buttonTaskUp;
		private System.Windows.Forms.Button buttonTaskDown;
		private System.Windows.Forms.Button buttonTaskDelete;
		private System.Windows.Forms.Button buttonTaskTest;
		private System.Windows.Forms.Button buttonOk;
		private System.Windows.Forms.Button buttonCancel;
	}
}
