using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using AForge.Video.DirectShow;
using AForge.Video.Ximea;
using Declarations;
using Declarations.Events;
using Declarations.Media;
using Declarations.Players;
using Implementation;
using iSpyApplication.Controls;
using iSpyApplication.Video;
using Microsoft.Kinect;
using FilterInfo = AForge.Video.DirectShow.FilterInfo;

namespace iSpyApplication
{
    public partial class VideoSource : Form
    {
        private IVideoPlayer _player;
        public CameraWindow CameraControl;
        public string CameraLogin;
        public string CameraPassword;
        public string FriendlyName = "";
        public int SourceIndex;
        public int VideoInputIndex = -1;
        public string UserAgent;
        public string VideoSourceString;
        public bool ForceBasic;
        public bool StartWizard = false;
        //private IVideoPlayer _player;
        private bool _loaded;


        // collection of available video devices
        private readonly FilterInfoCollection _videoDevices;
        // selected video device
        private VideoCaptureDevice _videoCaptureDevice;

        // supported capabilities of video and snapshots
        private readonly Dictionary<string, VideoCapabilities> _videoCapabilitiesDictionary = new Dictionary<string, VideoCapabilities>();
        private readonly Dictionary<string, VideoCapabilities> _snapshotCapabilitiesDictionary = new Dictionary<string, VideoCapabilities>();

        // available video inputs
        private VideoInput[] _availableVideoInputs;

        // flag telling if user wants to configure snapshots as well
        private bool _configureSnapshots;

        public bool ConfigureSnapshots
        {
            get { return _configureSnapshots; }
            set
            {
                _configureSnapshots = value;
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
            get { return _videoCaptureDevice; }
        }

        private string _videoDeviceMoniker = string.Empty;
        private Size _captureSize = new Size(0, 0);
        private Size _snapshotSize = new Size(0, 0);
        public int FrameRate = 0;
        private VideoInput _videoInput = VideoInput.Default;

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
            get { return _videoDeviceMoniker; }
            set { _videoDeviceMoniker = value; }
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
            get { return _captureSize; }
            set { _captureSize = value; }
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
            get { return _snapshotSize; }
            set { _snapshotSize = value; }
        }

