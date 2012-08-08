using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Google.GData.Client;
using Google.GData.YouTube;
using Google.YouTube;
using Microsoft.Win32;
using NAudio.Wave;

namespace iSpyApplication
{
    public partial class Settings : Form
    {
        private const int Rgbmax = 255;
        public int InitialTab;
        public bool ReloadResources;
        readonly string _noDevices = LocRm.GetString("NoAudioDevices");
        private RegistryKey _rkApp;

        public Settings()
        {
            InitializeComponent();
            RenderResources();
        }

        private void Button1Click(object sender, EventArgs e)
        {
            string password = txtPassword.Text;
            if (chkPasswordProtect.Checked)
            {
                if (password.Length < 3)
                {
                    MessageBox.Show(LocRm.GetString("Validate_Password"), LocRm.GetString("Note"));
                    return;
                }
            }
            string err = "";

            if (!Directory.Exists(txtMediaDirectory.Text))
            {
                err += LocRm.GetString("Validate_MediaDirectory") + "\n";
            }

            if (err != "")
            {
                MessageBox.Show(err, LocRm.GetString("Error"));
                return;
            }

            if (numJPEGQuality.Value != MainForm.Conf.JPEGQuality)
            {
                MainForm.EncoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (int) numJPEGQuality.Value);
            }
            MainForm.Conf.Enable_Error_Reporting = chkErrorReporting.Checked;
            MainForm.Conf.Enable_Update_Check = chkCheckForUpdates.Checked;
            MainForm.Conf.Enable_Password_Protect = chkPasswordProtect.Checked;
            MainForm.Conf.Password_Protect_Password = password;

            string dir = txtMediaDirectory.Text.Trim();
            if (!dir.EndsWith("\\"))
                dir += "\\";


            if (MainForm.Conf.MediaDirectory != dir)
            {
                MainForm.Conf.MediaDirectory = dir;
                Directory.CreateDirectory(dir + "audio");
                Directory.CreateDirectory(dir + "video");
                foreach (objectsCamera cam in MainForm.Cameras)
                {
                    Directory.CreateDirectory(dir + "video\\" + cam.directory);
                    Directory.CreateDirectory(dir + "video\\" + cam.directory+"\\thumbs");
                }
                foreach (objectsMicrophone mic in MainForm.Microphones)
                {
                    Directory.CreateDirectory(dir + "audio\\" + mic.directory);
                }
            }

            MainForm.Conf.NoActivityColor = btnNoDetectColor.BackColor.ToRGBString();
            MainForm.Conf.TimestampColor = btnTimestampColor.BackColor.ToRGBString();
            MainForm.Conf.ActivityColor = btnDetectColor.BackColor.ToRGBString();
            MainForm.Conf.TrackingColor = btnColorTracking.BackColor.ToRGBString();
            MainForm.Conf.VolumeLevelColor = btnColorVolume.BackColor.ToRGBString();
            MainForm.Conf.MainColor = btnColorMain.BackColor.ToRGBString();
            MainForm.Conf.AreaColor = btnColorArea.BackColor.ToRGBString();
            MainForm.Conf.BackColor = btnColorBack.BackColor.ToRGBString();
            MainForm.Conf.BorderHighlightColor = btnBorderHighlight.BackColor.ToRGBString();
            MainForm.Conf.Enabled_ShowGettingStarted = chkShowGettingStarted.Checked;
            MainForm.Conf.MaxMediaFolderSizeMB = Convert.ToInt32(txtMaxMediaSize.Value);
            MainForm.Conf.DeleteFilesOlderThanDays = Convert.ToInt32(txtDaysDelete.Value);
            MainForm.Conf.Opacity = tbOpacity.Value;
            MainForm.Conf.Enable_Storage_Management = chkStorage.Checked;
            MainForm.Conf.YouTubePassword = txtYouTubePassword.Text;
            MainForm.Conf.YouTubeUsername = txtYouTubeUsername.Text;
            MainForm.Conf.BalloonTips = chkBalloon.Checked;
            MainForm.Conf.TrayIconText = txtTrayIcon.Text;
            MainForm.Conf.IPCameraTimeout = Convert.ToInt32(txtIPCameraTimeout.Value);
            MainForm.Conf.ServerReceiveTimeout = Convert.ToInt32(txtServerReceiveTimeout.Value);
            MainForm.Conf.ServerName = txtServerName.Text;
            MainForm.Conf.AutoSchedule = chkAutoSchedule.Checked;
            MainForm.Conf.CPUMax = Convert.ToInt32(numMaxCPU.Value);
            MainForm.Conf.MaxRecordingThreads = (int)numMaxRecordingThreads.Value;
            MainForm.Conf.CreateAlertWindows = chkAlertWindows.Checked;
            MainForm.Conf.MaxRedrawRate = (int)numRedraw.Value;
            MainForm.Conf.Priority = ddlPriority.SelectedIndex + 1;
            MainForm.Conf.Monitor = chkMonitor.Checked;
            MainForm.Conf.ScreensaverWakeup = chkInterrupt.Checked;
            MainForm.Conf.PlaybackMode = ddlPlayback.SelectedIndex;
            MainForm.Conf.PreviewItems = (int)numMediaPanelItems.Value;
            MainForm.Conf.BigButtons = chkBigButtons.Checked;
            MainForm.Iconfont = new Font(FontFamily.GenericSansSerif, MainForm.Conf.BigButtons ? 22 : 15, FontStyle.Bold, GraphicsUnit.Pixel);
            MainForm.Conf.TalkMic = ddlTalkMic.Enabled ? ddlTalkMic.SelectedItem.ToString() : "";
            MainForm.Conf.MinimiseOnClose = chkMinimise.Checked;
            MainForm.Conf.JPEGQuality = (int) numJPEGQuality.Value;
            MainForm.Conf.IPv6Disabled = !chkEnableIPv6.Checked;

