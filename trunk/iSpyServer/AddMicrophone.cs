using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

namespace iSpyServer
{
    public partial class AddMicrophone : Form
    {
        public VolumeLevel VolumeLevel; 
        
        public AddMicrophone()
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
            bool _success = false;
            MicrophoneSource _ms = new MicrophoneSource();
            _ms.Mic = VolumeLevel.Micobject;


            _ms.ShowDialog(this);
            if (_ms.DialogResult == System.Windows.Forms.DialogResult.OK)
            {

                chkActive.Enabled = true;
                chkActive.Checked = false;
                Application.DoEvents();
                lblAudioSource.Text = VolumeLevel.Micobject.settings.sourcename;
                if (txtMicrophoneName.Text == "")
                    txtMicrophoneName.Text = lblAudioSource.Text;
                chkActive.Checked = true;
                _success = true;
            }
            _ms.Dispose();
            return _success;
        }     

        private void AddMicrophone_Load(object sender, EventArgs e)
        {
            
            VolumeLevel.IsEdit = true;

           
            rtbDescription.Text = VolumeLevel.Micobject.description;

            btnBack.Enabled = false;
            
            txtMicrophoneName.Text = VolumeLevel.Micobject.name;      

            
            chkSchedule.Checked = VolumeLevel.Micobject.schedule.active;
            chkActive.Checked = VolumeLevel.Micobject.settings.active;

            chkActive.Enabled = VolumeLevel.Micobject.settings.sourcename!= "";
            if (VolumeLevel.Micobject.settings.sourcename != "")
            {
                lblAudioSource.Text = VolumeLevel.Micobject.settings.sourcename;
            }
            else
            {
                lblAudioSource.Text = LocRM.GetString("NoSource");
                chkActive.Checked = false;
            }


            string[] _AlertOptions = VolumeLevel.Micobject.alerts.alertoptions.Split(',');//beep,restore
            
            this.Text = LocRM.GetString("EditMicrophone");
            if (VolumeLevel.Micobject.id > -1)
                this.Text += " (ID: " + VolumeLevel.Micobject.id + ", DIR: " + VolumeLevel.Micobject.directory + ")";

           
            pnlSchedule.Enabled = chkSchedule.Checked;

            

            ddlHourStart.SelectedIndex = ddlHourEnd.SelectedIndex = ddlMinuteStart.SelectedIndex = ddlMinuteEnd.SelectedIndex = 0;
            
            ShowSchedule(-1);

            if (VolumeLevel.Micobject.id == -1)
            {
                if (!SelectSource())
                    Close();
            }
        }

        private void RenderResources() {
           
            btnBack.Text = LocRM.GetString("Back");
            btnDelete.Text = LocRM.GetString("Delete");
            
            btnFinish.Text = LocRM.GetString("Finish");
            btnNext.Text = LocRM.GetString("Next");
            btnSelectSource.Text = LocRM.GetString("chars_3014702301470230147");
            btnUpdate.Text = LocRM.GetString("Update");
            button2.Text = LocRM.GetString("Add");
            chkActive.Text = LocRM.GetString("MicrophoneActive");
           
            chkFri.Text = LocRM.GetString("Fri");
            chkMon.Text = LocRM.GetString("Mon");
            
            chkRecordSchedule.Text = LocRM.GetString("RecordOnScheduleStart");
            chkSat.Text = LocRM.GetString("Sat");
            chkSchedule.Text = LocRM.GetString("ScheduleMicrophone");
            chkScheduleActive.Text = LocRM.GetString("ScheduleActive");
            chkScheduleAlerts.Text = LocRM.GetString("AlertsEnabled");
            chkScheduleRecordOnDetect.Text = LocRM.GetString("RecordOnDetect");
            
            chkSun.Text = LocRM.GetString("Sun");
            chkThu.Text = LocRM.GetString("Thu");
            chkTue.Text = LocRM.GetString("Tue");
            chkWed.Text = LocRM.GetString("Wed");
            
            label1.Text = LocRM.GetString("Name");
            label10.Text = LocRM.GetString("chars_3801146");
            
            label2.Text = LocRM.GetString("Source");
                       
            label49.Text = LocRM.GetString("Days");
            
            label50.Text = LocRM.GetString("ImportantMakeSureYourSche");
            
            label66.Text = LocRM.GetString("Description");
            label7.Text = LocRM.GetString("Start");
            label8.Text = LocRM.GetString("chars_3801146");
            label80.Text = LocRM.GetString("TipToCreateAScheduleOvern");
            label9.Text = LocRM.GetString("Stop");
            lblAudioSource.Text = LocRM.GetString("Audiosource");
            
            tabPage1.Text = LocRM.GetString("Microphone");
            
            tabPage3.Text = LocRM.GetString("Scheduling");

            this.Text = LocRM.GetString("Addmicrophone");

            toolTip1.SetToolTip(txtMicrophoneName, LocRM.GetString("ToolTip_MicrophoneName"));
            toolTip1.SetToolTip(lbSchedule, LocRM.GetString("ToolTip_PressDelete"));
        }


