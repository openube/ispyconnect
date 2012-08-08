using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using AForge.Video.DirectShow;


namespace iSpyServer
{
    public partial class AddCamera : Form
    {
        public VideoCaptureDevice captureDevice;
        public CameraWindow CameraControl;

        public AddCamera()
        {
            InitializeComponent();
            RenderResources();
        }


        private void btnSelectSource_Click(object sender, EventArgs e)
        {
            SelectSource();
        }

        private bool SelectSource()
        {
            bool success = false;
            var vs = new VideoSource();
            vs.CameraControl = CameraControl;
            vs.ShowDialog(this);
            if (vs.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                CameraControl.Camobject.settings.videosourcestring = vs.VideoSourceString;
                CameraControl.Camobject.settings.sourceindex = vs.SourceIndex;
                CameraControl.Camobject.settings.login = vs.CameraLogin;
                CameraControl.Camobject.settings.password = vs.CameraPassword;
                CameraControl.Camobject.settings.useragent = vs.UserAgent;

                bool su = CameraControl.Camobject.resolution != vs.CaptureSize.Width + "x" + vs.CaptureSize.Height;
                if (vs.SourceIndex == 3)
                {
                    CameraControl.Camobject.resolution = vs.CaptureSize.Width + "x" + vs.CaptureSize.Height;
                    CameraControl.Camobject.settings.framerate = vs.FrameRate;
                    CameraControl.Camobject.settings.crossbarindex = vs.VideoInput.Index;
                }

                chkActive.Enabled = true;
                chkActive.Checked = false;
                Thread.Sleep(1000); //allows unmanaged code to complete shutdown
                chkActive.Checked = true;

                CameraControl.NeedSizeUpdate = su;
                success = true;
            }
            vs.Dispose();
            return success;
        }
        private static string Shorten(string s, int Length)
        {
            if (s.Length < Length)
                return s;
            return s.Substring(0, Length - 3) + "...";
        }       
        private void AddCamera_Load(object sender, EventArgs e)
        {
            CameraControl.IsEdit = true;
            rtbDescription.Text = CameraControl.Camobject.description;
           
            ddlTimestampLocation.Items.AddRange(LocRM.GetString("TimeStampLocations").Split(','));
            //ddlPreset.Items.Add(new ListItem("WEBM (android/chrome)", "-i \"{filename}.avi\" -an -r {framerate} -vcodec libvpx -f webm \"{filename}.webm\""));

            ddlTimestamp.Text = CameraControl.Camobject.settings.timestampformatter;
            ddlTimestampLocation.SelectedIndex = CameraControl.Camobject.settings.timestamplocation;
                 
            
            txtCameraName.Text = CameraControl.Camobject.name;
            chkSchedule.Checked = CameraControl.Camobject.schedule.active;
            chkFlipX.Checked = CameraControl.Camobject.flipx;
            chkFlipY.Checked = CameraControl.Camobject.flipy;

            ShowSchedule(-1);

            
            if (CameraControl.Camera == null)
            {
                chkActive.Checked = CameraControl.Camobject.settings.active = false;
                btnAdvanced.Enabled = btnCrossbar.Enabled = false;
            }
            else
            {
                chkActive.Checked = CameraControl.Camobject.settings.active;
            }
            pnlScheduler.Enabled = chkSchedule.Checked;
            chkActive.Enabled = CameraControl.Camobject.settings.videosourcestring != "" && CameraControl.Camobject.settings.videosourcestring != null;
           
            this.Text = LocRM.GetString("EditCamera");
            if (CameraControl.Camobject.id > -1)
                this.Text += " (ID: " + CameraControl.Camobject.id + ", DIR: "+CameraControl.Camobject.directory+")";
            
            
            if (CameraControl.Camera != null)
            {
                CameraControl.Camera.NewFrame -= new EventHandler(Camera_NewFrame);
                CameraControl.Camera.NewFrame += new EventHandler(Camera_NewFrame);
            }

            
            btnBack.Enabled = false;

            ddlHourStart.SelectedIndex = ddlHourEnd.SelectedIndex = ddlMinuteStart.SelectedIndex = ddlMinuteEnd.SelectedIndex = 0;

            txtMaskImage.Text = CameraControl.Camobject.settings.maskimage;

            if (CameraControl.Camobject.id == -1)
            {
                if (!SelectSource())
                    Close();
            }
            
        }