        public VideoSource()
        {
            InitializeComponent();
            RenderResources();

            // show device list
            try
            {
                // enumerate video devices
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (_videoDevices.Count == 0)
                    throw new ApplicationException();

                // add all devices to combo
                foreach (FilterInfo device in _videoDevices)
                {
                    devicesCombo.Items.Add(device.Name);
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
                devicesCombo.Items.Add(LocRm.GetString("NoCaptureDevices"));
                devicesCombo.Enabled = false;
                //okButton.Enabled = false;
            }
        }

        private void VideoSourceLoad(object sender, EventArgs e)
        {
            UISync.Init(this);

            //tcSource.Controls.RemoveAt(5);
            tlpVLC.Enabled = VlcHelper.VlcInstalled;
            linkLabel3.Visible = lblInstallVLC.Visible = !tlpVLC.Enabled;

            cmbJPEGURL.Text = MainForm.Conf.JPEGURL;
            cmbMJPEGURL.Text = MainForm.Conf.MJPEGURL;
            cmbVLCURL.Text = MainForm.Conf.VLCURL;
            cmbFile.Text = MainForm.Conf.AVIFileName;

            txtLogin.Text = txtLogin2.Text = CameraControl.Camobject.settings.login;
            txtPassword.Text = txtPassword2.Text = CameraControl.Camobject.settings.password;
            txtUserAgent.Text = txtUserAgent2.Text = CameraControl.Camobject.settings.useragent;
            txtResizeWidth.Value = CameraControl.Camobject.settings.desktopresizewidth;
            txtResizeHeight.Value = CameraControl.Camobject.settings.desktopresizeheight;
            chkNoResize.Checked = !CameraControl.Camobject.settings.resize;
            chkUseHttp10.Checked = chkUseHttp102.Checked = CameraControl.Camobject.settings.usehttp10;

            VideoSourceString = CameraControl.Camobject.settings.videosourcestring;
            
            SourceIndex = CameraControl.Camobject.settings.sourceindex;
            if (SourceIndex == 3)
            {
                VideoDeviceMoniker = VideoSourceString;
                string[] wh= CameraControl.Camobject.resolution.Split('x');
                CaptureSize = new Size(Convert.ToInt32(wh[0]), Convert.ToInt32(wh[1]));
            }
            txtFrameInterval.Text = txtFrameInterval2.Text = CameraControl.Camobject.settings.frameinterval.ToString(CultureInfo.InvariantCulture);
            chkForceBasic1.Checked = chkForceBasic.Checked = CameraControl.Camobject.settings.forcebasic;
            txtVLCArgs.Text = CameraControl.Camobject.settings.vlcargs.Replace("\r\n","\n").Replace("\n\n","\n").Replace("\n", Environment.NewLine);
            txtReconnect.Value = CameraControl.Camobject.settings.reconnectinterval;
            txtCookies.Text = txtCookies1.Text = CameraControl.Camobject.settings.cookies;

            ddlCustomProvider.SelectedIndex = 0;
            switch (SourceIndex)
            {
                case 0:
                    cmbJPEGURL.Text = VideoSourceString;
                    txtFrameInterval.Text = CameraControl.Camobject.settings.frameinterval.ToString(CultureInfo.InvariantCulture);
                    break;
                case 1:
                    cmbMJPEGURL.Text = VideoSourceString;
                    break;
                case 2:
                    cmbFile.Text = VideoSourceString;
                    break;
                case 5:
                    cmbVLCURL.Text = VideoSourceString;
                    break;
                case 8:
                    txtCustomURL.Text = VideoSourceString;
                    switch (NV("custom"))
                    {
                        default:
                            ddlCustomProvider.SelectedIndex = 0;
                            break;
                    }
                    break;
            }

            if (!String.IsNullOrEmpty(CameraControl.Camobject.decodekey))
                txtDecodeKey.Text = CameraControl.Camobject.decodekey;

            chkMousePointer.Checked = CameraControl.Camobject.settings.desktopmouse;
            numBorderTimeout.Value = CameraControl.Camobject.settings.bordertimeout;

            cmbJPEGURL.Items.AddRange(MainForm.Conf.RecentJPGList.Split('|'));
            cmbMJPEGURL.Items.AddRange(MainForm.Conf.RecentMJPGList.Split('|'));
            cmbFile.Items.AddRange(MainForm.Conf.RecentFileList.Split('|'));
            cmbVLCURL.Items.AddRange(MainForm.Conf.RecentVLCList.Split('|'));

            chkCalibrate.Checked = Convert.ToBoolean(CameraControl.Camobject.settings.calibrateonreconnect);
           
            int selectedCameraIndex = 0;

            for (int i = 0; i < _videoDevices.Count; i++)
            {
                if (_videoDeviceMoniker == _videoDevices[i].MonikerString)
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
            ddlScreen.Items.Insert(0, LocRm.GetString("PleaseSelect"));
            if (SourceIndex == 4)
            {
                int screenIndex = Convert.ToInt32(VideoSourceString) + 1;
                ddlScreen.SelectedIndex = ddlScreen.Items.Count>screenIndex ? screenIndex : 1;
            }
            else
                ddlScreen.SelectedIndex = 0;
            ddlScreen.ResumeLayout();

            tcSource.SelectedIndex = SourceIndex;


            if (CameraControl != null && CameraControl.Camera != null && CameraControl.Camera.VideoSource is VideoCaptureDevice)
            {
                _videoCaptureDevice = (VideoCaptureDevice)CameraControl.Camera.VideoSource;
                _videoInput = _videoCaptureDevice.CrossbarVideoInput;
                EnumeratedSupportedFrameSizes();
            }


            //ximea

            int deviceCount = 0;

            try
            {
                deviceCount = XimeaCamera.CamerasCount;
            }
            catch(Exception)
            {
                //Ximea DLL not installed
                //MainForm.LogMessageToFile("This is not a XIMEA device");
            }

            pnlXimea.Enabled = deviceCount>0;

            if (pnlXimea.Enabled)
            {
                for (int i = 0; i < deviceCount; i++)
                {
                    ddlXimeaDevice.Items.Add("Device " + i);
                }
                if (NV("type")=="ximea")
                {
                    int deviceIndex = Convert.ToInt32(NV("device"));
                    ddlXimeaDevice.SelectedIndex = ddlXimeaDevice.Items.Count > deviceIndex?deviceIndex:0;
                    numXimeaWidth.Text = NV("width");
                    numXimeaHeight.Text = NV("height");
                    numXimeaOffsetX.Value = Convert.ToInt32(NV("x"));
                    numXimeaOffestY.Value = Convert.ToInt32(NV("y"));

                    decimal gain;
                    decimal.TryParse(NV("gain"), out gain);
                    numXimeaGain.Value =  gain;

                    decimal exp;
                    decimal.TryParse(NV("exposure"), out exp);
                    if (exp == 0)
                        exp = 100;
                    numXimeaExposure.Value = exp;

                    combo_dwnsmpl.SelectedItem  = NV("downsampling");
                }
            }
            else
            {
                ddlXimeaDevice.Items.Add(LocRm.GetString("NoDevicesFound"));
                ddlXimeaDevice.SelectedIndex = 0;
            }

            deviceCount = 0;
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
                MainForm.LogMessageToFile("Kinect supporting libraries not installed. ("+ex.Message+")" );
            }
            if (deviceCount>0)
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
                    chkTripWires.Checked = Convert.ToBoolean(NV("TripWires"));
                }
                catch {}
            }          

            _loaded = true;
            if (StartWizard) Wizard();

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
            Text = LocRm.GetString("VideoSource");
            button1.Text = LocRm.GetString("Ok");
            button2.Text = LocRm.GetString("Cancel");
            button3.Text = LocRm.GetString("chars_3014702301470230147");
            label1.Text = LocRm.GetString("JpegUrl");
            label10.Text = LocRm.GetString("milliseconds");
            label11.Text = LocRm.GetString("Screen");
            label12.Text = LocRm.GetString("milliseconds");
            label13.Text = LocRm.GetString("FrameInterval");
            label14.Text = LocRm.GetString("ResizeTo");
            label15.Text = LocRm.GetString("Username");
            label16.Text = LocRm.GetString("UserAgent");
            label17.Text = LocRm.GetString("Password");
            label2.Text = LocRm.GetString("MjpegUrl");
            //label3.Text = LocRm.GetString("OpenFile");
            //label4.Text = LocRm.GetString("SelectLocalDevice");
            label5.Text = label15.Text = LocRm.GetString("Username");
            label6.Text = label17.Text = LocRm.GetString("Password");
            label7.Text = LocRm.GetString("UserAgent");
            label8.Text = LocRm.GetString("X");
            label9.Text = LocRm.GetString("FrameInterval");
            linkLabel1.Text = LocRm.GetString("HelpMeFindTheRightUrl");
            linkLabel2.Text = LocRm.GetString("HelpMeFindTheRightUrl");
            tabPage1.Text = LocRm.GetString("JpegUrl");
            tabPage2.Text = LocRm.GetString("MjpegUrl");
            //tabPage3.Text = LocRm.GetString("VideoFile");
            tabPage4.Text = LocRm.GetString("LocalDevice");
            tabPage5.Text = LocRm.GetString("Desktop");
            tabPage6.Text = LocRm.GetString("VLCPlugin");
            label32.Text = label36.Text = LocRm.GetString("device");
            label31.Text = LocRm.GetString("Name");
            label31.Text = LocRm.GetString("Name");
            label30.Text = LocRm.GetString("serial");
            label29.Text = LocRm.GetString("type");
            label26.Text = LocRm.GetString("Width");
            label25.Text = LocRm.GetString("Height");
            label24.Text = LocRm.GetString("offsetx");
            label23.Text = LocRm.GetString("offsety");
            label27.Text = LocRm.GetString("gain");
            label28.Text = LocRm.GetString("exposure");
            button4.Text = LocRm.GetString("IPCameraWithWizard");

