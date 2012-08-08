namespace iSpyApplication
{
    partial class ErrorReporting
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ErrorReporting));
            this.label1 = new System.Windows.Forms.Label();
            this.txtHumanDescription = new System.Windows.Forms.TextBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnFeedback = new System.Windows.Forms.Button();
            this.chkErrorReporting = new System.Windows.Forms.CheckBox();
            this.txtErrorReport = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.label1, 2);
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(413, 55);
            this.label1.TabIndex = 0;
            this.label1.Text = "iSpy has encountered an unhandled error. It would really help us if you described" +
    " what you were doing when this happened with steps to reproduce it if possible.";
            // 
            // txtHumanDescription
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.txtHumanDescription, 2);
            this.txtHumanDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtHumanDescription.Location = new System.Drawing.Point(3, 211);
            this.txtHumanDescription.Multiline = true;
            this.txtHumanDescription.Name = "txtHumanDescription";
            this.txtHumanDescription.Size = new System.Drawing.Size(451, 90);
            this.txtHumanDescription.TabIndex = 1;
            // 
            // btnCancel
            // 
            this.btnCancel.AutoSize = true;
            this.btnCancel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnCancel.Location = new System.Drawing.Point(151, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(50, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancelClick);
            // 
            // btnFeedback
            // 
            this.btnFeedback.AutoSize = true;
            this.btnFeedback.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnFeedback.Location = new System.Drawing.Point(207, 3);
            this.btnFeedback.Name = "btnFeedback";
            this.btnFeedback.Size = new System.Drawing.Size(102, 23);
            this.btnFeedback.TabIndex = 3;
            this.btnFeedback.Text = "Send Error Report";
            this.btnFeedback.UseVisualStyleBackColor = true;
            this.btnFeedback.Click += new System.EventHandler(this.BtnFeedbackClick);
            // 
            // chkErrorReporting
            // 
            this.chkErrorReporting.AutoSize = true;
            this.chkErrorReporting.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkErrorReporting.Location = new System.Drawing.Point(3, 346);
            this.chkErrorReporting.Name = "chkErrorReporting";
            this.chkErrorReporting.Size = new System.Drawing.Size(133, 30);
            this.chkErrorReporting.TabIndex = 4;
            this.chkErrorReporting.Text = "Enable Error Reporting";
            this.chkErrorReporting.UseVisualStyleBackColor = true;
            // 
            // txtErrorReport
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.txtErrorReport, 2);
            this.txtErrorReport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtErrorReport.Location = new System.Drawing.Point(3, 58);
            this.txtErrorReport.Multiline = true;
            this.txtErrorReport.Name = "txtErrorReport";
            this.txtErrorReport.Size = new System.Drawing.Size(451, 134);
            this.txtErrorReport.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.label2, 2);
            this.label2.Location = new System.Drawing.Point(3, 195);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(184, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Please enter steps to reproduce here:";
            // 
            // label3
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.label3, 2);
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.label3.Location = new System.Drawing.Point(3, 304);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(413, 39);
            this.label3.TabIndex = 7;
            this.label3.Text = "Include your email address if you would like a response.";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.chkErrorReporting, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.txtErrorReport, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.txtHumanDescription, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 1, 5);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(6, 6);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 6;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(457, 375);
            this.tableLayoutPanel1.TabIndex = 8;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.btnFeedback);
            this.flowLayoutPanel1.Controls.Add(this.btnCancel);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(142, 346);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.flowLayoutPanel1.Size = new System.Drawing.Size(312, 30);
            this.flowLayoutPanel1.TabIndex = 8;
            // 
            // ErrorReporting
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.AutoScroll = true;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(469, 387);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ErrorReporting";
            this.Padding = new System.Windows.Forms.Padding(6);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Error Reporting";
            this.Load += new System.EventHandler(this.FeedbackLoad);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtHumanDescription;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnFeedback;
        private System.Windows.Forms.CheckBox chkErrorReporting;
        private System.Windows.Forms.TextBox txtErrorReport;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
    }
}