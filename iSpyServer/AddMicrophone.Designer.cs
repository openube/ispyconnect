namespace iSpyServer
{
    partial class AddMicrophone
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblAudioSource = new System.Windows.Forms.Label();
            this.btnSelectSource = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tcMicrophone = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.rtbDescription = new System.Windows.Forms.RichTextBox();
            this.label66 = new System.Windows.Forms.Label();
            this.chkActive = new System.Windows.Forms.CheckBox();
            this.txtMicrophoneName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.label80 = new System.Windows.Forms.Label();
            this.pnlSchedule = new System.Windows.Forms.Panel();
            this.chkScheduleAlerts = new System.Windows.Forms.CheckBox();
            this.chkScheduleRecordOnDetect = new System.Windows.Forms.CheckBox();
            this.chkScheduleActive = new System.Windows.Forms.CheckBox();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.chkRecordSchedule = new System.Windows.Forms.CheckBox();
            this.btnDelete = new System.Windows.Forms.Button();
            this.label50 = new System.Windows.Forms.Label();
            this.chkSun = new System.Windows.Forms.CheckBox();
            this.chkSat = new System.Windows.Forms.CheckBox();
            this.chkFri = new System.Windows.Forms.CheckBox();
            this.chkThu = new System.Windows.Forms.CheckBox();
            this.chkWed = new System.Windows.Forms.CheckBox();
            this.chkTue = new System.Windows.Forms.CheckBox();
            this.chkMon = new System.Windows.Forms.CheckBox();
            this.label49 = new System.Windows.Forms.Label();
            this.lbSchedule = new System.Windows.Forms.ListBox();
            this.button2 = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.ddlHourStart = new System.Windows.Forms.ComboBox();
            this.ddlMinuteEnd = new System.Windows.Forms.ComboBox();
            this.ddlMinuteStart = new System.Windows.Forms.ComboBox();
            this.ddlHourEnd = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.chkSchedule = new System.Windows.Forms.CheckBox();
            this.btnNext = new System.Windows.Forms.Button();
            this.btnBack = new System.Windows.Forms.Button();
            this.btnFinish = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.ofdDetect = new System.Windows.Forms.OpenFileDialog();
            this.tcMicrophone.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.pnlSchedule.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblAudioSource
            // 
            this.lblAudioSource.AutoSize = true;
            this.lblAudioSource.Location = new System.Drawing.Point(163, 11);
            this.lblAudioSource.Name = "lblAudioSource";
            this.lblAudioSource.Size = new System.Drawing.Size(68, 13);
            this.lblAudioSource.TabIndex = 15;
            this.lblAudioSource.Text = "AudioSource";
            // 
            // btnSelectSource
            // 
            this.btnSelectSource.Location = new System.Drawing.Point(134, 6);
            this.btnSelectSource.Name = "btnSelectSource";
            this.btnSelectSource.Size = new System.Drawing.Size(23, 23);
            this.btnSelectSource.TabIndex = 14;
            this.btnSelectSource.Text = "...";
            this.btnSelectSource.UseVisualStyleBackColor = true;
            this.btnSelectSource.Click += new System.EventHandler(this.btnSelectSource_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 11);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "Source";
            // 
            // tcMicrophone
            // 
            this.tcMicrophone.Controls.Add(this.tabPage1);
            this.tcMicrophone.Controls.Add(this.tabPage3);
            this.tcMicrophone.Dock = System.Windows.Forms.DockStyle.Top;
            this.tcMicrophone.Location = new System.Drawing.Point(5, 5);
            this.tcMicrophone.Name = "tcMicrophone";
            this.tcMicrophone.SelectedIndex = 0;
            this.tcMicrophone.Size = new System.Drawing.Size(540, 542);
            this.tcMicrophone.TabIndex = 16;
            this.tcMicrophone.SelectedIndexChanged += new System.EventHandler(this.tcMicrophone_SelectedIndexChanged);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.rtbDescription);
            this.tabPage1.Controls.Add(this.label66);
            this.tabPage1.Controls.Add(this.chkActive);
            this.tabPage1.Controls.Add(this.txtMicrophoneName);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.btnSelectSource);
            this.tabPage1.Controls.Add(this.lblAudioSource);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(532, 516);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Microphone";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // rtbDescription
            // 
            this.rtbDescription.Location = new System.Drawing.Point(134, 93);
            this.rtbDescription.Name = "rtbDescription";
            this.rtbDescription.Size = new System.Drawing.Size(340, 52);
            this.rtbDescription.TabIndex = 52;
            this.rtbDescription.Text = global::iSpyServer.Properties.Resources.nothing;
            // 
            // label66
            // 
            this.label66.AutoSize = true;
            this.label66.Location = new System.Drawing.Point(5, 96);
            this.label66.Name = "label66";
            this.label66.Size = new System.Drawing.Size(60, 13);
            this.label66.TabIndex = 51;
            this.label66.Text = "Description";
            // 
            // chkActive
            // 
            this.chkActive.AutoSize = true;
            this.chkActive.Location = new System.Drawing.Point(134, 70);
            this.chkActive.Name = "chkActive";
            this.chkActive.Size = new System.Drawing.Size(115, 17);
            this.chkActive.TabIndex = 23;
            this.chkActive.Text = "Microphone Active";
            this.chkActive.UseVisualStyleBackColor = true;
            this.chkActive.CheckedChanged += new System.EventHandler(this.chkActive_CheckedChanged);
            // 
            // txtMicrophoneName
            // 
            this.txtMicrophoneName.Location = new System.Drawing.Point(134, 39);
            this.txtMicrophoneName.MaxLength = 100;
            this.txtMicrophoneName.Name = "txtMicrophoneName";
            this.txtMicrophoneName.Size = new System.Drawing.Size(213, 20);
            this.txtMicrophoneName.TabIndex = 21;
            this.toolTip1.SetToolTip(this.txtMicrophoneName, "Give your mic a descriptive name, eg Office Mic");
            this.txtMicrophoneName.TextChanged += new System.EventHandler(this.txtMicrophoneName_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 20;
            this.label1.Text = "Name";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.label80);
            this.tabPage3.Controls.Add(this.pnlSchedule);
            this.tabPage3.Controls.Add(this.chkSchedule);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(532, 516);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Scheduling";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // label80
            // 
            this.label80.Location = new System.Drawing.Point(11, 416);
            this.label80.Name = "label80";
            this.label80.Size = new System.Drawing.Size(505, 56);
            this.label80.TabIndex = 22;
            this.label80.Text = "Tip: To create a schedule overnight create a start time with no stop time and a s" +
    "top time with no start time.";
            // 
            // pnlSchedule
            // 
            this.pnlSchedule.Controls.Add(this.chkScheduleAlerts);
            this.pnlSchedule.Controls.Add(this.chkScheduleRecordOnDetect);
            this.pnlSchedule.Controls.Add(this.chkScheduleActive);
            this.pnlSchedule.Controls.Add(this.btnUpdate);
            this.pnlSchedule.Controls.Add(this.chkRecordSchedule);
            this.pnlSchedule.Controls.Add(this.btnDelete);
            this.pnlSchedule.Controls.Add(this.label50);
            this.pnlSchedule.Controls.Add(this.chkSun);
            this.pnlSchedule.Controls.Add(this.chkSat);
            this.pnlSchedule.Controls.Add(this.chkFri);
            this.pnlSchedule.Controls.Add(this.chkThu);
            this.pnlSchedule.Controls.Add(this.chkWed);
            this.pnlSchedule.Controls.Add(this.chkTue);
            this.pnlSchedule.Controls.Add(this.chkMon);
            this.pnlSchedule.Controls.Add(this.label49);
            this.pnlSchedule.Controls.Add(this.lbSchedule);
            this.pnlSchedule.Controls.Add(this.button2);
            this.pnlSchedule.Controls.Add(this.label7);
            this.pnlSchedule.Controls.Add(this.label10);
            this.pnlSchedule.Controls.Add(this.ddlHourStart);
            this.pnlSchedule.Controls.Add(this.ddlMinuteEnd);
            this.pnlSchedule.Controls.Add(this.ddlMinuteStart);
            this.pnlSchedule.Controls.Add(this.ddlHourEnd);
            this.pnlSchedule.Controls.Add(this.label8);
            this.pnlSchedule.Controls.Add(this.label9);
            this.pnlSchedule.Location = new System.Drawing.Point(3, 35);
            this.pnlSchedule.Name = "pnlSchedule";
            this.pnlSchedule.Size = new System.Drawing.Size(513, 378);
            this.pnlSchedule.TabIndex = 10;
            // 
            // chkScheduleAlerts
            // 
            this.chkScheduleAlerts.AutoSize = true;
            this.chkScheduleAlerts.Location = new System.Drawing.Point(260, 246);
            this.chkScheduleAlerts.Name = "chkScheduleAlerts";
            this.chkScheduleAlerts.Size = new System.Drawing.Size(94, 17);
            this.chkScheduleAlerts.TabIndex = 28;
            this.chkScheduleAlerts.Text = "Alerts Enabled";
            this.chkScheduleAlerts.UseVisualStyleBackColor = true;
            // 
            // chkScheduleRecordOnDetect
            // 
            this.chkScheduleRecordOnDetect.AutoSize = true;
            this.chkScheduleRecordOnDetect.Location = new System.Drawing.Point(260, 222);
            this.chkScheduleRecordOnDetect.Name = "chkScheduleRecordOnDetect";
            this.chkScheduleRecordOnDetect.Size = new System.Drawing.Size(111, 17);
            this.chkScheduleRecordOnDetect.TabIndex = 27;
            this.chkScheduleRecordOnDetect.Text = "Record on Detect";
            this.chkScheduleRecordOnDetect.UseVisualStyleBackColor = true;
            this.chkScheduleRecordOnDetect.CheckedChanged += new System.EventHandler(this.chkScheduleRecordOnDetect_CheckedChanged);
            // 
            // chkScheduleActive
            // 
            this.chkScheduleActive.AutoSize = true;
            this.chkScheduleActive.Checked = true;
            this.chkScheduleActive.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkScheduleActive.Location = new System.Drawing.Point(60, 306);
            this.chkScheduleActive.Name = "chkScheduleActive";
            this.chkScheduleActive.Size = new System.Drawing.Size(104, 17);
            this.chkScheduleActive.TabIndex = 26;
            this.chkScheduleActive.Text = "Schedule Active";
            this.chkScheduleActive.UseVisualStyleBackColor = true;
            // 
            // btnUpdate
            // 
            this.btnUpdate.Location = new System.Drawing.Point(341, 302);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(75, 23);
            this.btnUpdate.TabIndex = 25;
            this.btnUpdate.Text = "Update";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // chkRecordSchedule
            // 
            this.chkRecordSchedule.AutoSize = true;
            this.chkRecordSchedule.Location = new System.Drawing.Point(260, 199);
            this.chkRecordSchedule.Name = "chkRecordSchedule";
            this.chkRecordSchedule.Size = new System.Drawing.Size(149, 17);
            this.chkRecordSchedule.TabIndex = 24;
            this.chkRecordSchedule.Text = "Record on Schedule Start";
            this.chkRecordSchedule.UseVisualStyleBackColor = true;
            this.chkRecordSchedule.CheckedChanged += new System.EventHandler(this.chkRecordSchedule_CheckedChanged);
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(260, 302);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(75, 23);
            this.btnDelete.TabIndex = 21;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // label50
            // 
            this.label50.AutoSize = true;
            this.label50.Location = new System.Drawing.Point(19, 352);
            this.label50.Name = "label50";
            this.label50.Size = new System.Drawing.Size(244, 13);
            this.label50.TabIndex = 20;
            this.label50.Text = "Important: make sure your schedules don\'t overlap";
            // 
            // chkSun
            // 
            this.chkSun.AutoSize = true;
            this.chkSun.Location = new System.Drawing.Point(60, 246);
            this.chkSun.Name = "chkSun";
            this.chkSun.Size = new System.Drawing.Size(45, 17);
            this.chkSun.TabIndex = 18;
            this.chkSun.Text = "Sun";
            this.chkSun.UseVisualStyleBackColor = true;
            // 
            // chkSat
            // 
            this.chkSat.AutoSize = true;
            this.chkSat.Location = new System.Drawing.Point(172, 223);
            this.chkSat.Name = "chkSat";
            this.chkSat.Size = new System.Drawing.Size(42, 17);
            this.chkSat.TabIndex = 17;
            this.chkSat.Text = "Sat";
            this.chkSat.UseVisualStyleBackColor = true;
            // 
            // chkFri
            // 
            this.chkFri.AutoSize = true;
            this.chkFri.Location = new System.Drawing.Point(114, 223);
            this.chkFri.Name = "chkFri";
            this.chkFri.Size = new System.Drawing.Size(37, 17);
            this.chkFri.TabIndex = 16;
            this.chkFri.Text = "Fri";
            this.chkFri.UseVisualStyleBackColor = true;
            // 
            // chkThu
            // 
            this.chkThu.AutoSize = true;
            this.chkThu.Location = new System.Drawing.Point(60, 223);
            this.chkThu.Name = "chkThu";
            this.chkThu.Size = new System.Drawing.Size(45, 17);
            this.chkThu.TabIndex = 15;
            this.chkThu.Text = "Thu";
            this.chkThu.UseVisualStyleBackColor = true;
            // 
            // chkWed
            // 
            this.chkWed.AutoSize = true;
            this.chkWed.Location = new System.Drawing.Point(172, 199);
            this.chkWed.Name = "chkWed";
            this.chkWed.Size = new System.Drawing.Size(49, 17);
            this.chkWed.TabIndex = 14;
            this.chkWed.Text = "Wed";
            this.chkWed.UseVisualStyleBackColor = true;
            // 
            // chkTue
            // 
            this.chkTue.AutoSize = true;
            this.chkTue.Location = new System.Drawing.Point(114, 200);
            this.chkTue.Name = "chkTue";
            this.chkTue.Size = new System.Drawing.Size(45, 17);
            this.chkTue.TabIndex = 13;
            this.chkTue.Text = "Tue";
            this.chkTue.UseVisualStyleBackColor = true;
            // 
            // chkMon
            // 
            this.chkMon.AutoSize = true;
            this.chkMon.Location = new System.Drawing.Point(61, 200);
            this.chkMon.Name = "chkMon";
            this.chkMon.Size = new System.Drawing.Size(47, 17);
            this.chkMon.TabIndex = 12;
            this.chkMon.Text = "Mon";
            this.chkMon.UseVisualStyleBackColor = true;
            // 
            // label49
            // 
            this.label49.AutoSize = true;
            this.label49.Location = new System.Drawing.Point(19, 200);
            this.label49.Name = "label49";
            this.label49.Size = new System.Drawing.Size(31, 13);
            this.label49.TabIndex = 11;
            this.label49.Text = "Days";
            // 
            // lbSchedule
            // 
            this.lbSchedule.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lbSchedule.FormattingEnabled = true;
            this.lbSchedule.Location = new System.Drawing.Point(20, 12);
            this.lbSchedule.Name = "lbSchedule";
            this.lbSchedule.Size = new System.Drawing.Size(477, 134);
            this.lbSchedule.TabIndex = 10;
            this.toolTip1.SetToolTip(this.lbSchedule, "Press delete to remove an entry");
            this.lbSchedule.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lbSchedule_DrawItem);
            this.lbSchedule.SelectedIndexChanged += new System.EventHandler(this.lbSchedule_SelectedIndexChanged);
            this.lbSchedule.KeyUp += new System.Windows.Forms.KeyEventHandler(this.lbSchedule_KeyUp);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(422, 302);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 9;
            this.button2.Text = "Add";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(19, 168);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(32, 13);
            this.label7.TabIndex = 1;
            this.label7.Text = "Start:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(441, 168);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(10, 13);
            this.label10.TabIndex = 8;
            this.label10.Text = ":";
            // 
            // ddlHourStart
            // 
            this.ddlHourStart.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlHourStart.FormattingEnabled = true;
            this.ddlHourStart.Items.AddRange(new object[] {
            "-",
            "00",
            "01",
            "02",
            "03",
            "04",
            "05",
            "06",
            "07",
            "08",
            "09",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23"});
            this.ddlHourStart.Location = new System.Drawing.Point(61, 165);
            this.ddlHourStart.Name = "ddlHourStart";
            this.ddlHourStart.Size = new System.Drawing.Size(44, 21);
            this.ddlHourStart.TabIndex = 2;
            // 
            // ddlMinuteEnd
            // 
            this.ddlMinuteEnd.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlMinuteEnd.FormattingEnabled = true;
            this.ddlMinuteEnd.Items.AddRange(new object[] {
            "-",
            "00",
            "01",
            "02",
            "03",
            "04",
            "05",
            "06",
            "07",
            "08",
            "09",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59"});
            this.ddlMinuteEnd.Location = new System.Drawing.Point(453, 165);
            this.ddlMinuteEnd.Name = "ddlMinuteEnd";
            this.ddlMinuteEnd.Size = new System.Drawing.Size(44, 21);
            this.ddlMinuteEnd.TabIndex = 7;
            // 
            // ddlMinuteStart
            // 
            this.ddlMinuteStart.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlMinuteStart.FormattingEnabled = true;
            this.ddlMinuteStart.Items.AddRange(new object[] {
            "-",
            "00",
            "01",
            "02",
            "03",
            "04",
            "05",
            "06",
            "07",
            "08",
            "09",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59"});
            this.ddlMinuteStart.Location = new System.Drawing.Point(120, 165);
            this.ddlMinuteStart.Name = "ddlMinuteStart";
            this.ddlMinuteStart.Size = new System.Drawing.Size(44, 21);
            this.ddlMinuteStart.TabIndex = 3;
            // 
            // ddlHourEnd
            // 
            this.ddlHourEnd.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlHourEnd.FormattingEnabled = true;
            this.ddlHourEnd.Items.AddRange(new object[] {
            "-",
            "00",
            "01",
            "02",
            "03",
            "04",
            "05",
            "06",
            "07",
            "08",
            "09",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23"});
            this.ddlHourEnd.Location = new System.Drawing.Point(395, 165);
            this.ddlHourEnd.Name = "ddlHourEnd";
            this.ddlHourEnd.Size = new System.Drawing.Size(44, 21);
            this.ddlHourEnd.TabIndex = 6;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(108, 168);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(10, 13);
            this.label8.TabIndex = 4;
            this.label8.Text = ":";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(354, 168);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(32, 13);
            this.label9.TabIndex = 5;
            this.label9.Text = "Stop:";
            // 
            // chkSchedule
            // 
            this.chkSchedule.AutoSize = true;
            this.chkSchedule.Location = new System.Drawing.Point(8, 12);
            this.chkSchedule.Name = "chkSchedule";
            this.chkSchedule.Size = new System.Drawing.Size(130, 17);
            this.chkSchedule.TabIndex = 0;
            this.chkSchedule.Text = "Schedule Microphone";
            this.chkSchedule.UseVisualStyleBackColor = true;
            this.chkSchedule.CheckedChanged += new System.EventHandler(this.chkSchedule_CheckedChanged);
            // 
            // btnNext
            // 
            this.btnNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnNext.Location = new System.Drawing.Point(386, 554);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(75, 23);
            this.btnNext.TabIndex = 22;
            this.btnNext.Text = "Next >>";
            this.btnNext.UseVisualStyleBackColor = true;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // btnBack
            // 
            this.btnBack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBack.Location = new System.Drawing.Point(305, 554);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(75, 23);
            this.btnBack.TabIndex = 11;
            this.btnBack.Text = "<< Back";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnFinish
            // 
            this.btnFinish.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFinish.Location = new System.Drawing.Point(466, 554);
            this.btnFinish.Name = "btnFinish";
            this.btnFinish.Size = new System.Drawing.Size(75, 23);
            this.btnFinish.TabIndex = 10;
            this.btnFinish.Text = "Finish";
            this.btnFinish.UseVisualStyleBackColor = true;
            this.btnFinish.Click += new System.EventHandler(this.btnFinish_Click);
            // 
            // ofdDetect
            // 
            this.ofdDetect.FileName = "openFileDialog1";
            // 
            // AddMicrophone
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(550, 594);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.tcMicrophone);
            this.Controls.Add(this.btnNext);
            this.Controls.Add(this.btnFinish);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "AddMicrophone";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Microphone";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AddMicrophone_FormClosing);
            this.Load += new System.EventHandler(this.AddMicrophone_Load);
            this.tcMicrophone.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.pnlSchedule.ResumeLayout(false);
            this.pnlSchedule.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblAudioSource;
        private System.Windows.Forms.Button btnSelectSource;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TabControl tcMicrophone;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.TextBox txtMicrophoneName;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox chkActive;
        private System.Windows.Forms.OpenFileDialog ofdDetect;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Button btnFinish;
        private System.Windows.Forms.CheckBox chkSchedule;
        private System.Windows.Forms.Panel pnlSchedule;
        private System.Windows.Forms.CheckBox chkSun;
        private System.Windows.Forms.CheckBox chkSat;
        private System.Windows.Forms.CheckBox chkFri;
        private System.Windows.Forms.CheckBox chkThu;
        private System.Windows.Forms.CheckBox chkWed;
        private System.Windows.Forms.CheckBox chkTue;
        private System.Windows.Forms.CheckBox chkMon;
        private System.Windows.Forms.Label label49;
        private System.Windows.Forms.ListBox lbSchedule;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox ddlHourStart;
        private System.Windows.Forms.ComboBox ddlMinuteEnd;
        private System.Windows.Forms.ComboBox ddlMinuteStart;
        private System.Windows.Forms.ComboBox ddlHourEnd;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label50;
        private System.Windows.Forms.RichTextBox rtbDescription;
        private System.Windows.Forms.Label label66;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Label label80;
        private System.Windows.Forms.CheckBox chkScheduleActive;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.CheckBox chkRecordSchedule;
        private System.Windows.Forms.CheckBox chkScheduleRecordOnDetect;
        private System.Windows.Forms.CheckBox chkScheduleAlerts;
    }
}