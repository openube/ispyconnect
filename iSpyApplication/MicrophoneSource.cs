using System;
using System.Linq;
using System.Windows.Forms;
using iSpyApplication.Video;
using NAudio.Wave;
using iSpy.Video.FFMPEG;
using AudioFileReader = iSpy.Video.FFMPEG.AudioFileReader;

namespace iSpyApplication
{
    public partial class MicrophoneSource : Form
    {
        private readonly string _noDevices = LocRm.GetString("NoAudioDevices");
        public objectsMicrophone Mic;

        public MicrophoneSource()
        {
            InitializeComponent();
            RenderResources();
        }

        private void Button1Click(object sender, EventArgs e)
        {
            Finish();
        }

        private void Finish()
        {
            var iReconnect = (int)txtReconnect.Value;
            if (iReconnect < 30 && iReconnect != 0)
            {
                MessageBox.Show(LocRm.GetString("Validate_ReconnectInterval"), LocRm.GetString("Note"));
                return;
            }

            switch (tcAudioSource.SelectedIndex)
            {
                case 0:
                    if (!ddlDevice.Enabled)
                    {
                        Close();
                        return;
                    }
                    Mic.settings.sourcename = ddlDevice.SelectedItem.ToString();

                    int i = 0, selind = -1;
                    for (int n = 0; n < WaveIn.DeviceCount; n++)
                    {
                        ddlDevice.Items.Add(WaveIn.GetCapabilities(n).ProductName);
                        if (WaveIn.GetCapabilities(n).ProductName == Mic.settings.sourcename)
                            selind = i;
                        i++;
                    }

                    int channels = WaveIn.GetCapabilities(selind).Channels;

                    Mic.settings.channels = channels;
                    Mic.settings.samples = Convert.ToInt32(ddlSampleRate.SelectedItem);
                    Mic.settings.bits = 16;

                    break;
                case 1:
                    try
                    {
                        var url = new Uri(txtNetwork.Text);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
                    }
                    Mic.settings.sourcename = txtNetwork.Text;

                    //set format
                    Mic.settings.channels = 1;
                    Mic.settings.samples = 22050;
                    Mic.settings.bits = 16;
                    break;
                case 2:
                    string t = cmbVLCURL.Text.Trim();
                    if (t == String.Empty)
                    {
                        MessageBox.Show(LocRm.GetString("Validate_Microphone_SelectSource"), LocRm.GetString("Error"));
                        return;
                    }
                    Mic.settings.sourcename = t;
                    break;
                case 3:
                    try
                    {
                        var url = new Uri(cmbFFMPEGURL.Text);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
                    }
                    Mic.settings.sourcename = cmbFFMPEGURL.Text;
                    break;
            }

            MainForm.Conf.VLCURL = cmbVLCURL.Text.Trim();
            if (!MainForm.Conf.RecentVLCList.Contains(MainForm.Conf.VLCURL) &&
                MainForm.Conf.VLCURL != "")
            {
                MainForm.Conf.RecentVLCList =
                    (MainForm.Conf.RecentVLCList + "|" + MainForm.Conf.VLCURL).Trim('|');
            }

            Mic.settings.typeindex = tcAudioSource.SelectedIndex;
            Mic.settings.decompress = true; // chkDecompress.Checked;
            Mic.settings.vlcargs = txtVLCArgs.Text.Trim();

            Mic.settings.reconnectinterval = (int)txtReconnect.Value;
            //int samplerate;
            //if (Int32.TryParse(txtSampleRate.Text, out samplerate))
            //    Mic.settings.samples = samplerate;

            //int bits;
            //if (Int32.TryParse(txtBits.Text, out bits))
            //    Mic.settings.bits = bits;

            //int channels;
            //if (Int32.TryParse(txtChannels.Text, out channels))
            //    Mic.settings.channels = channels;

            // Mic.settings.username = txtUsername.Text;
            // Mic.settings.password = txtPassword.Text;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void Button2Click(object sender, EventArgs e)
        {
            //cancel
            Close();
        }

        private void MicrophoneSourceLoad(object sender, EventArgs e)
        {
            tableLayoutPanel2.Enabled = VlcHelper.VlcInstalled;
            linkLabel3.Visible = lblInstallVLC.Visible = !tableLayoutPanel2.Enabled;
            cmbVLCURL.Text = MainForm.Conf.VLCURL;
            cmbVLCURL.Items.AddRange(MainForm.Conf.RecentVLCList.Split('|'));
            cmbFFMPEGURL.Items.AddRange(MainForm.Conf.RecentVLCList.Split('|'));
            try
            {
                int i = 0, selind = -1;
                for (int n = 0; n < WaveIn.DeviceCount; n++)
                {
                    ddlDevice.Items.Add(WaveIn.GetCapabilities(n).ProductName);
                    if (WaveIn.GetCapabilities(n).ProductName == Mic.settings.sourcename)
                        selind = i;
                    i++;
                }
                ddlDevice.Enabled = true;
                if (selind > -1)
                    ddlDevice.SelectedIndex = selind;
                else
                {
                    if (ddlDevice.Items.Count == 0)
                    {
                        ddlDevice.Items.Add(_noDevices);
                        ddlDevice.Enabled = false;
                    }
                    else
                        ddlDevice.SelectedIndex = 0;
                }
            }
            catch (ApplicationException ex)
            {
                MainForm.LogExceptionToFile(ex);
                ddlDevice.Items.Add(_noDevices);
                ddlDevice.Enabled = false;
            }
            ddlSampleRate.SelectedIndex = 0;
            

            tcAudioSource.SelectedIndex = Mic.settings.typeindex;

            if (Mic.settings.typeindex == 0 && ddlDevice.Items.Count > 0)
            {
                tcAudioSource.SelectedIndex = 0;
                int j = 0;
                foreach(string s in ddlSampleRate.Items)
                {
                    if (s == Mic.settings.samples.ToString())
                        ddlSampleRate.SelectedIndex = j;
                    j++;
                }
            }
            if (Mic.settings.typeindex == 1)
            {
                txtNetwork.Text = Mic.settings.sourcename;
            }
            if (Mic.settings.typeindex == 2)
            {
                cmbVLCURL.Text = Mic.settings.sourcename;
            }
            if (Mic.settings.typeindex==3)
            {
                cmbFFMPEGURL.Text = Mic.settings.sourcename;
            }
            if (Mic.settings.typeindex==4)
            {
                int i = 0;
                Int32.TryParse(Mic.settings.sourcename, out i);
                var c = MainForm.Cameras.SingleOrDefault(p => p.id == i);
                if (c == null)
                    lblCamera.Text = LocRm.GetString("Removed");
                else
                {
                    lblCamera.Text = c.name;
                }
            }

            txtVLCArgs.Text = Mic.settings.vlcargs;


            //chkDecompress.Checked = Mic.settings.decompress;
            txtReconnect.Value = Mic.settings.reconnectinterval;
            //txtUsername.Text = Mic.settings.username;
            //txtPassword.Text = Mic.settings.password;
        }

        private void RenderResources()
        {
            Text = LocRm.GetString("Microphonesource");
            button1.Text = LocRm.GetString("Ok");
            button2.Text = LocRm.GetString("Cancel");
            label8.Text = LocRm.GetString("Device");
            label9.Text = LocRm.GetString("Url");
            tabPage1.Text = LocRm.GetString("LocalDevice");
            tabPage3.Text = LocRm.GetString("iSpyServer");
            tabPage2.Text = LocRm.GetString("VLCPlugin");

            label18.Text = LocRm.GetString("Arguments");
            lblInstallVLC.Text = LocRm.GetString("VLCConnectInfo");
            linkLabel3.Text = LocRm.GetString("DownloadVLC");
            linkLabel1.Text = LocRm.GetString("UseiSpyServerText");

            label43.Text = LocRm.GetString("ReconnectEvery");
            label48.Text = LocRm.GetString("Seconds");

            llblHelp.Text = LocRm.GetString("help");
            LocRm.SetString(label21,"FileURL");
            LocRm.SetString(label18, "Arguments");
            LocRm.SetString(lblInstallVLC, "VLCHelp");
            lblInstallVLC.Text = lblInstallVLC.Text.Replace("x86", Program.Platform);
            LocRm.SetString(label2, "FileURL");
            LocRm.SetString(btnTest, "Test");
            LocRm.SetString(lblCamera, "NoCamera");

            LocRm.SetString(tabPage5, "Camera");

        }


        private void DdlDeviceSelectedIndexChanged(object sender, EventArgs e)
        {

        }


        private void TextBox1TextChanged(object sender, EventArgs e)
        {
        }

        private void LinkLabel3LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl( "http://www.videolan.org/vlc/download-windows.html");
        }

