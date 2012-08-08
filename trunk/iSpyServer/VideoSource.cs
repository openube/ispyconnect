using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AForge.Video.DirectShow;
using iSpyServer.Video;
using Microsoft.Kinect;

namespace iSpyServer
{
    public partial class VideoSource : Form
    {
        public CameraWindow CameraControl;
        public string CameraLogin;
        public string CameraPassword;
        public string FriendlyName = "";
        public int SourceIndex;
        public string UserAgent;
        public string VideoSourceString;

        // collection of available video devices
        private FilterInfoCollection videoDevices;
        // selected video device
        private VideoCaptureDevice videoDevice;

        // supported capabilities of video and snapshots
        private Dictionary<string, VideoCapabilities> videoCapabilitiesDictionary = new Dictionary<string, VideoCapabilities>();
        private Dictionary<string, VideoCapabilities> snapshotCapabilitiesDictionary = new Dictionary<string, VideoCapabilities>();

        // available video inputs
        private VideoInput[] availableVideoInputs = null;

        // flag telling if user wants to configure snapshots as well
        private bool configureSnapshots = false;

        public bool ConfigureSnapshots
        {
            get { return configureSnapshots; }
            set
            {
                configureSnapshots = value;
                snapshotsLabel.Visible = value;
                snapshotResolutionsCombo.Visible = value;
            }
        }

        /// <summary>
        /// Provides configured video device.
        /// </summary>
        /// 
        /// <remarks><para>The property provides configured video device if user confirmed
        /// the dialog using "OK" button. If user canceled the dialog, the property is
        /// set to <see langword="null"/>.</para></remarks>
        /// 
        public VideoCaptureDevice VideoDevice
        {
            get { return videoDevice; }
        }

        private string videoDeviceMoniker = string.Empty;
        private Size captureSize = new Size(0, 0);
        private Size snapshotSize = new Size(0, 0);
        public int FrameRate = 0;
        private VideoInput videoInput = VideoInput.Default;

        /// <summary>
        /// Moniker string of the selected video device.
        /// </summary>
        /// 
        /// <remarks><para>The property allows to get moniker string of the selected device
        /// on form completion or set video device which should be selected by default on
        /// form loading.</para></remarks>
        /// 
        public string VideoDeviceMoniker
        {
            get { return videoDeviceMoniker; }
            set { videoDeviceMoniker = value; }
        }

        /// <summary>
        /// Video frame size of the selected device.
        /// </summary>
        /// 
        /// <remarks><para>The property allows to get video size of the selected device
        /// on form completion or set the size to be selected by default on form loading.</para>
        /// </remarks>
        /// 
        public Size CaptureSize
        {
            get { return captureSize; }
            set { captureSize = value; }
        }

        /// <summary>
        /// Snapshot frame size of the selected device.
        /// </summary>
        /// 
        /// <remarks><para>The property allows to get snapshot size of the selected device
        /// on form completion or set the size to be selected by default on form loading
        /// (if <see cref="ConfigureSnapshots"/> property is set <see langword="true"/>).</para>
        /// </remarks>
        public Size SnapshotSize
        {
            get { return snapshotSize; }
            set { snapshotSize = value; }
        }

        /// <summary>
        /// Video input to use with video capture card.
        /// </summary>
        /// 
        /// <remarks><para>The property allows to get video input of the selected device
        /// on form completion or set it to be selected by default on form loading.</para></remarks>
        /// 
        public VideoInput VideoInput
        {
            get { return videoInput; }
            set { videoInput = value; }
        }

        public VideoSource()
        {
            InitializeComponent();
            RenderResources();
            // show device list
            try
            {
                // enumerate video devices
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count == 0)
                    throw new ApplicationException();

                // add all devices to combo
                foreach (FilterInfo device in videoDevices)
                {
                    devicesCombo.Items.Add(device.Name);
                }
            }
            catch (ApplicationException)
            {
                devicesCombo.Items.Add(LocRM.GetString("NoDevicesFound"));
                devicesCombo.Enabled = false;
                //okButton.Enabled = false;
            }
            ConfigureSnapshots = false;
        }

