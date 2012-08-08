namespace iSpyServer
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
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(413, 55);
            this.label1.TabIndex = 0;
            this.label1.Text = "iSpy has encountered an unhandled error. It would really help us if you described" +
                " what you were doing when this happened with steps to reproduce it if possible.";
            // 
            // txtHumanDescription
            // 
            this.txtHumanDescription.Location = new System.Drawing.Point(12, 226);
            this.txtHumanDescription.Multiline = true;
            this.txtHumanDescription.Name = "txtHumanDescription";
            this.txtHumanDescription.Size = new System.Drawing.Size(413, 90);
            this.txtHumanDescription.TabIndex = 1;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(350, 361);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 24);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnFeedback
            // 
            this.btnFeedback.Location = new System.Drawing.Point(229, 361);
            this.btnFeedback.Name = "btnFeedback";
            this.btnFeedback.Size = new System.Drawing.Size(107, 24);
            this.btnFeedback.TabIndex = 3;
            this.btnFeedback.Text = "Send Error Report";
            this.btnFeedback.UseVisualStyleBackColor = true;
            this.btnFeedback.Click += new System.EventHandler(this.btnFeedback_Click);
            // 
            // chkErrorReporting
            // 
            this.chkErrorReporting.AutoSize = true;
            this.chkErrorReporting.Location = new System.Drawing.Point(15, 366);
            this.chkErrorReporting.Name = "chkErrorReporting";
            this.chkErrorReporting.Size = new System.Drawing.Size(133, 17);
            this.chkErrorReporting.TabIndex = 4;
            this.chkErrorReporting.Text = "Enable Error Reporting";
            this.chkErrorReporting.UseVisualStyleBackColor = true;
            // 
            // txtErrorReport
            // 
            this.txtErrorReport.Location = new System.Drawing.Point(12, 67);
            this.txtErrorReport.Multiline = true;
            this.txtErrorReport.Name = "txtErrorReport";
            this.txtErrorReport.Size = new System.Drawing.Size(413, 134);
            this.txtErrorReport.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 204);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(184, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Please enter steps to reproduce here:";
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.label3.Location = new System.Drawing.Point(12, 319);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(413, 39);
            this.label3.TabIndex = 7;
            this.label3.Text = "Include your email address if you would like a response.";
            // 
            // ErrorReporting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(437, 397);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtErrorReport);
            this.Controls.Add(this.chkErrorReporting);
            this.Controls.Add(this.btnFeedback);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.txtHumanDescription);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ErrorReporting";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Error Reporting";
            this.Load += new System.EventHandler(this.Feedback_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

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
    }
}