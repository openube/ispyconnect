using System;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class GridView : Form
    {
        private readonly Controls.GridView _gv;
        public GridView(MainForm parent, ref configurationGrid layout)
        {
            InitializeComponent();
            _gv = new Controls.GridView(ref layout);
            Controls.Add(_gv);
            _gv.Dock = DockStyle.Fill;
            _gv._parent = parent;
            
        }

        private void GridView_Load(object sender, EventArgs e)
        {
            Text = _gv.Text;
        }

    }
}
