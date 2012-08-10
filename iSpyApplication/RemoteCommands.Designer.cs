namespace iSpyApplication
{
    partial class RemoteCommands
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
            this.label45 = new System.Windows.Forms.Label();
            this.button3 = new System.Windows.Forms.Button();
            this.label82 = new System.Windows.Forms.Label();
            this.label83 = new System.Windows.Forms.Label();
            this.lbManualAlerts = new System.Windows.Forms.ListBox();
            this.btnDelete = new System.Windows.Forms.Button();
            this.gpbSubscriber = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.linkLabel2 = new System.Windows.Forms.LinkLabel();
            this.label1 = new System.Windows.Forms.Label();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.txtExecute = new System.Windows.Forms.TextBox();
            this.btnAddCommand = new System.Windows.Forms.Button();
            this.txtName = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.linkLabel4 = new System.Windows.Forms.LinkLabel();
            this.llblHelp = new System.Windows.Forms.LinkLabel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.linkLabel3 = new System.Windows.Forms.LinkLabel();
            this.lblCommand = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.gpbSubscriber.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label45
            // 
            this.label45.Location = new System.Drawing.Point(299, 19);
            this.label45.Name = "label45";
            this.label45.Size = new System.Drawing.Size(265, 134);
            this.label45.TabIndex = 83;
            this.label45.Text = "For Example:\r\n\r\nSwitch on and off cameras, Execute a batch file for home automati" +
    "on,\r\nPlay an MP3 to stop your dogs barking, Sound an alarm";
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(279, 31);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(40, 23);
            this.button3.TabIndex = 5;
            this.button3.Text = "...";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.Button3Click);
            // 
            // label82
            // 
            this.label82.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.label82, 2);
            this.label82.Location = new System.Drawing.Point(3, 0);
            this.label82.Name = "label82";
            this.label82.Padding = new System.Windows.Forms.Padding(3);
            this.label82.Size = new System.Drawing.Size(432, 19);
            this.label82.TabIndex = 0;
            this.label82.Text = "You can trigger remote commands manually from the iSpy website or from mobile dev" +
    "ices.";
            // 
            // label83
            // 
            this.label83.AutoSize = true;
            this.label83.Location = new System.Drawing.Point(3, 28);
            this.label83.Name = "label83";
            this.label83.Size = new System.Drawing.Size(68, 13);
            this.label83.TabIndex = 82;
            this.label83.Text = "Execute File:";
            // 
            // lbManualAlerts
            // 
            this.lbManualAlerts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbManualAlerts.FormattingEnabled = true;
            this.lbManualAlerts.Location = new System.Drawing.Point(3, 22);
            this.lbManualAlerts.Name = "lbManualAlerts";
            this.lbManualAlerts.Size = new System.Drawing.Size(290, 134);
            this.lbManualAlerts.TabIndex = 0;
            this.lbManualAlerts.SelectedIndexChanged += new System.EventHandler(this.lbManualAlerts_SelectedIndexChanged);
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(3, 3);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(75, 23);
            this.btnDelete.TabIndex = 1;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.BtnDeleteClick);
            // 
            // gpbSubscriber
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.gpbSubscriber, 2);
            this.gpbSubscriber.Controls.Add(this.tableLayoutPanel2);
            this.gpbSubscriber.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gpbSubscriber.Location = new System.Drawing.Point(3, 198);
            this.gpbSubscriber.Name = "gpbSubscriber";
            this.gpbSubscriber.Size = new System.Drawing.Size(607, 107);
            this.gpbSubscriber.TabIndex = 86;
            this.gpbSubscriber.TabStop = false;
            this.gpbSubscriber.Text = "New Remote Command";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 4;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 81F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 195F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 72F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.linkLabel2, 3, 2);
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.linkLabel1, 2, 2);
            this.tableLayoutPanel2.Controls.Add(this.txtExecute, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.btnAddCommand, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.txtName, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.button3, 2, 1);
            this.tableLayoutPanel2.Controls.Add(this.label83, 0, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 3;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 11F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(601, 88);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // linkLabel2
            // 
            this.linkLabel2.AutoSize = true;
            this.linkLabel2.Location = new System.Drawing.Point(351, 63);
            this.linkLabel2.Name = "linkLabel2";
            this.linkLabel2.Size = new System.Drawing.Size(52, 13);
            this.linkLabel2.TabIndex = 8;
            this.linkLabel2.TabStop = true;
            this.linkLabel2.Text = "Examples";
            this.linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel2_LinkClicked);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name";
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(279, 63);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(28, 13);
            this.linkLabel1.TabIndex = 7;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "Test";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabel1LinkClicked);
            // 
            // txtExecute
            // 
            this.txtExecute.Location = new System.Drawing.Point(84, 31);
            this.txtExecute.Name = "txtExecute";
            this.txtExecute.Size = new System.Drawing.Size(139, 20);
            this.txtExecute.TabIndex = 4;
            // 
            // btnAddCommand
            // 
            this.btnAddCommand.Location = new System.Drawing.Point(84, 66);
            this.btnAddCommand.Name = "btnAddCommand";
            this.btnAddCommand.Size = new System.Drawing.Size(60, 19);
            this.btnAddCommand.TabIndex = 6;
            this.btnAddCommand.Text = "Add";
            this.btnAddCommand.UseVisualStyleBackColor = true;
            this.btnAddCommand.Click += new System.EventHandler(this.BtnAddCommandClick);
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(84, 3);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(139, 20);
            this.txtName.TabIndex = 3;
            // 
            // button1
            // 
            this.button1.AutoSize = true;
            this.button1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.button1.Location = new System.Drawing.Point(264, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(44, 23);
            this.button1.TabIndex = 10;
            this.button1.Text = "Finish";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Button1Click);
            // 
            // linkLabel4
            // 
            this.linkLabel4.AutoSize = true;
            this.linkLabel4.Location = new System.Drawing.Point(3, 4);
            this.linkLabel4.Name = "linkLabel4";
            this.linkLabel4.Size = new System.Drawing.Size(272, 13);
            this.linkLabel4.TabIndex = 0;
            this.linkLabel4.TabStop = true;
            this.linkLabel4.Text = "You need an active subscription to enable these options";
            this.linkLabel4.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabel4LinkClicked);
            // 
            // llblHelp
            // 
            this.llblHelp.AutoSize = true;
            this.llblHelp.Location = new System.Drawing.Point(229, 8);
            this.llblHelp.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
            this.llblHelp.Name = "llblHelp";
            this.llblHelp.Size = new System.Drawing.Size(29, 13);
            this.llblHelp.TabIndex = 9;
            this.llblHelp.TabStop = true;
            this.llblHelp.Text = "Help";
            this.llblHelp.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.llblHelp_LinkClicked);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.label82, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.lbManualAlerts, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label45, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.gpbSubscriber, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel2, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.lblCommand, 1, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(6, 34);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(613, 347);
            this.tableLayoutPanel1.TabIndex = 91;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.button1);
            this.flowLayoutPanel1.Controls.Add(this.llblHelp);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(299, 311);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.flowLayoutPanel1.Size = new System.Drawing.Size(311, 40);
            this.flowLayoutPanel1.TabIndex = 87;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Controls.Add(this.btnDelete);
            this.flowLayoutPanel2.Controls.Add(this.linkLabel3);
            this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(3, 162);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(290, 30);
            this.flowLayoutPanel2.TabIndex = 89;
            // 
            // linkLabel3
            // 
            this.linkLabel3.AutoSize = true;
            this.linkLabel3.Location = new System.Drawing.Point(87, 6);
            this.linkLabel3.Margin = new System.Windows.Forms.Padding(6);
            this.linkLabel3.Name = "linkLabel3";
            this.linkLabel3.Size = new System.Drawing.Size(35, 13);
            this.linkLabel3.TabIndex = 2;
            this.linkLabel3.TabStop = true;
            this.linkLabel3.Text = "Reset";
            this.linkLabel3.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel3_LinkClicked);
            // 
            // lblCommand
            // 
            this.lblCommand.AutoSize = true;
            this.lblCommand.Location = new System.Drawing.Point(299, 159);
            this.lblCommand.Name = "lblCommand";
            this.lblCommand.Padding = new System.Windows.Forms.Padding(6);
            this.lblCommand.Size = new System.Drawing.Size(66, 25);
            this.lblCommand.TabIndex = 90;
            this.lblCommand.Text = "Command";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.linkLabel4);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(6, 6);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(613, 28);
            this.panel1.TabIndex = 92;
            // 
            // RemoteCommands
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoScroll = true;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(625, 387);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.panel1);
            this.MinimizeBox = false;
            this.Name = "RemoteCommands";
            this.Padding = new System.Windows.Forms.Padding(6);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Remote Commands";
            this.Load += new System.EventHandler(this.ManualAlertsLoad);
            this.gpbSubscriber.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label45;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label label82;
        private System.Windows.Forms.Label label83;
        private System.Windows.Forms.ListBox lbManualAlerts;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.GroupBox gpbSubscriber;
        private System.Windows.Forms.Button btnAddCommand;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox txtExecute;
        private System.Windows.Forms.LinkLabel linkLabel4;
        private System.Windows.Forms.LinkLabel llblHelp;
        private System.Windows.Forms.LinkLabel linkLabel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.LinkLabel linkLabel3;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Label lblCommand;
    }
}