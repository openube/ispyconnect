using System;
using System.Linq;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class GridViewCustom : Form
    {
        public int Cols;
        public int Rows;
        public string GridName;
        public bool FullScreen;
        public bool AlwaysOnTop;
        public string Display;

        public GridViewCustom()
        {
            InitializeComponent();
            RenderResources();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (GridName == null && (String.IsNullOrEmpty(txtName.Text.Trim()) || MainForm.Conf.GridViews.Any(p => p.name.ToLower() == txtName.Text.Trim().ToLower())))
            {
                MessageBox.Show(this, LocRm.GetString("validate_uniquename"));
                return;
            }
            Rows = (int)numRows.Value;
            Cols = (int)numCols.Value;
            GridName = txtName.Text.Trim();
            FullScreen = chkFullScreen.Checked;
            AlwaysOnTop = chkAlwaysOnTop.Checked;
            Display = cmbDisplay.Text;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void GridViewCustom_Load(object sender, EventArgs e)
        {
            cmbDisplay.DataSource = Screen.AllScreens;
            cmbDisplay.DisplayMember = "DeviceName";

            numRows.Value = Rows;
            numCols.Value = Cols;
            txtName.Text = GridName;
            chkFullScreen.Checked = FullScreen;
            chkAlwaysOnTop.Checked = AlwaysOnTop;
            cmbDisplay.Text = Display;

            if (!String.IsNullOrEmpty(txtName.Text))
                return;
            
            int i = 1;
            bool cont = false;
            while(!cont)
            {
                cont = true;
                foreach (var g in MainForm.Conf.GridViews)
                {
                    if (g.name == "Grid "+i)
                        cont = false;
                }
                if (!cont)
                    i++;
            }
            txtName.Text = "Grid " + i;
        }

        private void RenderResources()
        {
            LocRm.SetString(this,"CustomiseGrid");
            LocRm.SetString(label3, "Name");
            LocRm.SetString(label1, "Columns");
            LocRm.SetString(label2, "Rows");
        }
    }
}