            label39.Text = LocRm.GetString("VideoDevice");
            label38.Text = LocRm.GetString("VideoResolution");
            label37.Text = LocRm.GetString("VideoInput");
            snapshotsLabel.Text = LocRm.GetString("SnapshotsResolution");

            label18.Text = LocRm.GetString("Arguments");
            lblInstallVLC.Text = LocRm.GetString("VLCConnectInfo");
            linkLabel3.Text = LocRm.GetString("DownloadVLC");
            linkLabel4.Text = LocRm.GetString("UseiSpyServerText");
            label48.Text = LocRm.GetString("Seconds");
            label43.Text = LocRm.GetString("ReconnectEvery");
            chkForceBasic.Text = chkForceBasic1.Text = LocRm.GetString("ForceBasic");

            llblHelp.Text = LocRm.GetString("help");
            
            
            LocRm.SetString(label35,"Cookies");
            LocRm.SetString(label44, "Cookies");
            LocRm.SetString(label20, "DecodeKey");
            LocRm.SetString(label22, "OptionaliSpyServer");
            LocRm.SetString(chkNoResize, "NoResize");
            LocRm.SetString(label3, "FileURL");
            LocRm.SetString(label4, "FFMPEGHelp");
            LocRm.SetString(label42,"DesktopHelp");
            LocRm.SetString(chkMousePointer, "MousePointer");
            LocRm.SetString(btnGetStreamSize, "Test");
            LocRm.SetString(linkLabel5, "Help");
            LocRm.SetString(label18, "Arguments");
            LocRm.SetString(lblInstallVLC, "VLCHelp");
            LocRm.SetString(linkLabel3, "DownloadVLC");
            LocRm.SetString(chkKinectSkeletal, "ShowSkeleton");
            LocRm.SetString(chkTripWires, "ShowTripWires");
            LocRm.SetString(label34, "Provider");
            LocRm.SetString(label45, "BorderTimeout");
            LocRm.SetString(chkCalibrate, "CalibrateOnReconnect");
  


        }

        private void Button1Click(object sender, EventArgs e)
        {
            SetupVideoSource();
        }

