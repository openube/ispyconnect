using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using AForge.Video.DirectShow;
using AForge.Vision.Motion;
using iSpyApplication.Controls;
using iSpyApplication.Kinect;
using iSpyApplication.Video;


namespace iSpyApplication
{
    public partial class AddCamera : Form
    {
        private readonly string[] _alertmodes = new[] {"movement", "nomovement", "objectcount"};

        private readonly object[] _detectortypes = new object[] { "Two Frames", "Custom Frame", "Background Modelling", "Two Frames (Color)", "Custom Frame (Color)", "Background Modelling (Color)", "None" };

        private readonly object[] _processortypes = new object[] {"Grid Processing", "Object Tracking", "Border Highlighting","Area Highlighting", "None"};

        public CameraWindow CameraControl;
        public bool StartWizard;
        public bool IsNew;
        private HSLFilteringForm _filterForm;
        private bool _loaded;
        private ConfigureTripWires _ctw;


        public AddCamera()
        {
            InitializeComponent();
            RenderResources();

            AreaControl.BoundsChanged += AsBoundsChanged;
            AreaControl.Invalidate();
        }

        private void AsBoundsChanged(object sender, EventArgs e)
        {
            if (CameraControl.Camera != null && CameraControl.Camera.MotionDetector != null)
            {
                CameraControl.Camera.SetMotionZones(AreaControl.MotionZones);
            }
            CameraControl.Camobject.detector.motionzones = AreaControl.MotionZones;
        }

        private void BtnSelectSourceClick(object sender, EventArgs e)
        {
            StartWizard = false;
            SelectSource();
        }

        private bool SelectSource()
        {
            bool success = false;
            FindCameras.LastConfig.PromptSave = false;
            
            var vs = new VideoSource { CameraControl = CameraControl, StartWizard = StartWizard };
            vs.ShowDialog(this);
            if (vs.DialogResult == DialogResult.OK)
            {
                CameraControl.Camobject.settings.videosourcestring = vs.VideoSourceString;
                CameraControl.Camobject.settings.sourceindex = vs.SourceIndex;
                CameraControl.Camobject.settings.login = vs.CameraLogin;
                CameraControl.Camobject.settings.password = vs.CameraPassword;
                CameraControl.Camobject.settings.useragent = vs.UserAgent;
                CameraControl.Camobject.settings.forcebasic = vs.ForceBasic;

                bool su = CameraControl.Camobject.resolution != vs.CaptureSize.Width + "x" + vs.CaptureSize.Height;
                if (vs.SourceIndex==3)
                {
                    CameraControl.Camobject.resolution = vs.CaptureSize.Width + "x" + vs.CaptureSize.Height;
                    CameraControl.Camobject.settings.framerate = vs.FrameRate;
                    CameraControl.Camobject.settings.crossbarindex = vs.VideoInputIndex;
                }
                
                chkActive.Enabled = true;
                chkActive.Checked = false;
                Thread.Sleep(1000); //allows unmanaged code to complete shutdown
                chkActive.Checked = true;

                CameraControl.NeedSizeUpdate = su;
                if (CameraControl.VolumeControl == null && CameraControl.Camera!=null)
                {
                    //do we need to add a paired volume control?
                    if (CameraControl.Camera.VideoSource is VlcStream)
                    {
                        ((VlcStream)CameraControl.Camera.VideoSource).HasAudioStream += AddCameraHasAudioStream;
                    }
                    if (CameraControl.Camera.VideoSource is FFMPEGStream)
                    {
                        ((FFMPEGStream)CameraControl.Camera.VideoSource).HasAudioStream += AddCameraHasAudioStream;
                    }
                    if (CameraControl.Camera.VideoSource is KinectStream)
                    {
                        ((KinectStream)CameraControl.Camera.VideoSource).HasAudioStream += AddCameraHasAudioStream;
                    }
                    if (FindCameras.LastConfig.PromptSave)
                    {
                        CameraControl.Camera.NewFrame -= NewCameraNewFrame;
                        CameraControl.Camera.NewFrame += NewCameraNewFrame;
                    }

                }
                LoadAlertTypes();
                success = true;

                
            }
            vs.Dispose();
            return success;
        }

        private delegate void ShareDelegate();
        void NewCameraNewFrame(object sender, EventArgs e)
        {
            if (CameraControl == null || CameraControl.Camera == null)
                return;

            if (LocRm.CurrentSet.CultureCode != "en")
                return;

            CameraControl.Camera.NewFrame -= NewCameraNewFrame;

            if (IsDisposed || !Visible)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(new ShareDelegate(DoShareCamera));
                return;
            }
            DoShareCamera();
        }

        void DoShareCamera()
        {
            if (FindCameras.LastConfig.PromptSave)
            {
                var sc = new ShareCamera();
                sc.ShowDialog(this);
                sc.Dispose();
            }
        }
            

        private delegate void EnableDelegate();