            MainForm.SetPriority();

            var ips = rtbAccessList.Text.Trim().Split(',');
            var t = ips.Select(ip => ip.Trim()).Where(ip2 => ip2 != "").Aggregate("", (current, ip2) => current + (ip2 + ","));
            MainForm.Conf.AllowedIPList = t.Trim(',');
            LocalServer.AllowedIPs = null;
            MainForm.Conf.ShowOverlayControls = chkOverlay.Checked;

            string lang = ((ListItem) ddlLanguage.SelectedItem).Value[0];
            if (lang != MainForm.Conf.Language)
            {
                ReloadResources = true;
                LocRm.CurrentSet = null;
            }
            MainForm.Conf.Language = lang;

            if (chkStartup.Checked)
            {
                try
                {
                    _rkApp = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                    if (_rkApp != null)
                        _rkApp.SetValue("iSpy", "\"" + Application.ExecutablePath + "\" -silent", RegistryValueKind.String);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    MainForm.LogExceptionToFile(ex);
                }
            }
            else
            {
                try
                {
                    _rkApp = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                    if (_rkApp != null) _rkApp.DeleteValue("iSpy", false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    MainForm.LogExceptionToFile(ex);
                }
            }

            MainForm.Conf.StopSavingOnStorageLimit = chkStopRecording.Checked;
            
            if (!MainForm.Conf.StopSavingOnStorageLimit)
                MainForm.StopRecordingFlag = false;

            
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Button2Click(object sender, EventArgs e)
        {
            Close();
        }

        private void SettingsLoad(object sender, EventArgs e)
        {
            UISync.Init(this);
            tcTabs.SelectedIndex = InitialTab;
            //lblBackground.Text = MainForm.Conf.BackgroundImage;
            chkErrorReporting.Checked = MainForm.Conf.Enable_Error_Reporting;
            chkCheckForUpdates.Checked = MainForm.Conf.Enable_Update_Check;
            chkPasswordProtect.Checked = MainForm.Conf.Enable_Password_Protect;
            chkShowGettingStarted.Checked = MainForm.Conf.Enabled_ShowGettingStarted;

            if (MainForm.Conf.Password_Protect_Password != "")
            {
                txtPassword.Text = MainForm.Conf.Password_Protect_Password;
            }
            _rkApp = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            chkStartup.Checked = (_rkApp != null && _rkApp.GetValue("iSpy") != null);
            txtMediaDirectory.Text = MainForm.Conf.MediaDirectory;

            btnDetectColor.BackColor = MainForm.Conf.ActivityColor.ToColor();
            btnNoDetectColor.BackColor = MainForm.Conf.NoActivityColor.ToColor();
            btnColorTracking.BackColor = MainForm.Conf.TrackingColor.ToColor();
            btnColorVolume.BackColor = MainForm.Conf.VolumeLevelColor.ToColor();
            btnColorMain.BackColor = MainForm.Conf.MainColor.ToColor();
            btnColorArea.BackColor = MainForm.Conf.AreaColor.ToColor();
            btnColorBack.BackColor = MainForm.Conf.BackColor.ToColor();
            btnBorderHighlight.BackColor = MainForm.Conf.BorderHighlightColor.ToColor();
            btnTimestampColor.BackColor = MainForm.Conf.TimestampColor.ToColor();
            txtDaysDelete.Text = MainForm.Conf.DeleteFilesOlderThanDays.ToString();
            txtMaxMediaSize.Value = MainForm.Conf.MaxMediaFolderSizeMB;
            chkAutoSchedule.Checked = MainForm.Conf.AutoSchedule;
            numMaxCPU.Value = MainForm.Conf.CPUMax;
            numMaxRecordingThreads.Value = MainForm.Conf.MaxRecordingThreads;
            numRedraw.Value = MainForm.Conf.MaxRedrawRate;
            numMediaPanelItems.Value = MainForm.Conf.PreviewItems;
            txtTrayIcon.Text = MainForm.Conf.TrayIconText;
            chkMinimise.Checked = MainForm.Conf.MinimiseOnClose;
            if (chkMonitor.Checked && !MainForm.Conf.Monitor)
            {
                Process.Start(Program.AppPath + "iSpyMonitor.exe");
            }
            chkMonitor.Checked = MainForm.Conf.Monitor;

            //ddlImageMode.SelectedItem = MainForm.Conf.BackgroundImageMode;

            tbOpacity.Value = MainForm.Conf.Opacity;
            SetColors();

            gbStorage.Enabled = chkStorage.Checked = MainForm.Conf.Enable_Storage_Management;

            txtYouTubePassword.Text = MainForm.Conf.YouTubePassword;
            txtYouTubeUsername.Text = MainForm.Conf.YouTubeUsername;
            chkBalloon.Checked = MainForm.Conf.BalloonTips;

            txtIPCameraTimeout.Value = MainForm.Conf.IPCameraTimeout;
            txtServerReceiveTimeout.Value = MainForm.Conf.ServerReceiveTimeout;
            txtServerName.Text = MainForm.Conf.ServerName;
            rtbAccessList.Text = MainForm.Conf.AllowedIPList;

            int i = 0, selind = 0;
            foreach (TranslationsTranslationSet set in LocRm.TranslationSets.OrderBy(p => p.Name))
            {
                ddlLanguage.Items.Add(new ListItem(set.Name, new[] {set.CultureCode}));
                if (set.CultureCode == MainForm.Conf.Language)
                    selind = i;
                i++;
            }
            ddlLanguage.SelectedIndex = selind;
            chkAlertWindows.Checked = MainForm.Conf.CreateAlertWindows;
            chkOverlay.Checked = MainForm.Conf.ShowOverlayControls;
            ddlPriority.SelectedIndex = MainForm.Conf.Priority - 1;
            chkInterrupt.Checked = MainForm.Conf.ScreensaverWakeup;
            chkEnableIPv6.Checked = !MainForm.Conf.IPv6Disabled;
            var pbModes = LocRm.GetString("PlaybackModes").Split(',');
            foreach (var s in pbModes)
                ddlPlayback.Items.Add(s.Trim());

            if (MainForm.Conf.PlaybackMode < 0)
                MainForm.Conf.PlaybackMode = 0;
            ddlPlayback.SelectedIndex = MainForm.Conf.PlaybackMode;
            try
            {
                numJPEGQuality.Value = MainForm.Conf.JPEGQuality;
            }
            catch (Exception)
            {
                
            }
            chkStopRecording.Checked = MainForm.Conf.StopSavingOnStorageLimit;
            chkBigButtons.Checked = MainForm.Conf.BigButtons;

            selind = -1;
            i = 0;
            try
            {
                for (int n = 0; n < WaveIn.DeviceCount; n++)
                {
                    ddlTalkMic.Items.Add(WaveIn.GetCapabilities(n).ProductName);
                    if (WaveIn.GetCapabilities(n).ProductName == MainForm.Conf.TalkMic)
                        selind = i;
                    i++;

                }
                ddlTalkMic.Enabled = true;
                if (selind > -1)
                    ddlTalkMic.SelectedIndex = selind;
                else
                {
                    if (ddlTalkMic.Items.Count == 0)
                    {
                        ddlTalkMic.Items.Add(_noDevices);
                        ddlTalkMic.Enabled = false;
                        ddlTalkMic.SelectedIndex = 0;
                    }
                    else
                        ddlTalkMic.SelectedIndex = 0;
                }
            }
            catch (ApplicationException ex)
            {
                MainForm.LogExceptionToFile(ex);
                ddlTalkMic.Items.Add(_noDevices);
                ddlTalkMic.Enabled = false;
            }
            

        }

        private void RenderResources()
        {
            Text = LocRm.GetString("settings");
            btnBrowseVideo.Text = LocRm.GetString("chars_3014702301470230147");
            btnColorArea.Text = LocRm.GetString("AreaHighlight");
            btnColorBack.Text = LocRm.GetString("ObjectBack");
            btnColorMain.Text = LocRm.GetString("MainPanel");
            btnColorTracking.Text = LocRm.GetString("Tracking");
            btnBorderHighlight.Text = LocRm.GetString("BorderHighlight");
            btnColorVolume.Text = LocRm.GetString("Level");
            btnDetectColor.Text = LocRm.GetString("Activity");
            btnNoDetectColor.Text = LocRm.GetString("NoActivity");
            btnTimestampColor.Text = LocRm.GetString("TimeStamp");
            button1.Text = LocRm.GetString("Ok");
            button2.Text = LocRm.GetString("Cancel");
            chkBalloon.Text = LocRm.GetString("ShowBalloonTips");
            chkCheckForUpdates.Text = LocRm.GetString("AutomaticallyCheckForUpda");
            chkErrorReporting.Text = LocRm.GetString("AnonymousErrorReporting");
            chkPasswordProtect.Text = LocRm.GetString("PasswordProtectWhenMinimi");
            chkShowGettingStarted.Text = LocRm.GetString("ShowGettingStarted");
            chkStartup.Text = LocRm.GetString("RunOnStartupthisUserOnly");
            chkStorage.Text = LocRm.GetString("EnableStorageManagementwa");
            chkAutoSchedule.Text = LocRm.GetString("AutoApplySchedule");
            gbStorage.Text = LocRm.GetString("StorageManagement");
            label1.Text = LocRm.GetString("Password");
            label10.Text = LocRm.GetString("MaxMediaFolderSize");
            label11.Text = LocRm.GetString("WhenOver70FullDeleteFiles");
            label12.Text = LocRm.GetString("DaysOld0ForNoDeletions");
            label14.Text = LocRm.GetString("IspyServerName");
            label16.Text = LocRm.GetString("ispyOpacitymayNotW");
            label19.Text = LocRm.GetString("ispyCanAutomaticallyUploa");
            label2.Text = LocRm.GetString("ServerReceiveTimeout");
            label20.Text = LocRm.GetString("additionalControlsForYout");
            label21.Text = LocRm.GetString("TrayIconText");
            label3.Text = LocRm.GetString("MediaDirectory");
            label4.Text = LocRm.GetString("ms");
            label5.Text = LocRm.GetString("YoutubeUsername");
            label6.Text = LocRm.GetString("YoutubePassword");
            label7.Text = LocRm.GetString("ms");
            label8.Text = LocRm.GetString("MjpegReceiveTimeout");
            label9.Text = LocRm.GetString("Mb");
            label18.Text = LocRm.GetString("MaxRecordingThreads");
            label13.Text = LocRm.GetString("PlaybackMode");
            tabPage1.Text = LocRm.GetString("Colors");
            tabPage2.Text = LocRm.GetString("Storage");
            tabPage3.Text = LocRm.GetString("Youtube");
            tabPage4.Text = LocRm.GetString("Timeouts");
            tabPage6.Text = LocRm.GetString("options");
            tabPage7.Text = LocRm.GetString("IPAccess");
            groupBox1.Text = LocRm.GetString("Language");
            linkLabel1.Text = LocRm.GetString("GetLatestList");
            Text = LocRm.GetString("settings");
            linkLabel2.Text = LocRm.GetString("HelpTranslateISpy");
            chkAlertWindows.Text = LocRm.GetString("CreateAlertWindow");
            chkOverlay.Text = LocRm.GetString("ShowOverlayControls");
            lblPriority.Text = LocRm.GetString("Priority");
            btnLogin.Text = LocRm.GetString("Test");
            chkInterrupt.Text = LocRm.GetString("InterruptScreensaverOnAlert");
            label23.Text = LocRm.GetString("JPEGQuality");
            llblHelp.Text = LocRm.GetString("help");
            label17.Text = LocRm.GetString("IPAccessExplainer");
            chkStopRecording.Text = LocRm.GetString("StopRecordingOnLimit");
            label24.Text = LocRm.GetString("MediaPanelItems");
        }


        private void SetColors()
        {
            btnDetectColor.ForeColor = InverseColor(btnDetectColor.BackColor);
            btnNoDetectColor.ForeColor = InverseColor(btnNoDetectColor.BackColor);
            btnColorTracking.ForeColor = InverseColor(btnColorTracking.BackColor);
            btnColorVolume.ForeColor = InverseColor(btnColorVolume.BackColor);
            btnColorMain.ForeColor = InverseColor(btnColorMain.BackColor);
            btnColorArea.ForeColor = InverseColor(btnColorArea.BackColor);
            btnColorBack.ForeColor = InverseColor(btnColorBack.BackColor);
            btnTimestampColor.ForeColor = InverseColor(btnTimestampColor.BackColor);
            btnBorderHighlight.ForeColor = InverseColor(btnBorderHighlight.BackColor);
        }

        private static Color InverseColor(Color colorIn)
        {
            return Color.FromArgb(Rgbmax - colorIn.R,
                                  Rgbmax - colorIn.G, Rgbmax - colorIn.B);
        }

        private void chkStartup_CheckedChanged(object sender, EventArgs e)
        {
        }


        private void BtnBrowseVideoClick(object sender, EventArgs e)
        {
            if (Directory.Exists(txtMediaDirectory.Text))
                fbdSaveLocation.SelectedPath = txtMediaDirectory.Text;
            fbdSaveLocation.ShowDialog(this);
            if (fbdSaveLocation.SelectedPath != "")
            {
                bool success = false;
                try
                {
                    string path = fbdSaveLocation.SelectedPath;
                    if (!path.EndsWith("\\"))
                        path += "\\";
                    Directory.CreateDirectory(path + "video");
                    success = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                if (success)
                {
                    txtMediaDirectory.Text = fbdSaveLocation.SelectedPath;
                    if (!txtMediaDirectory.Text.EndsWith(@"\"))
                        txtMediaDirectory.Text += @"\";
                }
            }
        }

        private void Button3Click(object sender, EventArgs e)
        {
            cdColorChooser.Color = btnNoDetectColor.BackColor;
            if (cdColorChooser.ShowDialog(this) == DialogResult.OK)
            {
                btnNoDetectColor.BackColor = cdColorChooser.Color;
                SetColors();
            }
        }

        private void BtnDetectColorClick(object sender, EventArgs e)
        {
            cdColorChooser.Color = btnDetectColor.BackColor;

            if (cdColorChooser.ShowDialog(this) == DialogResult.OK)
            {
                btnDetectColor.BackColor = cdColorChooser.Color;
                SetColors();
            }
        }

        private void BtnColorTrackingClick(object sender, EventArgs e)
        {
            cdColorChooser.Color = btnColorTracking.BackColor;
            if (cdColorChooser.ShowDialog(this) == DialogResult.OK)
            {
                btnColorTracking.BackColor = cdColorChooser.Color;
                SetColors();
            }
        }

        private void BtnColorVolumeClick(object sender, EventArgs e)
        {
            cdColorChooser.Color = btnColorVolume.BackColor;
            if (cdColorChooser.ShowDialog(this) == DialogResult.OK)
            {
                btnColorVolume.BackColor = cdColorChooser.Color;
                SetColors();
            }
        }

        private void BtnColorMainClick(object sender, EventArgs e)
        {
            cdColorChooser.Color = btnColorMain.BackColor;
            if (cdColorChooser.ShowDialog(this) == DialogResult.OK)
            {
                btnColorMain.BackColor = cdColorChooser.Color;
                MainForm.Conf.MainColor = btnColorMain.BackColor.ToRGBString();
                SetColors();
                ((MainForm) Owner).SetBackground();
            }
        }

        private void BtnColorBackClick(object sender, EventArgs e)
        {
            cdColorChooser.Color = btnColorBack.BackColor;
            if (cdColorChooser.ShowDialog(this) == DialogResult.OK)
            {
                btnColorBack.BackColor = cdColorChooser.Color;
                SetColors();
            }
        }

        private void BtnColorAreaClick(object sender, EventArgs e)
        {
            cdColorChooser.Color = btnColorArea.BackColor;
            if (cdColorChooser.ShowDialog(this) == DialogResult.OK)
            {
                btnColorArea.BackColor = cdColorChooser.Color;
                SetColors();
            }
        }

        private void chkPasswordProtect_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void TbOpacityScroll(object sender, EventArgs e)
        {
            (Owner).Opacity = Convert.ToDouble(tbOpacity.Value)/100;
        }


        private void ChkStorageCheckedChanged(object sender, EventArgs e)
        {
            gbStorage.Enabled = chkStorage.Checked;
        }

        private void BtnTimestampColorClick(object sender, EventArgs e)
        {
            cdColorChooser.Color = btnColorBack.BackColor;
            if (cdColorChooser.ShowDialog(this) == DialogResult.OK)
            {
                btnTimestampColor.BackColor = cdColorChooser.Color;
                SetColors();
            }
        }

        private void chkErrorReporting_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void chkShowGettingStarted_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void ddlLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void LinkLabel1LinkClicked1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkLabel1.Enabled = false;
            Thread t = new Thread(()=>DownloadResources());
            t.Start();
        }

        private void DownloadResources()
        {
            bool success = false;
            try
            {
                var httpRequest = (HttpWebRequest)WebRequest.Create(MainForm.Website + "/admin/translations.xml");

                httpRequest.Timeout = 100000;     // 100 secs

                var webResponse = (HttpWebResponse)httpRequest.GetResponse();

                var doc = new XmlDocument();
                using (var sr = new StreamReader(webResponse.GetResponseStream(), true))
                {
                    doc.Load(sr);
                }
                doc.Save(Program.AppDataPath + @"XML\Translations.xml");
                LocRm.TranslationsList = null;
                success = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, LocRm.GetString("Error"));
            }
            if (success)
            {
                UISync.Execute(ReloadLanguages);
            }
            UISync.Execute(() => linkLabel1.Enabled = true);
        }

        private void ReloadLanguages()
        {           
            ddlLanguage.Items.Clear();
            RenderResources();
            int i = 0, selind = 0;
            foreach (TranslationsTranslationSet set in LocRm.TranslationSets.OrderBy(p => p.Name))
            {
                ddlLanguage.Items.Add(new ListItem(set.Name, new[] { set.CultureCode }));
                if (set.CultureCode == MainForm.Conf.Language)
                    selind = i;
                i++;
            }
            ddlLanguage.SelectedIndex = selind;
            ReloadResources = true;
        }

        private class UISync
        {
            private static ISynchronizeInvoke _sync;

            public static void Init(ISynchronizeInvoke sync)
            {
                _sync = sync;
            }

            public static void Execute(Action action)
            {
                try { _sync.BeginInvoke(action, null); }
                catch { }
            }
        }

        private void LinkLabel2LinkClicked1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl( MainForm.Website+"/yaf/forum.aspx?g=posts&m=678&#post678#post678");
        }

        #region Nested type: ListItem

        private struct ListItem
        {
            private readonly string _name;
            internal readonly string[] Value;

            public ListItem(string name, string[] value)
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
            MainForm.OpenUrl( MainForm.Website+"/userguide-settings.aspx");
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            var service = new YouTubeService("iSpy", MainForm.Conf.YouTubeKey);
            service.setUserCredentials(txtYouTubeUsername.Text, txtYouTubePassword.Text);
            string token = "";
            try
            {
                token = service.QueryClientLoginToken();
            }
            catch(InvalidCredentialsException)
            {
                MessageBox.Show(this, "Login Failed");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            if (token != "")
                MessageBox.Show(this, "Login OK");

        }

        private void chkStopRecording_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void chkMonitor_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void btnBorderHighlight_Click(object sender, EventArgs e)
        {
            cdColorChooser.Color = btnBorderHighlight.BackColor;
            if (cdColorChooser.ShowDialog(this) == DialogResult.OK)
            {
                btnBorderHighlight.BackColor = cdColorChooser.Color;
                SetColors();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ((MainForm) Owner).RunStorageManagement();
        }

    }
}