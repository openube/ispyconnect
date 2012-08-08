namespace iSpyServer
{
    partial class AddCamera
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
            this.btnSelectSource = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tcCamera = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.chkFlipY = new System.Windows.Forms.CheckBox();
            this.chkFlipX = new System.Windows.Forms.CheckBox();
            this.label85 = new System.Windows.Forms.Label();
            this.btnMaskImage = new System.Windows.Forms.Button();
            this.label84 = new System.Windows.Forms.Label();
            this.txtMaskImage = new System.Windows.Forms.TextBox();
            this.ddlTimestampLocation = new System.Windows.Forms.ComboBox();
            this.ddlTimestamp = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.rtbDescription = new System.Windows.Forms.RichTextBox();
            this.label66 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtCameraName = new System.Windows.Forms.TextBox();
            this.chkActive = new System.Windows.Forms.CheckBox();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.label80 = new System.Windows.Forms.Label();
            this.pnlScheduler = new System.Windows.Forms.GroupBox();
            this.chkScheduleActive = new System.Windows.Forms.CheckBox();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.lbSchedule = new System.Windows.Forms.ListBox();
            this.label50 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.chkSun = new System.Windows.Forms.CheckBox();
            this.label8 = new System.Windows.Forms.Label();
            this.chkSat = new System.Windows.Forms.CheckBox();
            this.ddlHourEnd = new System.Windows.Forms.ComboBox();
            this.chkFri = new System.Windows.Forms.CheckBox();
            this.ddlMinuteStart = new System.Windows.Forms.ComboBox();
            this.chkThu = new System.Windows.Forms.CheckBox();
            this.ddlMinuteEnd = new System.Windows.Forms.ComboBox();
            this.chkWed = new System.Windows.Forms.CheckBox();
            this.ddlHourStart = new System.Windows.Forms.ComboBox();
            this.chkTue = new System.Windows.Forms.CheckBox();
            this.label10 = new System.Windows.Forms.Label();
            this.chkMon = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label49 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.chkSchedule = new System.Windows.Forms.CheckBox();
            this.btnNext = new System.Windows.Forms.Button();
            this.btnBack = new System.Windows.Forms.Button();
            this.btnFinish = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.ofdDetect = new System.Windows.Forms.OpenFileDialog();
            this.label17 = new System.Windows.Forms.Label();
            this.label35 = new System.Windows.Forms.Label();
            this.label26 = new System.Windows.Forms.Label();
            this.label34 = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label25 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label32 = new System.Windows.Forms.Label();
            this.label31 = new System.Windows.Forms.Label();
            this.cdTracking = new System.Windows.Forms.ColorDialog();
            this.btnAdvanced = new System.Windows.Forms.Button();
            this.btnCrossbar = new System.Windows.Forms.Button();
            this.tcCamera.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.tabPage5.SuspendLayout();
            this.pnlScheduler.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnSelectSource
            // 
            this.btnSelectSource.Location = new System.Drawing.Point(144, 17);
            this.btnSelectSource.Name = "btnSelectSource";
            this.btnSelectSource.Size = new System.Drawing.Size(35, 23);
            this.btnSelectSource.TabIndex = 14;
            this.btnSelectSource.Text = "...";
            this.btnSelectSource.UseVisualStyleBackColor = true;
            this.btnSelectSource.Click += new System.EventHandler(this.btnSelectSource_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "Source";
            // 
            // tcCamera
            // 
            this.tcCamera.Controls.Add(this.tabPage1);
            this.tcCamera.Controls.Add(this.tabPage5);
            this.tcCamera.Dock = System.Windows.Forms.DockStyle.Top;
            this.tcCamera.Location = new System.Drawing.Point(5, 5);
            this.tcCamera.Name = "tcCamera";
            this.tcCamera.SelectedIndex = 0;
            this.tcCamera.Size = new System.Drawing.Size(527, 491);
            this.tcCamera.TabIndex = 16;
            this.tcCamera.SelectedIndexChanged += new System.EventHandler(this.tcCamera_SelectedIndexChanged);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox3);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(519, 465);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Camera";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btnAdvanced);
            this.groupBox3.Controls.Add(this.btnCrossbar);
            this.groupBox3.Controls.Add(this.chkFlipY);
            this.groupBox3.Controls.Add(this.chkFlipX);
            this.groupBox3.Controls.Add(this.label85);
            this.groupBox3.Controls.Add(this.btnMaskImage);
            this.groupBox3.Controls.Add(this.label84);
            this.groupBox3.Controls.Add(this.txtMaskImage);
            this.groupBox3.Controls.Add(this.ddlTimestampLocation);
            this.groupBox3.Controls.Add(this.ddlTimestamp);
            this.groupBox3.Controls.Add(this.label11);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.rtbDescription);
            this.groupBox3.Controls.Add(this.btnSelectSource);
            this.groupBox3.Controls.Add(this.label66);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.txtCameraName);
            this.groupBox3.Controls.Add(this.chkActive);
            this.groupBox3.Location = new System.Drawing.Point(10, 18);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(504, 294);
            this.groupBox3.TabIndex = 56;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Video Source";
            // 
            // chkFlipY
            // 
            this.chkFlipY.AutoSize = true;
            this.chkFlipY.Location = new System.Drawing.Point(419, 75);
            this.chkFlipY.Name = "chkFlipY";
            this.chkFlipY.Size = new System.Drawing.Size(52, 17);
            this.chkFlipY.TabIndex = 70;
            this.chkFlipY.Text = "Flip-Y";
            this.chkFlipY.UseVisualStyleBackColor = true;
            this.chkFlipY.CheckedChanged += new System.EventHandler(this.chkFlipY_CheckedChanged);
            // 
            // chkFlipX
            // 
            this.chkFlipX.AutoSize = true;
            this.chkFlipX.Location = new System.Drawing.Point(335, 75);
            this.chkFlipX.Name = "chkFlipX";
            this.chkFlipX.Size = new System.Drawing.Size(52, 17);
            this.chkFlipX.TabIndex = 69;
            this.chkFlipX.Text = "Flip-X";
            this.chkFlipX.UseVisualStyleBackColor = true;
            this.chkFlipX.CheckedChanged += new System.EventHandler(this.chkFlipX_CheckedChanged);
            // 
            // label85
            // 
            this.label85.Location = new System.Drawing.Point(141, 239);
            this.label85.Name = "label85";
            this.label85.Size = new System.Drawing.Size(331, 42);
            this.label85.TabIndex = 68;
            this.label85.Text = "Create a transparent .png image to use as a mask and block out any areas you don\'" +
    "t want recorded.";
            // 
            // btnMaskImage
            // 
            this.btnMaskImage.Location = new System.Drawing.Point(317, 209);
            this.btnMaskImage.Name = "btnMaskImage";
            this.btnMaskImage.Size = new System.Drawing.Size(37, 23);
            this.btnMaskImage.TabIndex = 67;
            this.btnMaskImage.Text = "...";
            this.btnMaskImage.UseVisualStyleBackColor = true;
            this.btnMaskImage.Click += new System.EventHandler(this.btnMaskImage_Click);
            // 
            // label84
            // 
            this.label84.AutoSize = true;
            this.label84.Location = new System.Drawing.Point(20, 214);
            this.label84.Name = "label84";
            this.label84.Size = new System.Drawing.Size(65, 13);
            this.label84.TabIndex = 65;
            this.label84.Text = "Mask Image";
            // 
            // txtMaskImage
            // 
            this.txtMaskImage.Location = new System.Drawing.Point(144, 211);
            this.txtMaskImage.MaxLength = 50;
            this.txtMaskImage.Name = "txtMaskImage";
            this.txtMaskImage.Size = new System.Drawing.Size(146, 20);
            this.txtMaskImage.TabIndex = 66;
            this.toolTip1.SetToolTip(this.txtMaskImage, "Give your camera a descriptive name, eg Office Cam");
            this.txtMaskImage.TextChanged += new System.EventHandler(this.txtMaskImage_TextChanged);
            // 
            // ddlTimestampLocation
            // 
            this.ddlTimestampLocation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlTimestampLocation.FormattingEnabled = true;
            this.ddlTimestampLocation.Location = new System.Drawing.Point(317, 170);
            this.ddlTimestampLocation.Name = "ddlTimestampLocation";
            this.ddlTimestampLocation.Size = new System.Drawing.Size(132, 21);
            this.ddlTimestampLocation.TabIndex = 64;
            this.ddlTimestampLocation.SelectedIndexChanged += new System.EventHandler(this.ddlTimestampLocation_SelectedIndexChanged);
            // 
            // ddlTimestamp
            // 
            this.ddlTimestamp.FormattingEnabled = true;
            this.ddlTimestamp.Items.AddRange(new object[] {
            "FPS: {FPS} {0:G} "});
            this.ddlTimestamp.Location = new System.Drawing.Point(144, 167);
            this.ddlTimestamp.Name = "ddlTimestamp";
            this.ddlTimestamp.Size = new System.Drawing.Size(146, 21);
            this.ddlTimestamp.TabIndex = 63;
            this.ddlTimestamp.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ddlTimestamp_KeyUp);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(20, 170);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(63, 13);
            this.label11.TabIndex = 59;
            this.label11.Text = "Time Stamp";
            // 
            // rtbDescription
            // 
            this.rtbDescription.Location = new System.Drawing.Point(144, 98);
            this.rtbDescription.Name = "rtbDescription";
            this.rtbDescription.Size = new System.Drawing.Size(328, 52);
            this.rtbDescription.TabIndex = 50;
            this.rtbDescription.Text = global::iSpyServer.Properties.Resources.nothing;
            // 
            // label66
            // 
            this.label66.AutoSize = true;
            this.label66.Location = new System.Drawing.Point(19, 101);
            this.label66.Name = "label66";
            this.label66.Size = new System.Drawing.Size(60, 13);
            this.label66.TabIndex = 49;
            this.label66.Text = "Description";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 49);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 20;
            this.label1.Text = "Name";
            // 
            // txtCameraName
            // 
            this.txtCameraName.Location = new System.Drawing.Point(144, 46);
            this.txtCameraName.MaxLength = 50;
            this.txtCameraName.Name = "txtCameraName";
            this.txtCameraName.Size = new System.Drawing.Size(232, 20);
            this.txtCameraName.TabIndex = 21;
            this.toolTip1.SetToolTip(this.txtCameraName, "Give your camera a descriptive name, eg Office Cam");
            this.txtCameraName.TextChanged += new System.EventHandler(this.txtCameraName_TextChanged);
            this.txtCameraName.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtCameraName_KeyUp);
            // 
            // chkActive
            // 
            this.chkActive.AutoSize = true;
            this.chkActive.Location = new System.Drawing.Point(146, 75);
            this.chkActive.Name = "chkActive";
            this.chkActive.Size = new System.Drawing.Size(95, 17);
            this.chkActive.TabIndex = 23;
            this.chkActive.Text = "Camera Active";
            this.chkActive.UseVisualStyleBackColor = true;
            this.chkActive.CheckedChanged += new System.EventHandler(this.chkActive_CheckedChanged);
            // 
            // tabPage5
            // 
            this.tabPage5.Controls.Add(this.label80);
            this.tabPage5.Controls.Add(this.pnlScheduler);
            this.tabPage5.Controls.Add(this.chkSchedule);
            this.tabPage5.Location = new System.Drawing.Point(4, 22);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Size = new System.Drawing.Size(519, 465);
            this.tabPage5.TabIndex = 4;
            this.tabPage5.Text = "Scheduling";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // label80
            // 
            this.label80.Location = new System.Drawing.Point(5, 427);
            this.label80.Name = "label80";
            this.label80.Size = new System.Drawing.Size(505, 56);
            this.label80.TabIndex = 21;
            this.label80.Text = "Tip: To create a schedule overnight create a start time with no stop time and a s" +
    "top time with no start time.";
            // 
            // pnlScheduler
            // 
            this.pnlScheduler.Controls.Add(this.chkScheduleActive);
            this.pnlScheduler.Controls.Add(this.btnUpdate);
            this.pnlScheduler.Controls.Add(this.btnDelete);
            this.pnlScheduler.Controls.Add(this.lbSchedule);
            this.pnlScheduler.Controls.Add(this.label50);
            this.pnlScheduler.Controls.Add(this.label9);
            this.pnlScheduler.Controls.Add(this.chkSun);
            this.pnlScheduler.Controls.Add(this.label8);
            this.pnlScheduler.Controls.Add(this.chkSat);
            this.pnlScheduler.Controls.Add(this.ddlHourEnd);
            this.pnlScheduler.Controls.Add(this.chkFri);
            this.pnlScheduler.Controls.Add(this.ddlMinuteStart);
            this.pnlScheduler.Controls.Add(this.chkThu);
            this.pnlScheduler.Controls.Add(this.ddlMinuteEnd);
            this.pnlScheduler.Controls.Add(this.chkWed);
            this.pnlScheduler.Controls.Add(this.ddlHourStart);
            this.pnlScheduler.Controls.Add(this.chkTue);
            this.pnlScheduler.Controls.Add(this.label10);
            this.pnlScheduler.Controls.Add(this.chkMon);
            this.pnlScheduler.Controls.Add(this.label7);
            this.pnlScheduler.Controls.Add(this.label49);
            this.pnlScheduler.Controls.Add(this.button2);
            this.pnlScheduler.Location = new System.Drawing.Point(8, 39);
            this.pnlScheduler.Name = "pnlScheduler";
            this.pnlScheduler.Size = new System.Drawing.Size(502, 385);
            this.pnlScheduler.TabIndex = 10;
            this.pnlScheduler.TabStop = false;
            this.pnlScheduler.Text = "Scheduler";
            // 
            // chkScheduleActive
            // 
            this.chkScheduleActive.AutoSize = true;
            this.chkScheduleActive.Checked = true;
            this.chkScheduleActive.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkScheduleActive.Location = new System.Drawing.Point(58, 315);
            this.chkScheduleActive.Name = "chkScheduleActive";
            this.chkScheduleActive.Size = new System.Drawing.Size(104, 17);
            this.chkScheduleActive.TabIndex = 23;
            this.chkScheduleActive.Text = "Schedule Active";
            this.chkScheduleActive.UseVisualStyleBackColor = true;
            this.chkScheduleActive.CheckedChanged += new System.EventHandler(this.chkScheduleActive_CheckedChanged);
            // 
            // btnUpdate
            // 
            this.btnUpdate.Location = new System.Drawing.Point(335, 311);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(75, 23);
            this.btnUpdate.TabIndex = 22;
            this.btnUpdate.Text = "Update";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(250, 311);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(75, 23);
            this.btnDelete.TabIndex = 20;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.button3_Click);
            // 
            // lbSchedule
            // 
            this.lbSchedule.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lbSchedule.FormattingEnabled = true;
            this.lbSchedule.Location = new System.Drawing.Point(18, 29);
            this.lbSchedule.Name = "lbSchedule";
            this.lbSchedule.Size = new System.Drawing.Size(476, 147);
            this.lbSchedule.TabIndex = 10;
            this.toolTip1.SetToolTip(this.lbSchedule, "Press delete to remove an entry");
            this.lbSchedule.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lbSchedule_DrawItem);
            this.lbSchedule.SelectedIndexChanged += new System.EventHandler(this.lbSchedule_SelectedIndexChanged);
            this.lbSchedule.KeyUp += new System.Windows.Forms.KeyEventHandler(this.lbSchedule_KeyUp);
            this.lbSchedule.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lbSchedule_MouseUp);
            // 
            // label50
            // 
            this.label50.AutoSize = true;
            this.label50.Location = new System.Drawing.Point(17, 359);
            this.label50.Name = "label50";
            this.label50.Size = new System.Drawing.Size(244, 13);
            this.label50.TabIndex = 19;
            this.label50.Text = "Important: make sure your schedules don\'t overlap";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(348, 185);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(32, 13);
            this.label9.TabIndex = 5;
            this.label9.Text = "Stop:";
            // 
            // chkSun
            // 
            this.chkSun.AutoSize = true;
            this.chkSun.Location = new System.Drawing.Point(58, 263);
            this.chkSun.Name = "chkSun";
            this.chkSun.Size = new System.Drawing.Size(45, 17);
            this.chkSun.TabIndex = 18;
            this.chkSun.Text = "Sun";
            this.chkSun.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(106, 185);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(10, 13);
            this.label8.TabIndex = 4;
            this.label8.Text = ":";
            // 
            // chkSat
            // 
            this.chkSat.AutoSize = true;
            this.chkSat.Location = new System.Drawing.Point(170, 240);
            this.chkSat.Name = "chkSat";
            this.chkSat.Size = new System.Drawing.Size(42, 17);
            this.chkSat.TabIndex = 17;
            this.chkSat.Text = "Sat";
            this.chkSat.UseVisualStyleBackColor = true;
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
            this.ddlHourEnd.Location = new System.Drawing.Point(389, 182);
            this.ddlHourEnd.Name = "ddlHourEnd";
            this.ddlHourEnd.Size = new System.Drawing.Size(44, 21);
            this.ddlHourEnd.TabIndex = 6;
            // 
            // chkFri
            // 
            this.chkFri.AutoSize = true;
            this.chkFri.Location = new System.Drawing.Point(112, 240);
            this.chkFri.Name = "chkFri";
            this.chkFri.Size = new System.Drawing.Size(37, 17);
            this.chkFri.TabIndex = 16;
            this.chkFri.Text = "Fri";
            this.chkFri.UseVisualStyleBackColor = true;
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
            this.ddlMinuteStart.Location = new System.Drawing.Point(118, 182);
            this.ddlMinuteStart.Name = "ddlMinuteStart";
            this.ddlMinuteStart.Size = new System.Drawing.Size(44, 21);
            this.ddlMinuteStart.TabIndex = 3;
            // 
            // chkThu
            // 
            this.chkThu.AutoSize = true;
            this.chkThu.Location = new System.Drawing.Point(58, 240);
            this.chkThu.Name = "chkThu";
            this.chkThu.Size = new System.Drawing.Size(45, 17);
            this.chkThu.TabIndex = 15;
            this.chkThu.Text = "Thu";
            this.chkThu.UseVisualStyleBackColor = true;
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
            this.ddlMinuteEnd.Location = new System.Drawing.Point(447, 182);
            this.ddlMinuteEnd.Name = "ddlMinuteEnd";
            this.ddlMinuteEnd.Size = new System.Drawing.Size(44, 21);
            this.ddlMinuteEnd.TabIndex = 7;
            // 
            // chkWed
            // 
            this.chkWed.AutoSize = true;
            this.chkWed.Location = new System.Drawing.Point(170, 216);
            this.chkWed.Name = "chkWed";
            this.chkWed.Size = new System.Drawing.Size(49, 17);
            this.chkWed.TabIndex = 14;
            this.chkWed.Text = "Wed";
            this.chkWed.UseVisualStyleBackColor = true;
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
            this.ddlHourStart.Location = new System.Drawing.Point(59, 182);
            this.ddlHourStart.Name = "ddlHourStart";
            this.ddlHourStart.Size = new System.Drawing.Size(44, 21);
            this.ddlHourStart.TabIndex = 2;
            this.ddlHourStart.SelectedIndexChanged += new System.EventHandler(this.ddlHourStart_SelectedIndexChanged);
            // 
            // chkTue
            // 
            this.chkTue.AutoSize = true;
            this.chkTue.Location = new System.Drawing.Point(112, 217);
            this.chkTue.Name = "chkTue";
            this.chkTue.Size = new System.Drawing.Size(45, 17);
            this.chkTue.TabIndex = 13;
            this.chkTue.Text = "Tue";
            this.chkTue.UseVisualStyleBackColor = true;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(435, 185);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(10, 13);
            this.label10.TabIndex = 8;
            this.label10.Text = ":";
            // 
            // chkMon
            // 
            this.chkMon.AutoSize = true;
            this.chkMon.Location = new System.Drawing.Point(59, 217);
            this.chkMon.Name = "chkMon";
            this.chkMon.Size = new System.Drawing.Size(47, 17);
            this.chkMon.TabIndex = 12;
            this.chkMon.Text = "Mon";
            this.chkMon.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(17, 185);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(32, 13);
            this.label7.TabIndex = 1;
            this.label7.Text = "Start:";
            // 
            // label49
            // 
            this.label49.AutoSize = true;
            this.label49.Location = new System.Drawing.Point(17, 217);
            this.label49.Name = "label49";
            this.label49.Size = new System.Drawing.Size(31, 13);
            this.label49.TabIndex = 11;
            this.label49.Text = "Days";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(416, 311);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 9;
            this.button2.Text = "Add";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click_1);
            // 
            // chkSchedule
            // 
            this.chkSchedule.AutoSize = true;
            this.chkSchedule.Location = new System.Drawing.Point(8, 16);
            this.chkSchedule.Name = "chkSchedule";
            this.chkSchedule.Size = new System.Drawing.Size(110, 17);
            this.chkSchedule.TabIndex = 0;
            this.chkSchedule.Text = "Schedule Camera";
            this.chkSchedule.UseVisualStyleBackColor = true;
            this.chkSchedule.CheckedChanged += new System.EventHandler(this.chkSchedule_CheckedChanged);
            // 
            // btnNext
            // 
            this.btnNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnNext.Location = new System.Drawing.Point(367, 503);
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
            this.btnBack.Location = new System.Drawing.Point(286, 503);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(75, 23);
            this.btnBack.TabIndex = 45;
            this.btnBack.Text = "<< Back";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.button2_Click);
            // 
            // btnFinish
            // 
            this.btnFinish.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFinish.Location = new System.Drawing.Point(447, 503);
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
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(230, 165);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(38, 13);
            this.label17.TabIndex = 63;
            this.label17.Text = "frames";
            // 
            // label35
            // 
            this.label35.AutoSize = true;
            this.label35.Location = new System.Drawing.Point(230, 107);
            this.label35.Name = "label35";
            this.label35.Size = new System.Drawing.Size(47, 13);
            this.label35.TabIndex = 57;
            this.label35.Text = "seconds";
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(6, 165);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(91, 13);
            this.label26.TabIndex = 61;
            this.label26.Text = "Pre-Buffer Frames";
            // 
            // label34
            // 
            this.label34.AutoSize = true;
            this.label34.Location = new System.Drawing.Point(5, 110);
            this.label34.Name = "label34";
            this.label34.Size = new System.Drawing.Size(94, 13);
            this.label34.TabIndex = 56;
            this.label34.Text = "Max. Record Time";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(230, 139);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(47, 13);
            this.label24.TabIndex = 60;
            this.label24.Text = "seconds";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(6, 53);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(93, 13);
            this.label14.TabIndex = 52;
            this.label14.Text = "Record Timelapse";
            // 
            // label25
            // 
            this.label25.AutoSize = true;
            this.label25.Location = new System.Drawing.Point(6, 139);
            this.label25.Name = "label25";
            this.label25.Size = new System.Drawing.Size(86, 13);
            this.label25.TabIndex = 58;
            this.label25.Text = "Calibration Delay";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(230, 53);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(47, 13);
            this.label13.TabIndex = 29;
            this.label13.Text = "seconds";
            // 
            // label32
            // 
            this.label32.AutoSize = true;
            this.label32.Location = new System.Drawing.Point(6, 80);
            this.label32.Name = "label32";
            this.label32.Size = new System.Drawing.Size(87, 13);
            this.label32.TabIndex = 49;
            this.label32.Text = "Inactivity Record";
            // 
            // label31
            // 
            this.label31.AutoSize = true;
            this.label31.Location = new System.Drawing.Point(230, 80);
            this.label31.Name = "label31";
            this.label31.Size = new System.Drawing.Size(47, 13);
            this.label31.TabIndex = 51;
            this.label31.Text = "seconds";
            // 
            // btnAdvanced
            // 
            this.btnAdvanced.Location = new System.Drawing.Point(188, 17);
            this.btnAdvanced.Margin = new System.Windows.Forms.Padding(6);
            this.btnAdvanced.Name = "btnAdvanced";
            this.btnAdvanced.Size = new System.Drawing.Size(75, 23);
            this.btnAdvanced.TabIndex = 71;
            this.btnAdvanced.Text = "Advanced";
            this.btnAdvanced.UseVisualStyleBackColor = true;
            this.btnAdvanced.Click += new System.EventHandler(this.btnAdvanced_Click);
            // 
            // btnCrossbar
            // 
            this.btnCrossbar.Location = new System.Drawing.Point(275, 17);
            this.btnCrossbar.Margin = new System.Windows.Forms.Padding(6);
            this.btnCrossbar.Name = "btnCrossbar";
            this.btnCrossbar.Size = new System.Drawing.Size(75, 23);
            this.btnCrossbar.TabIndex = 72;
            this.btnCrossbar.Text = "Crossbar";
            this.btnCrossbar.UseVisualStyleBackColor = true;
            this.btnCrossbar.Click += new System.EventHandler(this.btnCrossbar_Click);
            // 
            // AddCamera
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(537, 534);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.btnFinish);
            this.Controls.Add(this.tcCamera);
            this.Controls.Add(this.btnNext);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "AddCamera";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add Camera";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AddCamera_FormClosing);
            this.Load += new System.EventHandler(this.AddCamera_Load);
            this.tcCamera.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.tabPage5.ResumeLayout(false);
            this.tabPage5.PerformLayout();
            this.pnlScheduler.ResumeLayout(false);
            this.pnlScheduler.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnSelectSource;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TabControl tcCamera;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.TextBox txtCameraName;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox chkActive;
        private System.Windows.Forms.OpenFileDialog ofdDetect;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.Button btnFinish;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox ddlHourStart;
        private System.Windows.Forms.ComboBox ddlMinuteEnd;
        private System.Windows.Forms.ComboBox ddlMinuteStart;
        private System.Windows.Forms.ComboBox ddlHourEnd;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.CheckBox chkSchedule;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label35;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.Label label34;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Label label13;

        private System.Windows.Forms.Label label32;
        private System.Windows.Forms.Label label31;
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
        private System.Windows.Forms.Label label50;
        private System.Windows.Forms.ColorDialog cdTracking;
        private System.Windows.Forms.RichTextBox rtbDescription;
        private System.Windows.Forms.Label label66;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox pnlScheduler;
        private System.Windows.Forms.Label label80;
        private System.Windows.Forms.ComboBox ddlTimestampLocation;
        private System.Windows.Forms.ComboBox ddlTimestamp;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button btnMaskImage;
        private System.Windows.Forms.Label label84;
        private System.Windows.Forms.TextBox txtMaskImage;
        private System.Windows.Forms.Label label85;
        private System.Windows.Forms.CheckBox chkFlipY;
        private System.Windows.Forms.CheckBox chkFlipX;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.CheckBox chkScheduleActive;
        private System.Windows.Forms.Button btnAdvanced;
        private System.Windows.Forms.Button btnCrossbar;
    }
}