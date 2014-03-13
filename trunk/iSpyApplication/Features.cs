using System;
using System.Linq;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class Features : Form
    {
        public Features()
        {
            InitializeComponent();
        }

        private void Features_Load(object sender, EventArgs e)
        {
            Text = LocRm.GetString("Features");
            var i = 1;
            var feats = Enum.GetValues(typeof (Enums.Features));
            foreach (var f in feats)
            {
                var cb = new CheckBox {Text = f.ToString(), Tag = f, AutoSize = true};
                if ((Convert.ToInt32(f) & MainForm.Conf.FeatureSet) == i)
                    cb.Checked = true;
                fpFeatures.Controls.Add(cb);
                i = i * 2;                
                
            }
        }
        private void Features_FormClosing(object sender, FormClosingEventArgs e)
        {
            var tot = (from CheckBox c in fpFeatures.Controls where c.Checked select (int) c.Tag).Sum();
            MainForm.Conf.FeatureSet = tot;
        }
    }
}
