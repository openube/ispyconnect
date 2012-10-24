using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class GridViewCamera : Form
    {
        public int Delay = 4;
        public List<int> SelectedIDs;

        public GridViewCamera()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Delay = Convert.ToInt32(numDelay.Value);
            SelectedIDs = new List<int>();
            foreach(MainForm.ListItem2 li in lbCameras.SelectedItems)
            {
                SelectedIDs.Add(li.Value);
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void GridViewCamera_Load(object sender, EventArgs e)
        {
            numDelay.Value = Delay;
            foreach(var c in MainForm.Cameras)
            {
                lbCameras.Items.Add(new MainForm.ListItem2(c.name, c.id));
            }           
            for(int j=0;j<lbCameras.Items.Count;j++)
            {
                var li = (MainForm.ListItem2) lbCameras.Items[j];
                if (SelectedIDs.Contains(li.Value))
                {
                    lbCameras.SetSelected(j, true);
                }
            }
        }
    }
}
