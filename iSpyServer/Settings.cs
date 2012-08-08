using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Diagnostics;
using AForge.Video.VFW;
using NAudio.Wave;

namespace iSpyServer
{
    public partial class Settings : Form
    {
        public Settings()
        {
            InitializeComponent();
            RenderResources();
        }

        private RegistryKey rkApp;
        public int InitialTab = 0;
        public bool ReloadResources = false;
        const int RGBMAX = 255;

        private void button1_Click(object sender, EventArgs e)
        {
            string _password = txtPassword.Text;
            if (chkPasswordProtect.Checked)
            {
                if (_password.Length<3)
                {
                    MessageBox.Show(LocRM.GetString("Validate_Password"), LocRM.GetString("Note"));
                    return;
                }
            }
            string err = "";
            
            

            if (err != "")
            {
                MessageBox.Show(err, LocRM.GetString("Error"));
                return;
            }

            iSpyServer.Default.Enable_Error_Reporting = chkErrorReporting.Checked;
            iSpyServer.Default.Enable_Update_Check = chkCheckForUpdates.Checked;
            iSpyServer.Default.Enable_Password_Protect = chkPasswordProtect.Checked;
            iSpyServer.Default.Password_Protect_Password = _password;
           
            iSpyServer.Default.TimestampColor = btnTimestampColor.BackColor;
            
            iSpyServer.Default.MainColor = btnColorMain.BackColor;
            
            iSpyServer.Default.BackColor = btnColorBack.BackColor;
            iSpyServer.Default.Enabled_ShowGettingStarted = chkShowGettingStarted.Checked;

            iSpyServer.Default.Opacity = tbOpacity.Value;

            iSpyServer.Default.BalloonTips = chkBalloon.Checked;
            iSpyServer.Default.TrayIconText = txtTrayIcon.Text;

            iSpyServer.Default.IPCameraTimeout = Convert.ToInt32(txtIPCameraTimeout.Value);
            iSpyServer.Default.ServerReceiveTimeout = Convert.ToInt32(txtServerReceiveTimeout.Value);
            iSpyServer.Default.ServerName = txtServerName.Text;

            string _lang = ((ListItem)ddlLanguage.SelectedItem).Value[0];
            if (_lang!= iSpyServer.Default.Language)
                ReloadResources = true;
            iSpyServer.Default.Language = _lang;

            iSpyServer.Default.IPMode = "IPv4";
            iSpyServer.Default.IPv4Address = lbIPv4Address.SelectedItem.ToString();
            MainForm.AddressIPv4 = iSpyServer.Default.IPv4Address;

            if (ddlAudioOut.Enabled)
                iSpyServer.Default.AudioOutDevice = ((ListItem)ddlAudioOut.SelectedItem).Value[0];
            else
                iSpyServer.Default.AudioOutDevice = "";
            iSpyServer.Default.Save();

            if (chkStartup.Checked)
            {
                rkApp = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                rkApp.SetValue("iSpy", "\""+Application.ExecutablePath + "\" -silent", RegistryValueKind.String);
            }
            else
            {
                try
                {
                    rkApp.DeleteValue("iSpy",false);
                }
                catch (Exception)
                {
                }
            }
            DialogResult = DialogResult.OK;

            bool _needsrestart = false;
            if (iSpyServer.Default.LANPort != Convert.ToInt32(txtLANPort.Value))
                _needsrestart = true;

            iSpyServer.Default.LANPort = Convert.ToInt32(txtLANPort.Value);
            if (_needsrestart)
                MainForm.StopAndStartServer();
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();            
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            tcTabs.SelectedIndex = InitialTab;
            //lblBackground.Text = iSpyServer.Default.BackgroundImage;
            chkErrorReporting.Checked=iSpyServer.Default.Enable_Error_Reporting;
            chkCheckForUpdates.Checked=iSpyServer.Default.Enable_Update_Check;
            chkPasswordProtect.Checked=iSpyServer.Default.Enable_Password_Protect;
            chkShowGettingStarted.Checked = iSpyServer.Default.Enabled_ShowGettingStarted;

            if (iSpyServer.Default.Password_Protect_Password != "")
            {
                txtPassword.Text = iSpyServer.Default.Password_Protect_Password;
            }
            rkApp = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            chkStartup.Checked = (rkApp.GetValue("iSpy") != null);
            
            btnColorMain.BackColor = iSpyServer.Default.MainColor;
           
            btnColorBack.BackColor = iSpyServer.Default.BackColor;
            btnTimestampColor.BackColor = iSpyServer.Default.TimestampColor;
           
            txtTrayIcon.Text = iSpyServer.Default.TrayIconText;
            txtLANPort.Value = Convert.ToInt32(iSpyServer.Default.LANPort);

            //ddlImageMode.SelectedItem = iSpyServer.Default.BackgroundImageMode;

            tbOpacity.Value = iSpyServer.Default.Opacity;
            SetColors();

           
            chkBalloon.Checked = iSpyServer.Default.BalloonTips;

            

            txtIPCameraTimeout.Value = iSpyServer.Default.IPCameraTimeout;
            txtServerReceiveTimeout.Value = iSpyServer.Default.ServerReceiveTimeout;
            txtServerName.Text = iSpyServer.Default.ServerName;

            int _i = 0, selind=0;
            foreach (TranslationsTranslationSet set in LocRM.TranslationSets)
            {
                ddlLanguage.Items.Add(new ListItem(set.Name, new[] {set.CultureCode}));
                if (set.CultureCode == iSpyServer.Default.Language)
                    selind = _i;
                _i++;
            }
            ddlLanguage.SelectedIndex = selind;

            int i2 = 0;
            foreach (IPAddress ipadd in MainForm.AddressListIPv4)
            {
                if (ipadd.AddressFamily == AddressFamily.InterNetwork)
                {
                    lbIPv4Address.Items.Add(ipadd.ToString());
                    if (ipadd.ToString() == MainForm.AddressIPv4)
                        lbIPv4Address.SelectedIndex = i2;
                    i2++;
                }
            }
            if (lbIPv4Address.Items.Count > 0 && lbIPv4Address.SelectedIndex == -1)
                lbIPv4Address.SelectedIndex = 0;


            int i = 0, j = 0;
            var d = DirectSoundOut.Devices;
            if (d != null)
            {
                foreach (var dev in d)
                {
                    ddlAudioOut.Items.Add(new ListItem(dev.Description, new string[] {dev.Guid.ToString()}));
                    if (dev.Guid.ToString() == iSpyServer.Default.AudioOutDevice)
                        i = j;
                    j++;
                }
                if (ddlAudioOut.Items.Count > 0)
                    ddlAudioOut.SelectedIndex = i;
                else
                {
                    ddlAudioOut.Enabled = false;
                }
            }
        }

