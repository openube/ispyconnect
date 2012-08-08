using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

namespace iSpyServer
{
	/// <summary>
	/// Summary description for AboutForm.
	/// </summary>
	public class NewVersion : Form
    {
        private WebBrowser wbProductHistory;
        private Panel panel1;
        private Button button2;
        private Button button1;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

        public NewVersion()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
            RenderResources();
			//
			// TODO: Add any constructor code after InitializeComponent call
			//
        }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.wbProductHistory = new System.Windows.Forms.WebBrowser();
            this.panel1 = new System.Windows.Forms.Panel();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // wbProductHistory
            // 
            this.wbProductHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wbProductHistory.Location = new System.Drawing.Point(0, 0);
            this.wbProductHistory.MinimumSize = new System.Drawing.Size(20, 20);
            this.wbProductHistory.Name = "wbProductHistory";
            this.wbProductHistory.Size = new System.Drawing.Size(495, 320);
            this.wbProductHistory.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.button2);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 320);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(495, 34);
            this.panel1.TabIndex = 1;
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.Location = new System.Drawing.Point(333, 6);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(140, 23);
            this.button2.TabIndex = 1;
            this.button2.Text = "No Thanks";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(3, 6);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(165, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Get latest version";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // NewVersion
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(495, 354);
            this.Controls.Add(this.wbProductHistory);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NewVersion";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "New Version";
            this.Load += new System.EventHandler(this.NewVersion_Load);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

		}
		#endregion

        private void NewVersion_Load(object sender, EventArgs e)
        {
            wbProductHistory.Navigate("http://www.ispyconnect.com/producthistory.aspx?productid=11");
        }

        private void RenderResources() {
            
            this.Text = LocRM.GetString("About");
            button1.Text = LocRM.GetString("GetLatestVersion");
            button2.Text = LocRM.GetString("NoThanks");
            Text = LocRM.GetString("NewVersion");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this, "http://www.ispyconnect.com/download_ispyserver.aspx");

            MessageBox.Show(LocRM.GetString("ExportWarning"), LocRM.GetString("Note"));
            Close();
        }

        
	}
}
