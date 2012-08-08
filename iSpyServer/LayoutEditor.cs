using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace iSpyServer
{
    public partial class LayoutEditor : Form
    {
        public int X=0, Y=0, W=0, H=0;
        public LayoutEditor()
        {
            InitializeComponent();
            RenderResources();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            X = Convert.ToInt32(numX.Value);
            Y = Convert.ToInt32(numY.Value);
            W = Convert.ToInt32(numW.Value);
            H = Convert.ToInt32(numH.Value);

            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void LayoutEditor_Load(object sender, EventArgs e)
        {
            numX.Value = X;
            numY.Value = Y;
            numW.Value = W;
            numH.Value = H;
        }

        private void RenderResources() {
            this.Text = LocRM.GetString("LayoutEditor");
            button1.Text = LocRM.GetString("Update");
            label1.Text = LocRM.GetString("X");
            label2.Text = LocRM.GetString("Y");
            label3.Text = LocRM.GetString("Width");
            label4.Text = LocRM.GetString("Height");
        }

    }
}