        private static string ZeroPad(int i)
        {
            if (i < 10)
                return "0" + i;
            return i.ToString();
        }
        
        private void btnNext_Click(object sender, EventArgs e)
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
            string _name = txtMicrophoneName.Text.Trim();
            if (_name == "")
                err += LocRM.GetString("Validate_Microphone_EnterName") + Environment.NewLine;
            if (MainForm.Microphones.SingleOrDefault(p => p.name.ToLower() == _name.ToLower() && p.id != VolumeLevel.Micobject.id) != null)
                err += LocRM.GetString("Validate_Microphone_NameInUse") + Environment.NewLine;
            
            
            if (VolumeLevel.Micobject.settings.sourcename == "")
            {
                err += LocRM.GetString("Validate_Microphone_SelectSource");//"";
            }
            if (err != "")
            {
                MessageBox.Show(err, LocRM.GetString("Error"));
                return false;
            }
            return true;
        }

        private void btnFinish_Click(object sender, EventArgs e)
        {
            //validate page 0
            if (CheckStep1())
            {
              
                int _nosoundinterval = 0;
                int _soundinterval = 0;

                string _sms = "";
                string _email = "";
                
            
                VolumeLevel.Micobject.description = rtbDescription.Text;
                VolumeLevel.Micobject.name = txtMicrophoneName.Text.Trim();
              
                VolumeLevel.Micobject.detector.nosoundinterval = _nosoundinterval;
                VolumeLevel.Micobject.detector.soundinterval = _soundinterval;
                VolumeLevel.Micobject.settings.smsnumber = _sms;
                VolumeLevel.Micobject.settings.emailaddress = _email;


                VolumeLevel.Micobject.schedule.active = chkSchedule.Checked;
                VolumeLevel.Micobject.width = VolumeLevel.Width;
                VolumeLevel.Micobject.height = VolumeLevel.Height;

                VolumeLevel.Micobject.settings.active = chkActive.Checked;               

                DialogResult = DialogResult.OK;
                Close();
            }
            
            
        }
        public bool IsNumeric(string numberString)
        {
            foreach (char c in numberString)
            {
                if (!char.IsNumber(c))
                    return false;
            }
            return true;
        }
        private void chkSound_CheckedChanged(object sender, EventArgs e)
        {

           
        }

        private void btnDetectMovement_Click(object sender, EventArgs e)
        {
            
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            GoPrevious();
        }

        private void chkSchedule_CheckedChanged(object sender, EventArgs e)
        {
            pnlSchedule.Enabled = chkSchedule.Checked;
            btnDelete.Enabled = btnUpdate.Enabled = lbSchedule.SelectedIndex > -1;
            lbSchedule.Refresh();
        }

        private void txtMicrophoneName_TextChanged(object sender, EventArgs e)
        {
            VolumeLevel.Micobject.name = txtMicrophoneName.Text;
        }

        private void chkSendSMSMovement_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.StartBrowser("http://"+iSpyServer.Default.ServerAddress+"/login.aspx");
        }

        private void chkSendEmailSound_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void txtNoSound_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != '\b';
        }

        private void llblAccount_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.GoSubscribe();
        }


        private void AddMicrophone_FormClosing(object sender, FormClosingEventArgs e)
        {
            VolumeLevel.IsEdit = false;
        }

        private void chkActive_CheckedChanged(object sender, EventArgs e)
        {
            if (chkActive.Checked != VolumeLevel.Micobject.settings.active)
            {
                if (chkActive.Checked)
                    VolumeLevel.Enable();
                else
                    VolumeLevel.Disable();
            }
        }

        private void rdoMovement_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void rdoNoMovement_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            GoNext();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            GoPrevious();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GoPrevious();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            GoNext();
        }

        private void txtBuffer_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != '\b';
        }

        private void txtInactiveRecord_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != '\b';
        }

        private void txtMinimumInterval_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != '\b';
        }

        private void txtMaxRecordTime_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !Char.IsDigit(e.KeyChar) && e.KeyChar != '\b';
        }

        private void tcMicrophone_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tcMicrophone.SelectedIndex == 0)
                btnBack.Enabled = false;
            else
                btnBack.Enabled = true;

            if (tcMicrophone.SelectedIndex == tcMicrophone.TabCount - 1)
                btnNext.Enabled = false;
            else
                btnNext.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            List<objectsMicrophoneScheduleEntry> _scheds = VolumeLevel.Micobject.schedule.entries.ToList();
            objectsMicrophoneScheduleEntry _sched = new objectsMicrophoneScheduleEntry();
            if (ConfigureSchedule(_sched))
            {
                _scheds.Add(_sched);
                VolumeLevel.Micobject.schedule.entries = _scheds.ToArray();
                ShowSchedule(VolumeLevel.Micobject.schedule.entries.Count() - 1);
            } 
        }

        private bool ConfigureSchedule(objectsMicrophoneScheduleEntry _sched)
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
                MessageBox.Show(LocRM.GetString("Validate_Camera_SelectOneDay"));//"Please select at least one day");
                return false;
            }

            _sched.recordonstart = chkRecordSchedule.Checked;
            _sched.active = chkScheduleActive.Checked;
            _sched.recordondetect = chkScheduleRecordOnDetect.Checked;
            _sched.alerts = chkScheduleAlerts.Checked;
            return true;
        }

        private void lbSchedule_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSchedule();                    
            }
        }

        private void ShowSchedule(int SelectedIndex)
        {
            lbSchedule.Items.Clear();
            int _i = 0;
            foreach (objectsMicrophoneScheduleEntry _sched in VolumeLevel.Micobject.schedule.entries)
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
                    _s += " "+LocRM.GetString("ALERT_UC");
                if (_sched.recordondetect)
                    _s += " "+LocRM.GetString("DETECT_UC");
                if (!_sched.active)
                    _s += " ("+LocRM.GetString("INACTIVE_UC")+")";

                lbSchedule.Items.Add(new ListItem(_s, _i.ToString()));
                _i++;
            }
            if (SelectedIndex > -1 && SelectedIndex < lbSchedule.Items.Count)
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

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.StartBrowser("http://www.ffmpeg.org/ffmpeg-doc.html");
        }

        private void ddlPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void linkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Login();
        }
        private void Login()
        {
            
        }

        private void chkRecord_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            DeleteSchedule();
        }

        private void DeleteSchedule()
        {

            if (lbSchedule.SelectedIndex > -1)
            {
                int _i = lbSchedule.SelectedIndex;
                List<objectsMicrophoneScheduleEntry> _scheds = VolumeLevel.Micobject.schedule.entries.ToList();
                _scheds.RemoveAt(_i);
                VolumeLevel.Micobject.schedule.entries = _scheds.ToArray();
                int j = _i - 1;
                if (j < 0)
                    j = 0;
                ShowSchedule(j);
                if (lbSchedule.Items.Count == 0)
                    btnDelete.Enabled = btnUpdate.Enabled = false;
                else
                    btnDelete.Enabled = btnUpdate.Enabled = (lbSchedule.SelectedIndex > -1);
            }
        }

        private void lbSchedule_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbSchedule.Items.Count == 0)
                btnDelete.Enabled = btnUpdate.Enabled = false;
            else
            {
                btnUpdate.Enabled = btnDelete.Enabled = (lbSchedule.SelectedIndex > -1);
                if (btnUpdate.Enabled)
                {
                    int _i = lbSchedule.SelectedIndex;
                    objectsMicrophoneScheduleEntry _sched = VolumeLevel.Micobject.schedule.entries[_i];

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

                    chkRecordSchedule.Checked = _sched.recordonstart;
                    chkScheduleActive.Checked = _sched.active;
                    chkScheduleRecordOnDetect.Checked = _sched.recordondetect;
                    chkScheduleAlerts.Checked = _sched.alerts;
                }
            }
        }

        private void linkLabel1_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Help.ShowHelp(null, "http://www.ispyconnect.com/countrycodes.aspx");
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            int _i = lbSchedule.SelectedIndex;
            objectsMicrophoneScheduleEntry _sched = VolumeLevel.Micobject.schedule.entries[_i];

            if (ConfigureSchedule(_sched))
            {
                ShowSchedule(_i);
            }
        }

        private void lbSchedule_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            int _i = e.Index;
            objectsMicrophoneScheduleEntry _sched = VolumeLevel.Micobject.schedule.entries[_i];

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

            e.Graphics.DrawString(lbSchedule.Items[e.Index].ToString(), _f, _b, e.Bounds);
            e.DrawFocusRectangle();
        }

        private void chkRecordSchedule_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRecordSchedule.Checked)
                chkScheduleRecordOnDetect.Checked = false;
        }

        private void chkScheduleRecordOnDetect_CheckedChanged(object sender, EventArgs e)
        {
            if (chkScheduleRecordOnDetect.Checked)
                chkRecordSchedule.Checked = false;
        }
    }
}