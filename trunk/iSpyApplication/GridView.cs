using System;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class GridView : Form
    {
        private readonly Controls.GridView _gv;
        public GridView(MainForm parent, string layout)
        {
            InitializeComponent();
            _gv = new Controls.GridView();
            Controls.Add(_gv);
            _gv.Dock = DockStyle.Fill;
            _gv._parent = parent;

            string t = layout;
            string[] rc = t.Split('x');
            _gv.Cols = Convert.ToInt32(rc[0]);
            _gv.Rows = Convert.ToInt32(rc[1]);
        }

        private void GridView_Load(object sender, EventArgs e)
        {

        }

    }
}
