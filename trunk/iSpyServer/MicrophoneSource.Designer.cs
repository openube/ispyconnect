namespace iSpyServer
{
    partial class MicrophoneSource
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
            this.ddlDevice = new System.Windows.Forms.ComboBox();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.tcAudioSource = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label8 = new System.Windows.Forms.Label();
            this.tcAudioSource.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ddlDevice
            // 
            this.ddlDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ddlDevice.FormattingEnabled = true;
            this.ddlDevice.Location = new System.Drawing.Point(24, 54);
            this.ddlDevice.Name = "ddlDevice";
            this.ddlDevice.Size = new System.Drawing.Size(320, 21);
            this.ddlDevice.TabIndex = 26;
            this.ddlDevice.SelectedIndexChanged += new System.EventHandler(this.ddlDevice_SelectedIndexChanged);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(224, 188);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(81, 23);
            this.button2.TabIndex = 25;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(311, 188);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(67, 23);
            this.button1.TabIndex = 24;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // tcAudioSource
            // 
            this.tcAudioSource.Controls.Add(this.tabPage1);
            this.tcAudioSource.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.tcAudioSource.Location = new System.Drawing.Point(11, 12);
            this.tcAudioSource.Name = "tcAudioSource";
            this.tcAudioSource.SelectedIndex = 0;
            this.tcAudioSource.Size = new System.Drawing.Size(371, 170);
            this.tcAudioSource.TabIndex = 45;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label8);
            this.tabPage1.Controls.Add(this.ddlDevice);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(363, 144);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Local Device";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 25);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(41, 13);
            this.label8.TabIndex = 31;
            this.label8.Text = "Device";
            // 
            // MicrophoneSource
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(389, 222);
            this.Controls.Add(this.tcAudioSource);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "MicrophoneSource";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MicrophoneSource";
            this.Load += new System.EventHandler(this.MicrophoneSource_Load);
            this.tcAudioSource.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox ddlDevice;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TabControl tcAudioSource;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Label label8;
    }
}