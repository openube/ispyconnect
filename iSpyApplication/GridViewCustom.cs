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
        public GridViewCustom()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Rows = (int)numRows.Value;
            Cols = (int)numCols.Value;
            GridName = txtName.Text.Trim();
            if (String.IsNullOrEmpty(GridName) || MainForm.Conf.GridViews.Any(p=>p.name.ToLower()==GridName.ToLower()))
            {
                MessageBox.Show(this, "Please enter a unique name");
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void GridViewCustom_Load(object sender, EventArgs e)
        {
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
    }
}