        private void RenderResources() {

            this.Text = LocRM.GetString("settings");
           
            btnColorBack.Text = LocRM.GetString("ObjectBack");
            btnColorMain.Text = LocRM.GetString("MainPanel");
           
            btnTimestampColor.Text = LocRM.GetString("Timestamp");
            button1.Text = LocRM.GetString("Ok");
            button2.Text = LocRM.GetString("Cancel");
            
            chkBalloon.Text = LocRM.GetString("ShowBalloonTips");
            chkCheckForUpdates.Text = LocRM.GetString("AutomaticallyCheckForUpda");
            chkErrorReporting.Text = LocRM.GetString("AnonymousErrorReporting");
            
            chkPasswordProtect.Text = LocRM.GetString("PasswordProtectWhenMinimi");
            chkShowGettingStarted.Text = LocRM.GetString("ShowGettingStarted");
            chkStartup.Text = LocRM.GetString("RunOnStartupthisUserOnly");
           
            label1.Text = LocRM.GetString("Password");
            
            label14.Text = LocRM.GetString("IspyServerName");
            label16.Text = LocRM.GetString("ispyOpacitymayNotW");
           
            label2.Text = LocRM.GetString("ServerReceiveTimeout");
            
            label21.Text = LocRM.GetString("TrayIconText");
            
            label4.Text = LocRM.GetString("ms");
            
            label7.Text = LocRM.GetString("ms");
            label8.Text = LocRM.GetString("MjpegReceiveTimeout");
           
            tabPage1.Text = LocRM.GetString("Colors");
           
            tabPage4.Text = LocRM.GetString("Timeouts");
            
            tabPage6.Text = LocRM.GetString("options");
            groupBox1.Text = LocRM.GetString("Language");
            linkLabel1.Text = LocRM.GetString("GetLatestList");
            Text = LocRM.GetString("settings");
            linkLabel2.Text = LocRM.GetString("HelpTranslateISpy");
            label3.Text = LocRM.GetString("LanPort");

            label5.Text = "IP Address";
        }


        private struct ListItem
        {
            internal string Name;
            internal string[] Value;
            public override string ToString()
            {
                return Name;
            }
            public ListItem(string Name, string[] Value) { this.Name = Name; this.Value = Value; }
        }