        private void VideoSource_Load(object sender, EventArgs e)
        {
            cmbJPEGURL.Text = iSpyServer.Default.JPEGURL;
            cmbMJPEGURL.Text = iSpyServer.Default.MJPEGURL;
            txtLogin.Text = txtLogin2.Text = CameraControl.Camobject.settings.login;
            txtPassword.Text = txtPassword2.Text = CameraControl.Camobject.settings.password;
            txtUserAgent.Text = txtUserAgent2.Text = CameraControl.Camobject.settings.useragent;
            txtResizeWidth.Value = CameraControl.Camobject.settings.desktopresizewidth;
            txtResizeHeight.Value = CameraControl.Camobject.settings.desktopresizeheight;
            chkNoResize.Checked = !CameraControl.Camobject.settings.resize;
            VideoSourceString = CameraControl.Camobject.settings.videosourcestring;
            SourceIndex = CameraControl.Camobject.settings.sourceindex;
            txtFrameInterval.Text = txtFrameInterval2.Text = CameraControl.Camobject.settings.frameinterval.ToString();
            chkMousePointer.Checked = CameraControl.Camobject.settings.desktopmouse;
            txtEncodeKey.Text = CameraControl.Camobject.encodekey;

            switch (SourceIndex)
            {
                case 0:
                    cmbJPEGURL.Text = VideoSourceString;
                    txtFrameInterval.Text = CameraControl.Camobject.settings.frameinterval.ToString();
                    break;
                case 1:
                    cmbMJPEGURL.Text = VideoSourceString;
                    break;
            }

            if (SourceIndex == 3)
            {
                VideoDeviceMoniker = VideoSourceString;
                string[] wh = CameraControl.Camobject.resolution.Split('x');
                CaptureSize = new Size(Convert.ToInt32(wh[0]), Convert.ToInt32(wh[1]));
            }

            cmbJPEGURL.Items.AddRange(iSpyServer.Default.RecentJPGList.Split('|'));
            cmbMJPEGURL.Items.AddRange(iSpyServer.Default.RecentMJPGList.Split('|'));

            int selectedCameraIndex = 0;

            for (int i = 0; i < videoDevices.Count; i++)
            {
                if (videoDeviceMoniker == videoDevices[i].MonikerString)
                {
                    selectedCameraIndex = i;
                    break;
                }
            }

            devicesCombo.SelectedIndex = selectedCameraIndex;

            ddlScreen.SuspendLayout();
            foreach (Screen s in Screen.AllScreens)
            {
                ddlScreen.Items.Add(s.DeviceName);
            }
            ddlScreen.Items.Insert(0, LocRM.GetString("PleaseSelect"));
            if (SourceIndex == 4)
            {
                ddlScreen.SelectedIndex = Convert.ToInt32(VideoSourceString) + 1;
            }
            else
                ddlScreen.SelectedIndex = 0;
            ddlScreen.ResumeLayout();

            tcSource.SelectedIndex = SourceIndex;

            if (CameraControl.Camera != null && CameraControl.Camera.VideoSource is VideoCaptureDevice)
            {
                videoDevice = (VideoCaptureDevice)CameraControl.Camera.VideoSource;
                videoInput = videoDevice.CrossbarVideoInput;
                EnumeratedSupportedFrameSizes(videoDevice);
            }

            int deviceCount = 0;
            try
            {
                foreach (var potentialSensor in KinectSensor.KinectSensors)
                {
                    if (potentialSensor.Status == KinectStatus.Connected)
                    {
                        deviceCount++;
                        ddlKinectDevice.Items.Add(potentialSensor.UniqueKinectId);

                        if (NV("type") == "kinect")
                        {
                            if (NV("UniqueKinectId") == potentialSensor.UniqueKinectId)
                            {
                                ddlKinectDevice.SelectedIndex = ddlKinectDevice.Items.Count - 1;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Type error if not installed
                MainForm.LogMessageToFile("Kinect supporting libraries not installed. (" + ex.Message + ")");
            }
            if (deviceCount > 0)
            {
                if (ddlKinectDevice.SelectedIndex == -1)
                    ddlKinectDevice.SelectedIndex = 0;
            }
            else
            {
                pnlKinect.Enabled = false;
            }

            if (NV("type") == "kinect")
            {
                try
                {
                    chkKinectSkeletal.Checked = Convert.ToBoolean(NV("KinectSkeleton"));
                }
                catch { }
            }
        }

        private string NV(string name)
        {
            if (String.IsNullOrEmpty(CameraControl.Camobject.settings.namevaluesettings))
                return "";
            name = name.ToLower().Trim();
            string[] settings = CameraControl.Camobject.settings.namevaluesettings.Split(',');
            foreach (string[] nv in settings.Select(s => s.Split('=')).Where(nv => nv[0].ToLower().Trim() == name))
            {
                return nv[1];
            }
            return "";
        }

        private void RenderResources()
        {
            Text = LocRM.GetString("VideoSource");
            button1.Text = LocRM.GetString("Ok");
            button2.Text = LocRM.GetString("Cancel");
            label1.Text = LocRM.GetString("JpegUrl");
            label10.Text = LocRM.GetString("milliseconds");
            label11.Text = LocRM.GetString("Screen");
            label12.Text = LocRM.GetString("milliseconds");
            label13.Text = LocRM.GetString("FrameInterval");
            label14.Text = LocRM.GetString("ResizeTo");
            label15.Text = LocRM.GetString("Username");
            label16.Text = LocRM.GetString("UserAgent");
            label17.Text = LocRM.GetString("Password");
            label2.Text = LocRM.GetString("MjpegUrl");
            label5.Text = LocRM.GetString("Username");
            label6.Text = LocRM.GetString("Password");
            label7.Text = LocRM.GetString("UserAgent");
            label8.Text = LocRM.GetString("X");
            label9.Text = LocRM.GetString("FrameInterval");
            linkLabel1.Text = LocRM.GetString("HelpMeFindTheRightUrl");
            linkLabel2.Text = LocRM.GetString("HelpMeFindTheRightUrl");
            tabPage1.Text = LocRM.GetString("JpegUrl");
            tabPage2.Text = LocRM.GetString("MjpegUrl");
            tabPage4.Text = LocRM.GetString("LocalDevice");
            tabPage5.Text = LocRM.GetString("Desktop");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            iSpyServer.Default.JPEGURL = cmbJPEGURL.Text.Trim();
            iSpyServer.Default.MJPEGURL = cmbMJPEGURL.Text.Trim();

            if (!iSpyServer.Default.RecentFileList.Contains(iSpyServer.Default.AVIFileName) &&
                iSpyServer.Default.AVIFileName != "")
            {
                iSpyServer.Default.RecentFileList =
                    (iSpyServer.Default.RecentFileList + "|" + iSpyServer.Default.AVIFileName).Trim('|');
            }
            if (!iSpyServer.Default.RecentJPGList.Contains(iSpyServer.Default.JPEGURL) &&
                iSpyServer.Default.JPEGURL != "")
            {
                iSpyServer.Default.RecentJPGList =
                    (iSpyServer.Default.RecentJPGList + "|" + iSpyServer.Default.JPEGURL).Trim('|');
            }
            if (!iSpyServer.Default.RecentMJPGList.Contains(iSpyServer.Default.MJPEGURL) &&
                iSpyServer.Default.MJPEGURL != "")
            {
                iSpyServer.Default.RecentMJPGList =
                    (iSpyServer.Default.RecentMJPGList + "|" + iSpyServer.Default.MJPEGURL).Trim('|');
            }
            SourceIndex = tcSource.SelectedIndex;
            CameraLogin = txtLogin.Text;
            CameraPassword = txtPassword.Text;
            UserAgent = txtUserAgent.Text;
            string nv = "";
            switch (SourceIndex)
            {
                case 0:
                    int _frameinterval = 0;
                    if (!Int32.TryParse(txtFrameInterval.Text, out _frameinterval))
                    {
                        MessageBox.Show(LocRM.GetString("Validate_FrameInterval"));
                        return;
                    }
                    VideoSourceString = cmbJPEGURL.Text.Trim();
                    CameraControl.Camobject.settings.frameinterval = _frameinterval;
                    FriendlyName = VideoSourceString;
                    break;
                case 1:
                    VideoSourceString = cmbMJPEGURL.Text.Trim();
                    FriendlyName = VideoSourceString;
                    CameraLogin = txtLogin2.Text;
                    CameraPassword = txtPassword2.Text;
                    UserAgent = txtUserAgent2.Text;
                    break;
                case 2:
                    MessageBox.Show(LocRM.GetString("Validate_SelectCamera"), LocRM.GetString("Note"));
                    break;
                case 3:
                    if (!devicesCombo.Enabled)
                    {
                        MessageBox.Show(LocRM.GetString("Validate_SelectCamera"), LocRM.GetString("Note"));
                        return;
                    }

                    videoDeviceMoniker = videoDevice.Source;
                    if (videoCapabilitiesDictionary.Count > 0)
                    {
                        VideoCapabilities caps =
                            videoCapabilitiesDictionary[(string) videoResolutionsCombo.SelectedItem];
                        captureSize = caps.FrameSize;
                        FrameRate = caps.FrameRate;
                        captureSize = new Size(captureSize.Width, captureSize.Height);
                    }

                    if ( configureSnapshots )
                    {
                        // set snapshots size
                        if ( snapshotCapabilitiesDictionary.Count != 0 )
                        {
                            VideoCapabilities caps2 = snapshotCapabilitiesDictionary[(string) snapshotResolutionsCombo.SelectedItem];
                            snapshotSize = caps2.FrameSize;
                        }
                    }

                    if ( availableVideoInputs.Length != 0 )
                    {
                        videoInput = availableVideoInputs[videoInputsCombo.SelectedIndex];
                    }

                    VideoSourceString = videoDeviceMoniker;
                    FriendlyName = videoDevice.Source;
                    break;
                case 4:
                    int _frameinterval2 = 0;
                    if (!Int32.TryParse(txtFrameInterval2.Text, out _frameinterval2))
                    {
                        MessageBox.Show(LocRM.GetString("Validate_FrameInterval"));
                        return;
                    }
                    if (ddlScreen.SelectedIndex < 1)
                    {
                        MessageBox.Show(LocRM.GetString("Validate_SelectCamera"), LocRM.GetString("Note"));
                        return;
                    }
                    VideoSourceString = (ddlScreen.SelectedIndex - 1).ToString();
                    FriendlyName = ddlScreen.SelectedItem.ToString();
                    CameraControl.Camobject.settings.frameinterval = _frameinterval2;
                    CameraControl.Camobject.settings.desktopresizewidth = Convert.ToInt32(txtResizeWidth.Value);
                    CameraControl.Camobject.settings.desktopresizeheight = Convert.ToInt32(txtResizeHeight.Value);
                    break;
                case 5:
                    if (!pnlKinect.Enabled)
                    {
                        MessageBox.Show(LocRM.GetString("Validate_SelectCamera"), LocRM.GetString("Note"));
                        return;
                    }
                    nv = "type=kinect";
                    nv += ",UniqueKinectId=" + ddlKinectDevice.SelectedItem;
                    nv += ",KinectSkeleton=" + chkKinectSkeletal.Checked;

                    VideoSourceString = nv;
                    CameraControl.Camobject.settings.namevaluesettings = nv;
                    break;
            }


            if (VideoSourceString.Trim() == "")
            {
                MessageBox.Show(LocRM.GetString("Validate_SelectCamera"), LocRM.GetString("Note"));
                return;
            }

            CameraControl.Camobject.settings.desktopmouse = chkMousePointer.Checked;
            CameraControl.Camobject.encodekey = txtEncodeKey.Text;
            CameraControl.Camobject.settings.resize = !chkNoResize.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void cmbJPEGURL_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void cmbJPEGURL_Click(object sender, EventArgs e)
        {
        }

        private void cmbMJPEGURL_Click(object sender, EventArgs e)
        {
        }

        private void ddlDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void VideoSource_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        

        private void cmbMJPEGURL_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void ddlScreen_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Help.ShowHelp(this, "http://www.ispyconnect.com/sources.aspx");
        }

        private void linkLabel1_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Help.ShowHelp(this, "http://www.ispyconnect.com/sources.aspx");
        }

        // Collect supported video and snapshot sizes
        private void EnumeratedSupportedFrameSizes(VideoCaptureDevice videoDevice)
        {
            this.Cursor = Cursors.WaitCursor;

            videoResolutionsCombo.Items.Clear();
            snapshotResolutionsCombo.Items.Clear();
            videoInputsCombo.Items.Clear();
            snapshotCapabilitiesDictionary.Clear();
            videoCapabilitiesDictionary.Clear();
            try
            {
                // collect video capabilities
                VideoCapabilities[] videoCapabilities = videoDevice.VideoCapabilities;
                int videoResolutionIndex = 0;
                foreach (VideoCapabilities capabilty in videoCapabilities)
                {
                    string item = string.Format(
                        "{0} x {1} ({2} fps)", capabilty.FrameSize.Width, capabilty.FrameSize.Height, capabilty.FrameRate);

                    if (!videoResolutionsCombo.Items.Contains(item))
                    {
                        if (captureSize == capabilty.FrameSize)
                        {
                            videoResolutionIndex = videoResolutionsCombo.Items.Count;
                        }

                        videoResolutionsCombo.Items.Add(item);
                    }

                    if (!videoCapabilitiesDictionary.ContainsKey(item))
                    {
                        videoCapabilitiesDictionary.Add(item, capabilty);
                    }
                }

                if (videoCapabilities.Length == 0)
                {
                    videoResolutionsCombo.Enabled = false;
                    videoResolutionsCombo.Items.Add("Not Supported");
                }
                else
                {
                    videoResolutionsCombo.Enabled = true;
                }

                videoResolutionsCombo.SelectedIndex = videoResolutionIndex;

                if (configureSnapshots)
                {
                    // collect snapshot capabilities
                    VideoCapabilities[] snapshotCapabilities = videoDevice.SnapshotCapabilities;
                    int snapshotResolutionIndex = 0;

                    foreach (VideoCapabilities capabilty in snapshotCapabilities)
                    {
                        string item = string.Format(
                            "{0} x {1}", capabilty.FrameSize.Width, capabilty.FrameSize.Height);

                        if (!snapshotResolutionsCombo.Items.Contains(item))
                        {
                            if (snapshotSize == capabilty.FrameSize)
                            {
                                snapshotResolutionIndex = snapshotResolutionsCombo.Items.Count;
                            }

                            snapshotResolutionsCombo.Items.Add(item);
                            snapshotCapabilitiesDictionary.Add(item, capabilty);
                        }
                    }

                    if (snapshotCapabilities.Length == 0)
                    {
                        snapshotResolutionsCombo.Enabled = false;
                        snapshotResolutionsCombo.Items.Add("Not Supported");
                    }
                    else
                    {
                        snapshotResolutionsCombo.Enabled = true;
                    }

                    snapshotResolutionsCombo.SelectedIndex = snapshotResolutionIndex;
                }

                // get video inputs
                availableVideoInputs = videoDevice.AvailableCrossbarVideoInputs;
                int videoInputIndex = 0;
                foreach (VideoInput input in availableVideoInputs)
                {
                    string item = string.Format("{0}: {1}", input.Index, input.Type);

                    if ((input.Index == videoInput.Index) && (input.Type == videoInput.Type))
                    {
                        videoInputIndex = videoInputsCombo.Items.Count;
                    }

                    videoInputsCombo.Items.Add(item);
                }

                if (availableVideoInputs.Length == 0)
                {
                    videoInputsCombo.Items.Add("Not Supported");
                    videoInputsCombo.Enabled = false;
                }
                else
                {
                    videoInputsCombo.Enabled = true;
                }

                videoInputsCombo.SelectedIndex = videoInputIndex;
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        #region Nested type: ListItem

        private struct ListItem
        {
            internal string Name;
            internal string Value;

            public ListItem(string Name, string Value)
            {
                this.Name = Name;
                this.Value = Value;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        #endregion

        private void devicesCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (videoDevices.Count != 0)
            {
                videoDevice = new VideoCaptureDevice(videoDevices[devicesCombo.SelectedIndex].MonikerString);
                EnumeratedSupportedFrameSizes(videoDevice);
            }
        }

        private void chkMousePointer_CheckedChanged(object sender, EventArgs e)
        {
            if (CameraControl != null && CameraControl.Camera != null && CameraControl.Camera.VideoSource is DesktopStream)
            {
                ((DesktopStream)CameraControl.Camera.VideoSource).MousePointer = chkMousePointer.Checked;
            }
        }

        private void chkNoResize_CheckedChanged(object sender, EventArgs e)
        {
            txtResizeHeight.Enabled = txtResizeWidth.Enabled = !chkNoResize.Checked;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            int j = ddlScreen.SelectedIndex - 1;
            if (j < 0) j = 0;
            var screen = Screen.AllScreens[j];

            Rectangle area = Rectangle.Empty;
            if (!String.IsNullOrEmpty(CameraControl.Camobject.settings.desktoparea))
            {
                var i = Array.ConvertAll(CameraControl.Camobject.settings.desktoparea.Split(','), int.Parse);
                area = new Rectangle(i[0], i[1], i[2], i[3]);
            }

            var screenArea = new ScreenArea(screen, area);

            screenArea.ShowDialog();
            CameraControl.Camobject.settings.desktoparea = screenArea.Area.Left + "," + screenArea.Area.Top + "," + screenArea.Area.Width + "," + screenArea.Area.Height;
            screenArea.Dispose();
        }
    }
}