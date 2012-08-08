using System;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace iSpyServer
{
    public partial class MicrophoneSource : Form
    {
        public objectsMicrophone Mic;
       

        private string NoDevices = LocRM.GetString("NoAudioDevices");

        public MicrophoneSource()
        {
            InitializeComponent();
            RenderResources();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Finish();
        }

        private void Finish()
        {
            
            Mic.settings.sourcename = ddlDevice.SelectedItem.ToString();
            Mic.settings.typeindex = tcAudioSource.SelectedIndex;

            Mic.settings.samples = 8000;

            Mic.settings.bits = 16;

            Mic.settings.channels = 1;

            
            DialogResult = DialogResult.OK;
            Close();
           
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //cancel
            Close();
        }

        private void MicrophoneSource_Load(object sender, EventArgs e)
        {
            try
            {

                int _i = 0, _selind=-1;
                for (int n = 0; n < WaveIn.DeviceCount; n++)
                {
                    ddlDevice.Items.Add(WaveIn.GetCapabilities(n).ProductName);
                    if (WaveIn.GetCapabilities(n).ProductName == Mic.settings.sourcename)
                        _selind = _i;
                    _i++;
                        
                }
                ddlDevice.Enabled = true;
                if (_selind > -1)
                    ddlDevice.SelectedIndex = _selind;
                else
                {
                    if (ddlDevice.Items.Count == 0)
                    {
                        ddlDevice.Items.Add(NoDevices);
                        ddlDevice.Enabled = false;
                    }
                    else
                        ddlDevice.SelectedIndex = 0;
                }
            }
            catch (ApplicationException ex)
            {
                MainForm.LogExceptionToFile(ex);
                ddlDevice.Items.Add(NoDevices);
                ddlDevice.Enabled = false;
            }
            
            tcAudioSource.SelectedIndex = Mic.settings.typeindex;
            if (Mic.settings.typeindex == 0 && ddlDevice.Items.Count > 0)
            {
                tcAudioSource.SelectedIndex = 0;
            }
        }

        private void RenderResources() {
            this.Text = LocRM.GetString("Microphonesource");
            button1.Text = LocRM.GetString("Ok");
            button2.Text = LocRM.GetString("Cancel");
            label8.Text = LocRM.GetString("Device");
            tabPage1.Text = LocRM.GetString("LocalDevice");
        }


        private void ddlDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            

        }
    }
}