        private void SetColors()
        {
           
            btnColorMain.ForeColor = inverseColor(btnColorMain.BackColor);
           
            btnColorBack.ForeColor = inverseColor(btnColorBack.BackColor);
            btnTimestampColor.ForeColor = inverseColor(btnTimestampColor.BackColor);

        }
        private System.Drawing.Color inverseColor(System.Drawing.Color colorIn)
        {
            return Color.FromArgb(RGBMAX - colorIn.R,
              RGBMAX - colorIn.G, RGBMAX - colorIn.B);
        }

        private void chkStartup_CheckedChanged(object sender, EventArgs e)
        {

        }


       

        private void label5_Click(object sender, EventArgs e)
        {

        }

        

        private void grouper1_Load(object sender, EventArgs e)
        {

        }

        

        

        

        private void btnColorMain_Click(object sender, EventArgs e)
        {
            cdColorChooser.Color = btnColorMain.BackColor;
            if (cdColorChooser.ShowDialog() == DialogResult.OK)
            {
                btnColorMain.BackColor = cdColorChooser.Color;
                iSpyServer.Default.MainColor = btnColorMain.BackColor;
                SetColors();
                ((MainForm)this.Owner).SetBackground();
            }
        }

        private void btnColorBack_Click(object sender, EventArgs e)
        {
            cdColorChooser.Color = btnColorBack.BackColor;
            if (cdColorChooser.ShowDialog() == DialogResult.OK)
            {
                btnColorBack.BackColor = cdColorChooser.Color;
                SetColors();
            }
        }

        

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.StartBrowser("http://www.ffmpeg.org/ffmpeg-doc.html");
        }

        private void chkPasswordProtect_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
           
        }

        private void tbOpacity_Scroll(object sender, EventArgs e)
        {
            ((MainForm)this.Owner).Opacity = Convert.ToDouble(tbOpacity.Value)/100;
        }

        private void ddlImageMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            //iSpyServer.Default.BackgroundImageMode = ddlImageMode.SelectedItem.ToString();
            ((MainForm)this.Owner).SetBackground();
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Help.ShowHelp(this, "http://www.regular-expressions.info/");
        }

        private void chkStorage_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void btnTimestampColor_Click(object sender, EventArgs e)
        {
            cdColorChooser.Color = btnColorBack.BackColor;
            if (cdColorChooser.ShowDialog() == DialogResult.OK)
            {
                btnTimestampColor.BackColor = cdColorChooser.Color;
                SetColors();
            }
        }

        private void txtYouTubeUsername_KeyUp(object sender, KeyEventArgs e)
        {
            UpdateAccount();
        }

        private void UpdateAccount()
        {
           
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            
        }

        private void chkErrorReporting_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void chkShowGettingStarted_CheckedChanged(object sender, EventArgs e)
        {

        }
       
        
        private void txtShortcut_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
        }

        private void txtShortcut_TextChanged(object sender, EventArgs e)
        {

        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void lbShortcuts_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void txtShortcut_KeyUp(object sender, KeyEventArgs e)
        {
            
        }

        private void ddlLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void linkLabel1_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            bool _success = false;
            try
            {
                XmlTextReader _rdr = new XmlTextReader("http://www.ispyconnect.com/admin/translations.xml");
                XmlDocument _doc = new XmlDocument();
                _doc.Load(_rdr);
                _rdr.Close();
                _doc.Save(Program.AppPath + @"XML\Translations.xml");
                LocRM.TranslationsList = null;
                _success = true;
            }
            catch(System.Exception ex)
            {
                MessageBox.Show(ex.Message, LocRM.GetString("Error"));
            }
            if (_success)
            {
                ddlLanguage.Items.Clear();
                RenderResources();
                int _i = 0, _selind = 0;
                foreach (TranslationsTranslationSet _set in LocRM.TranslationSets)
                {
                    ddlLanguage.Items.Add(new ListItem(_set.Name, new string[] { _set.CultureCode }));
                    if (_set.CultureCode == iSpyServer.Default.Language)
                        _selind = _i;
                    _i++;
                }
                ddlLanguage.SelectedIndex = _selind;
                MessageBox.Show(LocRM.GetString("ResourcesUpdated"));
                ReloadResources = true;
            }
        }

        private void linkLabel2_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Help.ShowHelp(this, "http://www.ispyconnect.com/yaf/forum.aspx?g=posts&m=678&#post678#post678");
        }

        private void txtLANPort_TextChanged(object sender, EventArgs e)
        {

        }

        private void lbIPv4Address_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void txtLANPort_ValueChanged(object sender, EventArgs e)
        {

        }

        private void ddlAudioOut_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}