        private void RenderResources() {
            
            btnBack.Text = LocRM.GetString("Back");
            btnDelete.Text = LocRM.GetString("Delete");
            
            btnFinish.Text = LocRM.GetString("Finish");
            btnMaskImage.Text = LocRM.GetString("chars_3014702301470230147");
            btnNext.Text = LocRM.GetString("Next");
            btnAdvanced.Text = LocRM.GetString("AdvProperties");
            
            btnSelectSource.Text = LocRM.GetString("chars_3014702301470230147");
            btnUpdate.Text = LocRM.GetString("Update");
            
            button2.Text = LocRM.GetString("Add");
            chkActive.Text = LocRM.GetString("CameraActive");
            
            chkFlipX.Text = LocRM.GetString("Flipx");
            chkFlipY.Text = LocRM.GetString("Flipy");
            chkFri.Text = LocRM.GetString("Fri");
            
            chkMon.Text = LocRM.GetString("Mon");
                      
            chkSat.Text = LocRM.GetString("Sat");
            chkSchedule.Text = LocRM.GetString("ScheduleCamera");
            chkScheduleActive.Text = LocRM.GetString("ScheduleActive");
                        
            chkSun.Text = LocRM.GetString("Sun");
            
            chkThu.Text = LocRM.GetString("Thu");
            chkTue.Text = LocRM.GetString("Tue");
            
            chkWed.Text = LocRM.GetString("Wed");
            
            groupBox3.Text = LocRM.GetString("VideoSource");
            
            label1.Text = LocRM.GetString("Name");
            label10.Text = LocRM.GetString("chars_3801146");
            label11.Text = LocRM.GetString("TimeStamp");
            
            label13.Text = LocRM.GetString("Seconds");
            label14.Text = LocRM.GetString("RecordTimelapse");
           
            label17.Text = LocRM.GetString("Frames");
            label2.Text = LocRM.GetString("Source");
            label24.Text = LocRM.GetString("Seconds");
            label25.Text = LocRM.GetString("CalibrationDelay");
            label26.Text = LocRM.GetString("PrebufferFrames");
            
            label31.Text = LocRM.GetString("Seconds");
            label32.Text = LocRM.GetString("InactivityRecord");
            
            label34.Text = LocRM.GetString("MaxRecordTime");
            label35.Text = LocRM.GetString("Seconds");
            
            label49.Text = LocRM.GetString("Days");
            
            label50.Text = LocRM.GetString("ImportantMakeSureYourSche");
            
            label66.Text = LocRM.GetString("Description");
            
            label7.Text = LocRM.GetString("Start");
            
           
            label8.Text = LocRM.GetString("chars_3801146");
            label80.Text = LocRM.GetString("TipToCreateAScheduleOvern");
            
            label84.Text = LocRM.GetString("MaskImage");
            label85.Text = LocRM.GetString("createATransparentpngImag");
            
            label9.Text = LocRM.GetString("Stop");
           
            pnlScheduler.Text = LocRM.GetString("Scheduler");
            
            tabPage1.Text = LocRM.GetString("Camera");
            
            tabPage5.Text = LocRM.GetString("Scheduling");

            toolTip1.SetToolTip(txtMaskImage, LocRM.GetString("ToolTip_CameraName"));
            toolTip1.SetToolTip(txtCameraName, LocRM.GetString("ToolTip_CameraName"));
            
            toolTip1.SetToolTip(lbSchedule, LocRM.GetString("ToolTip_PressDelete"));

            this.Text = LocRM.GetString("AddCamera");
        }




        
        private void ShowSchedule(int SelectedIndex)
        {
            lbSchedule.Items.Clear();
            int _i = 0;
            foreach (objectsCameraScheduleEntry _sched in CameraControl.Camobject.schedule.entries)
            {
                string daysofweek = _sched.daysofweek;
                daysofweek = daysofweek.Replace("0", LocRM.GetString("Sun"));
                daysofweek = daysofweek.Replace("1", LocRM.GetString("Mon"));
                daysofweek = daysofweek.Replace("2", LocRM.GetString("Tue"));
                daysofweek = daysofweek.Replace("3", LocRM.GetString("Wed"));
                daysofweek = daysofweek.Replace("4", LocRM.GetString("Thu"));
                daysofweek = daysofweek.Replace("5", LocRM.GetString("Fri"));
                daysofweek = daysofweek.Replace("6", LocRM.GetString("Sat"));

                string _s = _sched.start + " -> " + _sched.stop + " (" + daysofweek + ")";
                if (_sched.recordonstart)
                    _s += " "+LocRM.GetString("RECORD_UC");
                if (_sched.alerts)
                    _s+=" "+LocRM.GetString("ALERT_UC");
                if (_sched.recordondetect)
                    _s += " "+LocRM.GetString("DETECT_UC");
                if (!_sched.active)
                    _s += " ("+LocRM.GetString("INACTIVE_UC")+")";
                
                lbSchedule.Items.Add(new ListItem(_s, _i.ToString()));
                _i++;
            }
            if (SelectedIndex>-1 && SelectedIndex<lbSchedule.Items.Count)
                lbSchedule.SelectedIndex = SelectedIndex;
        }
        private struct ListItem
        {
            internal string Name;
            internal string Value;
            public override string ToString()
            {
                return Name;
            }
            public ListItem(string Name, string Value) { this.Name = Name; this.Value = Value; }
        }
        private void Camera_NewFrame(object sender, EventArgs e)
        {
            
        }
        private static string ZeroPad(int i)
        {
            if (i < 10)
                return "0" + i;
            return i.ToString();
        }
        private void tbSensitivity_Scroll(object sender, EventArgs e)
        {
            
        }
        private void btnNext_Click(object sender, EventArgs e)
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
            string _name = txtCameraName.Text.Trim();
            if (_name == "")
                err += LocRM.GetString("Validate_Camera_EnterName") + Environment.NewLine;
            if (MainForm.Cameras.SingleOrDefault(p=>p.name.ToLower()==_name.ToLower() && p.id!=CameraControl.Camobject.id)!=null)
                err += LocRM.GetString("Validate_Camera_NameInUse") + Environment.NewLine;

