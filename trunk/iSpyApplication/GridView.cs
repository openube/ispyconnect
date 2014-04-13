using System;
using System.Linq;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class GridView : Form
    {
        private readonly Controls.GridView _gv;
        private readonly configurationGrid _layout;


        public GridView(MainForm parent, ref configurationGrid layout)
        {
            InitializeComponent();
            _gv = new Controls.GridView(parent, ref layout);
            _gv.KeyDown += GridView_KeyDown;
            Controls.Add(_gv);
            _gv.Dock = DockStyle.Fill;
            _layout = layout;
            fullScreenToolStripMenuItem.Checked = layout.FullScreen;
            alwaysOnTopToolStripMenuItem.Checked = layout.AlwaysOnTop;

            
        }



        private void GridView_Load(object sender, EventArgs e)
        {
            Text = _gv.Text;

            var screen = Screen.AllScreens.Where(s => s.DeviceName == _layout.Display).DefaultIfEmpty(Screen.PrimaryScreen).First();
            StartPosition = FormStartPosition.Manual;
            Location = screen.Bounds.Location;

            if (fullScreenToolStripMenuItem.Checked)
                MaxMin();

            if (alwaysOnTopToolStripMenuItem.Checked)
                OnTop();
        }

        private void MaxMin()
        {
            if (fullScreenToolStripMenuItem.Checked)
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
        }

        private void OnTop()
        {
            TopMost = alwaysOnTopToolStripMenuItem.Checked;
        }

        private void Edit()
        {
            _gv.MainClass.EditGridView(_layout.name,this);
        }
        private void GridView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Alt && e.KeyCode == Keys.Enter)
            {
                fullScreenToolStripMenuItem.Checked = !fullScreenToolStripMenuItem.Checked;
                MaxMin();
            }
        }

        private void fullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MaxMin();
        }

        private void alwaysOnTopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OnTop();
        }

        private void closeGridViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Edit();
            _gv.Init();
        }

        private void switchFillModeAltFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _gv.Cg.Fill = !_gv.Cg.Fill;
        }

        private void GridView_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }

    }
}
