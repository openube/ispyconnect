using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using iSpyApplication.Controls;
using NAudio.Wave;

namespace iSpyApplication
{
    public partial class AddMicrophone : Form
    {
        public VolumeLevel VolumeLevel;
        private bool _loaded;

        public AddMicrophone()
        {
            InitializeComponent();
            RenderResources();
        }

        private void BtnSelectSourceClick(object sender, EventArgs e)
        {
            SelectSource();
        }

        private bool SelectSource()
        {
            bool success = false;
            var ms = new MicrophoneSource {Mic = VolumeLevel.Micobject};

            ms.ShowDialog(this);
            if (ms.DialogResult == DialogResult.OK)
            {
                chkActive.Enabled = true;
                chkActive.Checked = false;
                Application.DoEvents();
                VolumeLevel.Micobject.settings.needsupdate = true;
                lblAudioSource.Text = VolumeLevel.Micobject.settings.sourcename;
                chkActive.Checked = true;
                success = true;
            }
            ms.Dispose();
            return success;
        }

        private void AddMicrophoneLoad(object sender, EventArgs e)
        {
            if (VolumeLevel.Micobject.id == -1)
            {
                if (!SelectSource())
                {
                    Close();
                    return;
                }
            }
            VolumeLevel.IsEdit = true;
            if (VolumeLevel.CameraControl != null)
                VolumeLevel.CameraControl.IsEdit = true;
            
            btnBack.Enabled = false;
            gpbSubscriber.Enabled = MainForm.Conf.Subscribed;
            txtMicrophoneName.Text = VolumeLevel.Micobject.name;
            tbSensitivity.Value = VolumeLevel.Micobject.detector.sensitivity;
            //tbGain.Value = (int)(VolumeLevel.Micobject.settings.gain * 100);

            chkSound.Checked = VolumeLevel.Micobject.alerts.active;
            rdoRecordDetect.Checked = VolumeLevel.Micobject.detector.recordondetect;
            rdoRecordAlert.Checked = VolumeLevel.Micobject.detector.recordonalert;
            rdoNoRecord.Checked = !rdoRecordDetect.Checked && !rdoRecordAlert.Checked;
            txtExecuteAudio.Text = VolumeLevel.Micobject.alerts.executefile;

            if (VolumeLevel.Micobject.alerts.mode == "sound")
                rdoMovement.Checked = true;
            else
                rdoNoMovement.Checked = true;

            chkSendEmailSound.Checked = VolumeLevel.Micobject.notifications.sendemail;
            chkSendSMSMovement.Checked = VolumeLevel.Micobject.notifications.sendsms;

            
            txtEmailAlert.Text = VolumeLevel.Micobject.settings.emailaddress;
            txtSMSNumber.Text = VolumeLevel.Micobject.settings.smsnumber;
            chkSchedule.Checked = VolumeLevel.Micobject.schedule.active;
            chkActive.Checked = VolumeLevel.Micobject.settings.active;

            chkActive.Enabled = VolumeLevel.Micobject.settings.sourcename != "";

            if (VolumeLevel.Micobject.settings.sourcename != "")
            {
                lblAudioSource.Text = "";
                if (VolumeLevel.Micobject.settings.typeindex == 4)
                {
                    int icam;
                    if (Int32.TryParse(VolumeLevel.Micobject.settings.sourcename, out icam))
                    {
                        var c = MainForm.Cameras.SingleOrDefault(p => p.id == icam);
                        if (c != null)
                            lblAudioSource.Text = c.name;
                    }

                }
                if (lblAudioSource.Text=="")
                    lblAudioSource.Text = VolumeLevel.Micobject.settings.sourcename;
            }
            else
            {
                lblAudioSource.Text = LocRm.GetString("NoSource");
                chkActive.Checked = false;
            }


            string[] alertOptions = VolumeLevel.Micobject.alerts.alertoptions.Split(','); //beep,restore
            chkBeep.Checked = Convert.ToBoolean(alertOptions[0]);
            chkRestore.Checked = Convert.ToBoolean(alertOptions[1]);
            Text = LocRm.GetString("EditMicrophone");
            if (VolumeLevel.Micobject.id > -1)
                Text += string.Format(" (ID: {0}, DIR: {1})", VolumeLevel.Micobject.id, VolumeLevel.Micobject.directory);

            txtNoSound.Text = VolumeLevel.Micobject.detector.nosoundinterval.ToString(CultureInfo.InvariantCulture);
            txtSound.Text = VolumeLevel.Micobject.detector.soundinterval.ToString(CultureInfo.InvariantCulture);
            pnlSound.Enabled = chkSound.Checked;
            pnlScheduler.Enabled = chkSchedule.Checked;

            txtBuffer.Text = VolumeLevel.Micobject.settings.buffer.ToString(CultureInfo.InvariantCulture);
            txtInactiveRecord.Text = VolumeLevel.Micobject.recorder.inactiverecord.ToString(CultureInfo.InvariantCulture);
            txtMinimumInterval.Text = VolumeLevel.Micobject.alerts.minimuminterval.ToString(CultureInfo.InvariantCulture);
            txtMaxRecordTime.Text = VolumeLevel.Micobject.recorder.maxrecordtime.ToString(CultureInfo.InvariantCulture);

            ddlHourStart.SelectedIndex =
                ddlHourEnd.SelectedIndex = ddlMinuteStart.SelectedIndex = ddlMinuteEnd.SelectedIndex = 0;
            linkLabel5.Visible = !(MainForm.Conf.Subscribed);
            ShowSchedule(-1);
            chkEmailOnDisconnect.Checked = VolumeLevel.Micobject.settings.notifyondisconnect;
            txtArguments.Text = VolumeLevel.Micobject.alerts.arguments;

            txtAccessGroups.Text = VolumeLevel.Micobject.settings.accessgroups;
            txtDirectory.Text = VolumeLevel.Micobject.directory;

            chkTwitter.Checked = VolumeLevel.Micobject.notifications.sendtwitter;

            //try
            //{
            //    tbGain.Value = Convert.ToInt32(VolumeLevel.Micobject.settings.gain*100f);
            //}
            //catch (Exception ex)
            //{
            //    tbGain.Value = 100;
            //}

            ddlCopyFrom.Items.Clear();
            ddlCopyFrom.Items.Add(new ListItem(LocRm.GetString("CopyFrom"), "-1"));
            foreach (objectsMicrophone c in MainForm.Microphones)
            {
                if (c.id!=VolumeLevel.Micobject.id)
                    ddlCopyFrom.Items.Add(new ListItem(c.name, c.id.ToString(CultureInfo.InvariantCulture)));
            }
            ddlCopyFrom.SelectedIndex = 0;


            int i = 0, j = 0;
            foreach(var dev in DirectSoundOut.Devices)
            {
                ddlPlayback.Items.Add(new ListItem(dev.Description, dev.Guid.ToString()));
                if (dev.Guid.ToString() == VolumeLevel.Micobject.settings.deviceout)
                    i = j;
                j++;
            }
            if (ddlPlayback.Items.Count>0)
                ddlPlayback.SelectedIndex = i;
            else
            {
                ddlPlayback.Items.Add(LocRm.GetString("NoAudioDevices"));
                ddlPlayback.Enabled = false;
            }

            tmrUpdateSourceDetails.Start();



            string t = VolumeLevel.Micobject.alerts.trigger ?? "";
            string t2 = VolumeLevel.Micobject.recorder.trigger ?? "";

            ddlTrigger.Items.Add(new ListItem("None", ""));
            ddlTriggerRecording.Items.Add(new ListItem("None", ""));

            foreach (var c in MainForm.Cameras.Where(p => p.settings.micpair!=VolumeLevel.Micobject.id))
            {   
                ddlTrigger.Items.Add(new ListItem(c.name, "2," + c.id));
                ddlTriggerRecording.Items.Add(new ListItem(c.name, "2," + c.id));   
            }
            foreach (var c in MainForm.Microphones.Where(p=>p.id != VolumeLevel.Micobject.id))
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

            if (VolumeLevel.CameraControl != null)
            {
                txtBuffer.Enabled = false;
                toolTip1.SetToolTip(txtBuffer,"Change the buffer on the paired camera to update");
            }
            _loaded = true;

        }