            if (CameraControl.Camobject.settings.videosourcestring == null || CameraControl.Camobject.settings.videosourcestring == "")
            {
                err += LocRM.GetString("Validate_Camera_SelectVideoSource") + Environment.NewLine;
            }

            if (err != "")
            {
                MessageBox.Show(err, LocRM.GetString("Error"));
                tcCamera.SelectedIndex = 0;
                return false;
            }
            return true;
        }
        private void btnFinish_Click(object sender, EventArgs e)
        {
            Finish();
        }
        private bool Finish()
        {
            //validate page 0
            if (CheckStep1())
            {
                string err = "";
                
                
                
                //DateTime _dtStart = new DateTime(2007, 1, 1, Convert.ToInt32(ddlHourStart.SelectedItem), Convert.ToInt32(ddlMinuteStart.SelectedItem), 0);
                //DateTime _dtStop = new DateTime(2007, 1, 1, Convert.ToInt32(ddlHourEnd.SelectedItem), Convert.ToInt32(ddlMinuteEnd.SelectedItem), 0);

                
                int _nm = 0;

                
                int _mi = 0;


                string _sms = "";
                string _email = "";
                
                if (err != "")
                {
                    MessageBox.Show(err, LocRM.GetString("Error"));
                    return false;
                }
                
                
                int _ftpport = 21;
                
                int _ftpinterval = 30;
                

                int _timelapseframes = 0;
                

                int _timelapsemovie = 0;

                


                CameraControl.Camobject.description = rtbDescription.Text;

                CameraControl.Camobject.detector.processeveryframe = 1;

                
                CameraControl.Camobject.name = txtCameraName.Text.Trim();
                
                CameraControl.Camobject.detector.nomovementinterval = _nm;
                CameraControl.Camobject.detector.movementinterval = _mi;
               
                CameraControl.Camobject.settings.emailaddress = _email;
                CameraControl.Camobject.settings.smsnumber = _sms;
                                
                CameraControl.Camobject.schedule.active = chkSchedule.Checked;
                CameraControl.Camobject.settings.active = chkActive.Checked;
                

                int _bufferframes = 30, _calibrationdelay = 15, InactiveRecord = 3, _minimuminterval = 180, _maxrecord = 180, _emailgrabinterval = 60;
                


                CameraControl.Camobject.recorder.bufferframes = _bufferframes;
                CameraControl.Camobject.detector.calibrationdelay = _calibrationdelay;
                CameraControl.Camobject.recorder.inactiverecord = InactiveRecord;
                CameraControl.Camobject.alerts.minimuminterval = _minimuminterval;
                CameraControl.Camobject.recorder.maxrecordtime = _maxrecord;
                CameraControl.Camobject.notifications.emailgrabinterval = _emailgrabinterval;

                
                CameraControl.Camobject.ftp.port = _ftpport;
                CameraControl.Camobject.ftp.interval = _ftpinterval;
               

                CameraControl.Camobject.recorder.timelapseframes = _timelapseframes;
                CameraControl.Camobject.recorder.timelapse = _timelapsemovie;

                CameraControl.Camobject.settings.youtube.autoupload = false;// chkUploadYouTube.Checked;
               

                DialogResult = DialogResult.OK;            
                Close();
                return true;
            }
            return false;
        }


