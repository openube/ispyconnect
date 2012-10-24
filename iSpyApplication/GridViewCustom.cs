using System;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class GridViewCustom : Form
    {
        public int Cols;
        public int Rows;
        public GridViewCustom()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Rows = (int)numRows.Value;
            Cols = (int)numCols.Value;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
