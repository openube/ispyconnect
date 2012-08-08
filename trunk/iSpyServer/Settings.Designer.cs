namespace iSpyServer
{
    partial class Settings
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
            this.chkStartup = new System.Windows.Forms.CheckBox();
            this.chkErrorReporting = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.chkPasswordProtect = new System.Windows.Forms.CheckBox();
            this.chkCheckForUpdates = new System.Windows.Forms.CheckBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.fbdSaveLocation = new System.Windows.Forms.FolderBrowserDialog();
            this.tcTabs = new System.Windows.Forms.TabControl();
            this.tabPage6 = new System.Windows.Forms.TabPage();
            this.label5 = new System.Windows.Forms.Label();
            this.lbIPv4Address = new System.Windows.Forms.ListBox();
            this.txtLANPort = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.ddlLanguage = new System.Windows.Forms.ComboBox();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.linkLabel2 = new System.Windows.Forms.LinkLabel();
            this.txtServerName = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.txtTrayIcon = new System.Windows.Forms.TextBox();
            this.label21 = new System.Windows.Forms.Label();
            this.chkBalloon = new System.Windows.Forms.CheckBox();
            this.label16 = new System.Windows.Forms.Label();
            this.tbOpacity = new System.Windows.Forms.TrackBar();
            this.chkShowGettingStarted = new System.Windows.Forms.CheckBox();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.btnTimestampColor = new System.Windows.Forms.Button();
            this.btnColorBack = new System.Windows.Forms.Button();
            this.btnColorMain = new System.Windows.Forms.Button();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.label7 = new System.Windows.Forms.Label();
            this.txtIPCameraTimeout = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtServerReceiveTimeout = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.ddlAudioOut = new System.Windows.Forms.ComboBox();
            this.cdColorChooser = new System.Windows.Forms.ColorDialog();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.tcTabs.SuspendLayout();
            this.tabPage6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtLANPort)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbOpacity)).BeginInit();
            this.tabPage1.SuspendLayout();
            this.tabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtIPCameraTimeout)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtServerReceiveTimeout)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chkStartup
            // 
            this.chkStartup.AutoSize = true;
            this.chkStartup.Location = new System.Drawing.Point(19, 191);
            this.chkStartup.Name = "chkStartup";
            this.chkStartup.Size = new System.Drawing.Size(166, 17);
            this.chkStartup.TabIndex = 5;
            this.chkStartup.Text = "Run on startup (this user only)";
            this.chkStartup.UseVisualStyleBackColor = true;
            this.chkStartup.CheckedChanged += new System.EventHandler(this.chkStartup_CheckedChanged);
            // 
            // chkErrorReporting
            // 
            this.chkErrorReporting.AutoSize = true;
            this.chkErrorReporting.Location = new System.Drawing.Point(19, 145);
            this.chkErrorReporting.Name = "chkErrorReporting";
            this.chkErrorReporting.Size = new System.Drawing.Size(149, 17);
            this.chkErrorReporting.TabIndex = 4;
            this.chkErrorReporting.Text = "Anonymous error reporting";
            this.chkErrorReporting.UseVisualStyleBackColor = true;
            this.chkErrorReporting.CheckedChanged += new System.EventHandler(this.chkErrorReporting_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(294, 212);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Password";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(448, 210);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(100, 20);
            this.txtPassword.TabIndex = 2;
            this.txtPassword.UseSystemPasswordChar = true;
            // 
            // chkPasswordProtect
            // 
            this.chkPasswordProtect.Location = new System.Drawing.Point(297, 168);
            this.chkPasswordProtect.Name = "chkPasswordProtect";
            this.chkPasswordProtect.Size = new System.Drawing.Size(251, 36);
            this.chkPasswordProtect.TabIndex = 1;
            this.chkPasswordProtect.Text = "Password protect when minimised to tray";
            this.chkPasswordProtect.UseVisualStyleBackColor = true;
            this.chkPasswordProtect.CheckedChanged += new System.EventHandler(this.chkPasswordProtect_CheckedChanged);
            // 
            // chkCheckForUpdates
            // 
            this.chkCheckForUpdates.AutoSize = true;
            this.chkCheckForUpdates.Location = new System.Drawing.Point(19, 122);
            this.chkCheckForUpdates.Name = "chkCheckForUpdates";
            this.chkCheckForUpdates.Size = new System.Drawing.Size(177, 17);
            this.chkCheckForUpdates.TabIndex = 0;
            this.chkCheckForUpdates.Text = "Automatically check for updates";
            this.chkCheckForUpdates.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(500, 7);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(67, 23);
            this.button1.TabIndex = 9;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.Location = new System.Drawing.Point(434, 7);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(60, 23);
            this.button2.TabIndex = 10;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // tcTabs
            // 
            this.tcTabs.Controls.Add(this.tabPage6);
            this.tcTabs.Controls.Add(this.tabPage1);
            this.tcTabs.Controls.Add(this.tabPage4);
            this.tcTabs.Controls.Add(this.tabPage2);
            this.tcTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcTabs.Location = new System.Drawing.Point(0, 0);
            this.tcTabs.Name = "tcTabs";
            this.tcTabs.SelectedIndex = 0;
            this.tcTabs.Size = new System.Drawing.Size(579, 373);
            this.tcTabs.TabIndex = 39;
            // 
            // tabPage6
            // 
            this.tabPage6.Controls.Add(this.label5);
            this.tabPage6.Controls.Add(this.lbIPv4Address);
            this.tabPage6.Controls.Add(this.txtLANPort);
            this.tabPage6.Controls.Add(this.label3);
            this.tabPage6.Controls.Add(this.groupBox1);
            this.tabPage6.Controls.Add(this.linkLabel2);
            this.tabPage6.Controls.Add(this.txtServerName);
            this.tabPage6.Controls.Add(this.label14);
            this.tabPage6.Controls.Add(this.chkCheckForUpdates);
            this.tabPage6.Controls.Add(this.txtTrayIcon);
            this.tabPage6.Controls.Add(this.chkStartup);
            this.tabPage6.Controls.Add(this.label21);
            this.tabPage6.Controls.Add(this.chkErrorReporting);
            this.tabPage6.Controls.Add(this.chkBalloon);
            this.tabPage6.Controls.Add(this.label1);
            this.tabPage6.Controls.Add(this.txtPassword);
            this.tabPage6.Controls.Add(this.label16);
            this.tabPage6.Controls.Add(this.chkPasswordProtect);
            this.tabPage6.Controls.Add(this.tbOpacity);
            this.tabPage6.Controls.Add(this.chkShowGettingStarted);
            this.tabPage6.Location = new System.Drawing.Point(4, 22);
            this.tabPage6.Name = "tabPage6";
            this.tabPage6.Size = new System.Drawing.Size(571, 347);
            this.tabPage6.TabIndex = 7;
            this.tabPage6.Text = "Options";
            this.tabPage6.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(297, 16);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 13);
            this.label5.TabIndex = 58;
            this.label5.Text = "Upnp (IPv4)";
            // 
            // lbIPv4Address
            // 
            this.lbIPv4Address.FormattingEnabled = true;
            this.lbIPv4Address.Location = new System.Drawing.Point(297, 35);
            this.lbIPv4Address.Name = "lbIPv4Address";
            this.lbIPv4Address.Size = new System.Drawing.Size(251, 56);
            this.lbIPv4Address.TabIndex = 54;
            this.toolTip1.SetToolTip(this.lbIPv4Address, "Select the IP address you want to use");
            this.lbIPv4Address.SelectedIndexChanged += new System.EventHandler(this.lbIPv4Address_SelectedIndexChanged);
            // 
            // txtLANPort
            // 
            this.txtLANPort.Location = new System.Drawing.Point(448, 145);
            this.txtLANPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.txtLANPort.Name = "txtLANPort";
            this.txtLANPort.Size = new System.Drawing.Size(100, 20);
            this.txtLANPort.TabIndex = 57;
            this.txtLANPort.ValueChanged += new System.EventHandler(this.txtLANPort_ValueChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(294, 148);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(50, 13);
            this.label3.TabIndex = 56;
            this.label3.Text = "LAN Port";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.ddlLanguage);
            this.groupBox1.Controls.Add(this.linkLabel1);
            this.groupBox1.Location = new System.Drawing.Point(19, 16);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(272, 55);
            this.groupBox1.TabIndex = 55;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Language";
            // 
            // ddlLanguage
            // 
            this.ddlLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlLanguage.FormattingEnabled = true;
            this.ddlLanguage.Location = new System.Drawing.Point(10, 19);
            this.ddlLanguage.Name = "ddlLanguage";
            this.ddlLanguage.Size = new System.Drawing.Size(167, 21);
            this.ddlLanguage.TabIndex = 52;
            this.ddlLanguage.SelectedIndexChanged += new System.EventHandler(this.ddlLanguage_SelectedIndexChanged);
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(193, 22);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(56, 13);
            this.linkLabel1.TabIndex = 53;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "Get Latest";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked_1);
            // 
            // linkLabel2
            // 
            this.linkLabel2.AutoSize = true;
            this.linkLabel2.Location = new System.Drawing.Point(16, 80);
            this.linkLabel2.Name = "linkLabel2";
            this.linkLabel2.Size = new System.Drawing.Size(99, 13);
            this.linkLabel2.TabIndex = 54;
            this.linkLabel2.TabStop = true;
            this.linkLabel2.Text = "Help Translate iSpy";
            this.linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel2_LinkClicked_1);
            // 
            // txtServerName
            // 
            this.txtServerName.Location = new System.Drawing.Point(448, 119);
            this.txtServerName.MaxLength = 50;
            this.txtServerName.Name = "txtServerName";
            this.txtServerName.Size = new System.Drawing.Size(100, 20);
            this.txtServerName.TabIndex = 50;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(294, 122);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(92, 13);
            this.label14.TabIndex = 49;
            this.label14.Text = "iSpy Server Name";
            // 
            // txtTrayIcon
            // 
            this.txtTrayIcon.Location = new System.Drawing.Point(448, 236);
            this.txtTrayIcon.Name = "txtTrayIcon";
            this.txtTrayIcon.Size = new System.Drawing.Size(100, 20);
            this.txtTrayIcon.TabIndex = 47;
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(294, 238);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(76, 13);
            this.label21.TabIndex = 46;
            this.label21.Text = "Tray Icon Text";
            // 
            // chkBalloon
            // 
            this.chkBalloon.AutoSize = true;
            this.chkBalloon.Location = new System.Drawing.Point(19, 214);
            this.chkBalloon.Name = "chkBalloon";
            this.chkBalloon.Size = new System.Drawing.Size(109, 17);
            this.chkBalloon.TabIndex = 45;
            this.chkBalloon.Text = "Show balloon tips";
            this.chkBalloon.UseVisualStyleBackColor = true;
            // 
            // label16
            // 
            this.label16.Location = new System.Drawing.Point(19, 274);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(272, 45);
            this.label16.TabIndex = 43;
            this.label16.Text = "iSpy Opacity\r\n(may not work on all systems)";
            // 
            // tbOpacity
            // 
            this.tbOpacity.Location = new System.Drawing.Point(297, 274);
            this.tbOpacity.Maximum = 100;
            this.tbOpacity.Minimum = 5;
            this.tbOpacity.Name = "tbOpacity";
            this.tbOpacity.Size = new System.Drawing.Size(251, 45);
            this.tbOpacity.TabIndex = 42;
            this.tbOpacity.Value = 100;
            this.tbOpacity.Scroll += new System.EventHandler(this.tbOpacity_Scroll);
            // 
            // chkShowGettingStarted
            // 
            this.chkShowGettingStarted.AutoSize = true;
            this.chkShowGettingStarted.Location = new System.Drawing.Point(19, 168);
            this.chkShowGettingStarted.Name = "chkShowGettingStarted";
            this.chkShowGettingStarted.Size = new System.Drawing.Size(123, 17);
            this.chkShowGettingStarted.TabIndex = 41;
            this.chkShowGettingStarted.Text = "Show getting started";
            this.chkShowGettingStarted.UseVisualStyleBackColor = true;
            this.chkShowGettingStarted.CheckedChanged += new System.EventHandler(this.chkShowGettingStarted_CheckedChanged);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.btnTimestampColor);
            this.tabPage1.Controls.Add(this.btnColorBack);
            this.tabPage1.Controls.Add(this.btnColorMain);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(571, 347);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Colors";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // btnTimestampColor
            // 
            this.btnTimestampColor.Location = new System.Drawing.Point(225, 12);
            this.btnTimestampColor.Margin = new System.Windows.Forms.Padding(0);
            this.btnTimestampColor.Name = "btnTimestampColor";
            this.btnTimestampColor.Size = new System.Drawing.Size(92, 23);
            this.btnTimestampColor.TabIndex = 24;
            this.btnTimestampColor.Text = "Timestamp";
            this.btnTimestampColor.UseVisualStyleBackColor = true;
            this.btnTimestampColor.Click += new System.EventHandler(this.btnTimestampColor_Click);
            // 
            // btnColorBack
            // 
            this.btnColorBack.Location = new System.Drawing.Point(117, 12);
            this.btnColorBack.Margin = new System.Windows.Forms.Padding(0);
            this.btnColorBack.Name = "btnColorBack";
            this.btnColorBack.Size = new System.Drawing.Size(92, 23);
            this.btnColorBack.TabIndex = 19;
            this.btnColorBack.Text = "Object Back";
            this.btnColorBack.UseVisualStyleBackColor = true;
            this.btnColorBack.Click += new System.EventHandler(this.btnColorBack_Click);
            // 
            // btnColorMain
            // 
            this.btnColorMain.Location = new System.Drawing.Point(13, 12);
            this.btnColorMain.Margin = new System.Windows.Forms.Padding(0);
            this.btnColorMain.Name = "btnColorMain";
            this.btnColorMain.Size = new System.Drawing.Size(92, 23);
            this.btnColorMain.TabIndex = 18;
            this.btnColorMain.Text = "Main Panel";
            this.btnColorMain.UseVisualStyleBackColor = true;
            this.btnColorMain.Click += new System.EventHandler(this.btnColorMain_Click);
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.label7);
            this.tabPage4.Controls.Add(this.txtIPCameraTimeout);
            this.tabPage4.Controls.Add(this.label8);
            this.tabPage4.Controls.Add(this.label4);
            this.tabPage4.Controls.Add(this.txtServerReceiveTimeout);
            this.tabPage4.Controls.Add(this.label2);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Size = new System.Drawing.Size(571, 347);
            this.tabPage4.TabIndex = 6;
            this.tabPage4.Text = "Timeouts";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(353, 51);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(26, 13);
            this.label7.TabIndex = 5;
            this.label7.Text = "(ms)";
            // 
            // txtIPCameraTimeout
            // 
            this.txtIPCameraTimeout.Location = new System.Drawing.Point(264, 49);
            this.txtIPCameraTimeout.Maximum = new decimal(new int[] {
            20000,
            0,
            0,
            0});
            this.txtIPCameraTimeout.Minimum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.txtIPCameraTimeout.Name = "txtIPCameraTimeout";
            this.txtIPCameraTimeout.Size = new System.Drawing.Size(83, 20);
            this.txtIPCameraTimeout.TabIndex = 4;
            this.txtIPCameraTimeout.Value = new decimal(new int[] {
            200,
            0,
            0,
            0});
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(18, 51);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(127, 13);
            this.label8.TabIndex = 3;
            this.label8.Text = "MJPEG Receive Timeout";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(353, 18);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(26, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "(ms)";
            // 
            // txtServerReceiveTimeout
            // 
            this.txtServerReceiveTimeout.Location = new System.Drawing.Point(264, 16);
            this.txtServerReceiveTimeout.Maximum = new decimal(new int[] {
            20000,
            0,
            0,
            0});
            this.txtServerReceiveTimeout.Minimum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.txtServerReceiveTimeout.Name = "txtServerReceiveTimeout";
            this.txtServerReceiveTimeout.Size = new System.Drawing.Size(83, 20);
            this.txtServerReceiveTimeout.TabIndex = 1;
            this.txtServerReceiveTimeout.Value = new decimal(new int[] {
            200,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 18);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(122, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Server Receive Timeout";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.ddlAudioOut);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(571, 347);
            this.tabPage2.TabIndex = 8;
            this.tabPage2.Text = "Audio Out";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // ddlAudioOut
            // 
            this.ddlAudioOut.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlAudioOut.FormattingEnabled = true;
            this.ddlAudioOut.Location = new System.Drawing.Point(8, 15);
            this.ddlAudioOut.Name = "ddlAudioOut";
            this.ddlAudioOut.Size = new System.Drawing.Size(318, 21);
            this.ddlAudioOut.TabIndex = 0;
            this.ddlAudioOut.SelectedIndexChanged += new System.EventHandler(this.ddlAudioOut_SelectedIndexChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.button2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 373);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(579, 42);
            this.panel1.TabIndex = 40;
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(579, 415);
            this.Controls.Add(this.tcTabs);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Settings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Settings_FormClosing);
            this.Load += new System.EventHandler(this.Settings_Load);
            this.tcTabs.ResumeLayout(false);
            this.tabPage6.ResumeLayout(false);
            this.tabPage6.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtLANPort)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbOpacity)).EndInit();
            this.tabPage1.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            this.tabPage4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtIPCameraTimeout)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtServerReceiveTimeout)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.CheckBox chkPasswordProtect;
        private System.Windows.Forms.CheckBox chkCheckForUpdates;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.CheckBox chkErrorReporting;
        private System.Windows.Forms.CheckBox chkStartup;
        private System.Windows.Forms.FolderBrowserDialog fbdSaveLocation;
        private System.Windows.Forms.ColorDialog cdColorChooser;
        private System.Windows.Forms.Button btnColorBack;
        private System.Windows.Forms.Button btnColorMain;
        private System.Windows.Forms.TabControl tcTabs;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.CheckBox chkShowGettingStarted;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TrackBar tbOpacity;
        private System.Windows.Forms.Button btnTimestampColor;
        private System.Windows.Forms.CheckBox chkBalloon;
        private System.Windows.Forms.TextBox txtTrayIcon;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown txtIPCameraTimeout;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown txtServerReceiveTimeout;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TabPage tabPage6;
        private System.Windows.Forms.TextBox txtServerName;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.ComboBox ddlLanguage;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.LinkLabel linkLabel2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown txtLANPort;
        private System.Windows.Forms.ListBox lbIPv4Address;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.ComboBox ddlAudioOut;
    }
}