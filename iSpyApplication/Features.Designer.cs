namespace iSpyApplication
{
    partial class Features
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Features));
            this.fpFeatures = new System.Windows.Forms.FlowLayoutPanel();
            this.SuspendLayout();
            // 
            // fpFeatures
            // 
            this.fpFeatures.AutoScroll = true;
            this.fpFeatures.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fpFeatures.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.fpFeatures.Location = new System.Drawing.Point(0, 0);
            this.fpFeatures.Name = "fpFeatures";
            this.fpFeatures.Padding = new System.Windows.Forms.Padding(6);
            this.fpFeatures.Size = new System.Drawing.Size(429, 381);
            this.fpFeatures.TabIndex = 0;
            // 
            // Features
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(429, 381);
            this.Controls.Add(this.fpFeatures);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Features";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Features";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Features_FormClosing);
            this.Load += new System.EventHandler(this.Features_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel fpFeatures;
    }
}