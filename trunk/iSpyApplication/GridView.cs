using System;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class GridView : Form
    {
        private readonly Controls.GridView _gv;
        private bool _fullscreen;

        public GridView(MainForm parent, ref configurationGrid layout)
        {
            InitializeComponent();
            _gv = new Controls.GridView(ref layout);
            _gv.KeyDown += GridView_KeyDown;
            Controls.Add(_gv);
            _gv.Dock = DockStyle.Fill;
            _gv._parent = parent;
            
        }

        private void GridView_Load(object sender, EventArgs e)
        {
            Text = _gv.Text;
        }

        internal void MaxMin()
        {
            if (!_fullscreen)
            {
                WindowState = FormWindowState.Maximized;
                FormBorderStyle = FormBorderStyle.None;
                WinApi.SetWinFullScreen(Handle);
            }
            else
            {
                WindowState = FormWindowState.Maximized;
                FormBorderStyle = FormBorderStyle.Sizable;
            }
            _fullscreen = !_fullscreen;
        }

        private void GridView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Alt && e.KeyCode==Keys.Enter)
            {
                MaxMin();
            }
        }

        private void fullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MaxMin();
        }
    }
}