        private void chkSchedule_CheckedChanged(object sender, EventArgs e)
        {
            pnlScheduler.Enabled = chkSchedule.Checked;
            btnDelete.Enabled = btnUpdate.Enabled = lbSchedule.SelectedIndex > -1;
            lbSchedule.Refresh();
        }



        private void txtCameraName_KeyUp(object sender, KeyEventArgs e)
        {
            CameraControl.Camobject.name = txtCameraName.Text;
        }



        private void chkActive_CheckedChanged(object sender, EventArgs e)
        {
            if (CameraControl.Camobject.settings.active != chkActive.Checked)
            {
                if (chkActive.Checked)
                {
                    CameraControl.Enable();
                    if (CameraControl.Camera != null)
                        CameraControl.Camera.NewFrame += new EventHandler(Camera_NewFrame);
                }
                else
                {
                    if (CameraControl.Camera != null)
                        CameraControl.Camera.NewFrame -= new EventHandler(Camera_NewFrame);
                    CameraControl.Disable();
                }             
            }
            btnAdvanced.Enabled = btnCrossbar.Enabled = false;


            if (CameraControl.Camera != null)
            {
                if (CameraControl.Camera.VideoSource is VideoCaptureDevice)
                {
                    btnAdvanced.Enabled = true;
                    btnCrossbar.Enabled = ((VideoCaptureDevice)CameraControl.Camera.VideoSource).CheckIfCrossbarAvailable();
                }
            }
        }

        private void txtCameraName_TextChanged(object sender, EventArgs e)
        {
            CameraControl.Camobject.name = txtCameraName.Text;
        }

