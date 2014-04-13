using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using iSpyApplication.Video;
using NAudio.Wave;
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

            Mic.settings.analyzeduration = (int)numAnalyseDuration.Value;
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
        
        private object[] ObjectList(string str)
        {
            string[] ss = str.Split('|');
            var o = new object[ss.Length];
            int i = 0;
            foreach (string s in ss)
            {
                o[i] = s;
                i++;
            }
            return o;
        }

        private void MicrophoneSourceLoad(object sender, EventArgs e)
        {
            tableLayoutPanel2.Enabled = VlcHelper.VlcInstalled;
            linkLabel3.Visible = lblInstallVLC.Visible = !tableLayoutPanel2.Enabled;
            cmbVLCURL.Text = MainForm.Conf.VLCURL;
            cmbVLCURL.Items.AddRange(ObjectList(MainForm.Conf.RecentVLCList));
            cmbFFMPEGURL.Items.AddRange(ObjectList(MainForm.Conf.RecentVLCList));
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
                    if (s == Mic.settings.samples.ToString(CultureInfo.InvariantCulture))
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
                int i;
                Int32.TryParse(Mic.settings.sourcename, out i);
                var c = MainForm.Cameras.SingleOrDefault(p => p.id == i);
                lblCamera.Text = c == null ? LocRm.GetString("Removed") : c.name;
            }

            txtVLCArgs.Text = Mic.settings.vlcargs.Replace("\r\n", "\n").Replace("\n\n", "\n").Replace("\n", Environment.NewLine);

            numAnalyseDuration.Value = Mic.settings.analyzeduration;
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

            llblHelp.Text = LocRm.GetString("help");
            LocRm.SetString(label21,"FileURL");
            LocRm.SetString(label18, "Arguments");
            LocRm.SetString(lblInstallVLC, "VLCHelp");
            lblInstallVLC.Text = lblInstallVLC.Text.Replace("x86", Program.Platform);
            LocRm.SetString(label2, "FileURL");
            LocRm.SetString(btnTest, "Test");
            LocRm.SetString(lblCamera, "NoCamera");

            LocRm.SetString(tabPage5, "Camera");
            LocRm.SetString(label1,"SampleRate");
            LocRm.SetString(label7, "AnalyseDurationMS");

        }


        private void DdlDeviceSelectedIndexChanged(object sender, EventArgs e)
        {

        }


        private void TextBox1TextChanged(object sender, EventArgs e)
        {
        }

        private void LinkLabel3LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl(Program.Platform == "x64" ? MainForm.VLCx64 : MainForm.VLCx86);
            if (Program.Platform == "x64")
                MessageBox.Show(this, LocRm.GetString("InstallVLCx64").Replace("[DIR]", Environment.NewLine + Program.AppPath + "VLC64" + Environment.NewLine));
            else
                MessageBox.Show(this, LocRm.GetString("InstallVLCx86")); ;
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
            btnTest.Enabled = false;

            string res = "OK";
            try
            {
                Program.WriterMutex.WaitOne();
                var afr = new AudioFileReader();
                string source = cmbFFMPEGURL.Text;
                int i = source.IndexOf("://", StringComparison.Ordinal);
                if (i > -1)
                {
                    source = source.Substring(0, i).ToLower() + source.Substring(i);
                }

                afr.Timeout = Mic.settings.timeout;
                afr.AnalyzeDuration = (int)numAnalyseDuration.Value;
                afr.Open(source);
                afr.ReadAudioFrame();
                Mic.settings.channels = afr.Channels;
                Mic.settings.samples = afr.SampleRate;
                Mic.settings.bits = 16;

                afr.Dispose();
                afr = null;              
            }
            catch (Exception ex)
            {
                res = ex.Message;
            }
            finally
            {
                Program.WriterMutex.ReleaseMutex();                
            }
            MessageBox.Show(res);
            
            btnTest.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ofd.Filter = "Audio Files|*.*";
            ofd.InitialDirectory = Program.AppPath;
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                cmbVLCURL.Text = ofd.FileName;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ofd.Filter = "Audio Files|*.*";
            ofd.InitialDirectory = Program.AppPath;
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                cmbFFMPEGURL.Text = ofd.FileName;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var vsa = new MicrophoneSourceAdvanced { Micobject = Mic };
            vsa.ShowDialog(this);
            vsa.Dispose();
        }
    }
}