        private void LinkLabel1LinkClicked1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl( MainForm.Website+"/download_ispyserver.aspx");
        }

        private void llblHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = MainForm.Website+"/userguide-microphones.aspx";
            switch (tcAudioSource.SelectedIndex)
            {
                case 0:
                    url = MainForm.Website+"/userguide-microphones.aspx#1";
                    break;
                case 1:
                    url = MainForm.Website+"/userguide-microphones.aspx#3";
                    break;
                case 2:
                    url = MainForm.Website+"/userguide-microphones.aspx#2";
                    break;
            }
            MainForm.OpenUrl( url);
        }

        public bool NoBuffer;
        private void Test_Click(object sender, EventArgs e)
        {
            var afr = new AudioFileReader();

            try
            {
                afr.Open(cmbFFMPEGURL.Text, 8000, 2000, "", -1, NoBuffer);
                afr.ReadAudioFrame();

                Mic.settings.channels = afr.Channels;
                Mic.settings.samples = afr.SampleRate;
                Mic.settings.bits = 16;

                MessageBox.Show("OK");

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            try
            {
                afr.Close();
            }
            catch
            {
                
            }
            afr = null;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ofd.Filter = "Audio Files|*.*";
            ofd.InitialDirectory = MainForm.Conf.MediaDirectory;
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                cmbVLCURL.Text = ofd.FileName;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ofd.Filter = "Audio Files|*.*";
            ofd.InitialDirectory = MainForm.Conf.MediaDirectory;
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                cmbFFMPEGURL.Text = ofd.FileName;
            }
        }
    }
}