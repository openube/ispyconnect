using System;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class VideoSourceAdvanced : Form
    {
        public objectsCamera Camobject;
        public VideoSourceAdvanced()
        {
            InitializeComponent();
            RenderResources();
        }

        private void VideoSourceAdvanced_Load(object sender, EventArgs e)
        {
            txtUserAgent.Text = Camobject.settings.useragent;
            txtResizeWidth.Value = Camobject.settings.desktopresizewidth;
            txtResizeHeight.Value = Camobject.settings.desktopresizeheight;
            chkNoResize.Checked = !Camobject.settings.resize;
            chkHttp10.Checked = Camobject.settings.usehttp10;
            chkFBA.Checked = Camobject.settings.forcebasic;
            txtReconnect.Value = Camobject.settings.reconnectinterval;
            txtCookies.Text = Camobject.settings.cookies;
            chkCalibrate.Checked = Camobject.settings.calibrateonreconnect;
            txtHeaders.Text = Camobject.settings.headers;
            
            numTimeout.Value = Camobject.settings.timeout;
        }

        private void RenderResources()
        {
            label5.Text = LocRm.GetString("ResizeTo");
            label2.Text = LocRm.GetString("UserAgent");
            label6.Text = LocRm.GetString("X");
            label4.Text = LocRm.GetString("Seconds");
            label3.Text = LocRm.GetString("ReconnectEvery");
            chkFBA.Text = LocRm.GetString("ForceBasic");
            LocRm.SetString(label1, "Cookies");
            LocRm.SetString(chkNoResize, "NoResize");
            LocRm.SetString(chkCalibrate, "CalibrateOnReconnect");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var iReconnect = (int)txtReconnect.Value;
            if (iReconnect < 30 && iReconnect != 0)
            {
                MessageBox.Show(LocRm.GetString("Validate_ReconnectInterval"), LocRm.GetString("Note"));
                return;
            }

            Camobject.settings.reconnectinterval = iReconnect;
            Camobject.settings.calibrateonreconnect = chkCalibrate.Checked;

            //wh must be even for stride calculations
            int w = Convert.ToInt32(txtResizeWidth.Value);
            if (w % 2 != 0)
                w++;
            Camobject.settings.desktopresizewidth = w;

            int h = Convert.ToInt32(txtResizeHeight.Value);
            if (h % 2 != 0)
                h++;

            Camobject.settings.desktopresizeheight = h;
            Camobject.settings.resize = !chkNoResize.Checked;

            Camobject.settings.usehttp10 = chkHttp10.Checked;
            Camobject.settings.cookies = txtCookies.Text;
            Camobject.settings.forcebasic = chkFBA.Checked;
            Camobject.settings.useragent = txtUserAgent.Text;
            Camobject.settings.headers = txtHeaders.Text;

            
            Camobject.settings.timeout = (int) numTimeout.Value;
            Close();
        }

        private void chkNoResize_CheckedChanged(object sender, EventArgs e)
        {
            txtResizeHeight.Enabled = txtResizeWidth.Enabled = !chkNoResize.Checked;        
        }
    }
}