        private void RenderResources()
        {
            btnBack.Text = LocRm.GetString("Back");
            btnDelete.Text = LocRm.GetString("Delete");
            btnDetectSound.Text = LocRm.GetString("chars_3014702301470230147");
            btnFinish.Text = LocRm.GetString("Finish");
            btnNext.Text = LocRm.GetString("Next");
            btnSelectSource.Text = LocRm.GetString("chars_3014702301470230147");
            btnUpdate.Text = LocRm.GetString("Update");
            button2.Text = LocRm.GetString("Add");
            chkActive.Text = LocRm.GetString("MicrophoneActive");
            chkBeep.Text = LocRm.GetString("Beep");
            chkEmailOnDisconnect.Text = LocRm.GetString("SendEmailOnDisconnect");
            chkFri.Text = LocRm.GetString("Fri");
            chkMon.Text = LocRm.GetString("Mon");
            groupBox1.Text = LocRm.GetString("RecordingSettings");
            groupBox6.Text = LocRm.GetString("RecordingSettings");
            groupBox6.Text = LocRm.GetString("RecordingMode");
            rdoRecordDetect.Text = LocRm.GetString("RecordOnSoundDetection");
            rdoRecordAlert.Text = LocRm.GetString("RecordOnAlert");
            rdoNoRecord.Text = LocRm.GetString("NoRecord");
            chkRecordSchedule.Text = LocRm.GetString("RecordOnScheduleStart");
            chkRestore.Text = LocRm.GetString("ShowIspyWindow");
            chkSat.Text = LocRm.GetString("Sat");
            chkSchedule.Text = LocRm.GetString("ScheduleMicrophone");
            chkScheduleActive.Text = LocRm.GetString("ScheduleActive");
            chkScheduleAlerts.Text = LocRm.GetString("AlertsEnabled");
            chkScheduleRecordOnDetect.Text = LocRm.GetString("RecordOnDetect");
            chkRecordAlertSchedule.Text = LocRm.GetString("RecordOnAlert");
            chkSendEmailSound.Text = LocRm.GetString("SendEmailOnAlert");
            chkSendSMSMovement.Text = LocRm.GetString("SendSmsOnAlert");
            chkSound.Text = LocRm.GetString("AlertsEnabled");
            chkSun.Text = LocRm.GetString("Sun");
            chkThu.Text = LocRm.GetString("Thu");
            chkTue.Text = LocRm.GetString("Tue");
            chkWed.Text = LocRm.GetString("Wed");
            gpbSubscriber.Text = LocRm.GetString("WebServiceOptions");
            label1.Text = LocRm.GetString("Name");
            label10.Text = label18.Text = LocRm.GetString("chars_3801146");
            label11.Text = LocRm.GetString("SmsNumber");
            label12.Text = LocRm.GetString("MaxRecordTime");
            label13.Text = LocRm.GetString("Seconds");
            label14.Text = LocRm.GetString("Seconds");
            label15.Text = LocRm.GetString("DistinctAlertInterval");
            label16.Text = LocRm.GetString("Seconds");
            label17.Text = LocRm.GetString("Seconds");
            label19.Text = LocRm.GetString("InactivityRecord");
            label2.Text = LocRm.GetString("Source");
            label20.Text = LocRm.GetString("BufferAudio");
            label21.Text = LocRm.GetString("ExitThisToEnableAlertsAnd");
            label3.Text = LocRm.GetString("Sensitivity");
            label4.Text = LocRm.GetString("WhenSound");
            label45.Text = LocRm.GetString("EmailAddress");
            label48.Text = LocRm.GetString("Seconds");
            label49.Text = LocRm.GetString("Days");
            label5.Text = LocRm.GetString("Seconds");
            label50.Text = LocRm.GetString("ImportantMakeSureYourSche");
            label6.Text = LocRm.GetString("ExecuteFile");
            label8.Text = LocRm.GetString("Start");

            label9.Text = LocRm.GetString("chars_3801146");
            label7.Text = LocRm.GetString("TipToCreateAScheduleOvern");
            label10.Text = LocRm.GetString("Stop");

            lblAudioSource.Text = LocRm.GetString("Audiosource");
            linkLabel1.Text = LocRm.GetString("HowToEnterYourNumber");
            linkLabel5.Text = LocRm.GetString("YouNeedAnActiveSubscripti");
            rdoMovement.Text = LocRm.GetString("IsDetectedFor");
            rdoNoMovement.Text = LocRm.GetString("IsNotDetectedFor");
            tabPage1.Text = LocRm.GetString("Microphone");
            tabPage2.Text = LocRm.GetString("Alerts");
            tabPage3.Text = LocRm.GetString("Scheduling");
            tabPage4.Text = LocRm.GetString("Recording");
            Text = LocRm.GetString("Addmicrophone");

            toolTip1.SetToolTip(txtMicrophoneName, LocRm.GetString("ToolTip_MicrophoneName"));
            toolTip1.SetToolTip(txtMinimumInterval, LocRm.GetString("ToolTip_AlertInterval"));
            toolTip1.SetToolTip(txtInactiveRecord, LocRm.GetString("ToolTip_InactiveRecordAudio"));
            toolTip1.SetToolTip(txtBuffer, LocRm.GetString("ToolTip_BufferAudio"));
            toolTip1.SetToolTip(lbSchedule, LocRm.GetString("ToolTip_PressDelete"));
            llblHelp.Text = LocRm.GetString("help");
            label72.Text = LocRm.GetString("arguments");
            lblAccessGroups.Text = LocRm.GetString("AccessGroups");
            label74.Text = LocRm.GetString("Directory");

            LocRm.SetString(label23,"Listen");
            LocRm.SetString(chkTwitter, "MessageOnAlert");
            LocRm.SetString(linkLabel12, "AuthoriseTwitter");
            LocRm.SetString(label22, "TriggerRecording");

            HideTab(tabPage2, Helper.HasFeature(Enums.Features.Alerts));
            HideTab(tabPage4, Helper.HasFeature(Enums.Features.Recording));
            HideTab(tabPage3, Helper.HasFeature(Enums.Features.Scheduling));

            if (!Helper.HasFeature(Enums.Features.Web_Settings))
            {
                gpbSubscriber.Visible = linkLabel5.Visible = false;
            }
        }