        private void SetupVideoSource()
        {
            StopPlayer();
            MainForm.Conf.JPEGURL = cmbJPEGURL.Text.Trim();
            MainForm.Conf.MJPEGURL = cmbMJPEGURL.Text.Trim();
            MainForm.Conf.AVIFileName = cmbFile.Text.Trim();
            MainForm.Conf.VLCURL = cmbVLCURL.Text.Trim();

            var iReconnect = (int)txtReconnect.Value;
            if (iReconnect < 30 && iReconnect != 0)
            {
                MessageBox.Show(LocRm.GetString("Validate_ReconnectInterval"), LocRm.GetString("Note"));
                return;
            } 
            
            string nv;
            SourceIndex = tcSource.SelectedIndex;
            CameraLogin = txtLogin.Text;
            CameraPassword = txtPassword.Text;
            UserAgent = txtUserAgent.Text;
            
            string url;
            switch (SourceIndex)
            {
                case 0:
                    int frameinterval;
                    if (!Int32.TryParse(txtFrameInterval.Text, out frameinterval))
                    {
                        MessageBox.Show(LocRm.GetString("Validate_FrameInterval"));
                        return;
                    }
                    url = cmbJPEGURL.Text.Trim();
                    if (url == String.Empty)
                    {
                        MessageBox.Show(LocRm.GetString("Validate_SelectCamera"), LocRm.GetString("Note"));
                        return;
                    }
                    VideoSourceString = url;
                    CameraControl.Camobject.settings.frameinterval = frameinterval;
                    CameraControl.Camobject.settings.usehttp10 = chkUseHttp10.Checked;
                    FriendlyName = VideoSourceString;
                    ForceBasic = chkForceBasic.Checked;
                    CameraControl.Camobject.settings.cookies = txtCookies1.Text;
                    break;
                case 1:
                    url = cmbMJPEGURL.Text.Trim();
                    if (url == String.Empty)
                    {
                        MessageBox.Show(LocRm.GetString("Validate_SelectCamera"), LocRm.GetString("Note"));
                        return;
                    }
                    VideoSourceString = url;
                    FriendlyName = VideoSourceString;
                    CameraLogin = txtLogin2.Text;
                    CameraPassword = txtPassword2.Text;
                    UserAgent = txtUserAgent2.Text;
                    ForceBasic = chkForceBasic1.Checked;
                    CameraControl.Camobject.settings.usehttp10 = chkUseHttp102.Checked;
                    CameraControl.Camobject.decodekey = txtDecodeKey.Text;
                    CameraControl.Camobject.settings.cookies = txtCookies.Text;
                    break;
                case 2:
                    url = cmbFile.Text.Trim();
                    if (url == String.Empty)
                    {
                        MessageBox.Show(LocRm.GetString("Validate_SelectCamera"), LocRm.GetString("Note"));
                        return;
                    }
                    VideoSourceString = url;
                    FriendlyName = VideoSourceString;
                    break;
                case 3:
                    if (!devicesCombo.Enabled)
                    {
                        MessageBox.Show(LocRm.GetString("Validate_SelectCamera"), LocRm.GetString("Note"));
                        return;
                    }

                    _videoDeviceMoniker = _videoCaptureDevice.Source;
                    if (_videoCapabilitiesDictionary.Count != 0)
                    {
                        VideoCapabilities caps =
                            _videoCapabilitiesDictionary[(string) videoResolutionsCombo.SelectedItem];
                        _captureSize = caps.FrameSize;
                        FrameRate = caps.AverageFrameRate;
                    }

                    if ( _configureSnapshots )
                    {
                        // set snapshots size
                        if ( _snapshotCapabilitiesDictionary.Count != 0 )
                        {
                            VideoCapabilities caps = _snapshotCapabilitiesDictionary[(string) snapshotResolutionsCombo.SelectedItem];
                            _snapshotSize = caps.FrameSize;
                        }
                    }

                    VideoInputIndex = -1;
                    if (videoInputsCombo.SelectedIndex > 0)
                    {
                        if (_availableVideoInputs.Length != 0)
                        {
                            VideoInputIndex = _availableVideoInputs[videoInputsCombo.SelectedIndex-1].Index;
                        }
                    }
                    

                    VideoSourceString = _videoDeviceMoniker;
                    FriendlyName = _videoCaptureDevice.Source;
                    break;
                case 4:
                    int frameinterval2;
                    if (!Int32.TryParse(txtFrameInterval2.Text, out frameinterval2))
                    {
                        MessageBox.Show(LocRm.GetString("Validate_FrameInterval"));
                        return;
                    }
                    if (ddlScreen.SelectedIndex < 1)
                    {
                        MessageBox.Show(LocRm.GetString("Validate_SelectCamera"), LocRm.GetString("Note"));
                        return;
                    }
                    VideoSourceString = (ddlScreen.SelectedIndex - 1).ToString(CultureInfo.InvariantCulture);
                    FriendlyName = ddlScreen.SelectedItem.ToString();
                    CameraControl.Camobject.settings.frameinterval = frameinterval2;
                    CameraControl.Camobject.settings.desktopmouse = chkMousePointer.Checked;

                break;
                case 5:
                    if (!VlcHelper.VlcInstalled)
                    {
                        MessageBox.Show(LocRm.GetString("DownloadVLC") + " v" + VlcHelper.VMin+" or greater", LocRm.GetString("Note"));
                        return;
                    }
                    url = cmbVLCURL.Text.Trim();
                    if (url == String.Empty)
                    {
                        MessageBox.Show(LocRm.GetString("Validate_SelectCamera"), LocRm.GetString("Note"));
                        return;
                    }
                    if (iReconnect==0)
                    {
                        if (MessageBox.Show("VLC is unable to detect a lost connection. Highly recommend you set a reconnect interval for VLC sources. Do you want to continue?", "Warning", MessageBoxButtons.YesNo)==DialogResult.No)
                            return;
                    }
                    VideoSourceString = url;
                    FriendlyName = VideoSourceString;
                    CameraControl.Camobject.settings.vlcargs = txtVLCArgs.Text.Trim();
                    break;
                case 6:
                    if (!pnlXimea.Enabled)
                    {
                        MessageBox.Show(LocRm.GetString("Validate_SelectCamera"), LocRm.GetString("Note"));
                        return;
                    }
                    nv = "type=ximea";
                    nv += ",device=" + ddlXimeaDevice.SelectedIndex;
                    nv += ",width=" + numXimeaWidth.Text;
                    nv += ",height=" + numXimeaHeight.Text;
                    nv += ",x=" + (int)numXimeaOffsetX.Value;
                    nv += ",y=" + (int)numXimeaOffestY.Value;
                    nv += ",gain=" +
                          String.Format(CultureInfo.InvariantCulture, "{0:0.000}",
                                        numXimeaGain.Value);
                    nv += ",exposure=" + String.Format(CultureInfo.InvariantCulture, "{0:0.000}",
                                        numXimeaExposure.Value);
                    nv += ",downsampling=" + combo_dwnsmpl.SelectedItem;
                    VideoSourceString = nv;

                    CameraControl.Camobject.settings.namevaluesettings = nv;
                    break;
                case 7:
                    if (!pnlKinect.Enabled)
                    {
                        MessageBox.Show(LocRm.GetString("Validate_SelectCamera"), LocRm.GetString("Note"));
                        return;
                    }
                    nv = "type=kinect";
                    nv += ",UniqueKinectId=" + ddlKinectDevice.SelectedItem;
                    nv += ",KinectSkeleton=" + chkKinectSkeletal.Checked;
                    nv += ",TripWires=" + chkTripWires.Checked;
                    
                    VideoSourceString = nv;
                    CameraControl.Camobject.settings.namevaluesettings = nv;
                    break;
                case 8:
                    VideoSourceString = txtCustomURL.Text;
                    nv = "custom=" + ddlCustomProvider.SelectedItem;
                    CameraControl.Camobject.settings.namevaluesettings = nv;
                    CameraControl.Camobject.alerts.mode = "KinectPlugin";
                    CameraControl.Camobject.detector.recordonalert = true;
                    CameraControl.Camobject.alerts.minimuminterval = 10;
                    CameraControl.Camobject.detector.recordondetect = false;
                    CameraControl.Camobject.detector.type = "None";
                    CameraControl.Camobject.settings.audiomodel = "NetworkKinect";
                    try
                    {
                        var uri = new Uri(VideoSourceString);

                        if (!String.IsNullOrEmpty(uri.DnsSafeHost))
                        {
                            CameraControl.Camobject.settings.audioip = uri.DnsSafeHost;
                            CameraControl.Camobject.settings.audioport = uri.Port;
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Invalid URL", "Error");
                        return;
                    }
                    
                    CameraControl.Camobject.settings.audiousername = "";
                    CameraControl.Camobject.settings.audiopassword = "";
                    CameraControl.Camobject.settings.bordertimeout = Convert.ToInt32(numBorderTimeout.Value);
                    break;
            }

            

            if (String.IsNullOrEmpty(VideoSourceString))
            {
                MessageBox.Show(LocRm.GetString("Validate_SelectCamera"), LocRm.GetString("Note"));
                return;
            }

            if (!MainForm.Conf.RecentFileList.Contains(MainForm.Conf.AVIFileName) &&
                MainForm.Conf.AVIFileName != "")
            {
                MainForm.Conf.RecentFileList =
                    (MainForm.Conf.RecentFileList + "|" + MainForm.Conf.AVIFileName).Trim('|');
            }
            if (!MainForm.Conf.RecentJPGList.Contains(MainForm.Conf.JPEGURL) &&
                MainForm.Conf.JPEGURL != "")
            {
                MainForm.Conf.RecentJPGList =
                    (MainForm.Conf.RecentJPGList + "|" + MainForm.Conf.JPEGURL).Trim('|');
            }
            if (!MainForm.Conf.RecentMJPGList.Contains(MainForm.Conf.MJPEGURL) &&
                MainForm.Conf.MJPEGURL != "")
            {
                MainForm.Conf.RecentMJPGList =
                    (MainForm.Conf.RecentMJPGList + "|" + MainForm.Conf.MJPEGURL).Trim('|');
            }
            if (!MainForm.Conf.RecentVLCList.Contains(MainForm.Conf.VLCURL) &&
                MainForm.Conf.VLCURL != "")
            {
                MainForm.Conf.RecentVLCList =
                    (MainForm.Conf.RecentVLCList + "|" + MainForm.Conf.VLCURL).Trim('|');
            }
            CameraControl.Camobject.settings.reconnectinterval = iReconnect;
            CameraControl.Camobject.settings.calibrateonreconnect = chkCalibrate.Checked;
            
            //wh must be even for stride calculations
            int w = Convert.ToInt32(txtResizeWidth.Value);
            if (w % 2 != 0)
                w++;
            CameraControl.Camobject.settings.desktopresizewidth = w;

            int h = Convert.ToInt32(txtResizeHeight.Value);
            if (h % 2 != 0)
                h++;

            CameraControl.Camobject.settings.desktopresizeheight = h;
            CameraControl.Camobject.settings.resize = !chkNoResize.Checked;
            

            DialogResult = DialogResult.OK;
            Close();
        }

        private void Button3Click(object sender, EventArgs e)
        {
            ofd.Filter = "Video Files|*.*";
            ofd.InitialDirectory = MainForm.Conf.MediaDirectory;
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                cmbFile.Text = ofd.FileName;
            }
        }

