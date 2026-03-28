#nullable disable
namespace Launcher.UI {
	partial class SchedulerConfigForm {
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows フォーム デザイナで生成されたコード

		private void InitializeComponent() {
			this.listBoxItems = new System.Windows.Forms.ListBox();
			this.buttonAdd = new System.Windows.Forms.Button();
			this.buttonClone = new System.Windows.Forms.Button();
			this.buttonEdit = new System.Windows.Forms.Button();
			this.buttonUp = new System.Windows.Forms.Button();
			this.buttonDown = new System.Windows.Forms.Button();
			this.buttonDelete = new System.Windows.Forms.Button();
			this.buttonOk = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// listBoxItems
			//
			this.listBoxItems.Font = new System.Drawing.Font("MS ゴシック", 11F);
			this.listBoxItems.HorizontalScrollbar = true;
			this.listBoxItems.Location = new System.Drawing.Point(8, 8);
			this.listBoxItems.Name = "listBoxItems";
			this.listBoxItems.Size = new System.Drawing.Size(320, 212);
			this.listBoxItems.TabIndex = 0;
			this.listBoxItems.DoubleClick += new System.EventHandler(this.listBoxItems_DoubleClick);
			//
			// buttonAdd
			//
			this.buttonAdd.Location = new System.Drawing.Point(336, 8);
			this.buttonAdd.Name = "buttonAdd";
			this.buttonAdd.Size = new System.Drawing.Size(64, 23);
			this.buttonAdd.TabIndex = 1;
			this.buttonAdd.Text = "追加";
			this.buttonAdd.UseVisualStyleBackColor = true;
			this.buttonAdd.Click += new System.EventHandler(this.buttonAdd_Click);
			//
			// buttonClone
			//
			this.buttonClone.Location = new System.Drawing.Point(336, 36);
			this.buttonClone.Name = "buttonClone";
			this.buttonClone.Size = new System.Drawing.Size(64, 23);
			this.buttonClone.TabIndex = 2;
			this.buttonClone.Text = "複製";
			this.buttonClone.UseVisualStyleBackColor = true;
			this.buttonClone.Click += new System.EventHandler(this.buttonClone_Click);
			//
			// buttonEdit
			//
			this.buttonEdit.Location = new System.Drawing.Point(336, 64);
			this.buttonEdit.Name = "buttonEdit";
			this.buttonEdit.Size = new System.Drawing.Size(64, 23);
			this.buttonEdit.TabIndex = 3;
			this.buttonEdit.Text = "編集";
			this.buttonEdit.UseVisualStyleBackColor = true;
			this.buttonEdit.Click += new System.EventHandler(this.buttonEdit_Click);
			//
			// buttonUp
			//
			this.buttonUp.Location = new System.Drawing.Point(336, 96);
			this.buttonUp.Name = "buttonUp";
			this.buttonUp.Size = new System.Drawing.Size(30, 23);
			this.buttonUp.TabIndex = 4;
			this.buttonUp.Text = "↑";
			this.buttonUp.UseVisualStyleBackColor = true;
			this.buttonUp.Click += new System.EventHandler(this.buttonUp_Click);
			//
			// buttonDown
			//
			this.buttonDown.Location = new System.Drawing.Point(370, 96);
			this.buttonDown.Name = "buttonDown";
			this.buttonDown.Size = new System.Drawing.Size(30, 23);
			this.buttonDown.TabIndex = 5;
			this.buttonDown.Text = "↓";
			this.buttonDown.UseVisualStyleBackColor = true;
			this.buttonDown.Click += new System.EventHandler(this.buttonDown_Click);
			//
			// buttonDelete
			//
			this.buttonDelete.Location = new System.Drawing.Point(336, 124);
			this.buttonDelete.Name = "buttonDelete";
			this.buttonDelete.Size = new System.Drawing.Size(64, 23);
			this.buttonDelete.TabIndex = 6;
			this.buttonDelete.Text = "削除";
			this.buttonDelete.UseVisualStyleBackColor = true;
			this.buttonDelete.Click += new System.EventHandler(this.buttonDelete_Click);
			//
			// buttonOk
			//
			this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOk.Location = new System.Drawing.Point(248, 232);
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.Size = new System.Drawing.Size(75, 23);
			this.buttonOk.TabIndex = 7;
			this.buttonOk.Text = "OK";
			this.buttonOk.UseVisualStyleBackColor = true;
			this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(328, 232);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 8;
			this.buttonCancel.Text = "キャンセル";
			this.buttonCancel.UseVisualStyleBackColor = true;
			//
			// SchedulerConfigForm
			//
			this.AcceptButton = this.buttonOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(409, 265);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOk);
			this.Controls.Add(this.buttonDelete);
			this.Controls.Add(this.buttonDown);
			this.Controls.Add(this.buttonUp);
			this.Controls.Add(this.buttonEdit);
			this.Controls.Add(this.buttonClone);
			this.Controls.Add(this.buttonAdd);
			this.Controls.Add(this.listBoxItems);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.Name = "SchedulerConfigForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "らんちゃ : スケジューラー設定";
			this.ResumeLayout(false);
		}

		#endregion

		private System.Windows.Forms.ListBox listBoxItems;
		private System.Windows.Forms.Button buttonAdd;
		private System.Windows.Forms.Button buttonClone;
		private System.Windows.Forms.Button buttonEdit;
		private System.Windows.Forms.Button buttonUp;
		private System.Windows.Forms.Button buttonDown;
		private System.Windows.Forms.Button buttonDelete;
		private System.Windows.Forms.Button buttonOk;
		private System.Windows.Forms.Button buttonCancel;
	}
}