        private void HideTab(TabPage t, bool show)
        {
            if (!show)
            {
                tcMicrophone.TabPages.Remove(t);
            }
        }

        private void TbSensitivityScroll(object sender, EventArgs e)
        {
            VolumeLevel.Micobject.detector.sensitivity = tbSensitivity.Value;
        }

        private void BtnNextClick(object sender, EventArgs e)
        {
            GoNext();
        }

        private void GoNext()
        {
            tcMicrophone.SelectedIndex++;
        }

        private void GoPrevious()
        {
            tcMicrophone.SelectedIndex--;
        }

        private bool CheckStep1()
        {
            string err = "";
            string name = txtMicrophoneName.Text.Trim();
            if (name == "")
                err += LocRm.GetString("Validate_Microphone_EnterName") + Environment.NewLine;
            if (
                MainForm.Microphones.SingleOrDefault(
                    p => p.name.ToLower() == name.ToLower() && p.id != VolumeLevel.Micobject.id) != null)
                err += LocRm.GetString("Validate_Microphone_NameInUse") + Environment.NewLine;


            if (VolumeLevel.Micobject.settings.sourcename == "")
            {
                err += LocRm.GetString("Validate_Microphone_SelectSource"); //"";
            }
            if (err != "")
            {
                MessageBox.Show(err, LocRm.GetString("Error"));
                return false;
            }
            return true;
        }

