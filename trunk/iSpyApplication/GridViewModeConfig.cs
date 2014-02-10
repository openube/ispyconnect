using System;
using System.Globalization;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class GridViewModeConfig : Form
    {
        public string ModeConfig;
        public GridViewModeConfig()
        {
            InitializeComponent();
        }

        private void GridViewModeConfig_Load(object sender, EventArgs e)
        {
            int rd = 10;
            int id = -1;
            if (!String.IsNullOrEmpty(ModeConfig))
            {
                string[] cfg = ModeConfig.Split(',');
                if (cfg.Length == 2)
                {
                    rd = Convert.ToInt32(cfg[0]);
                    id = Convert.ToInt32(cfg[1]);
                }
            }

            int i = 1, j = 0;
            foreach (var c in MainForm.Cameras)
            {
                ddlDefault.Items.Add(new MainForm.ListItem2(c.name, c.id));
                if (c.id == id)
                    j = i;
                i++;
            }
            ddlDefault.SelectedIndex = j;
            numRemoveDelay.Value = rd;
        }

        private void GridViewModeConfig_FormClosing(object sender, FormClosingEventArgs e)
        {
            


        }

        private void button1_Click(object sender, EventArgs e)
        {
            string cfg = numRemoveDelay.Value.ToString(CultureInfo.InvariantCulture) + ",";
            if (ddlDefault.SelectedIndex > 0)
            {
                var o = (MainForm.ListItem2)ddlDefault.SelectedItem;
                cfg += o.Value;
            }
            ModeConfig = cfg;
            Close();
        }
    }
}
