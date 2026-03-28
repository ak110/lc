#nullable disable
namespace Launcher.UI {
	partial class ScheduleEditForm {
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
			this.groupBoxTime = new System.Windows.Forms.GroupBox();
			this.radioTimeSpecific = new System.Windows.Forms.RadioButton();
			this.radioTimeInterval = new System.Windows.Forms.RadioButton();
			this.panelTimeSpecific = new System.Windows.Forms.Panel();
			this.listBoxTimes = new System.Windows.Forms.ListBox();
			this.numTimeAddH = new System.Windows.Forms.NumericUpDown();
			this.labelTimeColon = new System.Windows.Forms.Label();
			this.numTimeAddM = new System.Windows.Forms.NumericUpDown();
			this.buttonTimeAdd = new System.Windows.Forms.Button();
			this.buttonTimeDelete = new System.Windows.Forms.Button();
			this.panelTimeInterval = new System.Windows.Forms.Panel();
			this.labelTimeStart = new System.Windows.Forms.Label();
			this.numTimeIntervalStartH = new System.Windows.Forms.NumericUpDown();
			this.labelTimeStartColon = new System.Windows.Forms.Label();
			this.numTimeIntervalStartM = new System.Windows.Forms.NumericUpDown();
			this.labelTimeTilde = new System.Windows.Forms.Label();
			this.numTimeIntervalEndH = new System.Windows.Forms.NumericUpDown();
			this.labelTimeEndColon = new System.Windows.Forms.Label();
			this.numTimeIntervalEndM = new System.Windows.Forms.NumericUpDown();
			this.labelTimeEvery = new System.Windows.Forms.Label();
			this.numTimeInterval = new System.Windows.Forms.NumericUpDown();
			this.labelTimeMin = new System.Windows.Forms.Label();
			this.groupBoxDate = new System.Windows.Forms.GroupBox();
			this.radioDateWeekday = new System.Windows.Forms.RadioButton();
			this.radioDateInterval = new System.Windows.Forms.RadioButton();
			this.panelDateWeekday = new System.Windows.Forms.Panel();
			this.numWeeksStartM = new System.Windows.Forms.NumericUpDown();
			this.labelWeeksStartSlash = new System.Windows.Forms.Label();
			this.numWeeksStartD = new System.Windows.Forms.NumericUpDown();
			this.labelWeeksTilde = new System.Windows.Forms.Label();
			this.numWeeksEndM = new System.Windows.Forms.NumericUpDown();
			this.labelWeeksEndSlash = new System.Windows.Forms.Label();
			this.numWeeksEndD = new System.Windows.Forms.NumericUpDown();
			this.checkMon = new System.Windows.Forms.CheckBox();
			this.checkTue = new System.Windows.Forms.CheckBox();
			this.checkWed = new System.Windows.Forms.CheckBox();
			this.checkThu = new System.Windows.Forms.CheckBox();
			this.checkFri = new System.Windows.Forms.CheckBox();
			this.checkSat = new System.Windows.Forms.CheckBox();
			this.checkSun = new System.Windows.Forms.CheckBox();
			this.panelDateInterval = new System.Windows.Forms.Panel();
			this.numDateIntervalStartM = new System.Windows.Forms.NumericUpDown();
			this.labelDateStartSlash = new System.Windows.Forms.Label();
			this.numDateIntervalStartD = new System.Windows.Forms.NumericUpDown();
			this.labelDateTilde = new System.Windows.Forms.Label();
			this.numDateIntervalEndM = new System.Windows.Forms.NumericUpDown();
			this.labelDateEndSlash = new System.Windows.Forms.Label();
			this.numDateIntervalEndD = new System.Windows.Forms.NumericUpDown();
			this.labelDateEvery = new System.Windows.Forms.Label();
			this.numDateInterval = new System.Windows.Forms.NumericUpDown();
			this.labelDateDay = new System.Windows.Forms.Label();
			this.buttonOk = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.groupBoxTime.SuspendLayout();
			this.panelTimeSpecific.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numTimeAddH)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numTimeAddM)).BeginInit();
			this.panelTimeInterval.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numTimeIntervalStartH)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numTimeIntervalStartM)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numTimeIntervalEndH)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numTimeIntervalEndM)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numTimeInterval)).BeginInit();
			this.groupBoxDate.SuspendLayout();
			this.panelDateWeekday.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numWeeksStartM)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numWeeksStartD)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numWeeksEndM)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numWeeksEndD)).BeginInit();
			this.panelDateInterval.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numDateIntervalStartM)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numDateIntervalStartD)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numDateIntervalEndM)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numDateIntervalEndD)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numDateInterval)).BeginInit();
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
			// groupBoxTime
			//
			this.groupBoxTime.Controls.Add(this.radioTimeSpecific);
			this.groupBoxTime.Controls.Add(this.radioTimeInterval);
			this.groupBoxTime.Controls.Add(this.panelTimeSpecific);
			this.groupBoxTime.Controls.Add(this.panelTimeInterval);
			this.groupBoxTime.Location = new System.Drawing.Point(8, 40);
			this.groupBoxTime.Name = "groupBoxTime";
			this.groupBoxTime.Size = new System.Drawing.Size(456, 152);
			this.groupBoxTime.TabIndex = 1;
			this.groupBoxTime.TabStop = false;
			this.groupBoxTime.Text = "時刻設定";
			//
			// radioTimeSpecific
			//
			this.radioTimeSpecific.AutoSize = true;
			this.radioTimeSpecific.Location = new System.Drawing.Point(16, 20);
			this.radioTimeSpecific.Name = "radioTimeSpecific";
			this.radioTimeSpecific.Size = new System.Drawing.Size(87, 16);
			this.radioTimeSpecific.TabIndex = 0;
			this.radioTimeSpecific.TabStop = true;
			this.radioTimeSpecific.Text = "指定時刻(&T)";
			this.radioTimeSpecific.UseVisualStyleBackColor = true;
			this.radioTimeSpecific.CheckedChanged += new System.EventHandler(this.radioTimeType_CheckedChanged);
			//
			// radioTimeInterval
			//
			this.radioTimeInterval.AutoSize = true;
			this.radioTimeInterval.Location = new System.Drawing.Point(120, 20);
			this.radioTimeInterval.Name = "radioTimeInterval";
			this.radioTimeInterval.Size = new System.Drawing.Size(63, 16);
			this.radioTimeInterval.TabIndex = 1;
			this.radioTimeInterval.TabStop = true;
			this.radioTimeInterval.Text = "間隔(&I)";
			this.radioTimeInterval.UseVisualStyleBackColor = true;
			this.radioTimeInterval.CheckedChanged += new System.EventHandler(this.radioTimeType_CheckedChanged);
			//
			// panelTimeSpecific
			//
			this.panelTimeSpecific.Controls.Add(this.listBoxTimes);
			this.panelTimeSpecific.Controls.Add(this.numTimeAddH);
			this.panelTimeSpecific.Controls.Add(this.labelTimeColon);
			this.panelTimeSpecific.Controls.Add(this.numTimeAddM);
			this.panelTimeSpecific.Controls.Add(this.buttonTimeAdd);
			this.panelTimeSpecific.Controls.Add(this.buttonTimeDelete);
			this.panelTimeSpecific.Location = new System.Drawing.Point(8, 40);
			this.panelTimeSpecific.Name = "panelTimeSpecific";
			this.panelTimeSpecific.Size = new System.Drawing.Size(440, 48);
			this.panelTimeSpecific.TabIndex = 2;
			//
			// listBoxTimes
			//
			this.listBoxTimes.FormattingEnabled = true;
			this.listBoxTimes.IntegralHeight = false;
			this.listBoxTimes.ItemHeight = 12;
			this.listBoxTimes.Location = new System.Drawing.Point(4, 4);
			this.listBoxTimes.Name = "listBoxTimes";
			this.listBoxTimes.Size = new System.Drawing.Size(160, 40);
			this.listBoxTimes.Sorted = true;
			this.listBoxTimes.TabIndex = 0;
			//
			// numTimeAddH
			//
			this.numTimeAddH.Location = new System.Drawing.Point(176, 8);
			this.numTimeAddH.Maximum = new decimal(new int[] { 23, 0, 0, 0 });
			this.numTimeAddH.Name = "numTimeAddH";
			this.numTimeAddH.Size = new System.Drawing.Size(44, 19);
			this.numTimeAddH.TabIndex = 1;
			//
			// labelTimeColon
			//
			this.labelTimeColon.AutoSize = true;
			this.labelTimeColon.Location = new System.Drawing.Point(222, 12);
			this.labelTimeColon.Name = "labelTimeColon";
			this.labelTimeColon.Size = new System.Drawing.Size(7, 12);
			this.labelTimeColon.TabIndex = 2;
			this.labelTimeColon.Text = ":";
			//
			// numTimeAddM
			//
			this.numTimeAddM.Location = new System.Drawing.Point(232, 8);
			this.numTimeAddM.Maximum = new decimal(new int[] { 59, 0, 0, 0 });
			this.numTimeAddM.Name = "numTimeAddM";
			this.numTimeAddM.Size = new System.Drawing.Size(44, 19);
			this.numTimeAddM.TabIndex = 3;
			//
			// buttonTimeAdd
			//
			this.buttonTimeAdd.Location = new System.Drawing.Point(284, 6);
			this.buttonTimeAdd.Name = "buttonTimeAdd";
			this.buttonTimeAdd.Size = new System.Drawing.Size(60, 23);
			this.buttonTimeAdd.TabIndex = 4;
			this.buttonTimeAdd.Text = "追加";
			this.buttonTimeAdd.UseVisualStyleBackColor = true;
			this.buttonTimeAdd.Click += new System.EventHandler(this.buttonTimeAdd_Click);
			//
			// buttonTimeDelete
			//
			this.buttonTimeDelete.Location = new System.Drawing.Point(352, 6);
			this.buttonTimeDelete.Name = "buttonTimeDelete";
			this.buttonTimeDelete.Size = new System.Drawing.Size(60, 23);
			this.buttonTimeDelete.TabIndex = 5;
			this.buttonTimeDelete.Text = "削除";
			this.buttonTimeDelete.UseVisualStyleBackColor = true;
			this.buttonTimeDelete.Click += new System.EventHandler(this.buttonTimeDelete_Click);
			//
			// panelTimeInterval
			//
			this.panelTimeInterval.Controls.Add(this.labelTimeStart);
			this.panelTimeInterval.Controls.Add(this.numTimeIntervalStartH);
			this.panelTimeInterval.Controls.Add(this.labelTimeStartColon);
			this.panelTimeInterval.Controls.Add(this.numTimeIntervalStartM);
			this.panelTimeInterval.Controls.Add(this.labelTimeTilde);
			this.panelTimeInterval.Controls.Add(this.numTimeIntervalEndH);
			this.panelTimeInterval.Controls.Add(this.labelTimeEndColon);
			this.panelTimeInterval.Controls.Add(this.numTimeIntervalEndM);
			this.panelTimeInterval.Controls.Add(this.labelTimeEvery);
			this.panelTimeInterval.Controls.Add(this.numTimeInterval);
			this.panelTimeInterval.Controls.Add(this.labelTimeMin);
			this.panelTimeInterval.Location = new System.Drawing.Point(8, 92);
			this.panelTimeInterval.Name = "panelTimeInterval";
			this.panelTimeInterval.Size = new System.Drawing.Size(440, 48);
			this.panelTimeInterval.TabIndex = 3;
			//
			// labelTimeStart
			//
			this.labelTimeStart.AutoSize = true;
			this.labelTimeStart.Location = new System.Drawing.Point(4, 12);
			this.labelTimeStart.Name = "labelTimeStart";
			this.labelTimeStart.Size = new System.Drawing.Size(29, 12);
			this.labelTimeStart.TabIndex = 0;
			this.labelTimeStart.Text = "開始";
			//
			// numTimeIntervalStartH
			//
			this.numTimeIntervalStartH.Location = new System.Drawing.Point(40, 8);
			this.numTimeIntervalStartH.Maximum = new decimal(new int[] { 23, 0, 0, 0 });
			this.numTimeIntervalStartH.Name = "numTimeIntervalStartH";
			this.numTimeIntervalStartH.Size = new System.Drawing.Size(44, 19);
			this.numTimeIntervalStartH.TabIndex = 1;
			//
			// labelTimeStartColon
			//
			this.labelTimeStartColon.AutoSize = true;
			this.labelTimeStartColon.Location = new System.Drawing.Point(86, 12);
			this.labelTimeStartColon.Name = "labelTimeStartColon";
			this.labelTimeStartColon.Size = new System.Drawing.Size(7, 12);
			this.labelTimeStartColon.TabIndex = 2;
			this.labelTimeStartColon.Text = ":";
			//
			// numTimeIntervalStartM
			//
			this.numTimeIntervalStartM.Location = new System.Drawing.Point(96, 8);
			this.numTimeIntervalStartM.Maximum = new decimal(new int[] { 59, 0, 0, 0 });
			this.numTimeIntervalStartM.Name = "numTimeIntervalStartM";
			this.numTimeIntervalStartM.Size = new System.Drawing.Size(44, 19);
			this.numTimeIntervalStartM.TabIndex = 3;
			//
			// labelTimeTilde
			//
			this.labelTimeTilde.AutoSize = true;
			this.labelTimeTilde.Location = new System.Drawing.Point(144, 12);
			this.labelTimeTilde.Name = "labelTimeTilde";
			this.labelTimeTilde.Size = new System.Drawing.Size(17, 12);
			this.labelTimeTilde.TabIndex = 4;
			this.labelTimeTilde.Text = "～";
			//
			// numTimeIntervalEndH
			//
			this.numTimeIntervalEndH.Location = new System.Drawing.Point(164, 8);
			this.numTimeIntervalEndH.Maximum = new decimal(new int[] { 23, 0, 0, 0 });
			this.numTimeIntervalEndH.Name = "numTimeIntervalEndH";
			this.numTimeIntervalEndH.Size = new System.Drawing.Size(44, 19);
			this.numTimeIntervalEndH.TabIndex = 5;
			//
			// labelTimeEndColon
			//
			this.labelTimeEndColon.AutoSize = true;
			this.labelTimeEndColon.Location = new System.Drawing.Point(210, 12);
			this.labelTimeEndColon.Name = "labelTimeEndColon";
			this.labelTimeEndColon.Size = new System.Drawing.Size(7, 12);
			this.labelTimeEndColon.TabIndex = 6;
			this.labelTimeEndColon.Text = ":";
			//
			// numTimeIntervalEndM
			//
			this.numTimeIntervalEndM.Location = new System.Drawing.Point(220, 8);
			this.numTimeIntervalEndM.Maximum = new decimal(new int[] { 59, 0, 0, 0 });
			this.numTimeIntervalEndM.Name = "numTimeIntervalEndM";
			this.numTimeIntervalEndM.Size = new System.Drawing.Size(44, 19);
			this.numTimeIntervalEndM.TabIndex = 7;
			//
			// labelTimeEvery
			//
			this.labelTimeEvery.AutoSize = true;
			this.labelTimeEvery.Location = new System.Drawing.Point(272, 12);
			this.labelTimeEvery.Name = "labelTimeEvery";
			this.labelTimeEvery.Size = new System.Drawing.Size(17, 12);
			this.labelTimeEvery.TabIndex = 8;
			this.labelTimeEvery.Text = "毎";
			//
			// numTimeInterval
			//
			this.numTimeInterval.Location = new System.Drawing.Point(296, 8);
			this.numTimeInterval.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
			this.numTimeInterval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numTimeInterval.Name = "numTimeInterval";
			this.numTimeInterval.Size = new System.Drawing.Size(56, 19);
			this.numTimeInterval.TabIndex = 9;
			this.numTimeInterval.Value = new decimal(new int[] { 60, 0, 0, 0 });
			//
			// labelTimeMin
			//
			this.labelTimeMin.AutoSize = true;
			this.labelTimeMin.Location = new System.Drawing.Point(356, 12);
			this.labelTimeMin.Name = "labelTimeMin";
			this.labelTimeMin.Size = new System.Drawing.Size(17, 12);
			this.labelTimeMin.TabIndex = 10;
			this.labelTimeMin.Text = "分";
			//
			// groupBoxDate
			//
			this.groupBoxDate.Controls.Add(this.radioDateWeekday);
			this.groupBoxDate.Controls.Add(this.radioDateInterval);
			this.groupBoxDate.Controls.Add(this.panelDateWeekday);
			this.groupBoxDate.Controls.Add(this.panelDateInterval);
			this.groupBoxDate.Location = new System.Drawing.Point(8, 200);
			this.groupBoxDate.Name = "groupBoxDate";
			this.groupBoxDate.Size = new System.Drawing.Size(456, 152);
			this.groupBoxDate.TabIndex = 2;
			this.groupBoxDate.TabStop = false;
			this.groupBoxDate.Text = "日付設定";
			//
			// radioDateWeekday
			//
			this.radioDateWeekday.AutoSize = true;
			this.radioDateWeekday.Location = new System.Drawing.Point(16, 20);
			this.radioDateWeekday.Name = "radioDateWeekday";
			this.radioDateWeekday.Size = new System.Drawing.Size(73, 16);
			this.radioDateWeekday.TabIndex = 0;
			this.radioDateWeekday.TabStop = true;
			this.radioDateWeekday.Text = "曜日(&W)";
			this.radioDateWeekday.UseVisualStyleBackColor = true;
			this.radioDateWeekday.CheckedChanged += new System.EventHandler(this.radioDateType_CheckedChanged);
			//
			// radioDateInterval
			//
			this.radioDateInterval.AutoSize = true;
			this.radioDateInterval.Location = new System.Drawing.Point(120, 20);
			this.radioDateInterval.Name = "radioDateInterval";
			this.radioDateInterval.Size = new System.Drawing.Size(93, 16);
			this.radioDateInterval.TabIndex = 1;
			this.radioDateInterval.TabStop = true;
			this.radioDateInterval.Text = "日数間隔(&D)";
			this.radioDateInterval.UseVisualStyleBackColor = true;
			this.radioDateInterval.CheckedChanged += new System.EventHandler(this.radioDateType_CheckedChanged);
			//
			// panelDateWeekday
			//
			this.panelDateWeekday.Controls.Add(this.numWeeksStartM);
			this.panelDateWeekday.Controls.Add(this.labelWeeksStartSlash);
			this.panelDateWeekday.Controls.Add(this.numWeeksStartD);
			this.panelDateWeekday.Controls.Add(this.labelWeeksTilde);
			this.panelDateWeekday.Controls.Add(this.numWeeksEndM);
			this.panelDateWeekday.Controls.Add(this.labelWeeksEndSlash);
			this.panelDateWeekday.Controls.Add(this.numWeeksEndD);
			this.panelDateWeekday.Controls.Add(this.checkMon);
			this.panelDateWeekday.Controls.Add(this.checkTue);
			this.panelDateWeekday.Controls.Add(this.checkWed);
			this.panelDateWeekday.Controls.Add(this.checkThu);
			this.panelDateWeekday.Controls.Add(this.checkFri);
			this.panelDateWeekday.Controls.Add(this.checkSat);
			this.panelDateWeekday.Controls.Add(this.checkSun);
			this.panelDateWeekday.Location = new System.Drawing.Point(8, 40);
			this.panelDateWeekday.Name = "panelDateWeekday";
			this.panelDateWeekday.Size = new System.Drawing.Size(440, 48);
			this.panelDateWeekday.TabIndex = 2;
			//
			// numWeeksStartM
			//
			this.numWeeksStartM.Location = new System.Drawing.Point(4, 4);
			this.numWeeksStartM.Maximum = new decimal(new int[] { 12, 0, 0, 0 });
			this.numWeeksStartM.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numWeeksStartM.Name = "numWeeksStartM";
			this.numWeeksStartM.Size = new System.Drawing.Size(40, 19);
			this.numWeeksStartM.TabIndex = 0;
			this.numWeeksStartM.Value = new decimal(new int[] { 1, 0, 0, 0 });
			//
			// labelWeeksStartSlash
			//
			this.labelWeeksStartSlash.AutoSize = true;
			this.labelWeeksStartSlash.Location = new System.Drawing.Point(46, 8);
			this.labelWeeksStartSlash.Name = "labelWeeksStartSlash";
			this.labelWeeksStartSlash.Size = new System.Drawing.Size(7, 12);
			this.labelWeeksStartSlash.TabIndex = 1;
			this.labelWeeksStartSlash.Text = "/";
			//
			// numWeeksStartD
			//
			this.numWeeksStartD.Location = new System.Drawing.Point(56, 4);
			this.numWeeksStartD.Maximum = new decimal(new int[] { 31, 0, 0, 0 });
			this.numWeeksStartD.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numWeeksStartD.Name = "numWeeksStartD";
			this.numWeeksStartD.Size = new System.Drawing.Size(40, 19);
			this.numWeeksStartD.TabIndex = 2;
			this.numWeeksStartD.Value = new decimal(new int[] { 1, 0, 0, 0 });
			//
			// labelWeeksTilde
			//
			this.labelWeeksTilde.AutoSize = true;
			this.labelWeeksTilde.Location = new System.Drawing.Point(100, 8);
			this.labelWeeksTilde.Name = "labelWeeksTilde";
			this.labelWeeksTilde.Size = new System.Drawing.Size(17, 12);
			this.labelWeeksTilde.TabIndex = 3;
			this.labelWeeksTilde.Text = "～";
			//
			// numWeeksEndM
			//
			this.numWeeksEndM.Location = new System.Drawing.Point(120, 4);
			this.numWeeksEndM.Maximum = new decimal(new int[] { 12, 0, 0, 0 });
			this.numWeeksEndM.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numWeeksEndM.Name = "numWeeksEndM";
			this.numWeeksEndM.Size = new System.Drawing.Size(40, 19);
			this.numWeeksEndM.TabIndex = 4;
			this.numWeeksEndM.Value = new decimal(new int[] { 12, 0, 0, 0 });
			//
			// labelWeeksEndSlash
			//
			this.labelWeeksEndSlash.AutoSize = true;
			this.labelWeeksEndSlash.Location = new System.Drawing.Point(162, 8);
			this.labelWeeksEndSlash.Name = "labelWeeksEndSlash";
			this.labelWeeksEndSlash.Size = new System.Drawing.Size(7, 12);
			this.labelWeeksEndSlash.TabIndex = 5;
			this.labelWeeksEndSlash.Text = "/";
			//
			// numWeeksEndD
			//
			this.numWeeksEndD.Location = new System.Drawing.Point(172, 4);
			this.numWeeksEndD.Maximum = new decimal(new int[] { 31, 0, 0, 0 });
			this.numWeeksEndD.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numWeeksEndD.Name = "numWeeksEndD";
			this.numWeeksEndD.Size = new System.Drawing.Size(40, 19);
			this.numWeeksEndD.TabIndex = 6;
			this.numWeeksEndD.Value = new decimal(new int[] { 31, 0, 0, 0 });
			//
			// checkMon
			//
			this.checkMon.AutoSize = true;
			this.checkMon.Location = new System.Drawing.Point(4, 28);
			this.checkMon.Name = "checkMon";
			this.checkMon.Size = new System.Drawing.Size(36, 16);
			this.checkMon.TabIndex = 7;
			this.checkMon.Text = "月";
			this.checkMon.UseVisualStyleBackColor = true;
			//
			// checkTue
			//
			this.checkTue.AutoSize = true;
			this.checkTue.Location = new System.Drawing.Point(48, 28);
			this.checkTue.Name = "checkTue";
			this.checkTue.Size = new System.Drawing.Size(36, 16);
			this.checkTue.TabIndex = 8;
			this.checkTue.Text = "火";
			this.checkTue.UseVisualStyleBackColor = true;
			//
			// checkWed
			//
			this.checkWed.AutoSize = true;
			this.checkWed.Location = new System.Drawing.Point(92, 28);
			this.checkWed.Name = "checkWed";
			this.checkWed.Size = new System.Drawing.Size(36, 16);
			this.checkWed.TabIndex = 9;
			this.checkWed.Text = "水";
			this.checkWed.UseVisualStyleBackColor = true;
			//
			// checkThu
			//
			this.checkThu.AutoSize = true;
			this.checkThu.Location = new System.Drawing.Point(136, 28);
			this.checkThu.Name = "checkThu";
			this.checkThu.Size = new System.Drawing.Size(36, 16);
			this.checkThu.TabIndex = 10;
			this.checkThu.Text = "木";
			this.checkThu.UseVisualStyleBackColor = true;
			//
			// checkFri
			//
			this.checkFri.AutoSize = true;
			this.checkFri.Location = new System.Drawing.Point(180, 28);
			this.checkFri.Name = "checkFri";
			this.checkFri.Size = new System.Drawing.Size(36, 16);
			this.checkFri.TabIndex = 11;
			this.checkFri.Text = "金";
			this.checkFri.UseVisualStyleBackColor = true;
			//
			// checkSat
			//
			this.checkSat.AutoSize = true;
			this.checkSat.Location = new System.Drawing.Point(224, 28);
			this.checkSat.Name = "checkSat";
			this.checkSat.Size = new System.Drawing.Size(36, 16);
			this.checkSat.TabIndex = 12;
			this.checkSat.Text = "土";
			this.checkSat.UseVisualStyleBackColor = true;
			//
			// checkSun
			//
			this.checkSun.AutoSize = true;
			this.checkSun.Location = new System.Drawing.Point(268, 28);
			this.checkSun.Name = "checkSun";
			this.checkSun.Size = new System.Drawing.Size(36, 16);
			this.checkSun.TabIndex = 13;
			this.checkSun.Text = "日";
			this.checkSun.UseVisualStyleBackColor = true;
			//
			// panelDateInterval
			//
			this.panelDateInterval.Controls.Add(this.numDateIntervalStartM);
			this.panelDateInterval.Controls.Add(this.labelDateStartSlash);
			this.panelDateInterval.Controls.Add(this.numDateIntervalStartD);
			this.panelDateInterval.Controls.Add(this.labelDateTilde);
			this.panelDateInterval.Controls.Add(this.numDateIntervalEndM);
			this.panelDateInterval.Controls.Add(this.labelDateEndSlash);
			this.panelDateInterval.Controls.Add(this.numDateIntervalEndD);
			this.panelDateInterval.Controls.Add(this.labelDateEvery);
			this.panelDateInterval.Controls.Add(this.numDateInterval);
			this.panelDateInterval.Controls.Add(this.labelDateDay);
			this.panelDateInterval.Location = new System.Drawing.Point(8, 92);
			this.panelDateInterval.Name = "panelDateInterval";
			this.panelDateInterval.Size = new System.Drawing.Size(440, 48);
			this.panelDateInterval.TabIndex = 3;
			//
			// numDateIntervalStartM
			//
			this.numDateIntervalStartM.Location = new System.Drawing.Point(4, 8);
			this.numDateIntervalStartM.Maximum = new decimal(new int[] { 12, 0, 0, 0 });
			this.numDateIntervalStartM.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numDateIntervalStartM.Name = "numDateIntervalStartM";
			this.numDateIntervalStartM.Size = new System.Drawing.Size(40, 19);
			this.numDateIntervalStartM.TabIndex = 0;
			this.numDateIntervalStartM.Value = new decimal(new int[] { 1, 0, 0, 0 });
			//
			// labelDateStartSlash
			//
			this.labelDateStartSlash.AutoSize = true;
			this.labelDateStartSlash.Location = new System.Drawing.Point(46, 12);
			this.labelDateStartSlash.Name = "labelDateStartSlash";
			this.labelDateStartSlash.Size = new System.Drawing.Size(7, 12);
			this.labelDateStartSlash.TabIndex = 1;
			this.labelDateStartSlash.Text = "/";
			//
			// numDateIntervalStartD
			//
			this.numDateIntervalStartD.Location = new System.Drawing.Point(56, 8);
			this.numDateIntervalStartD.Maximum = new decimal(new int[] { 31, 0, 0, 0 });
			this.numDateIntervalStartD.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numDateIntervalStartD.Name = "numDateIntervalStartD";
			this.numDateIntervalStartD.Size = new System.Drawing.Size(40, 19);
			this.numDateIntervalStartD.TabIndex = 2;
			this.numDateIntervalStartD.Value = new decimal(new int[] { 1, 0, 0, 0 });
			//
			// labelDateTilde
			//
			this.labelDateTilde.AutoSize = true;
			this.labelDateTilde.Location = new System.Drawing.Point(100, 12);
			this.labelDateTilde.Name = "labelDateTilde";
			this.labelDateTilde.Size = new System.Drawing.Size(17, 12);
			this.labelDateTilde.TabIndex = 3;
			this.labelDateTilde.Text = "～";
			//
			// numDateIntervalEndM
			//
			this.numDateIntervalEndM.Location = new System.Drawing.Point(120, 8);
			this.numDateIntervalEndM.Maximum = new decimal(new int[] { 12, 0, 0, 0 });
			this.numDateIntervalEndM.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numDateIntervalEndM.Name = "numDateIntervalEndM";
			this.numDateIntervalEndM.Size = new System.Drawing.Size(40, 19);
			this.numDateIntervalEndM.TabIndex = 4;
			this.numDateIntervalEndM.Value = new decimal(new int[] { 12, 0, 0, 0 });
			//
			// labelDateEndSlash
			//
			this.labelDateEndSlash.AutoSize = true;
			this.labelDateEndSlash.Location = new System.Drawing.Point(162, 12);
			this.labelDateEndSlash.Name = "labelDateEndSlash";
			this.labelDateEndSlash.Size = new System.Drawing.Size(7, 12);
			this.labelDateEndSlash.TabIndex = 5;
			this.labelDateEndSlash.Text = "/";
			//
			// numDateIntervalEndD
			//
			this.numDateIntervalEndD.Location = new System.Drawing.Point(172, 8);
			this.numDateIntervalEndD.Maximum = new decimal(new int[] { 31, 0, 0, 0 });
			this.numDateIntervalEndD.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numDateIntervalEndD.Name = "numDateIntervalEndD";
			this.numDateIntervalEndD.Size = new System.Drawing.Size(40, 19);
			this.numDateIntervalEndD.TabIndex = 6;
			this.numDateIntervalEndD.Value = new decimal(new int[] { 31, 0, 0, 0 });
			//
			// labelDateEvery
			//
			this.labelDateEvery.AutoSize = true;
			this.labelDateEvery.Location = new System.Drawing.Point(220, 12);
			this.labelDateEvery.Name = "labelDateEvery";
			this.labelDateEvery.Size = new System.Drawing.Size(17, 12);
			this.labelDateEvery.TabIndex = 7;
			this.labelDateEvery.Text = "毎";
			//
			// numDateInterval
			//
			this.numDateInterval.Location = new System.Drawing.Point(244, 8);
			this.numDateInterval.Maximum = new decimal(new int[] { 366, 0, 0, 0 });
			this.numDateInterval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numDateInterval.Name = "numDateInterval";
			this.numDateInterval.Size = new System.Drawing.Size(56, 19);
			this.numDateInterval.TabIndex = 8;
			this.numDateInterval.Value = new decimal(new int[] { 1, 0, 0, 0 });
			//
			// labelDateDay
			//
			this.labelDateDay.AutoSize = true;
			this.labelDateDay.Location = new System.Drawing.Point(304, 12);
			this.labelDateDay.Name = "labelDateDay";
			this.labelDateDay.Size = new System.Drawing.Size(17, 12);
			this.labelDateDay.TabIndex = 9;
			this.labelDateDay.Text = "日";
			//
			// buttonOk
			//
			this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOk.Location = new System.Drawing.Point(308, 364);
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.Size = new System.Drawing.Size(75, 23);
			this.buttonOk.TabIndex = 3;
			this.buttonOk.Text = "OK";
			this.buttonOk.UseVisualStyleBackColor = true;
			this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(388, 364);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 4;
			this.buttonCancel.Text = "キャンセル";
			this.buttonCancel.UseVisualStyleBackColor = true;
			//
			// ScheduleEditForm
			//
			this.AcceptButton = this.buttonOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(473, 401);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOk);
			this.Controls.Add(this.groupBoxDate);
			this.Controls.Add(this.groupBoxTime);
			this.Controls.Add(this.checkBoxEnable);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.Name = "ScheduleEditForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "らんちゃ : スケジュールの編集";
			this.groupBoxTime.ResumeLayout(false);
			this.groupBoxTime.PerformLayout();
			this.panelTimeSpecific.ResumeLayout(false);
			this.panelTimeSpecific.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numTimeAddH)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numTimeAddM)).EndInit();
			this.panelTimeInterval.ResumeLayout(false);
			this.panelTimeInterval.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numTimeIntervalStartH)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numTimeIntervalStartM)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numTimeIntervalEndH)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numTimeIntervalEndM)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numTimeInterval)).EndInit();
			this.groupBoxDate.ResumeLayout(false);
			this.groupBoxDate.PerformLayout();
			this.panelDateWeekday.ResumeLayout(false);
			this.panelDateWeekday.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numWeeksStartM)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numWeeksStartD)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numWeeksEndM)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numWeeksEndD)).EndInit();
			this.panelDateInterval.ResumeLayout(false);
			this.panelDateInterval.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numDateIntervalStartM)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numDateIntervalStartD)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numDateIntervalEndM)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numDateIntervalEndD)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numDateInterval)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox checkBoxEnable;
		private System.Windows.Forms.GroupBox groupBoxTime;
		private System.Windows.Forms.RadioButton radioTimeSpecific;
		private System.Windows.Forms.RadioButton radioTimeInterval;
		private System.Windows.Forms.Panel panelTimeSpecific;
		private System.Windows.Forms.ListBox listBoxTimes;
		private System.Windows.Forms.NumericUpDown numTimeAddH;
		private System.Windows.Forms.Label labelTimeColon;
		private System.Windows.Forms.NumericUpDown numTimeAddM;
		private System.Windows.Forms.Button buttonTimeAdd;
		private System.Windows.Forms.Button buttonTimeDelete;
		private System.Windows.Forms.Panel panelTimeInterval;
		private System.Windows.Forms.Label labelTimeStart;
		private System.Windows.Forms.NumericUpDown numTimeIntervalStartH;
		private System.Windows.Forms.Label labelTimeStartColon;
		private System.Windows.Forms.NumericUpDown numTimeIntervalStartM;
		private System.Windows.Forms.Label labelTimeTilde;
		private System.Windows.Forms.NumericUpDown numTimeIntervalEndH;
		private System.Windows.Forms.Label labelTimeEndColon;
		private System.Windows.Forms.NumericUpDown numTimeIntervalEndM;
		private System.Windows.Forms.Label labelTimeEvery;
		private System.Windows.Forms.NumericUpDown numTimeInterval;
		private System.Windows.Forms.Label labelTimeMin;
		private System.Windows.Forms.GroupBox groupBoxDate;
		private System.Windows.Forms.RadioButton radioDateWeekday;
		private System.Windows.Forms.RadioButton radioDateInterval;
		private System.Windows.Forms.Panel panelDateWeekday;
		private System.Windows.Forms.NumericUpDown numWeeksStartM;
		private System.Windows.Forms.Label labelWeeksStartSlash;
		private System.Windows.Forms.NumericUpDown numWeeksStartD;
		private System.Windows.Forms.Label labelWeeksTilde;
		private System.Windows.Forms.NumericUpDown numWeeksEndM;
		private System.Windows.Forms.Label labelWeeksEndSlash;
		private System.Windows.Forms.NumericUpDown numWeeksEndD;
		private System.Windows.Forms.CheckBox checkMon;
		private System.Windows.Forms.CheckBox checkTue;
		private System.Windows.Forms.CheckBox checkWed;
		private System.Windows.Forms.CheckBox checkThu;
		private System.Windows.Forms.CheckBox checkFri;
		private System.Windows.Forms.CheckBox checkSat;
		private System.Windows.Forms.CheckBox checkSun;
		private System.Windows.Forms.Panel panelDateInterval;
		private System.Windows.Forms.NumericUpDown numDateIntervalStartM;
		private System.Windows.Forms.Label labelDateStartSlash;
		private System.Windows.Forms.NumericUpDown numDateIntervalStartD;
		private System.Windows.Forms.Label labelDateTilde;
		private System.Windows.Forms.NumericUpDown numDateIntervalEndM;
		private System.Windows.Forms.Label labelDateEndSlash;
		private System.Windows.Forms.NumericUpDown numDateIntervalEndD;
		private System.Windows.Forms.Label labelDateEvery;
		private System.Windows.Forms.NumericUpDown numDateInterval;
		private System.Windows.Forms.Label labelDateDay;
		private System.Windows.Forms.Button buttonOk;
		private System.Windows.Forms.Button buttonCancel;
	}
}