        private void BtnFinishClick(object sender, EventArgs e)
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
                    if (!String.IsNullOrEmpty(sms2))
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

                int nosoundinterval;
                if (!int.TryParse(txtNoSound.Text, out nosoundinterval))
                    err += LocRm.GetString("Validate_Microphone_NoSound") + Environment.NewLine;
                int soundinterval;
                if (!int.TryParse(txtSound.Text, out soundinterval))
                    err += LocRm.GetString("Validate_Microphone_Sound") + Environment.NewLine;

                string email = txtEmailAlert.Text.Replace(" ", "");
                if (email != "" && !email.IsValidEmail())
                {
                    err += LocRm.GetString("Validate_Camera_EmailAlerts") + Environment.NewLine;
                }
                
                if (email == "")
                {
                    chkSendEmailSound.Checked = false;
                    chkEmailOnDisconnect.Checked = false;
                }
                if (sms == "")
                {
                    chkSendSMSMovement.Checked = false;
                }

                if (txtBuffer.Text.Length < 1 || txtInactiveRecord.Text.Length < 1 || txtMinimumInterval.Text.Length < 1 ||
                    txtMaxRecordTime.Text.Length < 1)
                {
                    err += LocRm.GetString("Validate_Camera_RecordingSettings") + Environment.NewLine;
                }

