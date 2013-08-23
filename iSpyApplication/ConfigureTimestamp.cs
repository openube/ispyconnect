using System;
using System.Drawing;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class ConfigureTimestamp : Form
    {
        public int TimeStampLocation = 0, FontSize = 10;
        public decimal Offset = 0;
        public Color TimestampForeColor;
        public Color TimestampBackColor;

        public ConfigureTimestamp()
        {
            InitializeComponent();
            RenderResources();
        }

        private void RenderResources()
        {
            Text = LocRm.GetString("Configure");
            label1.Text = LocRm.GetString("Location");
            label2.Text = LocRm.GetString("Size");
            label3.Text = LocRm.GetString("Offset");
            button1.Text = LocRm.GetString("OK");
        }

        private void ConfigureTimestamp_Load(object sender, EventArgs e)
        {
            ddlTimestampLocation.Items.AddRange(LocRm.GetString("TimeStampLocations").Split(','));
            ddlTimestampLocation.SelectedIndex = TimeStampLocation;
            numFontSize.Value = FontSize;
            numOffset.Value = Offset;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TimeStampLocation = ddlTimestampLocation.SelectedIndex;
            FontSize = (int) numFontSize.Value;
            Offset = numOffset.Value;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var cd = new ColorDialog { Color = TimestampForeColor, AllowFullOpen = true, SolidColorOnly = false };
            if (cd.ShowDialog(this)==DialogResult.OK)
            {
                TimestampForeColor = cd.Color;
            }
            cd.Dispose();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var cd = new ColorDialog { Color = TimestampBackColor, AllowFullOpen = true, SolidColorOnly = false };
            if (cd.ShowDialog(this) == DialogResult.OK)
            {
                TimestampBackColor = cd.Color;
            }
            cd.Dispose();
        }
    }
}