        private void AddCamera_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CameraControl.Camera != null)
                CameraControl.Camera.NewFrame -= new EventHandler(Camera_NewFrame);
            CameraControl.IsEdit = false;
        }

        private void rdoMovement_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void rdoNoMovement_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void chkRecord_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void ddlMovementDetector_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            
        }

        private void SetDetector()
        {
            
        }

        private void SetProcessor()
        {
            
        }

        private void chkSuppressNoise_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void chkKeepEdges_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GoNext();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            GoPrevious();
        }

        private void label23_Click(object sender, EventArgs e)
        {

        }

        private void txtCalibrationDelay_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtCalibrationDelay_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != '\b';
        }

        private void txtBuffer_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != '\b';
        }

        private void txtInactiveRecord_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != '\b';
        }

        private void txtTimeLapse_KeyPress_1(object sender, KeyPressEventArgs e)
        {
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != '\b';
        }

        private void txtMaxRecordTime_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != '\b';
        }

        private void llblEmailFrame_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.GoSubscribe();
        }

        private void txtBuffer_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != '\b';
        }

        private void tcCamera_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tcCamera.SelectedIndex == 0)
                btnBack.Enabled = false;
            else
                btnBack.Enabled = true;

            if (tcCamera.SelectedIndex == tcCamera.TabCount-1)
                btnNext.Enabled = false;
            else
                btnNext.Enabled = true;

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            
        }

        private void ddlProcessor_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            List<objectsCameraScheduleEntry> _scheds = CameraControl.Camobject.schedule.entries.ToList();
            objectsCameraScheduleEntry _sched = new objectsCameraScheduleEntry();
            if (ConfigureSchedule(_sched))
            {
                _scheds.Add(_sched);
                CameraControl.Camobject.schedule.entries = _scheds.ToArray();
                ShowSchedule(CameraControl.Camobject.schedule.entries.Count()-1);
            }          

        }

        private bool ConfigureSchedule(objectsCameraScheduleEntry _sched)
        {
            if (ddlHourStart.SelectedItem.ToString() == "-" || ddlMinuteStart.SelectedItem.ToString() == "-")
            {
                _sched.start = "-:-";
            }
            else
                _sched.start = ddlHourStart.SelectedItem + ":" + ddlMinuteStart.SelectedItem;
            if (ddlHourEnd.SelectedItem.ToString() == "-" || ddlMinuteEnd.SelectedItem.ToString() == "-")
            {
                _sched.stop = "-:-";
            }
            else
                _sched.stop = ddlHourEnd.SelectedItem + ":" + ddlMinuteEnd.SelectedItem;

            _sched.daysofweek = "";
            if (chkMon.Checked)
            {
                _sched.daysofweek += "1,";
            }
            if (chkTue.Checked)
            {
                _sched.daysofweek += "2,";
            }
            if (chkWed.Checked)
            {
                _sched.daysofweek += "3,";
            }
            if (chkThu.Checked)
            {
                _sched.daysofweek += "4,";
            }
            if (chkFri.Checked)
            {
                _sched.daysofweek += "5,";
            }
            if (chkSat.Checked)
            {
                _sched.daysofweek += "6,";
            }
            if (chkSun.Checked)
            {
                _sched.daysofweek += "0,";
            }
            _sched.daysofweek = _sched.daysofweek.Trim(',');
            if (_sched.daysofweek == "")
            {
                MessageBox.Show(LocRM.GetString("Validate_Camera_SelectOneDay"));
                return false;
            }

            _sched.active = chkScheduleActive.Checked;
            return true;
        }

        private void lbSchedule_KeyUp(object sender, KeyEventArgs e)
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
                int _i = lbSchedule.SelectedIndex;
                List<objectsCameraScheduleEntry> _scheds = CameraControl.Camobject.schedule.entries.ToList();
                _scheds.RemoveAt(_i);
                CameraControl.Camobject.schedule.entries = _scheds.ToArray();
                int j = _i;
                if (j == _scheds.Count)
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

        private void ddlHourStart_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void linkLabel1_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Help.ShowHelp(this, "http://www.ispyconnect.com/help.aspx#4");
        }

        private void btnSaveFTP_Click(object sender, EventArgs e)
        {
            
        }  
        private bool ThumbnailCallback()
        {
            return false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Help.ShowHelp(this,"http://www.ispyconnect.com/help.aspx#8.6");
        }

        private void ddlPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.StartBrowser("http://www.ffmpeg.org/ffmpeg-doc.html");
        }

        private void pnlTrackingColor_Paint(object sender, PaintEventArgs e)
        {
            
        }

        private void label47_Click(object sender, EventArgs e)
        {
            ShowTrackingColor();
        }

        private void ShowTrackingColor()
        {
            
        }

        private void pnlTrackingColor_Click(object sender, EventArgs e)
        {
            ShowTrackingColor();
        }

        private void ddlProcessFrames_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void ddlFFPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            

        }

        private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Login();
        }

        private void Login()
        {
            
        }

        private void linkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Login();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DeleteSchedule();
        }

        private void lbSchedule_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbSchedule.Items.Count==0)
                btnDelete.Enabled = btnUpdate.Enabled = false;
            else
            {
                btnUpdate.Enabled =  btnDelete.Enabled = (lbSchedule.SelectedIndex > -1);
                if (btnUpdate.Enabled)
                {
                    int _i = lbSchedule.SelectedIndex;
                    objectsCameraScheduleEntry _sched = CameraControl.Camobject.schedule.entries[_i];
                    
                    string[] start = _sched.start.Split(':');
                    string[] stop = _sched.stop.Split(':');


                    ddlHourStart.SelectedItem = start[0];
                    ddlHourEnd.SelectedItem = stop[0];
                    ddlMinuteStart.SelectedItem = start[1];
                    ddlMinuteEnd.SelectedItem = stop[1];

                    chkMon.Checked = _sched.daysofweek.IndexOf("1") != -1;
                    chkTue.Checked = _sched.daysofweek.IndexOf("2") != -1;
                    chkWed.Checked = _sched.daysofweek.IndexOf("3") != -1;
                    chkThu.Checked = _sched.daysofweek.IndexOf("4") != -1;
                    chkFri.Checked = _sched.daysofweek.IndexOf("5") != -1;
                    chkSat.Checked = _sched.daysofweek.IndexOf("6") != -1;
                    chkSun.Checked = _sched.daysofweek.IndexOf("0") != -1;

                    
                    chkScheduleActive.Checked = _sched.active;
                    
                }
            }
                
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            
        }    

        private void ddlTimestampLocation_SelectedIndexChanged(object sender, EventArgs e)
        {
            CameraControl.Camobject.settings.timestamplocation = ddlTimestampLocation.SelectedIndex;

        }

        private void ddlTimestamp_KeyUp(object sender, KeyEventArgs e)
        {
            CameraControl.Camobject.settings.timestampformatter = ddlTimestamp.Text;
        }

        private void btnMaskImage_Click(object sender, EventArgs e)
        {
            ofdDetect.FileName = "";
            ofdDetect.InitialDirectory = Program.AppPath + @"backgrounds\";
            ofdDetect.Filter = "Image Files (*.png)|*.png";
            ofdDetect.ShowDialog();
            if (ofdDetect.FileName != "")
            {
                txtMaskImage.Text = ofdDetect.FileName;
            }
        }

        private void txtMaskImage_TextChanged(object sender, EventArgs e)
        {
            if (File.Exists(txtMaskImage.Text))
            {
                try
                {
                    CameraControl.Camera.Mask = Image.FromFile(txtMaskImage.Text);
                    CameraControl.Camobject.settings.maskimage = txtMaskImage.Text;
                }
                catch (Exception)
                {
                }
            }
            else
            {
                CameraControl.Camera.Mask = null;
                CameraControl.Camobject.settings.maskimage = "";
            }
        }

        private void linkLabel5_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Help.ShowHelp(null, "http://www.ispyconnect.com/countrycodes.aspx");
        }

        private void chkEmailOnDisconnect_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label83_Click_1(object sender, EventArgs e)
        {

        }

        private void button3_Click_2(object sender, EventArgs e)
        {

        }

        private void chkFlipY_CheckedChanged(object sender, EventArgs e)
        {
            CameraControl.Camobject.flipy = chkFlipY.Checked;
        }

        private void chkFlipX_CheckedChanged(object sender, EventArgs e)
        {
            CameraControl.Camobject.flipx = chkFlipX.Checked;
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            int _i = lbSchedule.SelectedIndex;
            objectsCameraScheduleEntry _sched = CameraControl.Camobject.schedule.entries[_i];

            if (ConfigureSchedule(_sched))
            {
                ShowSchedule(_i);
            }
        }

        private void chkScheduleActive_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void lbSchedule_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            int _i = e.Index;
            if (_i >= 0)
            {
                objectsCameraScheduleEntry _sched = CameraControl.Camobject.schedule.entries[_i];

                Font _f;
                Brush _b;
                if (_sched.active)
                    _f = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Bold);
                else
                    _f = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Regular);
                if (!chkSchedule.Checked)
                {
                    _b = Brushes.Gray;
                }
                else
                    _b = Brushes.Black;

                e.Graphics.DrawString(lbSchedule.Items[_i].ToString(), _f, _b, e.Bounds);

                //_f.Dispose();
                //_b.Dispose();
                e.DrawFocusRectangle();
            }
        }

        private void lbSchedule_MouseUp(object sender, MouseEventArgs e)
        {
 
        }

        private void chkRecordSchedule_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void chkScheduleRecordOnDetect_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void linkLabel8_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
           
        }

        private void btnAdvanced_Click(object sender, EventArgs e)
        {
            try
            {
                ((VideoCaptureDevice)CameraControl.Camera.VideoSource).DisplayPropertyPage(this.Handle);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnCrossbar_Click(object sender, EventArgs e)
        {
            try
            {
                ((VideoCaptureDevice)CameraControl.Camera.VideoSource).DisplayCrossbarPropertyPage(this.Handle);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}