                if (err != "")
                {
                    MessageBox.Show(err, LocRm.GetString("Error"));
                    return;
                }


                VolumeLevel.Micobject.settings.buffer = Convert.ToInt32(txtBuffer.Value);
                VolumeLevel.Micobject.recorder.inactiverecord = Convert.ToInt32(txtInactiveRecord.Value);
                VolumeLevel.Micobject.alerts.minimuminterval = Convert.ToInt32(txtMinimumInterval.Value);
                VolumeLevel.Micobject.recorder.maxrecordtime = Convert.ToInt32(txtMaxRecordTime.Value);

                VolumeLevel.Micobject.name = txtMicrophoneName.Text.Trim();
                VolumeLevel.Micobject.detector.sensitivity = tbSensitivity.Value;
                VolumeLevel.Micobject.alerts.active = chkSound.Checked;
                VolumeLevel.Micobject.alerts.executefile = txtExecuteAudio.Text;
                VolumeLevel.Micobject.alerts.alertoptions = chkBeep.Checked + "," + chkRestore.Checked;
                VolumeLevel.Micobject.alerts.mode = "sound";
                if (rdoNoMovement.Checked)
                    VolumeLevel.Micobject.alerts.mode = "nosound";
                VolumeLevel.Micobject.detector.nosoundinterval = nosoundinterval;
                VolumeLevel.Micobject.detector.soundinterval = soundinterval;
                VolumeLevel.Micobject.notifications.sendemail = chkSendEmailSound.Checked;
                VolumeLevel.Micobject.notifications.sendsms = chkSendSMSMovement.Checked;
                VolumeLevel.Micobject.settings.smsnumber = sms;
                VolumeLevel.Micobject.settings.emailaddress = email;

                VolumeLevel.Micobject.schedule.active = chkSchedule.Checked;
                VolumeLevel.Micobject.width = VolumeLevel.Width;
                VolumeLevel.Micobject.height = VolumeLevel.Height;

                VolumeLevel.Micobject.settings.active = chkActive.Checked;
                VolumeLevel.Micobject.detector.recordondetect = rdoRecordDetect.Checked;
                VolumeLevel.Micobject.detector.recordonalert = rdoRecordAlert.Checked;
                VolumeLevel.Micobject.notifications.sendtwitter = chkTwitter.Checked;
                VolumeLevel.Micobject.settings.notifyondisconnect = chkEmailOnDisconnect.Checked;

                VolumeLevel.Micobject.alerts.arguments = txtArguments.Text;

                VolumeLevel.Micobject.settings.accessgroups = txtAccessGroups.Text;

                if (txtDirectory.Text.Trim() == "")
                    txtDirectory.Text = MainForm.RandomString(5);

                if (VolumeLevel.Micobject.directory != txtDirectory.Text)
                {
                    string path = MainForm.Conf.MediaDirectory + "audio\\" + txtDirectory.Text + "\\";
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                }

                VolumeLevel.Micobject.directory = txtDirectory.Text;

                VolumeLevel.Micobject.alerts.trigger = ((ListItem)ddlTrigger.SelectedItem).Value;
                VolumeLevel.Micobject.recorder.trigger = ((ListItem)ddlTriggerRecording.SelectedItem).Value;