        void AddCameraHasAudioStream(object sender, EventArgs eventArgs)
        {
            if (IsDisposed || !Visible)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(new EnableDelegate(AddAudioStream));
                return;
            }
            AddAudioStream();
        }

        private void AddAudioStream()
        {
            var m = MainForm.Microphones.SingleOrDefault(p => p.id == CameraControl.Camobject.settings.micpair);
            
            if (m!=null)
            {
                lblMicSource.Text = m.name;
            }
        }

        private void AddCameraLoad(object sender, EventArgs e)
        {
            if (CameraControl.Camobject.id == -1)
            {
                if (!SelectSource())
                {
                    Close();
                    return;
                }
            }
            if (CameraControl.Camobject.id == -1)
            {
                CameraControl.Camobject.id = MainForm.NextCameraId;
                MainForm.Cameras.Add(CameraControl.Camobject);
            }
            _loaded = false;
            CameraControl.IsEdit = true;
            if (CameraControl.VolumeControl != null)
                CameraControl.VolumeControl.IsEdit = true;
            ddlTimestamp.Text = CameraControl.Camobject.settings.timestampformatter;

            //chkUploadYouTube.Checked = CameraControl.Camobject.settings.youtube.autoupload;
            chkPublic.Checked = CameraControl.Camobject.settings.youtube.@public;
            txtTags.Text = CameraControl.Camobject.settings.youtube.tags;
            chkMovement.Checked = CameraControl.Camobject.alerts.active;

            var youTubeCats = MainForm.Conf.YouTubeCategories.Split(',');

            int i = 0, ytcInd = 0;
            foreach (var cat in youTubeCats)
            {
                ddlCategory.Items.Add(cat);
                if (cat == CameraControl.Camobject.settings.youtube.category)
                {
                    ytcInd = i;
                }
                i++;
            }
            ddlCategory.SelectedIndex = ytcInd;

            
            gpbSubscriber.Enabled = gpbSubscriber2.Enabled = MainForm.Conf.Subscribed;

            ddlMotionDetector.Items.AddRange(_detectortypes);
            ddlProcessor.Items.AddRange(_processortypes);

            for (int j = 0; j < _detectortypes.Length; j++)
            {
                if ((string) _detectortypes[j] == CameraControl.Camobject.detector.type)
                {
                    ddlMotionDetector.SelectedIndex = j;
                    break;
                }
            }
            for (int j = 0; j < _processortypes.Length; j++)
            {
                if ((string) _processortypes[j] == CameraControl.Camobject.detector.postprocessor)
                {
                    ddlProcessor.SelectedIndex = j;
                    break;
                }
            }

            LoadAlertTypes();

            ddlProcessFrames.SelectedItem = CameraControl.Camobject.detector.processeveryframe.ToString(CultureInfo.InvariantCulture);
            txtCameraName.Text = CameraControl.Camobject.name;

            ranger1.Maximum = 100;
            ranger1.Minimum = 0.001;
            ranger1.ValueMin = CameraControl.Camobject.detector.minsensitivity;
            ranger1.ValueMax = CameraControl.Camobject.detector.maxsensitivity;
            ranger1.ValueMinChanged += Ranger1ValueMinChanged;
            ranger1.ValueMaxChanged += Ranger1ValueMaxChanged;
            
            rdoRecordDetect.Checked = CameraControl.Camobject.detector.recordondetect;
            rdoRecordAlert.Checked = CameraControl.Camobject.detector.recordonalert;
            rdoNoRecord.Checked = !rdoRecordDetect.Checked && !rdoRecordAlert.Checked;
            txtExecuteMovement.Text = CameraControl.Camobject.alerts.executefile;

            chkSendEmailMovement.Checked = CameraControl.Camobject.notifications.sendemail;
            chkSendSMSMovement.Checked = CameraControl.Camobject.notifications.sendsms;
            txtSMSNumber.Text = CameraControl.Camobject.settings.smsnumber;
            txtEmailAlert.Text = CameraControl.Camobject.settings.emailaddress;
            chkMMS.Checked = CameraControl.Camobject.notifications.sendmms;
            chkSchedule.Checked = CameraControl.Camobject.schedule.active;
            chkFlipX.Checked = CameraControl.Camobject.flipx;
            chkFlipY.Checked = CameraControl.Camobject.flipy;
            chkRotate.Checked = CameraControl.Camobject.rotate90;
            chkTrack.Checked = CameraControl.Camobject.settings.ptzautotrack;
            chkColourProcessing.Checked = CameraControl.Camobject.detector.colourprocessingenabled;
            numMaxFR.Value = CameraControl.Camobject.settings.maxframerate;
            numMaxFRRecording.Value = CameraControl.Camobject.settings.maxframeraterecord;
            txtArguments.Text = CameraControl.Camobject.alerts.arguments;
            txtDirectory.Text = CameraControl.Camobject.directory;
            chkAutoHome.Checked = CameraControl.Camobject.settings.ptzautohome;
            chkCRF.Checked = CameraControl.Camobject.recorder.crf;
            numTTH.Value = CameraControl.Camobject.settings.ptztimetohome;
            rdoContinuous.Checked = CameraControl.Camobject.alerts.processmode == "continuous";
            rdoMotion.Checked = CameraControl.Camobject.alerts.processmode == "motion";
            tbFTPQuality.Value = CameraControl.Camobject.ftp.quality;
            chkSchedulePTZ.Checked = CameraControl.Camobject.ptzschedule.active;
            chkSuspendOnMovement.Checked = CameraControl.Camobject.ptzschedule.suspend;
            chkLocalSaving.Checked = CameraControl.Camobject.ftp.savelocal;
            txtLocalFilename.Text = CameraControl.Camobject.ftp.localfilename;
            chkMaximise.Checked = CameraControl.Camobject.alerts.maximise;
            txtPTZChannel.Text = CameraControl.Camobject.settings.ptzchannel;
            chkReverseTracking.Checked = CameraControl.Camobject.settings.ptzautotrackreverse;
            txtSound.Text = CameraControl.Camobject.alerts.playsound;
            ShowSchedule(-1);

            chkActive.Checked = CameraControl.Camobject.settings.active;
            if (CameraControl.Camera==null)
            {
                chkActive.Checked = CameraControl.Camobject.settings.active = false;
                btnAdvanced.Enabled = btnCrossbar.Enabled = false;
            }
            else
            {
                chkActive.Checked = CameraControl.Camobject.settings.active;
            }
            pnlScheduler.Enabled = chkSchedule.Checked;

            AreaControl.MotionZones = CameraControl.Camobject.detector.motionzones;

            chkActive.Enabled = !string.IsNullOrEmpty(CameraControl.Camobject.settings.videosourcestring);
            string[] alertOptions = CameraControl.Camobject.alerts.alertoptions.Split(','); //beep,restore
            chkBeep.Checked = Convert.ToBoolean(alertOptions[0]);
            chkRestore.Checked = Convert.ToBoolean(alertOptions[1]);
            Text = LocRm.GetString("EditCamera");
            if (CameraControl.Camobject.id > -1)
                Text += string.Format(" (ID: {0}, DIR: {1})", CameraControl.Camobject.id, CameraControl.Camobject.directory);


            txtTimeLapse.Text = CameraControl.Camobject.recorder.timelapse.ToString(CultureInfo.InvariantCulture);
            pnlMovement.Enabled = chkMovement.Checked;
            chkSuppressNoise.Checked = CameraControl.Camobject.settings.suppressnoise;
            
           
            linkLabel4.Visible = linkLabel9.Visible = !(MainForm.Conf.Subscribed);

            if (CameraControl.Camera != null)
            {
                CameraControl.Camera.NewFrame -= CameraNewFrame;
                CameraControl.Camera.NewFrame += CameraNewFrame;
            }

            txtBuffer.Text = CameraControl.Camobject.recorder.bufferseconds.ToString(CultureInfo.InvariantCulture);
            txtCalibrationDelay.Text = CameraControl.Camobject.detector.calibrationdelay.ToString(CultureInfo.InvariantCulture);
            txtInactiveRecord.Text = CameraControl.Camobject.recorder.inactiverecord.ToString(CultureInfo.InvariantCulture);
            txtMinimumInterval.Text = CameraControl.Camobject.alerts.minimuminterval.ToString(CultureInfo.InvariantCulture);
            txtMaxRecordTime.Text = CameraControl.Camobject.recorder.maxrecordtime.ToString(CultureInfo.InvariantCulture);
            btnBack.Enabled = false;

            ddlHourStart.SelectedIndex =
                ddlHourEnd.SelectedIndex = ddlMinuteStart.SelectedIndex = ddlMinuteEnd.SelectedIndex = 0;

            txtFTPServer.Text = CameraControl.Camobject.ftp.server;
            txtFTPUsername.Text = CameraControl.Camobject.ftp.username;
            txtFTPPassword.Text = CameraControl.Camobject.ftp.password;
            txtFTPPort.Text = CameraControl.Camobject.ftp.port.ToString(CultureInfo.InvariantCulture);
            txtUploadEvery.Text = CameraControl.Camobject.ftp.interval.ToString(CultureInfo.InvariantCulture);
            txtFTPFilename.Text = CameraControl.Camobject.ftp.filename;
            chkFTP.Checked = gbFTP.Enabled = CameraControl.Camobject.ftp.enabled;
            gbLocal.Enabled = CameraControl.Camobject.ftp.savelocal;
            txtTimeLapseFrames.Text = CameraControl.Camobject.recorder.timelapseframes.ToString(CultureInfo.InvariantCulture);

            chkTimelapse.Checked = CameraControl.Camobject.recorder.timelapseenabled;
            if (!chkTimelapse.Checked)
                groupBox1.Enabled = false;

            chkEmailOnDisconnect.Checked = CameraControl.Camobject.settings.notifyondisconnect;
            txtMaskImage.Text = CameraControl.Camobject.settings.maskimage;

            chkUsePassive.Checked = CameraControl.Camobject.ftp.usepassive;
            chkPTZFlipX.Checked = CameraControl.Camobject.settings.ptzflipx;
            chkPTZFlipY.Checked = CameraControl.Camobject.settings.ptzflipy;
            chkPTZRotate90.Checked = CameraControl.Camobject.settings.ptzrotate90;

            txtFTPText.Text = CameraControl.Camobject.ftp.text;


            rdoFTPMotion.Checked = CameraControl.Camobject.ftp.mode == 0;
            rdoFTPAlerts.Checked = CameraControl.Camobject.ftp.mode == 1;
            rdoFTPInterval.Checked = CameraControl.Camobject.ftp.mode == 2;

            txtUploadEvery.Enabled = rdoFTPInterval.Checked;

            pnlTrack.Enabled = chkTrack.Checked;

            rdoAny.Checked = CameraControl.Camobject.settings.ptzautotrackmode == 0;
            rdoVert.Checked = CameraControl.Camobject.settings.ptzautotrackmode == 1;
            rdoHor.Checked = CameraControl.Camobject.settings.ptzautotrackmode == 2;
            
            LoadPTZs();
            txtPTZURL.Text = CameraControl.Camobject.settings.ptzurlbase;

            txtAccessGroups.Text = CameraControl.Camobject.settings.accessgroups;
            numAutoHomeDelay.Value = CameraControl.Camobject.settings.ptzautohomedelay;

            ShowPTZSchedule();


            ddlCopyFrom.Items.Clear();
            ddlCopyFrom.Items.Add(new ListItem(LocRm.GetString("CopyFrom"), "-1"));
            foreach(objectsCamera c in MainForm.Cameras)
            {
                if (c.id != CameraControl.Camobject.id)
                    ddlCopyFrom.Items.Add(new ListItem(c.name,c.id.ToString(CultureInfo.InvariantCulture)));
            }
            ddlCopyFrom.SelectedIndex = 0;


            txtPTZUsername.Text = CameraControl.Camobject.settings.ptzusername;
            txtPTZPassword.Text = CameraControl.Camobject.settings.ptzpassword;
            tbQuality.Value = CameraControl.Camobject.recorder.quality;

            numTimelapseSave.Value = CameraControl.Camobject.recorder.timelapsesave;
            numFramerate.Value = CameraControl.Camobject.recorder.timelapseframerate;

            try
            {
                ddlProfile.SelectedIndex = CameraControl.Camobject.recorder.profile;
            }
            catch
            {
                ddlProfile.SelectedIndex = 0;
            }
            chkTwitter.Checked = CameraControl.Camobject.notifications.sendtwitter;

            var m = MainForm.Microphones.SingleOrDefault(p => p.id == CameraControl.Camobject.settings.micpair);
            lblMicSource.Text = m != null ? m.name : LocRm.GetString("None");

            PopulateCodecsCombo();
            numTalkPort.Value = CameraControl.Camobject.settings.audioport > -1 ? CameraControl.Camobject.settings.audioport : 80;
            txtAudioOutIP.Text = CameraControl.Camobject.settings.audioip;
            txtTalkUsername.Text = CameraControl.Camobject.settings.audiousername;
            txtTalkPassword.Text = CameraControl.Camobject.settings.audiopassword;

            string t = CameraControl.Camobject.alerts.trigger ?? "";
            string t2 = CameraControl.Camobject.recorder.trigger ?? "";

            ddlTrigger.Items.Add(new ListItem("None",""));
            ddlTriggerRecording.Items.Add(new ListItem("None", ""));

            foreach (var c in MainForm.Cameras.Where(p=>p.id!=CameraControl.Camobject.id))
            {
                ddlTrigger.Items.Add(new ListItem(c.name, "2," + c.id));
                ddlTriggerRecording.Items.Add(new ListItem(c.name, "2," + c.id));                
            }
            foreach (var c in MainForm.Microphones.Where(p => p.id != CameraControl.Camobject.settings.micpair))
            {
                ddlTrigger.Items.Add(new ListItem(c.name, "1," + c.id));
                ddlTriggerRecording.Items.Add(new ListItem(c.name, "1," + c.id));
            }
            foreach (ListItem li in ddlTrigger.Items)
            {
                if (li.Value == t)
                    ddlTrigger.SelectedItem = li;
                if (li.Value == t2)
                    ddlTriggerRecording.SelectedItem = li;
            }
            if (ddlTrigger.SelectedIndex == -1)
                ddlTrigger.SelectedIndex = 0;
            if (ddlTriggerRecording.SelectedIndex == -1)
                ddlTriggerRecording.SelectedIndex = 0;


            dtpSchedulePTZ.Value = new DateTime(2012,1,1,0,0,0,0);
            numMaxCounter.Value = CameraControl.Camobject.ftp.countermax;
            _loaded = true;
        }

        private void LoadAlertTypes()
        {
            ddlAlertMode.Items.Clear();
            int iMode = 0;

            foreach (string s in _alertmodes)
            {
                ddlAlertMode.Items.Add(LocRm.GetString(s));
            }

            //provider specific alert options
            switch (CameraControl.Camobject.settings.sourceindex)
            {
                case 7:
                    ddlAlertMode.Items.Add("Virtual Trip Wires");
                    break;
            }


            foreach (String plugin in MainForm.Plugins)
            {
                string name = plugin.Substring(plugin.LastIndexOf("\\", StringComparison.Ordinal) + 1);
                name = name.Substring(0, name.LastIndexOf(".", StringComparison.Ordinal));
                ddlAlertMode.Items.Add(name);
            }


            int iCount = 0;
            if (CameraControl.Camobject.alerts.mode != null)
            {
                foreach (string name in ddlAlertMode.Items)
                {
                    if (name.ToLower() == CameraControl.Camobject.alerts.mode.ToLower())
                    {
                        iMode = iCount;
                        break;
                    }
                    iCount++;
                }
            }


            ddlAlertMode.SelectedIndex = iMode;
        }

        void Ranger1ValueMinChanged()
        {
            CameraControl.Camobject.detector.minsensitivity = ranger1.ValueMin;
            if (CameraControl.Camera != null)
            {
                CameraControl.Camera.AlarmLevel = Helper.CalculateTrigger(ranger1.ValueMin);
            }
        
        }

        void Ranger1ValueMaxChanged()
        {
            CameraControl.Camobject.detector.maxsensitivity = ranger1.ValueMax;
            if (CameraControl.Camera != null)
            {
                CameraControl.Camera.AlarmLevelMax = Helper.CalculateTrigger(ranger1.ValueMax);
            }

        }


        private void ShowPTZSchedule()
        {
            tableLayoutPanel20.Enabled = chkSchedulePTZ.Checked;

            lbPTZSchedule.Items.Clear();
            var s = CameraControl.Camobject.ptzschedule.entries.ToList().OrderBy(p => p.time).ToList();
            foreach (var ptzs in s)
            {
                lbPTZSchedule.Items.Add(ptzs.time.ToString("hh:mm:ss tt")+" " + ptzs.command);
            }
        }

        private void RenderResources()
        {
            btnBack.Text = LocRm.GetString("Back");
            btnDelete.Text = LocRm.GetString("Delete");
            btnDetectMovement.Text = LocRm.GetString("chars_3014702301470230147");
            btnFinish.Text = LocRm.GetString("Finish");
            btnMaskImage.Text = LocRm.GetString("chars_3014702301470230147");
            btnNext.Text = LocRm.GetString("Next");
            btnAdvanced.Text = LocRm.GetString("AdvProperties");
            btnSaveFTP.Text = LocRm.GetString("Test");
            btnSelectSource.Text = LocRm.GetString("chars_3014702301470230147");
            btnUpdate.Text = LocRm.GetString("Update");
            button1.Text = LocRm.GetString("ClearAll");
            button2.Text = LocRm.GetString("Add");
            chkActive.Text = LocRm.GetString("CameraActive");
            chkBeep.Text = LocRm.GetString("Beep");
            chkEmailOnDisconnect.Text = LocRm.GetString("SendEmailOnDisconnect");
            chkFlipX.Text = LocRm.GetString("Flipx");
            chkFlipY.Text = LocRm.GetString("Flipy");
            chkFri.Text = LocRm.GetString("Fri");
            chkFTP.Text = LocRm.GetString("FtpEnabled");
            label22.Text = LocRm.GetString("Username");
            label42.Text = LocRm.GetString("Password");
            button6.Text = LocRm.GetString("Add");
            btnDeletePTZ.Text = LocRm.GetString("Delete");
            chkSchedulePTZ.Text = LocRm.GetString("Scheduler");
            rdoMotion.Text = LocRm.GetString("WhenMotionDetected");
            rdoContinuous.Text = LocRm.GetString("Continuous");
            chkCRF.Text = LocRm.GetString("Auto");
            chkMMS.Text = LocRm.GetString("SendAsMmsWithImage2Credit");
            chkMon.Text = LocRm.GetString("Mon");
            chkMovement.Text = LocRm.GetString("AlertsEnabled");
            chkPublic.Text = LocRm.GetString("PubliccheckThisToMakeYour");
            rdoRecordDetect.Text = LocRm.GetString("RecordOnMovementDetection");
            rdoRecordAlert.Text = LocRm.GetString("RecordOnAlert");
            rdoNoRecord.Text = LocRm.GetString("NoRecord");
            chkRecordSchedule.Text = LocRm.GetString("RecordOnScheduleStart");
            chkRestore.Text = LocRm.GetString("ShowIspyWindow");
            chkSat.Text = LocRm.GetString("Sat");
            chkSchedule.Text = LocRm.GetString("ScheduleCamera");
            chkScheduleActive.Text = LocRm.GetString("ScheduleActive");
            chkScheduleAlerts.Text = LocRm.GetString("AlertsEnabled");
            chkScheduleRecordOnDetect.Text = LocRm.GetString("RecordOnDetect");
            chkRecordAlertSchedule.Text = LocRm.GetString("RecordOnAlert");
            chkSendEmailMovement.Text = LocRm.GetString("SendEmailOnAlert");
            chkSendSMSMovement.Text = LocRm.GetString("SendSmsOnAlert");
            chkSun.Text = LocRm.GetString("Sun");
            chkSuppressNoise.Text = LocRm.GetString("SupressNoise");
            chkThu.Text = LocRm.GetString("Thu");
            chkTue.Text = LocRm.GetString("Tue");
            //chkUploadYouTube.Text = LocRm.GetString("AutomaticallyUploadGenera");
            chkUsePassive.Text = LocRm.GetString("PassiveMode");
            chkWed.Text = LocRm.GetString("Wed");
            chkScheduleTimelapse.Text = LocRm.GetString("TimelapseEnabled");
            chkTimelapse.Text = LocRm.GetString("TimelapseEnabled");
            gbFTP.Text = LocRm.GetString("FtpDetails");
            gbZones.Text = LocRm.GetString("DetectionZones");
            gpbSubscriber.Text = gpbSubscriber2.Text = LocRm.GetString("WebServiceOptions");
            groupBox1.Text = LocRm.GetString("TimelapseRecording");
            groupBox3.Text = LocRm.GetString("VideoSource");
            groupBox4.Text = LocRm.GetString("RecordingSettings");
            groupBox5.Text = LocRm.GetString("Detector");
            label1.Text = LocRm.GetString("Name");
            label10.Text = LocRm.GetString("chars_3801146");
            label11.Text = LocRm.GetString("TimeStamp");
            label12.Text = LocRm.GetString("UseDetector");
            label13.Text = LocRm.GetString("Seconds");
            label14.Text = LocRm.GetString("RecordTimelapse");
            label15.Text = LocRm.GetString("DistinctAlertInterval");
            label17.Text = LocRm.GetString("Frames");
            label19.Text = groupBox2.Text = LocRm.GetString("Microphone");
            label2.Text = LocRm.GetString("Source");
            
            label24.Text = LocRm.GetString("Seconds");
            label25.Text = LocRm.GetString("CalibrationDelay");
            label26.Text = LocRm.GetString("PrebufferFrames");
            label27.Text = LocRm.GetString("Seconds");
            label28.Text = LocRm.GetString("Seconds");
            label29.Text = LocRm.GetString("Buffer");
            label3.Text = LocRm.GetString("TriggerRange");
            label30.Text = LocRm.GetString("MaxRecordTime");
            label31.Text = LocRm.GetString("Seconds");
            label32.Text = LocRm.GetString("InactivityRecord");
            label33.Text = LocRm.GetString("Seconds");
            label34.Text = LocRm.GetString("MaxRecordTime");
            label35.Text = LocRm.GetString("Seconds");
            label36.Text = LocRm.GetString("Seconds");
            label37.Text = rdoFTPInterval.Text = LocRm.GetString("Interval");
            label38.Text = LocRm.GetString("MaxCalibrationDelay");
            label39.Text = LocRm.GetString("Seconds");
            label4.Text = LocRm.GetString("Mode");
            label40.Text = LocRm.GetString("InactivityRecord");
            label41.Text = LocRm.GetString("Seconds");
            label44.Text = LocRm.GetString("savesAFrameToAMovieFileNS");
            label45.Text = LocRm.GetString("EmailAddress");
            label46.Text = LocRm.GetString("DisplayStyle");
            label48.Text = LocRm.GetString("ColourFiltering");
           
            label49.Text = LocRm.GetString("Days");
            label50.Text = LocRm.GetString("ImportantMakeSureYourSche");
            label51.Text = LocRm.GetString("ProcessEvery");
            label52.Text = LocRm.GetString("Server");
            label53.Text = LocRm.GetString("Port");
            label54.Text = LocRm.GetString("Username");
            label55.Text = LocRm.GetString("Password");
            label56.Text = LocRm.GetString("Filename");
            label57.Text = LocRm.GetString("UploadOn");
            label58.Text = LocRm.GetString("Seconds");
            label6.Text = LocRm.GetString("ExecuteFile");
            label60.Text = LocRm.GetString("Egimagesmycamimagejpg");
            label64.Text = LocRm.GetString("Frames");
            label67.Text = LocRm.GetString("Images");
            label68.Text = LocRm.GetString("Interval");
            label69.Text = LocRm.GetString("Seconds");
            label7.Text = LocRm.GetString("Start");
            label70.Text = LocRm.GetString("savesAFrameEveryNSecondsn");
            label71.Text = LocRm.GetString("Movie");
            label73.Text = LocRm.GetString("CameraModel");
            //label74.Text = LocRM.GetString("NoteOnlyAvailableForIpCam");
            label75.Text = LocRm.GetString("ExtendedCommands");
            label76.Text = LocRm.GetString("ExitThisToEnableAlertsAnd");
            label77.Text = LocRm.GetString("Tags");
            label78.Text = LocRm.GetString("Category");
            label79.Text = LocRm.GetString("TipYouCanSelectivelyUploa");
            label8.Text = LocRm.GetString("chars_3801146");
            label80.Text = LocRm.GetString("TipToCreateAScheduleOvern");
            //label81.Text = LocRm.GetString("tipUseADateStringFormatTo");
            label82.Text = LocRm.GetString("YourSmsNumber");
            label83.Text = LocRm.GetString("ClickAndDragTodraw");
            label84.Text = LocRm.GetString("MaskImage");
            label85.Text = LocRm.GetString("createATransparentpngImag");
            label86.Text = LocRm.GetString("OverlayText");
            label9.Text = LocRm.GetString("Stop");
            //lblVideoSource.Text = LocRm.GetString("VideoSource");
            linkLabel1.Text = LocRm.GetString("UsageTips");
            groupBox7.Text = LocRm.GetString("SaveUpload");
            linkLabel2.Text = LocRm.GetString("ScriptToRenderThisImageOn");
            linkLabel4.Text = linkLabel9.Text = LocRm.GetString("YouNeedAnActiveSubscripti");
            linkLabel5.Text = LocRm.GetString("HowToEnterYourNumber");
            linkLabel6.Text = LocRm.GetString("GetLatestList");
            linkLabel7.Text = LocRm.GetString("YoutubeSettings");
            linkLabel8.Text = linkLabel14.Text = LocRm.GetString("help");
            pnlScheduler.Text = LocRm.GetString("Scheduler");
            chkLocalSaving.Text = LocRm.GetString("LocalSavingEnabled");
            linkLabel11.Text = LocRm.GetString("OpenLocalFolder");
            tabPage1.Text = LocRm.GetString("Camera");
            tabPage2.Text = rdoFTPAlerts.Text = LocRm.GetString("Alerts");
            tabPage3.Text = rdoFTPMotion.Text = LocRm.GetString("MotionDetection");
            tabPage4.Text = LocRm.GetString("Recording");
            tabPage5.Text = LocRm.GetString("Scheduling");
            tabPage7.Text = LocRm.GetString("SaveFramesFtp");
            tabPage8.Text = LocRm.GetString("Ptz");
            tabPage9.Text = LocRm.GetString("Youtube");
            toolTip1.SetToolTip(txtMaskImage, LocRm.GetString("ToolTip_CameraName"));
            toolTip1.SetToolTip(txtCameraName, LocRm.GetString("ToolTip_CameraName"));
            toolTip1.SetToolTip(ranger1, LocRm.GetString("ToolTip_MotionSensitivity"));
            toolTip1.SetToolTip(txtExecuteMovement, LocRm.GetString("ToolTip_EGMP3"));
            toolTip1.SetToolTip(txtTimeLapseFrames, LocRm.GetString("ToolTip_TimeLapseFrames"));
            toolTip1.SetToolTip(txtTimeLapse, LocRm.GetString("ToolTip_TimeLapseVideo"));
            toolTip1.SetToolTip(txtMaxRecordTime, LocRm.GetString("ToolTip_MaxDuration"));
            toolTip1.SetToolTip(txtInactiveRecord, LocRm.GetString("ToolTip_InactiveRecord"));
            toolTip1.SetToolTip(txtBuffer, LocRm.GetString("ToolTip_BufferFrames"));
            toolTip1.SetToolTip(txtCalibrationDelay, LocRm.GetString("ToolTip_DelayAlerts"));
            toolTip1.SetToolTip(lbSchedule, LocRm.GetString("ToolTip_PressDelete"));
            label16.Text = LocRm.GetString("PTZNote");
            chkRotate.Text = LocRm.GetString("Rotate90");
            chkPTZFlipX.Text = LocRm.GetString("Flipx");
            chkPTZFlipY.Text = LocRm.GetString("Flipy");
            chkPTZRotate90.Text = LocRm.GetString("Rotate90");
            label43.Text = LocRm.GetString("MaxFramerate");
            label47.Text = LocRm.GetString("WhenRecording");
            label74.Text = LocRm.GetString("Directory");
            chkAutoHome.Text = LocRm.GetString("AutoHome");
            label87.Text = LocRm.GetString("TimeToHome");
            llblHelp.Text = LocRm.GetString("help");
            label5.Text = LocRm.GetString("homedelay");
            chkSchedFTPEnabled.Text = LocRm.GetString("FtpEnabled");
            chkSchedSaveLocalEnabled.Text = LocRm.GetString("LocalSavingEnabled");

            chkColourProcessing.Text = LocRm.GetString("Apply");
            Text = LocRm.GetString("AddCamera");
            label72.Text = LocRm.GetString("arguments");
            rdoAny.Text = LocRm.GetString("AnyDirection");
            rdoVert.Text = LocRm.GetString("VertOnly");
            rdoHor.Text = LocRm.GetString("HorOnly");
            lblAccessGroups.Text = LocRm.GetString("AccessGroups");
            groupBox6.Text = LocRm.GetString("RecordingMode");
            llblEditPTZ.Text = LocRm.GetString("Edit");
            lblQuality.Text = lblQuality2.Text = LocRm.GetString("Quality");
            lblMinutes.Text = LocRm.GetString("Minutes");
            lblSaveEvery.Text = LocRm.GetString("SaveEvery");
            label61.Text = LocRm.GetString("Profile");
            label62.Text = LocRm.GetString("Framerate");
            label59.Text = LocRm.GetString("Command");
            linkLabel3.Text = LocRm.GetString("Plugins");
            chkTrack.Text = LocRm.GetString("TrackObjects");
            linkLabel10.Text = LocRm.GetString("Reload");
        }


        private void LoadPTZs()
        {
            ddlPTZ.Items.Clear();
            ddlPTZ.Items.Add(new ListItem("Digital", "-1"));
            if (MainForm.PTZs != null)
            {
                var ptzEntries = new List<PTZEntry>();

                foreach (PTZSettings2Camera ptz in MainForm.PTZs)
                {
                    int j = 0;
                    foreach(var m in ptz.Makes)
                    {
                        string ttl = (m.Name+" "+m.Model).Trim();
                        var ptze = new PTZEntry(ttl,ptz.id,j);

                        if (!ptzEntries.Contains(ptze))
                            ptzEntries.Add(ptze);
                        j++;
                    }
                }
                foreach(var e in ptzEntries.OrderBy(p=>p.Entry))
                {
                    ddlPTZ.Items.Add(e);

                    if (CameraControl.Camobject.ptz == e.Id && CameraControl.Camobject.ptzentryindex==e.Index)
                    {
                        ddlPTZ.SelectedIndex = ddlPTZ.Items.Count-1;
                        if (CameraControl.Camobject.settings.ptzurlbase == "")
                            CameraControl.Camobject.settings.ptzurlbase = MainForm.PTZs.Single(p=>p.id==e.Id).CommandURL;
                    }
                }
                if (ddlPTZ.SelectedIndex == -1)
                    ddlPTZ.SelectedIndex = 0;
            }
        }

        private struct PTZEntry
        {
            public readonly string Entry;
            public readonly int Id;
            public readonly int Index;
            public PTZEntry(string entry, int id, int index)
            {
                Id = id;
                Entry = entry;
                Index = index;
            }
            public override string ToString()
            {
                return Entry;
            }
        }

        private void ShowSchedule(int selectedIndex)
        {
            lbSchedule.Items.Clear();
            int i = 0;
            foreach (string sched in CameraControl.ScheduleDetails)
            {
                if (sched != "")
                {
                    lbSchedule.Items.Add(new ListItem(sched, i.ToString(CultureInfo.InvariantCulture)));
                    i++;
                }
            }
            if (selectedIndex > -1 && selectedIndex < lbSchedule.Items.Count)
                lbSchedule.SelectedIndex = selectedIndex;
        }

        private void CameraNewFrame(object sender, EventArgs e)
        {
            AreaControl.LastFrame = CameraControl.Camera.LastFrame;
            if (_filterForm != null)
                _filterForm.ImageProcess = CameraControl.Camera.LastFrame;

            if (_ctw != null && _ctw.TripWireEditor1 != null)
            {
                _ctw.TripWireEditor1.LastFrame = CameraControl.Camera.LastFrame;
            }
        }

        private void BtnNextClick(object sender, EventArgs e)
        {
            GoNext();
        }

        private void GoNext()
        {
            tcCamera.SelectedIndex++;
        }

        private void GoPrevious()
        {
            tcCamera.SelectedIndex--;
        }

        private bool CheckStep1()
        {
            string err = "";
            string name = txtCameraName.Text.Trim();
            if (name == "")
                err += LocRm.GetString("Validate_Camera_EnterName") + Environment.NewLine;
            if (
                MainForm.Cameras.SingleOrDefault(
                    p => p.name.ToLower() == name.ToLower() && p.id != CameraControl.Camobject.id) != null)
                err += LocRm.GetString("Validate_Camera_NameInUse") + Environment.NewLine;

            if (string.IsNullOrEmpty(CameraControl.Camobject.settings.videosourcestring))
            {
                err += LocRm.GetString("Validate_Camera_SelectVideoSource") + Environment.NewLine;
            }

            if (err != "")
            {
                MessageBox.Show(err, LocRm.GetString("Error"));
                tcCamera.SelectedIndex = 0;
                return false;
            }
            return true;
        }

        private void BtnFinishClick(object sender, EventArgs e)
        {
            Finish();
        }

        private void Finish()
        {
            //validate page 0
            if (CheckStep1())
            {
                string err = "";
                if (chkSendSMSMovement.Checked && MainForm.Conf.ServicesEnabled &&
                    txtSMSNumber.Text.Trim() == "")
                    err += LocRm.GetString("Validate_Camera_MobileNumber") + Environment.NewLine;

                string[] smss = txtSMSNumber.Text.Trim().Replace(" ", "").Split(';');
                string sms = "";
                foreach (string s in smss)
                {
                    string sms2 = s.Trim();
                    if (!String.IsNullOrEmpty(sms))
                    {
                        if (sms2.StartsWith("00"))
                            sms2 = sms2.Substring(2);
                        if (sms2.StartsWith("+"))
                            sms2 = sms2.Substring(1);
                        if (sms2 != "")
                        {
                            sms += sms2 + ";";
                            if (!IsNumeric(sms2))
                            {
                                err += LocRm.GetString("Validate_Camera_SMSNumbers") + Environment.NewLine;
                                break;
                            }
                            
                        }
                    }
                }
                sms = sms.Trim(';');

                string email = txtEmailAlert.Text.Replace(" ", "");
                if (email != "" && !email.IsValidEmail())
                {
                    err += LocRm.GetString("Validate_Camera_EmailAlerts") + Environment.NewLine;
                }
                
                if (email == "")
                {
                    chkSendEmailMovement.Checked = false;
                    chkEmailOnDisconnect.Checked = false;
                }
                if (sms == "")
                {
                    chkSendSMSMovement.Checked = false;
                    chkMMS.Checked = false;
                }

                if (txtBuffer.Text.Length < 1 || txtInactiveRecord.Text.Length < 1 ||
                    txtCalibrationDelay.Text.Length < 1 || txtMaxRecordTime.Text.Length < 1)
                {
                    err += LocRm.GetString("Validate_Camera_RecordingSettings") + Environment.NewLine;
                }
                if (err != "")
                {
                    MessageBox.Show(err, LocRm.GetString("Error"));
                    return;
                }

                if (chkFTP.Checked)
                {
                    try
                    {
                        var request = (FtpWebRequest) WebRequest.Create(txtFTPServer.Text);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(LocRm.GetString("Validate_Camera_CheckFTP"));
                        return;
                    }
                }

                if (chkSendEmailMovement.Checked)
                {
                    if (MainForm.Conf.WSUsername == "")
                    {
                        if (MessageBox.Show(
                            LocRm.GetString("Validate_Camera_Login"), LocRm.GetString("Note"), MessageBoxButtons.YesNo) ==
                            DialogResult.Yes)
                        {
                            var ws = new Webservices();
                            ws.ShowDialog(this);
                            if (ws.DialogResult != DialogResult.Yes)
                            {
                                chkSendEmailMovement.Checked = false;
                                chkMMS.Checked = false;
                                chkEmailOnDisconnect.Checked = false;
                                chkSendSMSMovement.Checked = false;
                            }
                            ws.Dispose();
                        }
                    }
                }
                int ftpport;
                if (!int.TryParse(txtFTPPort.Text, out ftpport))
                {
                    MessageBox.Show(LocRm.GetString("Validate_Camera_FTPPort"));
                    return;
                }
                int ftpinterval;
                if (!int.TryParse(txtUploadEvery.Text, out ftpinterval))
                {
                    MessageBox.Show(LocRm.GetString("Validate_Camera_FTPInterval"));
                    return;
                }

                int timelapseframes;
                if (!int.TryParse(txtTimeLapseFrames.Text, out timelapseframes))
                {
                    MessageBox.Show(LocRm.GetString("Validate_Camera_TimelapseInterval"));
                    return;
                }

                int timelapsemovie;

                if (!int.TryParse(txtTimeLapse.Text, out timelapsemovie))
                {
                    MessageBox.Show(LocRm.GetString("Validate_Camera_TimelapseBuffer"));
                    return;
                }
                string localFilename=txtLocalFilename.Text.Trim();
                if (localFilename.IndexOf("\\", StringComparison.Ordinal)!=-1)
                {
                    MessageBox.Show("Please enter a filename only for local saving (no path information)");
                    return;
                }

                string audioip = txtAudioOutIP.Text.Trim();
                
                
                if (!String.IsNullOrEmpty(audioip))
                {
                    IPAddress _aip;
                    if (!IPAddress.TryParse(audioip, out _aip))
                    {
                        try
                        {
                            IPHostEntry ipE = Dns.GetHostEntry(audioip);
                            IPAddress[] ipA = ipE.AddressList;
                            if (ipA==null || ipA.Length == 0)
                            {
                                MessageBox.Show("Enter a valid IP address or domain for talk or clear the field.");
                                return;
                            }
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show("Enter a valid IP address or domain for talk or clear the field. ("+ex.Message+")");
                            return;
                        }
                    }
                }

                CameraControl.Camobject.detector.processeveryframe =
                    Convert.ToInt32(ddlProcessFrames.SelectedItem.ToString());
                CameraControl.Camobject.detector.motionzones = AreaControl.MotionZones;
                CameraControl.Camobject.detector.type = (string) _detectortypes[ddlMotionDetector.SelectedIndex];
                CameraControl.Camobject.detector.postprocessor = (string) _processortypes[ddlProcessor.SelectedIndex];
                CameraControl.Camobject.name = txtCameraName.Text.Trim();

                //update to plugin if connected and supported
                if (CameraControl.Camera != null && CameraControl.Camera.Plugin != null)
                {
                    try
                    {
                        var plugin = CameraControl.Camera.Plugin;
                        plugin.GetType().GetProperty("CameraName").SetValue(plugin, CameraControl.Camobject.name, null);
                    }
                    catch
                    {
                    }
                }


                CameraControl.Camobject.alerts.active = chkMovement.Checked;
                CameraControl.Camobject.alerts.executefile = txtExecuteMovement.Text;
                CameraControl.Camobject.alerts.alertoptions = chkBeep.Checked + "," + chkRestore.Checked;
                CameraControl.Camobject.notifications.sendemail = chkSendEmailMovement.Checked;
                CameraControl.Camobject.notifications.sendsms = chkSendSMSMovement.Checked;
                CameraControl.Camobject.notifications.sendmms = chkMMS.Checked;
                CameraControl.Camobject.settings.emailaddress = email;
                CameraControl.Camobject.settings.smsnumber = sms;
                CameraControl.Camobject.settings.notifyondisconnect = chkEmailOnDisconnect.Checked;
                CameraControl.Camobject.settings.ptzautohomedelay = (int)numAutoHomeDelay.Value;
                CameraControl.Camobject.settings.ptzusername = txtPTZUsername.Text;
                CameraControl.Camobject.settings.ptzpassword = txtPTZPassword.Text;
                CameraControl.Camobject.settings.ptzchannel = txtPTZChannel.Text;
                CameraControl.Camobject.ptzschedule.active = chkSchedulePTZ.Checked;
                CameraControl.Camobject.recorder.quality = tbQuality.Value;
                CameraControl.Camobject.recorder.timelapsesave = (int)numTimelapseSave.Value;
                CameraControl.Camobject.recorder.timelapseframerate = (int)numFramerate.Value;
                CameraControl.Camobject.ftp.savelocal = chkLocalSaving.Checked;
                CameraControl.Camobject.ftp.quality = tbFTPQuality.Value;
                CameraControl.Camobject.notifications.sendtwitter = chkTwitter.Checked;
                CameraControl.Camobject.ftp.localfilename = txtLocalFilename.Text.Trim();
                CameraControl.Camobject.alerts.maximise = chkMaximise.Checked;
                CameraControl.Camobject.ptzschedule.suspend = chkSuspendOnMovement.Checked;
                CameraControl.Camobject.alerts.playsound = txtSound.Text;
                CameraControl.Camobject.ftp.countermax = (int) numMaxCounter.Value;
                
                if (txtDirectory.Text.Trim() == "")
                    txtDirectory.Text = MainForm.RandomString(5);

                if (CameraControl.Camobject.directory != txtDirectory.Text)
                {
                    string path = MainForm.Conf.MediaDirectory + "video\\" + txtDirectory.Text + "\\";
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                    path = MainForm.Conf.MediaDirectory + "video\\" + txtDirectory.Text + "\\thumbs\\";

                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                }

                CameraControl.Camobject.directory = txtDirectory.Text;

                CameraControl.Camobject.schedule.active = chkSchedule.Checked;
                CameraControl.Camobject.settings.active = chkActive.Checked;

                int bufferseconds,
                    calibrationdelay,
                    inactiveRecord,
                    minimuminterval,
                    maxrecord;
                int.TryParse(txtBuffer.Text, out bufferseconds);
                int.TryParse(txtCalibrationDelay.Text, out calibrationdelay);
                int.TryParse(txtInactiveRecord.Text, out inactiveRecord);
                int.TryParse(txtMinimumInterval.Text, out minimuminterval);
                int.TryParse(txtMaxRecordTime.Text, out maxrecord);

                CameraControl.Camobject.recorder.bufferseconds = bufferseconds;

                var m = MainForm.Microphones.SingleOrDefault(p => p.id == CameraControl.Camobject.settings.micpair);
                if (m != null)
                    m.settings.buffer = CameraControl.Camobject.recorder.bufferseconds;

                CameraControl.Camobject.detector.calibrationdelay = calibrationdelay;
                CameraControl.Camobject.recorder.inactiverecord = inactiveRecord;
                CameraControl.Camobject.alerts.minimuminterval = minimuminterval;
                CameraControl.Camobject.alerts.processmode = "continuous";
                if (rdoMotion.Checked)
                    CameraControl.Camobject.alerts.processmode = "motion";
                CameraControl.Camobject.recorder.maxrecordtime = maxrecord;
                CameraControl.Camobject.recorder.timelapseenabled = chkTimelapse.Checked;

                CameraControl.Camobject.ftp.enabled = chkFTP.Checked;
                CameraControl.Camobject.ftp.server = txtFTPServer.Text;
                CameraControl.Camobject.ftp.usepassive = chkUsePassive.Checked;
                CameraControl.Camobject.ftp.username = txtFTPUsername.Text;
                CameraControl.Camobject.ftp.password = txtFTPPassword.Text;
                CameraControl.Camobject.ftp.port = ftpport;
                CameraControl.Camobject.ftp.interval = ftpinterval;
                CameraControl.Camobject.ftp.filename = txtFTPFilename.Text;
                CameraControl.Camobject.ftp.text = txtFTPText.Text;
                CameraControl.Camobject.settings.ptzautotrack = chkTrack.Checked;
                CameraControl.Camobject.settings.ptzautohome = chkAutoHome.Checked;
                CameraControl.Camobject.settings.ptzautotrackmode = 0;

                if (rdoVert.Checked)
                    CameraControl.Camobject.settings.ptzautotrackmode = 1;
                if (rdoHor.Checked)
                    CameraControl.Camobject.settings.ptzautotrackmode = 2;

                CameraControl.Camobject.settings.ptztimetohome = Convert.ToInt32(numTTH.Value);

                int ftpmode = 0;
                if (rdoFTPAlerts.Checked)
                    ftpmode = 1;
                if (rdoFTPInterval.Checked)
                    ftpmode = 2;

                CameraControl.Camobject.ftp.mode = ftpmode;

                CameraControl.Camobject.recorder.timelapseframes = timelapseframes;
                CameraControl.Camobject.recorder.timelapse = timelapsemovie;
                CameraControl.Camobject.recorder.profile = ddlProfile.SelectedIndex;

                //CameraControl.Camobject.settings.youtube.autoupload = chkUploadYouTube.Checked;
                CameraControl.Camobject.settings.youtube.category = ddlCategory.SelectedItem.ToString();
                CameraControl.Camobject.settings.youtube.@public = chkPublic.Checked;
                CameraControl.Camobject.settings.youtube.tags = txtTags.Text;
                CameraControl.Camobject.settings.maxframeraterecord = (int)numMaxFRRecording.Value;

                CameraControl.Camobject.alerts.arguments = txtArguments.Text;

                CameraControl.Camobject.settings.accessgroups = txtAccessGroups.Text;
                CameraControl.Camobject.detector.recordonalert = rdoRecordAlert.Checked;
                CameraControl.Camobject.detector.recordondetect = rdoRecordDetect.Checked;

                CameraControl.UpdateFloorplans(false);

                CameraControl.Camobject.settings.audiomodel = ddlTalkModel.SelectedItem.ToString();
                CameraControl.Camobject.settings.audioport = (int)numTalkPort.Value;
                CameraControl.Camobject.settings.audioip = txtAudioOutIP.Text.Trim();
                CameraControl.Camobject.settings.audiousername = txtTalkUsername.Text;
                CameraControl.Camobject.settings.audiopassword = txtTalkPassword.Text;

                CameraControl.Camobject.alerts.trigger = ((ListItem) ddlTrigger.SelectedItem).Value;
                CameraControl.Camobject.recorder.trigger = ((ListItem)ddlTriggerRecording.SelectedItem).Value;

                DialogResult = DialogResult.OK;
                MainForm.NeedsSync = true;
                IsNew = false;
                Close();
            }
        }

        private static bool IsNumeric(IEnumerable<char> numberString)
        {
            return numberString.All(char.IsNumber);
        }

        private void ChkMovementCheckedChanged(object sender, EventArgs e)
        {
            pnlMovement.Enabled = (chkMovement.Checked);
            CameraControl.Camobject.alerts.active = chkMovement.Checked;
        }

        private void BtnDetectMovementClick(object sender, EventArgs e)
        {
            ofdDetect.FileName = "";
            ofdDetect.Filter = "";
            var initpath = "";
            if (txtExecuteMovement.Text.Trim()!="")
            {
                try
                {
                    var fi = new FileInfo(txtExecuteMovement.Text);
                    initpath = fi.DirectoryName;
                }
                catch {}
            }
            ofdDetect.InitialDirectory = initpath;
            ofdDetect.ShowDialog(this);
            if (ofdDetect.FileName != "")
            {
                txtExecuteMovement.Text = ofdDetect.FileName;
            }
        }

        private void ChkScheduleCheckedChanged(object sender, EventArgs e)
        {
            pnlScheduler.Enabled = chkSchedule.Checked;
            btnDelete.Enabled = btnUpdate.Enabled = lbSchedule.SelectedIndex > -1;
            lbSchedule.Refresh();
        }

        private void ChkSendSmsMovementCheckedChanged(object sender, EventArgs e)
        {
            if (chkSendSMSMovement.Checked)
                chkMMS.Checked = false;
        }

        private void ChkSendEmailMovementCheckedChanged(object sender, EventArgs e)
        {
        }

        private void TxtCameraNameKeyUp(object sender, KeyEventArgs e)
        {
            CameraControl.Camobject.name = txtCameraName.Text;
        }


        private void ChkActiveCheckedChanged(object sender, EventArgs e)
        {
            if (CameraControl.Camobject.settings.active != chkActive.Checked)
            {
                if (chkActive.Checked)
                {
                    CameraControl.Enable();
                    if (CameraControl.Camera != null)
                        CameraControl.Camera.NewFrame += CameraNewFrame;
                }
                else
                {
                    if (CameraControl.Camera != null)
                        CameraControl.Camera.NewFrame -= CameraNewFrame;
                    CameraControl.Disable();
                }
            }
            btnAdvanced.Enabled = btnCrossbar.Enabled = false;


            if (CameraControl.Camera != null && CameraControl.Camera.VideoSource is VideoCaptureDevice)
            {
                btnAdvanced.Enabled = true;
                btnCrossbar.Enabled = CameraControl.Camobject.settings.crossbarindex > -1 &&
                                      ((VideoCaptureDevice) CameraControl.Camera.VideoSource).CheckIfCrossbarAvailable();
            }
        }

        private void TxtCameraNameTextChanged(object sender, EventArgs e)
        {
            CameraControl.Camobject.name = txtCameraName.Text;
        }

        private void AddCameraFormClosing(object sender, FormClosingEventArgs e)
        {
            if (IsNew)
            {
                if (MessageBox.Show(this, "Discard this camera?", "Warning", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
                if (CameraControl.VolumeControl!=null)
                    ((MainForm)Owner).RemoveMicrophone(CameraControl.VolumeControl, false);
                    
            }
            if (CameraControl.Camera != null)
                CameraControl.Camera.NewFrame -= CameraNewFrame;
            AreaControl.Dispose();
            CameraControl.IsEdit = false;
            if (CameraControl.VolumeControl != null)
                CameraControl.VolumeControl.IsEdit = false;
        }

        private void DdlMovementDetectorSelectedIndexChanged1(object sender, EventArgs e)
        {
            ddlProcessor.Enabled = rdoMotion.Enabled = (string) _detectortypes[ddlMotionDetector.SelectedIndex] != "None";
            if (!rdoMotion.Enabled)
                rdoContinuous.Checked = true;

            if (CameraControl.Camera != null && CameraControl.Camera.VideoSource != null)
            {
                if ((string) _detectortypes[ddlMotionDetector.SelectedIndex] != CameraControl.Camobject.detector.type)
                {
                    CameraControl.Camobject.detector.type = (string) _detectortypes[ddlMotionDetector.SelectedIndex];
                    SetDetector();
                }
            }
            CameraControl.Camobject.detector.type = (string) _detectortypes[ddlMotionDetector.SelectedIndex];
        }

        private void SetDetector()
        {
            if (CameraControl.Camera == null)
                return;
            CameraControl.Camera.MotionDetector = null;
            switch (CameraControl.Camobject.detector.type)
            {
                case "Two Frames":
                    CameraControl.Camera.MotionDetector =
                        new MotionDetector(
                            new TwoFramesDifferenceDetector(CameraControl.Camobject.settings.suppressnoise));
                    SetProcessor();
                    break;
                case "Custom Frame":
                    CameraControl.Camera.MotionDetector =
                        new MotionDetector(
                            new CustomFrameDifferenceDetector(CameraControl.Camobject.settings.suppressnoise,
                                                              CameraControl.Camobject.detector.keepobjectedges));
                    SetProcessor();
                    break;
                case "Background Modelling":
                    CameraControl.Camera.MotionDetector =
                        new MotionDetector(
                            new SimpleBackgroundModelingDetector(CameraControl.Camobject.settings.suppressnoise,
                                                                 CameraControl.Camobject.detector.keepobjectedges));
                    SetProcessor();
                    break;
                case "Two Frames (Color)":
                    CameraControl.Camera.MotionDetector =
                        new MotionDetector(
                            new TwoFramesColorDifferenceDetector(CameraControl.Camobject.settings.suppressnoise));
                    SetProcessor();
                    break;
                case "Custom Frame (Color)":
                    CameraControl.Camera.MotionDetector =
                        new MotionDetector(
                            new CustomFrameColorDifferenceDetector(CameraControl.Camobject.settings.suppressnoise,
                                                              CameraControl.Camobject.detector.keepobjectedges));
                    SetProcessor();
                    break;
                case "Background Modelling (Color)":
                    CameraControl.Camera.MotionDetector =
                        new MotionDetector(
                            new SimpleColorBackgroundModelingDetector(CameraControl.Camobject.settings.suppressnoise,
                                                                 CameraControl.Camobject.detector.keepobjectedges));
                    SetProcessor();
                    break;
                case "None":
                    break;
            }
        }

        private void SetProcessor()
        {
            if (CameraControl.Camera == null || CameraControl.Camera.MotionDetector == null)
                return;
            CameraControl.Camera.MotionDetector.MotionProcessingAlgorithm = null;
            
            switch (CameraControl.Camobject.detector.postprocessor)
            {
                case "Grid Processing":
                    CameraControl.Camera.MotionDetector.MotionProcessingAlgorithm = new GridMotionAreaProcessing
                                   {
                                       HighlightColor = ColorTranslator.FromHtml(CameraControl.Camobject.detector.color),
                                       HighlightMotionGrid = CameraControl.Camobject.detector.highlight
                                   };
                    break;
                case "Object Tracking":
                    CameraControl.Camera.MotionDetector.MotionProcessingAlgorithm = new BlobCountingObjectsProcessing
                                    {
                                        HighlightColor = ColorTranslator.FromHtml(CameraControl.Camobject.detector.color),
                                        HighlightMotionRegions = CameraControl.Camobject.detector.highlight,
                                        MinObjectsHeight = CameraControl.Camobject.detector.minheight,
                                        MinObjectsWidth = CameraControl.Camobject.detector.minwidth
                                    };

                    break;
                case "Border Highlighting":
                    CameraControl.Camera.MotionDetector.MotionProcessingAlgorithm = new MotionBorderHighlighting
                                    {
                                        HighlightColor = ColorTranslator.FromHtml(CameraControl.Camobject.detector.color)
                                    };
                    break;
                case "Area Highlighting":
                    CameraControl.Camera.MotionDetector.MotionProcessingAlgorithm = new MotionAreaHighlighting
                                    {
                                        HighlightColor = ColorTranslator.FromHtml(CameraControl.Camobject.detector.color)
                                    };
                    break;
                case "None":
                    break;
            }
        }

        private void ChkSuppressNoiseCheckedChanged(object sender, EventArgs e)
        {
            if (CameraControl.Camera != null && CameraControl.Camera.VideoSource != null)
            {
                if (CameraControl.Camobject.settings.suppressnoise != chkSuppressNoise.Checked)
                {
                    CameraControl.Camobject.settings.suppressnoise = chkSuppressNoise.Checked;
                    SetDetector();
                }
            }
        }


        private void Button2Click(object sender, EventArgs e)
        {
            GoPrevious();
        }

        private void TcCameraSelectedIndexChanged(object sender, EventArgs e)
        {
            btnBack.Enabled = tcCamera.SelectedIndex != 0;

            btnNext.Enabled = tcCamera.SelectedIndex != tcCamera.TabCount - 1;
        }

        private void Button1Click1(object sender, EventArgs e)
        {
            AreaControl.ClearRectangles();
            if (CameraControl.Camera != null && CameraControl.Camera.MotionDetector != null)
            {
                CameraControl.Camera.ClearMotionZones();
            }
        }

        private void DdlProcessorSelectedIndexChanged(object sender, EventArgs e)
        {
            if (CameraControl.Camera != null && CameraControl.Camera.VideoSource != null &&
                CameraControl.Camera.MotionDetector != null)
            {
                if ((string) _processortypes[ddlProcessor.SelectedIndex] != CameraControl.Camobject.detector.postprocessor)
                {
                    CameraControl.Camobject.detector.postprocessor = (string) _processortypes[ddlProcessor.SelectedIndex];
                    SetProcessor();
                }
            }
            CameraControl.Camobject.detector.postprocessor = (string) _processortypes[ddlProcessor.SelectedIndex];
        }

        private void Button2Click1(object sender, EventArgs e)
        {
            List<objectsCameraScheduleEntry> scheds = CameraControl.Camobject.schedule.entries.ToList();
            var sched = new objectsCameraScheduleEntry();
            if (ConfigureSchedule(sched))
            {
                scheds.Add(sched);
                CameraControl.Camobject.schedule.entries = scheds.ToArray();
                ShowSchedule(CameraControl.Camobject.schedule.entries.Count() - 1);
            }
        }

        private bool ConfigureSchedule(objectsCameraScheduleEntry sched)
        {
            if (ddlHourStart.SelectedItem.ToString() != "-" && ddlMinuteStart.SelectedItem.ToString() == "-")
            {
                ddlMinuteStart.SelectedIndex = 1;
            }
            if (ddlHourEnd.SelectedItem.ToString() != "-" && ddlMinuteEnd.SelectedItem.ToString() == "-")
            {
                ddlMinuteEnd.SelectedIndex = 1;
            }

            if (ddlHourStart.SelectedItem.ToString() == "-" || ddlMinuteStart.SelectedItem.ToString() == "-")
            {
                sched.start = "-:-";
            }
            else
                sched.start = ddlHourStart.SelectedItem + ":" + ddlMinuteStart.SelectedItem;
            if (ddlHourEnd.SelectedItem.ToString() == "-" || ddlMinuteEnd.SelectedItem.ToString() == "-")
            {
                sched.stop = "-:-";
            }
            else
                sched.stop = ddlHourEnd.SelectedItem + ":" + ddlMinuteEnd.SelectedItem;

            sched.daysofweek = "";
            if (chkMon.Checked)
            {
                sched.daysofweek += "1,";
            }
            if (chkTue.Checked)
            {
                sched.daysofweek += "2,";
            }
            if (chkWed.Checked)
            {
                sched.daysofweek += "3,";
            }
            if (chkThu.Checked)
            {
                sched.daysofweek += "4,";
            }
            if (chkFri.Checked)
            {
                sched.daysofweek += "5,";
            }
            if (chkSat.Checked)
            {
                sched.daysofweek += "6,";
            }
            if (chkSun.Checked)
            {
                sched.daysofweek += "0,";
            }
            sched.daysofweek = sched.daysofweek.Trim(',');
            if (sched.daysofweek == "")
            {
                MessageBox.Show(LocRm.GetString("Validate_Camera_SelectOneDay"));
                return false;
            }

            sched.recordonstart = chkRecordSchedule.Checked;
            sched.active = chkScheduleActive.Checked;
            sched.recordondetect = chkScheduleRecordOnDetect.Checked;
            sched.recordonalert = chkRecordAlertSchedule.Checked;
            sched.alerts = chkScheduleAlerts.Checked;
            sched.timelapseenabled = chkScheduleTimelapse.Checked;
            sched.ftpenabled = chkSchedFTPEnabled.Checked;
            sched.savelocalenabled = chkSchedSaveLocalEnabled.Checked;
            return true;
        }

        private void LbScheduleKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSchedule();
            }
        }

        private void DeleteSchedule()
        {
            if (lbSchedule.SelectedIndex > -1)
            {
                int i = lbSchedule.SelectedIndex;
                List<objectsCameraScheduleEntry> scheds = CameraControl.Camobject.schedule.entries.ToList();
                scheds.RemoveAt(i);
                CameraControl.Camobject.schedule.entries = scheds.ToArray();
                int j = i;
                if (j == scheds.Count)
                    j--;
                if (j < 0)
                    j = 0;
                ShowSchedule(j);
                if (lbSchedule.Items.Count == 0)
                    btnDelete.Enabled = btnUpdate.Enabled = false;
                else
                    btnDelete.Enabled = btnUpdate.Enabled = (lbSchedule.SelectedIndex > -1);
            }
        }

        private void DdlHourStartSelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void LinkLabel1LinkClicked1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl(MainForm.Website + "/userguide-motion-detection.aspx#2");
        }

        private void BtnSaveFtpClick(object sender, EventArgs e)
        {
            btnSaveFTP.Enabled = false;
            Application.DoEvents();

            using (var imageStream = new MemoryStream())
            {

                Image.GetThumbnailImageAbort myCallback = ThumbnailCallback;
                Image myThumbnail = null;

                try
                {
                    if (CameraControl.Camera != null && CameraControl.Camera.LastFrame != null)
                        myThumbnail = CameraControl.Camera.LastFrame;
                        //CameraControl.Camera.LastFrame.GetThumbnailImage(320, 240, myCallback, IntPtr.Zero);
                    else
                        myThumbnail =
                            Image.FromFile(Program.AppDataPath + @"WebServerRoot\images\camoffline.jpg").
                                GetThumbnailImage(
                                    320, 240, myCallback, IntPtr.Zero);

                    // put the image into the memory stream
                    if (MainForm.Encoder != null)
                    {
                        //  Set the quality
                        var parameters = new EncoderParameters(1);
                        parameters.Param[0] = new EncoderParameter(Encoder.Quality, tbFTPQuality.Value);
                        myThumbnail.Save(imageStream, MainForm.Encoder, parameters);
                    }

                    string error;
                    txtFTPServer.Text = txtFTPServer.Text.Trim('/');
                    string fn = String.Format(CultureInfo.InvariantCulture, txtFTPFilename.Text,
                                              DateTime.Now);
                    if ((new AsynchronousFtpUpLoader()).FTP(txtFTPServer.Text + ":" + txtFTPPort.Text,
                                                            chkUsePassive.Checked,
                                                            txtFTPUsername.Text, txtFTPPassword.Text, fn,0,
                                                            imageStream.ToArray(), out error))
                    {
                        MessageBox.Show(LocRm.GetString("ImageUploaded"), LocRm.GetString("Success"));
                    }
                    else
                        MessageBox.Show(string.Format("{0}: {1}", LocRm.GetString("UploadFailed"), error), LocRm.GetString("Failed"));
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    MessageBox.Show(ex.Message);
                }
                if (myThumbnail != null)
                    myThumbnail.Dispose();
                imageStream.Close();
            }
            btnSaveFTP.Enabled = true;
        }

        private static bool ThumbnailCallback()
        {
            return false;
        }

        private void CheckBox1CheckedChanged(object sender, EventArgs e)
        {
            gbFTP.Enabled = chkFTP.Checked;
        }

        private void LinkLabel2LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl( MainForm.Website+"/userguide-ftp.aspx");
        }

        private void DdlProcessFramesSelectedIndexChanged(object sender, EventArgs e)
        {
            CameraControl.Camobject.detector.processeveryframe = Convert.ToInt32(ddlProcessFrames.SelectedItem);
        }

        private void Login()
        {
            ((MainForm) Owner).Connect(MainForm.Website+"/subscribe.aspx", false);
            gpbSubscriber.Enabled = gpbSubscriber2.Enabled = MainForm.Conf.Subscribed;
        }


        private void Button3Click(object sender, EventArgs e)
        {
            DeleteSchedule();
        }

        private void LbScheduleSelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbSchedule.Items.Count == 0)
                btnDelete.Enabled = btnUpdate.Enabled = false;
            else
            {
                btnUpdate.Enabled = btnDelete.Enabled = (lbSchedule.SelectedIndex > -1);
                if (btnUpdate.Enabled)
                {
                    int i = lbSchedule.SelectedIndex;
                    objectsCameraScheduleEntry sched = CameraControl.Camobject.schedule.entries[i];

                    string[] start = sched.start.Split(':');
                    string[] stop = sched.stop.Split(':');


                    ddlHourStart.SelectedItem = start[0];
                    ddlHourEnd.SelectedItem = stop[0];
                    ddlMinuteStart.SelectedItem = start[1];
                    ddlMinuteEnd.SelectedItem = stop[1];

                    chkMon.Checked = sched.daysofweek.IndexOf("1", StringComparison.Ordinal) != -1;
                    chkTue.Checked = sched.daysofweek.IndexOf("2", StringComparison.Ordinal) != -1;
                    chkWed.Checked = sched.daysofweek.IndexOf("3", StringComparison.Ordinal) != -1;
                    chkThu.Checked = sched.daysofweek.IndexOf("4", StringComparison.Ordinal) != -1;
                    chkFri.Checked = sched.daysofweek.IndexOf("5", StringComparison.Ordinal) != -1;
                    chkSat.Checked = sched.daysofweek.IndexOf("6", StringComparison.Ordinal) != -1;
                    chkSun.Checked = sched.daysofweek.IndexOf("0", StringComparison.Ordinal) != -1;

                    chkRecordSchedule.Checked = sched.recordonstart;
                    chkScheduleActive.Checked = sched.active;
                    chkScheduleRecordOnDetect.Checked = sched.recordondetect;
                    chkScheduleAlerts.Checked = sched.alerts;
                    chkRecordAlertSchedule.Checked = sched.recordonalert;
                    chkScheduleTimelapse.Checked = sched.timelapseenabled;
                    chkSchedFTPEnabled.Checked = sched.ftpenabled;
                    chkSchedSaveLocalEnabled.Checked = sched.savelocalenabled;
                }
            }
        }

        private void PnlPtzMouseDown(object sender, MouseEventArgs e)
        {
            ProcessPtzInput(e.Location);
        }


        private void ProcessPtzInput(Point p)
        {
            var comm = Enums.PtzCommand.Center;
            if (p.X < 60 && p.Y > 60 && p.Y < 106)
            {
                comm = Enums.PtzCommand.Left;
            }
            if (p.X < 60 && p.Y < 60)
            {
                comm = Enums.PtzCommand.Upleft;
            }
            if (p.X > 60 && p.X < 104 && p.Y < 60)
            {
                comm = Enums.PtzCommand.Up;
            }
            if (p.X > 104 && p.X < 164 && p.Y < 60)
            {
                comm = Enums.PtzCommand.UpRight;
            }
            if (p.X > 104 && p.X < 170 && p.Y > 60 && p.Y < 104)
            {
                comm = Enums.PtzCommand.Right;
            }
            if (p.X > 104 && p.X < 170 && p.Y > 104)
            {
                comm = Enums.PtzCommand.DownRight;
            }
            if (p.X > 60 && p.X < 104 && p.Y > 104)
            {
                comm = Enums.PtzCommand.Down;
            }
            if (p.X < 60 && p.Y > 104)
            {
                comm = Enums.PtzCommand.DownLeft;
            }

            if (p.X > 170 && p.Y < 45)
            {
                comm = Enums.PtzCommand.ZoomIn;
            }
            if (p.X > 170 && p.Y > 45 && p.Y < 90)
            {
                comm = Enums.PtzCommand.ZoomOut;
            }

            CameraControl.PTZ.SendPTZCommand(comm);
        }

        private void DdlPtzSelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddlPTZ.SelectedIndex > 0)
            {
                var entry = (PTZEntry) ddlPTZ.SelectedItem;
                CameraControl.Camobject.ptz = entry.Id;
                CameraControl.Camobject.ptzentryindex = entry.Index;
            }
            else
            {
                CameraControl.Camobject.ptz = -1;
                CameraControl.Camobject.ptzentryindex = -1;
                CameraControl.PTZ.PTZSettings = null;
            }
           

            lbExtended.Items.Clear();
            ddlScheduleCommand.Items.Clear();
            ddlHomeCommand.Items.Clear();

            ddlHomeCommand.Items.Add(new ListItem("Center", "Center"));

            if (CameraControl.Camobject.ptz > -1)
            {
                PTZSettings2Camera ptz = MainForm.PTZs.Single(p => p.id == CameraControl.Camobject.ptz);
                CameraControl.PTZ.PTZSettings = ptz;
                if (ptz.ExtendedCommands != null && ptz.ExtendedCommands.Command!=null)
                {
                    foreach (var extcmd in ptz.ExtendedCommands.Command)
                    {
                        lbExtended.Items.Add(new ListItem(extcmd.Name, extcmd.Value));
                        ddlScheduleCommand.Items.Add(new ListItem(extcmd.Name, extcmd.Value));
                        ddlHomeCommand.Items.Add(new ListItem(extcmd.Name, extcmd.Value));
                        if (CameraControl.Camobject.settings.ptzautohomecommand == extcmd.Value)
                        {
                            ddlHomeCommand.SelectedIndex = ddlHomeCommand.Items.Count - 1;
                        }
                    }
                }
                if (_loaded)    
                    txtPTZURL.Text = ptz.CommandURL;
            }
            if (ddlScheduleCommand.Items.Count > 0)
                ddlScheduleCommand.SelectedIndex = 0;

            if (ddlHomeCommand.SelectedIndex==-1)
            {
                ddlHomeCommand.SelectedIndex = 0;
            }

            pnlPTZControls.Enabled = CameraControl.Camobject.ptz > -1;   
            if (!pnlPTZControls.Enabled)
                chkTrack.Checked = false;
        }

        private void PnlPtzPaint(object sender, PaintEventArgs e)
        {
        }

        private void LbExtendedClick(object sender, EventArgs e)
        {
            if (lbExtended.SelectedIndex > -1)
            {
                var li = ((ListItem) lbExtended.SelectedItem);
                SendPtzCommand(li.Value, true);
            }
        }


        private void PnlPtzMouseUp(object sender, MouseEventArgs e)
        {
            PTZSettings2Camera ptz = MainForm.PTZs.SingleOrDefault(p => p.id == CameraControl.Camobject.ptz);
            if (ptz != null && ptz.Commands.Stop!="")
                SendPtzCommand(ptz.Commands.Stop,true);
        }

        private void SendPtzCommand(string cmd, bool wait)
        {
            if (cmd == "")
            {
                MessageBox.Show(LocRm.GetString("CommandNotSupported"));
                return;
            }
            try
            {
                CameraControl.PTZ.SendPTZCommand(cmd, wait);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocRm.GetString("Validate_Camera_PTZIPOnly") + Environment.NewLine + Environment.NewLine +
                    ex.Message, LocRm.GetString("Error"));
            }
        }

        private void PnlPtzMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                //todo: add drag to move cam around
            }
        }

        private void LbExtendedSelectedIndexChanged(object sender, EventArgs e)
        {
        }

        //private void ChkUploadYouTubeCheckedChanged(object sender, EventArgs e)
        //{
        //    if (chkUploadYouTube.Checked)
        //    {
        //        if (string.IsNullOrEmpty(MainForm.Conf.YouTubeUsername))
        //        {
        //            if (
        //                MessageBox.Show(LocRm.GetString("Validate_Camera_YouTubeDetails"), LocRm.GetString("Confirm"),
        //                                MessageBoxButtons.OKCancel) == DialogResult.OK)
        //            {
        //                string lang = MainForm.Conf.Language;
        //                ((MainForm) Owner).ShowSettings(3);
        //                if (lang != MainForm.Conf.Language)
        //                    RenderResources();
        //            }
        //        }
        //    }

        //    if (string.IsNullOrEmpty(MainForm.Conf.YouTubeUsername))
        //    {
        //        chkUploadYouTube.Checked = false;
        //    }
        //}

        private void LinkLabel6LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var d = new downloader
            {
                Url = MainForm.Website + "/getcontent.aspx?name=PTZ2",
                SaveLocation = Program.AppDataPath + @"XML\PTZ2.xml"
            };
            d.ShowDialog(this);
            if (d.DialogResult == DialogResult.OK)
            {
                MainForm.PTZs = null;
                LoadPTZs();
            }
            d.Dispose();
        }

        private void TabPage9Click(object sender, EventArgs e)
        {
        }

        private void LinkLabel7LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string lang = MainForm.Conf.Language;
            ((MainForm) Owner).ShowSettings(3);
            if (lang != MainForm.Conf.Language)
                RenderResources();
        }

        private void LinkLabel4LinkClicked1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Login();
        }

        private void DdlTimestampKeyUp(object sender, KeyEventArgs e)
        {
            CameraControl.Camobject.settings.timestampformatter = ddlTimestamp.Text;
        }

        private void BtnMaskImageClick(object sender, EventArgs e)
        {
            ofdDetect.FileName = "";
            ofdDetect.InitialDirectory = Program.AppPath + @"backgrounds\";
            ofdDetect.Filter = "Image Files (*.png)|*.png";
            ofdDetect.ShowDialog(this);
            if (ofdDetect.FileName != "")
            {
                txtMaskImage.Text = ofdDetect.FileName;
            }
        }

        private void TxtMaskImageTextChanged(object sender, EventArgs e)
        {
            if (File.Exists(txtMaskImage.Text))
            {
                try
                {
                    CameraControl.Camobject.settings.maskimage = txtMaskImage.Text;
                    if (CameraControl.Camera != null)
                        CameraControl.Camera.Mask = Image.FromFile(txtMaskImage.Text);
                }
                catch
                {
                }
            }
            else
            {
                CameraControl.Camobject.settings.maskimage = "";
                if (CameraControl.Camera!=null)
                    CameraControl.Camera.Mask = null;
            }
        }

        private void LinkLabel5LinkClicked1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl( MainForm.Website+"/countrycodes.aspx");
        }

        private void ChkEmailOnDisconnectCheckedChanged(object sender, EventArgs e)
        {
        }

        private void ChkFlipYCheckedChanged(object sender, EventArgs e)
        {
            CameraControl.Camobject.flipy = chkFlipY.Checked;
        }

        private void ChkFlipXCheckedChanged(object sender, EventArgs e)
        {
            CameraControl.Camobject.flipx = chkFlipX.Checked;
        }

        private void BtnUpdateClick(object sender, EventArgs e)
        {
            int i = lbSchedule.SelectedIndex;
            objectsCameraScheduleEntry sched = CameraControl.Camobject.schedule.entries[i];

            if (ConfigureSchedule(sched))
            {
                ShowSchedule(i);
            }
        }

        private void ChkScheduleActiveCheckedChanged(object sender, EventArgs e)
        {
        }

        private void LbScheduleDrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            int i = e.Index;
            if (i >= 0)
            {
                objectsCameraScheduleEntry sched = CameraControl.Camobject.schedule.entries[i];

                Font f = sched.active ? new Font("Microsoft Sans Serif", 8.25f, FontStyle.Bold) : new Font("Microsoft Sans Serif", 8.25f, FontStyle.Regular);
                Brush b = !chkSchedule.Checked ? Brushes.Gray : Brushes.Black;

                e.Graphics.DrawString(lbSchedule.Items[i].ToString(), f, b, e.Bounds);
                e.DrawFocusRectangle();
            }
        }


        private void ChkRecordScheduleCheckedChanged(object sender, EventArgs e)
        {
            if (chkRecordSchedule.Checked)
            {
                chkScheduleRecordOnDetect.Checked = false;
                chkRecordAlertSchedule.Checked = false;
            }
        }

        private void ChkScheduleRecordOnDetectCheckedChanged(object sender, EventArgs e)
        {
            if (chkScheduleRecordOnDetect.Checked)
            {
                chkRecordSchedule.Checked = false;
                chkRecordAlertSchedule.Checked = false;
            }
        }

        private void LinkLabel8LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl( MainForm.Website+"/userguide-pairing.aspx");
        }

        private void ChkRecordAlertScheduleCheckedChanged(object sender, EventArgs e)
        {
            if (chkRecordAlertSchedule.Checked)
            {
                chkRecordSchedule.Checked = false;
                chkScheduleRecordOnDetect.Checked = false;
                chkScheduleAlerts.Checked = true;
            }
        }

        private void ChkScheduleAlertsCheckedChanged(object sender, EventArgs e)
        {
            if (!chkScheduleAlerts.Checked)
                chkRecordAlertSchedule.Checked = false;
        }

        private void ChkMmsCheckedChanged(object sender, EventArgs e)
        {
            if (chkMMS.Checked)
                chkSendSMSMovement.Checked = false;
        }

        private void RdoFtpIntervalCheckedChanged(object sender, EventArgs e)
        {
            txtUploadEvery.Enabled = rdoFTPInterval.Checked;
        }

        private void rdoFTPAlerts_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void Button3Click3(object sender, EventArgs e)
        {
            ConfigureSeconds cf;
            switch (ddlAlertMode.SelectedIndex)
            {
                case 0:
                    cf = new ConfigureSeconds
                             {
                                 Seconds = CameraControl.Camobject.detector.movementinterval
                             };
                    cf.ShowDialog(this);
                    if (cf.DialogResult == DialogResult.OK)
                        CameraControl.Camobject.detector.movementinterval = cf.Seconds;
                    cf.Dispose();
                    break;
                case 1:
                    cf = new ConfigureSeconds
                             {
                                 Seconds = CameraControl.Camobject.detector.nomovementinterval
                             };
                    cf.ShowDialog(this);
                    if (cf.DialogResult == DialogResult.OK)
                        CameraControl.Camobject.detector.nomovementinterval = cf.Seconds;
                    cf.Dispose();
                    break;
                case 2:
                    var coc = new ConfigureObjectCount
                    {
                        Objects = CameraControl.Camobject.alerts.objectcountalert
                    };
                    coc.ShowDialog(this);

                    if (coc.DialogResult == DialogResult.OK)
                        CameraControl.Camobject.alerts.objectcountalert = coc.Objects;
                    coc.Dispose();
                    break;
                default:
                    switch (ddlAlertMode.SelectedItem.ToString())
                    {
                        case  "Virtual Trip Wires":
                            _ctw = new ConfigureTripWires();
                            _ctw.TripWireEditor1.Init(CameraControl.Camobject.alerts.pluginconfig);
                            _ctw.ShowDialog(this);
                            CameraControl.Camobject.alerts.pluginconfig = _ctw.TripWireEditor1.Config;
                            if (CameraControl.Camera!=null && CameraControl.Camera.VideoSource is KinectStream)
                            {
                                ((KinectStream) CameraControl.Camera.VideoSource).InitTripWires(
                                    CameraControl.Camobject.alerts.pluginconfig);
                            }
                            _ctw.Dispose();
                            break;
                        default:
                            if (CameraControl.Camera != null && CameraControl.Camera.Plugin != null)
                            {
                                var o = CameraControl.Camera.Plugin.GetType();
                                var config = (string)o.GetMethod("Configure").Invoke(CameraControl.Camera.Plugin, null);

                                CameraControl.Camobject.alerts.pluginconfig = config;
                            }
                            else
                            {
                                MessageBox.Show(this, "You need to initialise the camera before you can configure the plugin.");
                            }
                            break;
                    }
                    
                    
                    break;
            }
        }        

        private void DdlAlertModeSelectedIndexChanged(object sender, EventArgs e)
        {
            string last = CameraControl.Camobject.alerts.mode;
            flowLayoutPanel5.Enabled = ddlAlertMode.SelectedIndex > _alertmodes.Length-1;
            if (!flowLayoutPanel5.Enabled)
                rdoContinuous.Checked = true;
            if (ddlAlertMode.SelectedIndex < _alertmodes.Length)
            {
                CameraControl.Camobject.alerts.mode = _alertmodes[ddlAlertMode.SelectedIndex];
                if (ddlAlertMode.SelectedIndex==2)
                {
                    ddlProcessor.SelectedIndex = 1;
                }
            }
            else
            {
                CameraControl.Camobject.alerts.mode = ddlAlertMode.SelectedItem.ToString();
            }
            if (last != ddlAlertMode.SelectedItem.ToString())
            {
                if (CameraControl.Camera != null && CameraControl.Camera.Plugin != null)
                {
                    CameraControl.Camera.Plugin = null;
                    CameraControl.Camobject.alerts.pluginconfig = "";
                }
            }
            button3.Enabled = true;
        }

        private void ChkTimelapseCheckedChanged(object sender, EventArgs e)
        {
            groupBox1.Enabled = chkTimelapse.Checked;
        }

        private void chkPublic_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void LinkLabel9LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Login();
        }

        #region Nested type: ListItem

        private struct ListItem
        {
            private readonly string _name;
            internal readonly string Value;

            public ListItem(string name, string value)
            {
                _name = name;
                Value = value;
            }

            public override string ToString()
            {
                return _name;
            }
        }

        #endregion

        private void chkRotate_CheckedChanged(object sender, EventArgs e)
        {
            bool changed = CameraControl.Camobject.rotate90 != chkRotate.Checked;
            CameraControl.Camobject.rotate90 = chkRotate.Checked;
            if (changed)
            {
                CameraControl.NeedSizeUpdate = true;
                if (CameraControl.Camobject.settings.active)
                {
                    chkActive.Enabled = true;
                    chkActive.Checked = false;
                    Thread.Sleep(500); //allows unmanaged code to complete shutdown
                    chkActive.Checked = true;
                    CameraControl.NeedSizeUpdate = true;
                }
            }           
        }

        private void chkPTZFlipX_CheckedChanged(object sender, EventArgs e)
        {
            CameraControl.Camobject.settings.ptzflipx = chkPTZFlipX.Checked;
        }

        private void chkPTZFlipY_CheckedChanged(object sender, EventArgs e)
        {
            CameraControl.Camobject.settings.ptzflipy = chkPTZFlipY.Checked;
        }

        private void chkPTZRotate90_CheckedChanged(object sender, EventArgs e)
        {
            CameraControl.Camobject.settings.ptzrotate90 = chkPTZRotate90.Checked;
        }

        private void label16_Click(object sender, EventArgs e)
        {

        }

        private void txtPTZURL_TextChanged(object sender, EventArgs e)
        {
            CameraControl.Camobject.settings.ptzurlbase = txtPTZURL.Text;
        }

        private void numMaxFR_ValueChanged(object sender, EventArgs e)
        {
            CameraControl.Camobject.settings.maxframerate = (int)numMaxFR.Value;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var cp = new ConfigureProcessor(CameraControl);
            if (cp.ShowDialog(this)== DialogResult.OK)
            {
                if (CameraControl.Camera != null && CameraControl.Camera.MotionDetector != null)
                {
                    SetDetector();
                }
            }
            cp.Dispose();
        }

        private void chkTrack_CheckedChanged(object sender, EventArgs e)
        {
            pnlTrack.Enabled = chkTrack.Checked;
            if (chkTrack.Checked)
            {
                ddlMotionDetector.SelectedIndex = 0;
                ddlProcessor.SelectedIndex = 1;
                CameraControl.Camobject.settings.ptzautotrack = true;
                CameraControl.Camobject.detector.highlight = false;
                SetDetector();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ConfigFilter();
        }

        private void chkColourProcessing_CheckedChanged(object sender, EventArgs e)
        {
            if (chkColourProcessing.Checked)
            {
                if (String.IsNullOrEmpty(CameraControl.Camobject.detector.colourprocessing))
                {
                    if (!ConfigFilter())
                        chkColourProcessing.Checked = false;
                }
            }
            CameraControl.Camobject.detector.colourprocessingenabled = chkColourProcessing.Checked;
        }

        private bool ConfigFilter()
        {
            _filterForm = new HSLFilteringForm(CameraControl.Camobject.detector.colourprocessing) { ImageProcess = CameraControl.Camera==null?null: CameraControl.Camera.LastFrame };
            _filterForm.ShowDialog(this);
            if (_filterForm.DialogResult == DialogResult.OK)
            {
                CameraControl.Camobject.detector.colourprocessing = _filterForm.Configuration;
                if (CameraControl.Camera!=null)
                    CameraControl.Camera.FilterChanged();
                _filterForm.Dispose();
                _filterForm = null;
                chkColourProcessing.Checked = true;
                return true;
            }

            _filterForm.Dispose();
            _filterForm = null;
            return false;
        }

        private void AddCamera_HelpButtonClicked(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MainForm.OpenUrl( MainForm.Website+"/userguide-camera-settings.aspx");
        }

        private void llblHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = MainForm.Website+"/userguide-camera-settings.aspx";
            switch (tcCamera.SelectedIndex)
            {
                case 0:
                    url=MainForm.Website+"/userguide-camera-settings.aspx";
                    break;
                case 1:
                    url = MainForm.Website+"/userguide-motion-detection.aspx";
                    break;
                case 2:
                    url = MainForm.Website+"/userguide-alerts.aspx";
                    break;
                case 3:
                    url = MainForm.Website+"/userguide-recording.aspx";
                    break;
                case 4:
                    url = MainForm.Website+"/userguide-ptz.aspx";
                    break;
                case 5:
                    url = MainForm.Website+"/userguide-ftp.aspx";
                    break;
                case 6:
                    url = MainForm.Website+"/userguide-youtube.aspx";
                    break;
                case 7:
                    url = MainForm.Website+"/userguide-scheduling.aspx";
                    break;
            }
            MainForm.OpenUrl( url);
        }

        private void btnTimestamp_Click(object sender, EventArgs e)
        {
            var ct = new ConfigureTimestamp
                         {
                             TimeStampLocation = CameraControl.Camobject.settings.timestamplocation,
                             FontSize = CameraControl.Camobject.settings.timestampfontsize,
                             Offset = CameraControl.Camobject.settings.timestampoffset
                         };
            if (ct.ShowDialog(this)== DialogResult.OK)
            {
                CameraControl.Camobject.settings.timestamplocation = ct.TimeStampLocation;
                CameraControl.Camobject.settings.timestampfontsize = ct.FontSize;
                CameraControl.Camobject.settings.timestampoffset = ct.Offset;

                if (CameraControl.Camera!=null) 
                    CameraControl.Camera.Drawfont = null;
            }
            ct.Dispose();
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl(MainForm.Website+"/plugins.aspx");
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void rdoContinuous_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void ddlCopyFrom_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddlCopyFrom.SelectedIndex>0)
            {
                var cam =
                    MainForm.Cameras.SingleOrDefault(
                        p => p.id == Convert.ToInt32(((ListItem) ddlCopyFrom.SelectedItem).Value));
                if (cam!=null)
                {
                    List<objectsCameraScheduleEntry> scheds = cam.schedule.entries.ToList();

                    CameraControl.Camobject.schedule.entries = scheds.ToArray();
                    ShowSchedule(CameraControl.Camobject.schedule.entries.Count() - 1);                    
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (ddlScheduleCommand.SelectedIndex>-1)
            {
                if (ddlScheduleCommand.SelectedIndex > -1)
                {
                    var cmd = ddlScheduleCommand.SelectedItem.ToString();
                    var time = dtpSchedulePTZ.Value;
                    var s = new objectsCameraPtzscheduleEntry {command = cmd, time = time};
                    List<objectsCameraPtzscheduleEntry> scheds = CameraControl.Camobject.ptzschedule.entries.ToList();
                    scheds.Add(s);
                    CameraControl.Camobject.ptzschedule.entries = scheds.ToArray();
                    ShowPTZSchedule();
                }
            }
        }

        private void chkSchedulePTZ_CheckedChanged(object sender, EventArgs e)
        {
            tableLayoutPanel20.Enabled = chkSchedulePTZ.Checked;
        }

        private void btnDeletePTZ_Click(object sender, EventArgs e)
        {
            int i = lbPTZSchedule.SelectedIndex;
            if (i>-1)
            {
                var s = CameraControl.Camobject.ptzschedule.entries.ToList().OrderBy(p => p.time).ToList();
                s.RemoveAt(i);
                CameraControl.Camobject.ptzschedule.entries = s.ToArray();
                ShowPTZSchedule();
            }
        }

        private void chkRestore_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void llblEditPTZ_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("Notepad.exe", Program.AppDataPath + @"XML\PTZ2.xml");
        }

        private void linkLabel10_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.PTZs = null;
            LoadPTZs();
        }

        private void chkCRF_CheckedChanged(object sender, EventArgs e)
        {
            CameraControl.Camobject.recorder.crf = chkCRF.Checked;
            tbQuality.Enabled = !chkCRF.Checked;
        }

        private void txtBuffer_ValueChanged(object sender, EventArgs e)
        {

        }

        private void linkLabel11_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string path = MainForm.Conf.MediaDirectory + "video\\" + txtDirectory.Text + "\\";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = MainForm.Conf.MediaDirectory + "video\\" + txtDirectory.Text + "\\grabs\\";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            Process.Start(path);
        }

        private void ddlHomeCommand_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddlHomeCommand.SelectedIndex > -1)
            {
                var li = ((ListItem) ddlHomeCommand.SelectedItem);
                CameraControl.Camobject.settings.ptzautohomecommand = li.Value;
            }
        }

        private void ddlProfile_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddlProfile.SelectedIndex > 2)
            {
                chkCRF.Enabled = true;
                chkCRF.Checked = false;
            }
            else
            {
                chkCRF.Enabled = false;
                chkCRF.Checked = true;
            }
        }

        private void linkLabel12_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl(MainForm.Webserver + "/account.aspx?task=twitter-auth");
        }

        private void btnAdvanced_Click(object sender, EventArgs e)
        {
            try
            {
                ((VideoCaptureDevice) CameraControl.Camera.VideoSource).DisplayPropertyPage(Handle);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnCrossbar_Click(object sender, EventArgs e)
        {
            try {
                ((VideoCaptureDevice)CameraControl.Camera.VideoSource).DisplayCrossbarPropertyPage(Handle);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnMic_Click(object sender, EventArgs e)
        {
            var cms = new CameraMicSource
                          {
                              CameraControl = this.CameraControl,
                              StartPosition = FormStartPosition.CenterParent
                          };
            cms.ShowDialog(this);
            if (CameraControl.Camobject.settings.micpair>-1)
            {
                var m = MainForm.Microphones.SingleOrDefault(p => p.id == CameraControl.Camobject.settings.micpair);
                if (m != null)
                {
                    lblMicSource.Text = m.name;
                    m.settings.buffer = CameraControl.Camobject.recorder.bufferseconds;
                }
            }
            else
            {
                lblMicSource.Text = LocRm.GetString("None");
            }
        }

        private void ddlTimestamp_SelectedIndexChanged(object sender, EventArgs e)
        {
            CameraControl.Camobject.settings.timestampformatter = ddlTimestamp.Text;
        }

        private void chkLocalSaving_CheckedChanged(object sender, EventArgs e)
        {
            gbLocal.Enabled = chkLocalSaving.Checked;
        }

        private void PopulateCodecsCombo()
        {
            var models = new [] {"None", "Axis", "Foscam", "iSpyServer", "NetworkKinect"};
            foreach(string m in models)
            {
                ddlTalkModel.Items.Add(m);
            }
            ddlTalkModel.SelectedItem = CameraControl.Camobject.settings.audiomodel;
        }

        private void linkLabel13_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string lang = MainForm.Conf.Language;
            ((MainForm)Owner).ShowSettings(6);
            if (lang != MainForm.Conf.Language)
                RenderResources();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            int i = lbPTZSchedule.SelectedIndex;
            if (i > -1)
            {
                var s = CameraControl.Camobject.ptzschedule.entries.ToList().OrderBy(p => p.time).ToList();
                var si = s[i];
                var cr = new ConfigureRepeat {Interval = 60, Until = si.time};
                if (cr.ShowDialog(this)== DialogResult.OK)
                {
                    var dtCurrent = si.time.AddSeconds(cr.Interval);
                    while (dtCurrent.TimeOfDay < cr.Until.TimeOfDay)
                    {
                        s.Add(new objectsCameraPtzscheduleEntry { command = si.command, time = dtCurrent });
                        dtCurrent = dtCurrent.AddSeconds(cr.Interval);
                    }
                }
                cr.Dispose();
                CameraControl.Camobject.ptzschedule.entries = s.ToArray();
                ShowPTZSchedule();
            }
            else
            {
                MessageBox.Show(this, "Select a ptz schedule to repeat");
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            CameraControl.Camobject.ptzschedule.entries = new objectsCameraPtzscheduleEntry[0];
            ShowPTZSchedule();
        }

        private void linkLabel14_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl(MainForm.Website + "/userguide-grant-access.aspx");
        }

        private void chkReverseTracking_CheckedChanged(object sender, EventArgs e)
        {
            CameraControl.Camobject.settings.ptzautotrackreverse = chkReverseTracking.Checked;
        }

        private void txtPTZPassword_TextChanged(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            ofdDetect.FileName = "";
            ofdDetect.Filter = "Sound Files|*.wav";
            var initpath = Program.AppPath + @"sounds\";
            if (txtSound.Text.Trim() != "")
            {
                try
                {
                    var fi = new FileInfo(txtSound.Text);
                    initpath = fi.DirectoryName;
                }
                catch { }
            }
            ofdDetect.InitialDirectory = initpath;
            
            ofdDetect.ShowDialog(this);
            if (ofdDetect.FileName != "")
            {
                txtSound.Text = ofdDetect.FileName;
            }
        }

        private void label81_Click(object sender, EventArgs e)
        {

        }
    }
}