        private void Button2Click(object sender, EventArgs e)
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

        private void cmbFile_TextChanged(object sender, EventArgs e)
        {
        }


        private void cmbFile_Click(object sender, EventArgs e)
        {
        }


        private void VideoSource_FormClosing(object sender, FormClosingEventArgs e)
        {
        }


        private void cmbFile_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void cmbMJPEGURL_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void ddlScreen_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void LinkLabel2LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl( MainForm.Website+"/sources.aspx");
        }

        private void LinkLabel1LinkClicked1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl( MainForm.Website+"/sources.aspx");
        }

        private void LinkLabel3LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl( "http://www.videolan.org/vlc/download-windows.html");
        }

        private void pnlVLC_Paint(object sender, PaintEventArgs e)
        {
        }

        private void lblInstallVLC_Click(object sender, EventArgs e)
        {
        }

        private void Button4Click(object sender, EventArgs e)
        {
            string url = cmbVLCURL.Text.Trim();
            if (url == String.Empty)
            {
                MessageBox.Show(LocRm.GetString("Validate_SelectCamera"), LocRm.GetString("Note"));
                return;
            }

            btnGetStreamSize.Enabled = false;
            StopPlayer();
            try
            {
                var factory = new MediaPlayerFactory(false);
                _player = factory.CreatePlayer<IVideoPlayer>();
                var media = factory.CreateMedia<IMedia>(url, txtVLCArgs.Text);
                _player.Open(media);
                _player.Mute = true;
                _player.Events.PlayerPositionChanged += EventsPlayerPositionChanged;
                _player.Events.PlayerEncounteredError += EventsPlayerEncounteredError;
                _player.CustomRenderer.SetCallback(bmp => bmp.Dispose());
                _player.CustomRenderer.SetFormat(new BitmapFormat(100, 100, ChromaType.RV24));

                _player.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, LocRm.GetString("Error"));
            }
        }

        private void EventsPlayerEncounteredError(object sender, EventArgs e)
        {
            _player.Events.PlayerPositionChanged -= EventsPlayerPositionChanged;
            _player.Events.PlayerEncounteredError -= EventsPlayerEncounteredError;
            UISync.Execute(StopPlayer);
            MessageBox.Show("VLC Error", LocRm.GetString("Error"));
            UISync.Execute(() => btnGetStreamSize.Enabled = true);
        }

        private void SetVideoSize(Size size)
        {
            txtResizeWidth.Value = size.Width;
            txtResizeHeight.Value = size.Height;
        }

        private void StopPlayer()
        {
            if (_player != null)
            {
                _player.Stop();
                _player.Dispose();
                _player = null;
            }
            
        }

        private void EventsPlayerPositionChanged(object sender, MediaPlayerPositionChanged e)
        {
            Size size = _player.GetVideoSize(0);
            if (!size.IsEmpty)
            {
                _player.Events.PlayerPositionChanged -= EventsPlayerPositionChanged;
                _player.Events.PlayerEncounteredError -= EventsPlayerEncounteredError;
                UISync.Execute(() => SetVideoSize(size));
                UISync.Execute(StopPlayer);
                MessageBox.Show("OK");
                UISync.Execute(() => btnGetStreamSize.Enabled = true);
            }
        }

        #region Nested type: UISync

        private class UISync
        {
            private static ISynchronizeInvoke _sync;

            public static void Init(ISynchronizeInvoke sync)
            {
                _sync = sync;
            }

            public static void Execute(Action action)
            {
                try
                {
                    _sync.BeginInvoke(action, null);
                }
                catch
                {
                }
            }
        }

        #endregion

        private void ddlXimeaDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            ConnectXimea();
        }


        private void ConnectXimea()
        {
            // close whatever is open now
            if (!pnlXimea.Enabled) return;
            try
            {
                if (CameraControl.XimeaSource==null)
                    CameraControl.XimeaSource = new XimeaVideoSource( ddlXimeaDevice.SelectedIndex );
                    
                // start the camera
                if (!CameraControl.XimeaSource.IsRunning)
                    CameraControl.XimeaSource.Start();

                // get some parameters
                nameBox.Text = CameraControl.XimeaSource.GetParamString(CameraParameter.DeviceName);
                snBox.Text = CameraControl.XimeaSource.GetParamString(CameraParameter.DeviceSerialNumber);
                typeBox.Text = CameraControl.XimeaSource.GetParamString(CameraParameter.DeviceType);

                // width
                numXimeaWidth.Text = CameraControl.XimeaSource.GetParamInt(CameraParameter.Width ).ToString(CultureInfo.InvariantCulture);

                // height
                numXimeaHeight.Text = CameraControl.XimeaSource.GetParamInt(CameraParameter.Height).ToString(CultureInfo.InvariantCulture);

                // exposure
                numXimeaExposure.Minimum = (decimal)CameraControl.XimeaSource.GetParamFloat(CameraParameter.ExposureMin) / 1000;
                numXimeaExposure.Maximum = (decimal)CameraControl.XimeaSource.GetParamFloat(CameraParameter.ExposureMax) / 1000;
                numXimeaExposure.Value = new Decimal(CameraControl.XimeaSource.GetParamFloat(CameraParameter.Exposure)) / 1000;
                if (numXimeaExposure.Value == 0)
                    numXimeaExposure.Value = 100;

                // gain
                numXimeaGain.Minimum = new Decimal(CameraControl.XimeaSource.GetParamFloat(CameraParameter.GainMin));
                numXimeaGain.Maximum = new Decimal(CameraControl.XimeaSource.GetParamFloat(CameraParameter.GainMax));
                numXimeaGain.Value = new Decimal(CameraControl.XimeaSource.GetParamFloat(CameraParameter.Gain));

                int maxDwnsmpl = CameraControl.XimeaSource.GetParamInt(CameraParameter.DownsamplingMax);

                switch (maxDwnsmpl)
                {
                    case 8:
                        combo_dwnsmpl.Items.Add("1");
                        combo_dwnsmpl.Items.Add("2");
                        combo_dwnsmpl.Items.Add("4");
                        combo_dwnsmpl.Items.Add("8");
                        break;
                    case 6:
                        combo_dwnsmpl.Items.Add("1");
                        combo_dwnsmpl.Items.Add("2");
                        combo_dwnsmpl.Items.Add("4");
                        combo_dwnsmpl.Items.Add("6");
                        break;
                    case 4:
                        combo_dwnsmpl.Items.Add("1");
                        combo_dwnsmpl.Items.Add("2");
                        combo_dwnsmpl.Items.Add("4");
                        break;
                    case 2:
                        combo_dwnsmpl.Items.Add("1");
                        combo_dwnsmpl.Items.Add("2");
                        break;
                    default:
                        combo_dwnsmpl.Items.Add("1");
                        break;
                }
                combo_dwnsmpl.SelectedIndex = combo_dwnsmpl.Items.Count-1;
            }
            catch ( Exception ex )
            {
                MainForm.LogExceptionToFile(ex);
                MessageBox.Show( ex.Message, LocRm.GetString("Error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error );
            }

        }

        private void devicesCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!pnlKinect.Enabled) return;

        }

        private void offsetYUpDown_ValueChanged(object sender, EventArgs e)
        {

        }

        private void llblHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = MainForm.Website+"/userguide-connecting-cameras.aspx";
            switch (tcSource.SelectedIndex)
            {
                case 0:
                    url = MainForm.Website+"/userguide-connecting-cameras.aspx#4";
                    break;
                case 1:
                    url = MainForm.Website+"/userguide-connecting-cameras.aspx#4";
                    break;
                case 2:
                    url = MainForm.Website+"/userguide-connecting-cameras.aspx";
                    break;
                case 3:
                    url = MainForm.Website+"/userguide-connecting-cameras.aspx#2";
                    break;
                case 4:
                    url = MainForm.Website+"/userguide-connecting-cameras.aspx#6";
                    break;
                case 5:
                    url = MainForm.Website+"/userguide-connecting-cameras.aspx#5";
                    break;
                case 6:
                    url = MainForm.Website+"/userguide-connecting-cameras.aspx#7";
                    break;
                case 7:
                    url = MainForm.Website+"/userguide-connecting-cameras.aspx#8";
                    break;
            }
            MainForm.OpenUrl( url);
        }

        private void combo_dwnsmpl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_loaded)
                return;
            if (combo_dwnsmpl.SelectedIndex > -1 && CameraControl.XimeaSource!=null)
            {
                CameraControl.XimeaSource.SetParam(CameraParameter.Downsampling,
                                                   Convert.ToInt32(
                                                       combo_dwnsmpl.Items[combo_dwnsmpl.SelectedIndex].ToString()));

                //update width and height info
                numXimeaWidth.Text = CameraControl.XimeaSource.GetParamInt(CameraParameter.Width).ToString();
                numXimeaHeight.Text = CameraControl.XimeaSource.GetParamInt(CameraParameter.Height).ToString();

                //reset gain slider
                numXimeaGain.Minimum = new Decimal(CameraControl.XimeaSource.GetParamFloat(CameraParameter.GainMin));
                numXimeaGain.Maximum = new Decimal(CameraControl.XimeaSource.GetParamFloat(CameraParameter.GainMax));
                numXimeaGain.Value = new Decimal(CameraControl.XimeaSource.GetParamFloat(CameraParameter.Gain));
            }
        }

        private void numXimeaExposure_ValueChanged(object sender, EventArgs e)
        {

        }

        private void numXimeaGain_ValueChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            Wizard();
        }

        private void Wizard()
        {
            var fc = new FindCameras();
            if (fc.ShowDialog(this) == DialogResult.OK)
            {
                tcSource.SelectedIndex = fc.VideoSourceType;
                switch (fc.VideoSourceType)
                {
                    case 0:
                        cmbJPEGURL.Text = fc.FinalUrl;
                        txtLogin.Text = fc.Username;
                        txtPassword.Text = fc.Password;
                        txtCookies1.Text = fc.Cookies;
                        break;
                    case 1:
                        cmbMJPEGURL.Text = fc.FinalUrl;
                        txtLogin2.Text = fc.Username;
                        txtPassword2.Text = fc.Password;
                        txtCookies.Text = fc.Cookies;
                        break;
                    case 2:
                        cmbFile.Text = fc.FinalUrl;
                        break;
                    case 5:
                        cmbVLCURL.Text = fc.FinalUrl;
                        break;
                }

                if (!String.IsNullOrEmpty(fc.Flags))
                {
                    string[] flags = fc.Flags.Split(',');
                    foreach(string f in flags)
                    {
                        if (!string.IsNullOrEmpty(f))
                        {
                            switch(f.ToUpper())
                            {
                                case "FBA":
                                    CameraControl.Camobject.settings.forcebasic = true;
                                    chkForceBasic1.Checked = chkForceBasic.Checked = CameraControl.Camobject.settings.forcebasic;
                                    break;
                            }
                        }
                    }
                }
                if (fc.Ptzid>-1)
                {
                    CameraControl.Camobject.ptz = fc.Ptzid;
                    CameraControl.Camobject.ptzentryindex = fc.Ptzentryid;
                    CameraControl.Camobject.settings.ptzchannel = fc.Channel;

                    CameraControl.Camobject.settings.ptzusername = fc.Username;
                    CameraControl.Camobject.settings.ptzpassword = fc.Password;
                    CameraControl.Camobject.settings.ptzurlbase = MainForm.PTZs.Single(p => p.id == fc.Ptzid).CommandURL;
                }

                if (!String.IsNullOrEmpty(fc.AudioModel))
                {
                    var uri = new Uri(fc.FinalUrl);
                    if (!String.IsNullOrEmpty(uri.DnsSafeHost))
                    {
                        CameraControl.Camobject.settings.audioip = uri.DnsSafeHost;
                    }
                    CameraControl.Camobject.settings.audiomodel = fc.AudioModel;
                    CameraControl.Camobject.settings.audioport = uri.Port;
                    CameraControl.Camobject.settings.audiousername = fc.Username;
                    CameraControl.Camobject.settings.audiopassword = fc.Password;



                }
                SetupVideoSource();

                
            }
            fc.Dispose();
        }

        private void devicesCombo_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (_videoDevices.Count != 0)
            {
                _videoCaptureDevice = new VideoCaptureDevice(_videoDevices[devicesCombo.SelectedIndex].MonikerString);
                EnumeratedSupportedFrameSizes();
            }
        }

        // Collect supported video and snapshot sizes
        private void EnumeratedSupportedFrameSizes()
        {
            this.Cursor = Cursors.WaitCursor;

            videoResolutionsCombo.Items.Clear();
            snapshotResolutionsCombo.Items.Clear();
            videoInputsCombo.Items.Clear();
            _snapshotCapabilitiesDictionary.Clear();
            _videoCapabilitiesDictionary.Clear();
            try
            {
                // collect video capabilities
                VideoCapabilities[] videoCapabilities = _videoCaptureDevice.VideoCapabilities;
                int videoResolutionIndex = 0;
                foreach (VideoCapabilities capabilty in videoCapabilities)
                {
                    string item = string.Format(
                        "{0} x {1} ({2} fps)", capabilty.FrameSize.Width, capabilty.FrameSize.Height, capabilty.AverageFrameRate);

                    if (!videoResolutionsCombo.Items.Contains(item))
                    {
                        if (_captureSize == capabilty.FrameSize)
                        {
                            videoResolutionIndex = videoResolutionsCombo.Items.Count;
                        }

                        videoResolutionsCombo.Items.Add(item);
                    }

                    if (!_videoCapabilitiesDictionary.ContainsKey(item))
                    {
                        _videoCapabilitiesDictionary.Add(item, capabilty);
                    }
                }

                if (videoCapabilities.Length == 0)
                {
                    videoResolutionsCombo.Enabled = false;
                    videoResolutionsCombo.Items.Add(LocRm.GetString("NotSupported"));
                }
                else
                {
                    videoResolutionsCombo.Enabled = true;
                }

                videoResolutionsCombo.SelectedIndex = videoResolutionIndex;

                if (_configureSnapshots)
                {
                    // collect snapshot capabilities
                    VideoCapabilities[] snapshotCapabilities = _videoCaptureDevice.SnapshotCapabilities;
                    int snapshotResolutionIndex = 0;

                    foreach (VideoCapabilities capabilty in snapshotCapabilities)
                    {
                        string item = string.Format(
                            "{0} x {1}", capabilty.FrameSize.Width, capabilty.FrameSize.Height);

                        if (!snapshotResolutionsCombo.Items.Contains(item))
                        {
                            if (_snapshotSize == capabilty.FrameSize)
                            {
                                snapshotResolutionIndex = snapshotResolutionsCombo.Items.Count;
                            }

                            snapshotResolutionsCombo.Items.Add(item);
                            _snapshotCapabilitiesDictionary.Add(item, capabilty);
                        }
                    }

                    if (snapshotCapabilities.Length == 0)
                    {
                        snapshotResolutionsCombo.Enabled = false;
                        snapshotResolutionsCombo.Items.Add(LocRm.GetString("NotSupported"));
                    }
                    else
                    {
                        snapshotResolutionsCombo.Enabled = true;
                    }

                    snapshotResolutionsCombo.SelectedIndex = snapshotResolutionIndex;                   
                }             

                // get video inputs
                _availableVideoInputs = _videoCaptureDevice.AvailableCrossbarVideoInputs;
                int videoInputIndex = -1;

                foreach (VideoInput input in _availableVideoInputs)
                {
                    string item = string.Format("{0}: {1}", input.Index, input.Type);

                    if ((input.Index == _videoInput.Index) && (input.Type == _videoInput.Type))
                    {
                        videoInputIndex = videoInputsCombo.Items.Count;
                    }

                    videoInputsCombo.Items.Add(item);
                }

                if (_availableVideoInputs.Length == 0)
                {
                    videoInputsCombo.Items.Add(LocRm.GetString("NotSupported"));
                    videoInputsCombo.Enabled = false;
                    videoInputsCombo.SelectedIndex = 0;
                }
                else
                {
                    videoInputsCombo.Items.Insert(0,LocRm.GetString("PleaseSelect"));
                    videoInputsCombo.Enabled = true;
                    videoInputsCombo.SelectedIndex = videoInputIndex+1;
                }
                
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void videoResolutionsCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void videoInputsCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void chkNoResize_CheckedChanged(object sender, EventArgs e)
        {
            txtResizeHeight.Enabled = txtResizeWidth.Enabled = !chkNoResize.Checked;
        }

        private void chkMousePointer_CheckedChanged(object sender, EventArgs e)
        {
            if (CameraControl != null && CameraControl.Camera != null && CameraControl.Camera.VideoSource is DesktopStream)
            {
                ((DesktopStream) CameraControl.Camera.VideoSource).MousePointer = chkMousePointer.Checked;
            }
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
                area = new Rectangle(i[0],i[1],i[2],i[3]);
            }

            var screenArea = new ScreenArea(screen,area);
                          
            screenArea.ShowDialog();
            CameraControl.Camobject.settings.desktoparea = screenArea.Area.Left + "," + screenArea.Area.Top + "," + screenArea.Area.Width + "," + screenArea.Area.Height;
            screenArea.Dispose();
        }

        private void label42_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void txtVLCArgs_PastedText(object sender, ClipboardTextBoxExample.ClipboardEventArgs e)
        {
            //reformat VLC local arguments to input arguments
            Clipboard.SetText(e.ClipboardText.Trim().Replace(":", Environment.NewLine+"-").Trim());

        }

        private void linkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl(MainForm.Website+"/userguide-vlc.aspx");
        }

        private void snapshotResolutionsCombo_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void chkKinectSkeletal_CheckedChanged(object sender, EventArgs e)
        {

        }

    }
}