                DialogResult = DialogResult.OK;
                MainForm.NeedsSync = true;
                Close();
            }
        }

        public bool IsNumeric(string numberString)
        {
            return numberString.All(char.IsNumber);
        }

        private void ChkSoundCheckedChanged(object sender, EventArgs e)
        {
            pnlSound.Enabled = chkSound.Checked;
            VolumeLevel.Micobject.alerts.active = chkSound.Checked;
        }

        private void BtnDetectMovementClick(object sender, EventArgs e)
        {
            ofdDetect.FileName = "";
            var initpath = Program.AppPath + @"sounds\";
            if (txtExecuteAudio.Text.Trim() != "")
            {
                try
                {
                    var fi = new FileInfo(txtExecuteAudio.Text);
                    initpath = fi.DirectoryName;
                }
                catch { }
            }
            ofdDetect.InitialDirectory = initpath;
            ofdDetect.ShowDialog(this);
            if (ofdDetect.FileName != "")
            {
                txtExecuteAudio.Text = ofdDetect.FileName;
            }
        }

        private void ChkScheduleCheckedChanged(object sender, EventArgs e)
        {
            pnlScheduler.Enabled = chkSchedule.Checked;
            btnDelete.Enabled = btnUpdate.Enabled = lbSchedule.SelectedIndex > -1;
            lbSchedule.Refresh();
        }

        private void TxtMicrophoneNameTextChanged(object sender, EventArgs e)
        {
            VolumeLevel.Micobject.name = txtMicrophoneName.Text;
        }

        private void ChkSendSmsMovementCheckedChanged(object sender, EventArgs e)
        {
        }

        private void ChkSendEmailSoundCheckedChanged(object sender, EventArgs e)
        {
        }

        private void AddMicrophoneFormClosing(object sender, FormClosingEventArgs e)
        {
            VolumeLevel.IsEdit = false;
            if (VolumeLevel.CameraControl != null)
                VolumeLevel.CameraControl.IsEdit = false;
            tmrUpdateSourceDetails.Stop();
            tmrUpdateSourceDetails.Dispose();
        }

        private void ChkActiveCheckedChanged(object sender, EventArgs e)
        {
            if (chkActive.Checked != VolumeLevel.Micobject.settings.active)
            {
                if (chkActive.Checked)
                    VolumeLevel.Enable();
                else
                    VolumeLevel.Disable();
            }
        }

        private void RdoMovementCheckedChanged(object sender, EventArgs e)
        {
            if (VolumeLevel.Micobject.alerts.mode != "sound" && rdoMovement.Checked)
            {
                VolumeLevel.Micobject.alerts.mode = "sound";
            }
        }

        private void RdoNoMovementCheckedChanged(object sender, EventArgs e)
        {
            if (VolumeLevel.Micobject.alerts.mode != "nosound" && rdoNoMovement.Checked)
            {
                VolumeLevel.Micobject.alerts.mode = "nosound";
            }
        }

        private void Button1Click(object sender, EventArgs e)
        {
            GoPrevious();
        }


        private void TcMicrophoneSelectedIndexChanged(object sender, EventArgs e)
        {
            btnBack.Enabled = tcMicrophone.SelectedIndex != 0;

            btnNext.Enabled = tcMicrophone.SelectedIndex != tcMicrophone.TabCount - 1;
        }

        private void Button2Click(object sender, EventArgs e)
        {
            List<objectsMicrophoneScheduleEntry> scheds = VolumeLevel.Micobject.schedule.entries.ToList();
            var sched = new objectsMicrophoneScheduleEntry();
            if (ConfigureSchedule(sched))
            {
                scheds.Add(sched);
                VolumeLevel.Micobject.schedule.entries = scheds.ToArray();
                ShowSchedule(VolumeLevel.Micobject.schedule.entries.Count() - 1);
            }
        }

        private bool ConfigureSchedule(objectsMicrophoneScheduleEntry sched)
        {
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
                MessageBox.Show(LocRm.GetString("Validate_Camera_SelectOneDay")); //"Please select at least one day");
                return false;
            }

            sched.recordonstart = chkRecordSchedule.Checked;
            sched.active = chkScheduleActive.Checked;
            sched.recordondetect = chkScheduleRecordOnDetect.Checked;
            sched.alerts = chkScheduleAlerts.Checked;
            return true;
        }

        private void LbScheduleKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSchedule();
            }
        }

        private void ShowSchedule(int selectedIndex)
        {
            lbSchedule.Items.Clear();
            int i = 0;
            foreach (string sched in VolumeLevel.ScheduleDetails)
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

        private void LinkLabel5LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Login();
        }

        private void Login()
        {
            ((MainForm) Owner).Connect(MainForm.Website+"/subscribe.aspx", false);
            gpbSubscriber.Enabled = MainForm.Conf.Subscribed;
        }


        private void BtnDeleteClick(object sender, EventArgs e)
        {
            DeleteSchedule();
        }

        private void DeleteSchedule()
        {
            if (lbSchedule.SelectedIndex > -1)
            {
                int i = lbSchedule.SelectedIndex;
                List<objectsMicrophoneScheduleEntry> scheds = VolumeLevel.Micobject.schedule.entries.ToList();
                scheds.RemoveAt(i);
                VolumeLevel.Micobject.schedule.entries = scheds.ToArray();
                int j = i - 1;
                if (j < 0)
                    j = 0;
                ShowSchedule(j);
                if (lbSchedule.Items.Count == 0)
                    btnDelete.Enabled = btnUpdate.Enabled = false;
                else
                    btnDelete.Enabled = btnUpdate.Enabled = (lbSchedule.SelectedIndex > -1);
            }
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
                    objectsMicrophoneScheduleEntry sched = VolumeLevel.Micobject.schedule.entries[i];

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
                }
            }
        }

        private void LinkLabel1LinkClicked1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl(MainForm.Website + "/countrycodes.aspx");
        }

        private void BtnUpdateClick(object sender, EventArgs e)
        {
            int i = lbSchedule.SelectedIndex;
            objectsMicrophoneScheduleEntry sched = VolumeLevel.Micobject.schedule.entries[i];

            if (ConfigureSchedule(sched))
            {
                ShowSchedule(i);
            }
        }

        private void LbScheduleDrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            int i = e.Index;
            if (i >= 0)
            {
                objectsMicrophoneScheduleEntry sched = VolumeLevel.Micobject.schedule.entries[i];

                Font f = sched.active ? new Font("Microsoft Sans Serif", 8.25f, FontStyle.Bold) : new Font("Microsoft Sans Serif", 8.25f, FontStyle.Regular);
                Brush b = !chkSchedule.Checked ? Brushes.Gray : Brushes.Black;

                e.Graphics.DrawString(lbSchedule.Items[i].ToString(), f, b, e.Bounds);
                e.DrawFocusRectangle();
            } 
            
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

        private void llblHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = MainForm.Website+"/userguide-microphone-settings.aspx";
            switch (tcMicrophone.SelectedTab.Name)
            {
                case "tabPage1":
                    url = MainForm.Website+"/userguide-microphone-settings.aspx#1";
                    break;
                case "tabPage2":
                    url = MainForm.Website+"/userguide-microphone-alerts.aspx";
                    break;
                case "tabPage4":
                    url = MainForm.Website+"/userguide-microphone-recording.aspx#2";
                    break;
                case "tabPage3":
                    url = MainForm.Website+"/userguide-scheduling.aspx#6";
                    break;
            }
            MainForm.OpenUrl( url);
        }

        private void ddlCopyFrom_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddlCopyFrom.SelectedIndex > 0)
            {
                var mic =
                    MainForm.Microphones.SingleOrDefault(
                        p => p.id == Convert.ToInt32(((ListItem)ddlCopyFrom.SelectedItem).Value));
                if (mic != null)
                {
                    List<objectsMicrophoneScheduleEntry> scheds = mic.schedule.entries.ToList();

                    VolumeLevel.Micobject.schedule.entries = scheds.ToArray();
                    ShowSchedule(VolumeLevel.Micobject.schedule.entries.Count() - 1);
                }
            }
        }

        private void linkLabel12_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl(MainForm.Webserver+"/account.aspx?task=twitter-auth");
        }

        private void tmrUpdateSourceDetails_Tick(object sender, EventArgs e)
        {
            if (VolumeLevel.Micobject.settings.needsupdate)
                lblFormat.Text = "...";
            else
            {
                string txt = VolumeLevel.Micobject.settings.samples + " hz (" +
                                 VolumeLevel.Micobject.settings.channels + " Channel";
                if (VolumeLevel.Micobject.settings.channels != 1)
                    txt += "s";
                txt += ")";
                lblFormat.Text = txt;
            }
            
        }

        private void ddlPlayback_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loaded && ddlPlayback.SelectedIndex>-1 && ddlPlayback.Enabled)
            {
                var g = new Guid(((ListItem) ddlPlayback.SelectedItem).Value);
                VolumeLevel.Micobject.settings.deviceout = g.ToString();
                bool listen = false;
                if (VolumeLevel.WaveOut!=null)
                {
                    if (VolumeLevel.Listening)
                    {
                        listen = true;
                        VolumeLevel.Listening = false;
                        Application.DoEvents();
                    }
                }
                VolumeLevel.WaveOut = new DirectSoundOut(g, 100);
                VolumeLevel.Listening = listen;
            }
        }

        private void ddlTrigger_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void tbGain_Scroll(object sender, EventArgs e)
        {
            //if (VolumeLevel.AudioSource!=null)
            //    VolumeLevel.Gain = tbGain.Value/100f;
        }

    }
}