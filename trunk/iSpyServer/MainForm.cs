using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using System.Xml.Serialization;
using AForge.Video.DirectShow;
using C2BP;
using iSpyServer.iSpyWS;
using Microsoft.Win32;
using Renderers;
using PictureBox = AForge.Controls.PictureBox;
using Timer = System.Timers.Timer;

namespace iSpyServer
{
    /// <summary>
    /// Summary description for MainForm
    /// </summary>
    public class MainForm : Form
    {
        #region Delegates

        public delegate void UpdateLevelHandler(int newLevel);

        #endregion

        public static bool NeedsSync;
        public static bool LoopBack;
        public static bool ShownWarningMedia;
        public static string NL = Environment.NewLine;
        //public static string WebServerIP = "";
        public static string Identifier;
        public static string EmailAddress = "", MobileNumber = "";
        internal static iSpyLANServer MWS;

        private static FilterInfoCollection _VideoFilters;
        public static FilterInfoCollection VideoFilters
        {
            get
            {
                if (_VideoFilters == null)
                    _VideoFilters = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                return _VideoFilters;

            }
        }
        
        private static bool _logging;
        private static string _browser = String.Empty;
        private static readonly Random Random = new Random();
        private static readonly StringBuilder LogFile = new StringBuilder();

        private static readonly string LogTemplate =
            "<html><head><title>iSpyServer Log File</title><style type=\"text/css\">body,td,th,div {font-family:Verdana;font-size:10px}</style></head><body><h1>Log Start: " +
            DateTime.Now.ToLongDateString() +
            "</h1><p><table cellpadding=\"2px\"><!--CONTENT--></table></p></body></html>";

        private static List<objectsMicrophone> _microphones;
        private static List<objectsFloorplan> _floorplans;
        private static List<objectsCommand> _remotecommands;
        private static List<objectsCamera> _cameras;

        private static IPAddress[] _ipv4Addresses; //, _ipv6addresses;
        private readonly FileSystemWatcher _fsw = new FileSystemWatcher();
        public object ContextTarget;

        private Timer _houseKeepingTimer;
        public string LoadFile = "";
        public string NextLog = "";
        private bool _shuttingDown;
        public bool SilentStartup;
        private Timer _updateTimer;
        private bool _closing;
        private MenuItem _aboutHelpItem;
        private ToolStripMenuItem _activateToolStripMenuItem;
        private ToolStripMenuItem _addCameraToolStripMenuItem;
        private ToolStripMenuItem _addMicrophoneToolStripMenuItem;
        private ToolStripMenuItem _autoLayoutToolStripMenuItem;
        private IContainer components;
        private ContextMenuStrip _ctxtMainForm;
        private ContextMenuStrip _ctxtMnu;
        private ContextMenuStrip _ctxtTaskbar;
        private ToolStripMenuItem _deleteToolStripMenuItem;
        private ToolStripMenuItem _editToolStripMenuItem;
        private MenuItem _exitFileItem;
        private ToolStripMenuItem _exitToolStripMenuItem;
        private MenuItem _fileItem;

        private ToolStripMenuItem _fileMenuToolStripMenuItem;
        private ToolStripMenuItem _fullScreenToolStripMenuItem;
        private MenuItem _helpItem;
        private ToolStripMenuItem _helpToolstripMenuItem;
        private ToolStripMenuItem _iPCameraToolStripMenuItem;
        private ToolStripMenuItem _localCameraToolStripMenuItem;
        private PersistWindowState _mWindowState;
        private MainMenu _mainMenu;
        private MenuItem _menuItem1;
        private MenuItem _menuItem10;
        private MenuItem _menuItem11;
        private MenuItem _menuItem12;
        private MenuItem _menuItem14;
        private MenuItem _menuItem16;
        private MenuItem _menuItem19;
        private MenuItem _menuItem2;
        private MenuItem _menuItem20;
        private MenuItem _menuItem21;
        private MenuItem _menuItem22;
        private MenuItem _menuItem26;
        private MenuItem _menuItem27;
        private MenuItem _menuItem29;
        private MenuItem _menuItem30;
        private MenuItem _menuItem31;
        private MenuItem _menuItem33;
        private MenuItem _menuItem34;


        private MenuItem _menuItem36;
        private MenuItem _menuItem37;
        private MenuItem _menuItem38;
        private MenuItem _menuItem39;
        private MenuItem _menuItem5;
        private MenuItem _menuItem8;
        private MenuItem _menuItem9;
        private MenuItem _miApplySchedule;

        private ToolStripMenuItem _microphoneToolStripMenuItem;
        private NotifyIcon _notifyIcon1;
        private ToolStripMenuItem _opacityToolStripMenuItem;
        private ToolStripMenuItem _opacityToolStripMenuItem1;
        private ToolStripMenuItem _opacityToolStripMenuItem2;
        private object _origScreenSaveSetting;
        private Panel _pnlCameras;
        private ToolStripMenuItem _positionToolStripMenuItem;
        private RegistryKey _regkeyScreenSaver;
        private ToolStripMenuItem _resetSizeToolStripMenuItem;
        private ToolStripMenuItem _setInactiveToolStripMenuItem;
        private ToolStripMenuItem _settingsToolStripMenuItem;
        private ToolStripMenuItem _showISpy100PercentOpacityToolStripMenuItem;
        private ToolStripMenuItem _showISpy10PercentOpacityToolStripMenuItem;
        private ToolStripMenuItem _showISpy30OpacityToolStripMenuItem;
        private ToolStripMenuItem _showToolstripMenuItem;
        private ToolStripMenuItem _statusBarToolStripMenuItem;
        private StatusStrip _statusStrip1;
        private ToolStripMenuItem _switchAllOffToolStripMenuItem;
        private ToolStripMenuItem _switchAllOnToolStripMenuItem;
        private ToolStripMenuItem _takePhotoToolStripMenuItem;
        private System.Windows.Forms.Timer _tmrStartup;
        private ToolStrip _toolStrip1;
        private ToolStripButton _toolStripButton4;
        private ToolStripDropDownButton _toolStripDropDownButton2;
        private ToolStripMenuItem _toolStripToolStripMenuItem;
        private ToolStripStatusLabel _tsslStats;
        private ToolStripMenuItem _unlockToolstripMenuItem;
        private ToolStripMenuItem _websiteToolstripMenuItem;

        public MainForm(bool silent, string loadFile)
        {
            SilentStartup = silent;

            if (!SilentStartup)
            {
                _mWindowState = new PersistWindowState();
                _mWindowState.Parent = this;
                // set registry path in HKEY_CURRENT_USER
                _mWindowState.RegistryPath = @"Software\ispy\startup";
            }
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            RenderResources();
            LoadFile = Program.AppPath + @"XML\objects.xml";

            if (loadFile != "")
                LoadFile = loadFile;

            _toolStrip1.Renderer = new WindowsVistaRenderer();
            _pnlCameras.BackColor = iSpyServer.Default.MainColor;
            if (SilentStartup)
            {
                ShowInTaskbar = false;
                ShowIcon = false;
                WindowState = FormWindowState.Minimized;
            }
        }

        public static List<objectsCamera> Cameras
        {
            get
            {
                if (_cameras == null)
                {
                    LoadObjects(Program.AppPath + @"XML\objects.xml");
                }
                return _cameras;
            }
            set { _cameras = value; }
        }


        public static List<objectsMicrophone> Microphones
        {
            get
            {
                if (_microphones == null)
                {
                    LoadObjects(Program.AppPath + @"XML\objects.xml");
                }
                return _microphones;
            }
            set { _microphones = value; }
        }

        public static List<objectsCommand> RemoteCommands
        {
            get
            {
                if (_remotecommands == null)
                {
                    LoadObjects(Program.AppPath + @"XML\objects.xml");
                }
                return _remotecommands;
            }
            set { _remotecommands = value; }
        }

        public static List<objectsFloorplan> FloorPlans
        {
            get
            {
                if (_floorplans == null)
                {
                    LoadObjects(Program.AppPath + @"XML\objects.xml");
                }
                return _floorplans;
            }
            set { _floorplans = value; }
        }

        public static IPAddress[] AddressesIPv4
        {
            get
            {
                if (_ipv4Addresses != null)
                    return _ipv4Addresses;
                _ipv4Addresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                return _ipv4Addresses;
            }
        }

        //IPv6
        //public static IPAddress[] IPv6Addresses
        //{
        //    get
        //    {
        //        if (_ipv6addresses != null)
        //            return _ipv6addresses;
        //        List<IPAddress> _ipv6adds = new List<IPAddress>();
        //        var addressInfoCollection = IPGlobalProperties.GetIPGlobalProperties().GetUnicastAddresses();

        //        foreach (var addressInfo in addressInfoCollection)
        //        {
        //            if (addressInfo.Address.IsIPv6LinkLocal)
        //                _ipv6adds.Add(addressInfo.Address);
        //        }
        //        _ipv6addresses = _ipv6adds.ToArray();
        //        return _ipv6addresses;
        //    }
        //}
        private static string _ipv4Address = "";
        public static string AddressIPv4
        {
            get
            {
                if (_ipv4Address != "")
                    return _ipv4Address;

                string detectip = "";
                foreach (IPAddress ip in AddressListIPv4)
                {

                    if (iSpyServer.Default.IPv4Address == ip.ToString())
                    {
                        _ipv4Address = ip.ToString();
                        break;
                    }

                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {

                        if (!System.Net.IPAddress.IsLoopback(ip))
                        {
                            if (detectip == "")
                                detectip = ip.ToString();
                        }
                    }
                }
                if (_ipv4Address == "")
                    _ipv4Address = detectip;
                iSpyServer.Default.IPv4Address = _ipv4Address;

                return _ipv4Address;
            }
            set { _ipv4Address = value; }
        }

        //IPv6
        //public static string IPv6Address
        //{
        //    get
        //    {

        //        if (iSpyServer.Default.IPv6Address != "")
        //            return iSpyServer.Default.IPv6Address;
        //        foreach (IPAddress _ip in IPv6Addresses)
        //        {
        //            if (_ip.IsIPv6LinkLocal)
        //            {
        //                iSpyServer.Default.IPv6Address = _ip.ToString();
        //                break;
        //            }
        //        }
        //        if (iSpyServer.Default.IPv6Address != "")
        //            return iSpyServer.Default.IPv6Address;
        //        return "localhost";

        //    }
        //}
        public static string IPAddress
        {
            get
            {
                //if (iSpyServer.Default.IPMode == "IPv4")
                return AddressIPv4;
                //return MakeIPv6URL(IPv6Address);
            }
        }

        private static void LoadObjects(string path)
        {
            objects c;
            var s = new XmlSerializer(typeof (objects));
            var fs = new FileStream(path, FileMode.Open);
            TextReader reader = new StreamReader(fs);
            try
            {
                fs.Position = 0;
                c = (objects) s.Deserialize(reader);
                fs.Close();

                _cameras = c.cameras != null ? c.cameras.ToList() : new List<objectsCamera>();
                foreach (objectsCamera oc in _cameras)
                {
                    //will trigger error for old ispy configs
                    int rw = oc.settings.desktopresizewidth;
                    if (rw == 0)
                        throw new Exception("err_old_config");
                }

                _microphones = c.microphones != null ? c.microphones.ToList() : new List<objectsMicrophone>();

                _floorplans = c.floorplans != null ? c.floorplans.ToList() : new List<objectsFloorplan>();

                _remotecommands = c.remotecommands != null ? c.remotecommands.ToList() : new List<objectsCommand>();

                if (_remotecommands.Count == 0)
                {
                    //add default remote commands
                    var cmd = new objectsCommand
                                  {
                                      command = "ispy ALLON",
                                      id = 0,
                                      name = "Switch all on",
                                      smscommand = "ALL ON"
                                  };
                    _remotecommands.Add(cmd);
                    cmd = new objectsCommand
                              {
                                  command = "ispy ALLOFF",
                                  id = 1,
                                  name = "Switch all off",
                                  smscommand = "ALL OFF"
                              };
                    _remotecommands.Add(cmd);
                }

                int camid = 0;
                foreach (objectsCamera cam in _cameras)
                {
                    if (cam.id >= camid)
                        camid = cam.id + 1;

                    if (cam.settings.youtube == null)
                    {
                        cam.settings.youtube = new objectsCameraSettingsYoutube
                                                    {
                                                        autoupload = false,
                                                        category = iSpyServer.Default.YouTubeDefaultCategory,
                                                        tags = "iSpy, Motion Detection, Surveillance",
                                                        @public = true
                                                    };
                    }
                    cam.newrecordingcount = 0;
                    if (cam.directory == null)
                        throw new Exception("err_old_config");

                }
                int micid = 0;
                foreach (objectsMicrophone mic in _microphones)
                {
                    if (mic.id >= micid)
                        micid = mic.id + 1;
                    if (mic.directory == null)
                        throw new Exception("err_old_config");
                    mic.newrecordingcount = 0;
                }
                int fpid = 0;
                foreach (objectsFloorplan ofp in _floorplans)
                {
                    if (ofp.id >= fpid)
                        fpid = ofp.id + 1;
                }
                int rcid = 0;
                foreach (objectsCommand ocmd in _remotecommands)
                {
                    if (ocmd.id >= rcid)
                        rcid = ocmd.id + 1;
                }

                iSpyServer.Default.NextCameraID = camid;
                iSpyServer.Default.NextMicrophoneID = micid;
                iSpyServer.Default.NextFloorPlanID = fpid;
                iSpyServer.Default.NextCommandID = rcid;
                NeedsSync = true;
            }
            catch (Exception)
            {
                MessageBox.Show(LocRM.GetString("ConfigurationChanged"), LocRM.GetString("Error"));
                _cameras = new List<objectsCamera>();
                _microphones = new List<objectsMicrophone>();
                _remotecommands = new List<objectsCommand>();
                _floorplans = new List<objectsFloorplan>();
            }
            reader.Dispose();
            if (fs != null)
                fs.Dispose();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _notifyIcon1.Visible = false;
                _notifyIcon1.Dispose();

                if (components != null)
                {
                    components.Dispose();
                }
                if (_mWindowState != null)
                    _mWindowState.Dispose();
            }
            base.Dispose(disposing);
        }

        // Close the main form
        private void ExitFileItemClick(object sender, EventArgs e)
        {
            Close();
        }

        // On "Help->About"
        private void AboutHelpItemClick(object sender, EventArgs e)
        {
            var form = new AboutForm();
            form.ShowDialog();
            form.Dispose();
        }

        private void VolumeControlDoubleClick(object sender, EventArgs e)
        {
            EditMicrophone(((VolumeLevel) sender).Micobject);
        }


        private static string Zeropad(int i)
        {
            if (i > 9)
                return i.ToString();
            return "0" + i;
        }

        public static void OpenURL(string URL)
        {
            try
            {
                Process.Start(URL);
            }
            catch (Exception)
            {
                try
                {
                    var p = new Process();
                    p.StartInfo.FileName = DefaultBrowser;
                    p.StartInfo.Arguments = URL;
                    p.Start();
                }
                catch (Exception ex2)
                {
                    LogExceptionToFile(ex2);
                }
            }
        }

        private static string DefaultBrowser
        {
            get
            {
                if (!String.IsNullOrEmpty(_browser))
                    return _browser;

                _browser = string.Empty;
                RegistryKey key = null;
                try
                {
                    key = Registry.ClassesRoot.OpenSubKey(@"HTTP\shell\open\command", false);

                    //trim off quotes
                    _browser = key.GetValue(null).ToString().ToLower().Replace("\"", "");
                    if (!_browser.EndsWith(".exe"))
                    {
                        _browser = _browser.Substring(0, _browser.LastIndexOf(".exe") + 4);
                    }
                }
                finally
                {
                    if (key != null) key.Close();
                }
                return _browser;
            }
        }

        private void MainFormLoad(object sender, EventArgs e)
        {
            try { File.WriteAllText(Program.AppDataPath + "exit.txt", "RUNNING"); }
            catch (Exception ex) { LogExceptionToFile(ex); }

            DateTime logdate = DateTime.Now;

            foreach (string s in Directory.GetFiles(Program.AppPath, "log_*", SearchOption.TopDirectoryOnly))
            {
                var fi = new FileInfo(s);
                if (fi.CreationTime < DateTime.Now.AddDays(-5))
                    File.Delete(s);
            }
            NextLog = Zeropad(logdate.Day) + Zeropad(logdate.Month) + logdate.Year;
            int i = 0;
            while (File.Exists(NextLog + "_" + i))
                i++;
            if (i > 0)
                NextLog += "_" + i;

            _fsw.Path = Program.AppPath;
            _fsw.IncludeSubdirectories = false;
            _fsw.Filter = "external_command.txt";
            _fsw.NotifyFilter = NotifyFilters.LastWrite;
            _fsw.Changed += FswChanged;
            _fsw.EnableRaisingEvents = true;

            try
            {
                File.WriteAllText(Program.AppPath + "log.txt", "iSpy Log Start: " + DateTime.Now + Environment.NewLine);
                _logging = true;
            }
            catch (Exception ex)
            {
                if (
                    MessageBox.Show(LocRM.GetString("LogStartError").Replace("[MESSAGE]", ex.Message),
                                    LocRM.GetString("Warning"), MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    Close();
                    return;
                }
            }

            MWS = new iSpyLANServer(this);

            Menu = _mainMenu;
            try
            {
                _notifyIcon1.ContextMenuStrip = _ctxtTaskbar;
                //MWS.RemoteCommand += new iSpyServer.RemoteCommandEventHandler(MWS_RemoteCommand);

                Identifier = Guid.NewGuid().ToString();

                _tmrStartup.Start();
                _regkeyScreenSaver = Registry.CurrentUser.OpenSubKey("Control Panel");
                if (_regkeyScreenSaver!=null)
                {
                    _regkeyScreenSaver = _regkeyScreenSaver.OpenSubKey("Desktop", true);
                    if (_regkeyScreenSaver != null)
                    {
                        _origScreenSaveSetting = _regkeyScreenSaver.GetValue("ScreenSaveActive");
                        _regkeyScreenSaver.SetValue("ScreenSaveActive", "0");
                    }
                }
                


                Application.DoEvents();
                GC.KeepAlive(Program.mutex);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                while (ex.InnerException != null)
                {
                    MessageBox.Show(ex.InnerException.Message, LocRM.GetString("Error"));
                    ex = ex.InnerException;
                }
            }
            try
            {
                LoadObjects(LoadFile);
                RenderObjects();
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocRM.GetString("LoadFailed").Replace("[MESSAGE]", ex.Message));
            }
            SetBackground();
            Opacity = Convert.ToDouble(iSpyServer.Default.Opacity)/100;

            if (iSpyServer.Default.ServerName == "NotSet")
            {
                iSpyServer.Default.ServerName = SystemInformation.ComputerName;
            }

            if (iSpyServer.Default.WSUsername != "" && iSpyServer.Default.WSPassword != "")
            {
            }
            _notifyIcon1.Text = iSpyServer.Default.TrayIconText;
            _autoLayoutToolStripMenuItem.Checked = iSpyServer.Default.AutoLayout;


            _updateTimer = new Timer(500);
            _updateTimer.Elapsed += UpdateTimerElapsed;
            _updateTimer.AutoReset = true;
            _updateTimer.Start();

            _houseKeepingTimer = new Timer(1000);
            _houseKeepingTimer.Elapsed += HouseKeepingTimerElapsed;
            _houseKeepingTimer.AutoReset = true;
            _houseKeepingTimer.SynchronizingObject = this;
            _houseKeepingTimer.Start();
        }

        private void RenderResources()
        {
            Text = "iSpy Server v" + Application.ProductVersion;
            _aboutHelpItem.Text = LocRM.GetString("About");
            _activateToolStripMenuItem.Text = LocRM.GetString("Switchon");
            _addCameraToolStripMenuItem.Text = LocRM.GetString("AddCamera");
            _addMicrophoneToolStripMenuItem.Text = LocRM.GetString("Addmicrophone");
            _autoLayoutToolStripMenuItem.Text = LocRM.GetString("AutoLayout");
            _deleteToolStripMenuItem.Text = LocRM.GetString("remove");
            _editToolStripMenuItem.Text = LocRM.GetString("Edit");
            _exitFileItem.Text = LocRM.GetString("Exit");
            _exitToolStripMenuItem.Text = LocRM.GetString("Exit");
            _fileItem.Text = LocRM.GetString("file");
            _fileMenuToolStripMenuItem.Text = LocRM.GetString("Filemenu");
            _fullScreenToolStripMenuItem.Text = LocRM.GetString("fullScreen");
            _helpItem.Text = LocRM.GetString("help");
            _helpToolstripMenuItem.Text = LocRM.GetString("help");
            _iPCameraToolStripMenuItem.Text = LocRM.GetString("IpCamera");
            _localCameraToolStripMenuItem.Text = LocRM.GetString("LocalCamera");
            _menuItem1.Text = LocRM.GetString("chars_2949165");
            _menuItem10.Text = LocRM.GetString("checkForUpdates");
            _menuItem11.Text = LocRM.GetString("reportBugFeedback");
            _menuItem16.Text = LocRM.GetString("View");

            _menuItem19.Text = LocRM.GetString("saveObjectList");
            _menuItem2.Text = LocRM.GetString("help");
            _menuItem20.Text = LocRM.GetString("Logfile");
            _menuItem21.Text = LocRM.GetString("openObjectList");
            _menuItem22.Text = LocRM.GetString("LogFiles");

            _menuItem26.Text = LocRM.GetString("supportIspyWithADonation");
            _menuItem27.Text = LocRM.GetString("chars_2949165");
            _menuItem29.Text = LocRM.GetString("Current");

            _menuItem30.Text = LocRM.GetString("chars_2949165");
            _menuItem31.Text = LocRM.GetString("removeAllObjects");
            _miApplySchedule.Text = LocRM.GetString("ApplySchedule");
            _menuItem33.Text = LocRM.GetString("SwitchOffAllObjects");
            _menuItem34.Text = LocRM.GetString("SwitchOnAllObjects");

            _menuItem36.Text = LocRM.GetString("Edit");
            _menuItem37.Text = LocRM.GetString("CamerasAndMicrophones");
            _menuItem38.Text = LocRM.GetString("ViewUpdateInformation");
            _menuItem39.Text = LocRM.GetString("AutoLayoutObjects");

            _menuItem5.Text = LocRM.GetString("GoTowebsite");

            _menuItem8.Text = LocRM.GetString("settings");
            _menuItem9.Text = LocRM.GetString("options");
            _microphoneToolStripMenuItem.Text = LocRM.GetString("Microphone");
            _notifyIcon1.Text = LocRM.GetString("Ispy");

            _opacityToolStripMenuItem.Text = LocRM.GetString("Opacity10");
            _opacityToolStripMenuItem1.Text = LocRM.GetString("Opacity30");
            _opacityToolStripMenuItem2.Text = LocRM.GetString("Opacity100");
            _positionToolStripMenuItem.Text = LocRM.GetString("Position");

            _resetSizeToolStripMenuItem.Text = LocRM.GetString("ResetSize");
            _setInactiveToolStripMenuItem.Text = LocRM.GetString("switchOff");
            _settingsToolStripMenuItem.Text = LocRM.GetString("settings");

            _showISpy100PercentOpacityToolStripMenuItem.Text = LocRM.GetString("ShowIspy100Opacity");
            _showISpy10PercentOpacityToolStripMenuItem.Text = LocRM.GetString("ShowIspy10Opacity");
            _showISpy30OpacityToolStripMenuItem.Text = LocRM.GetString("ShowIspy30Opacity");
            _showToolstripMenuItem.Text = LocRM.GetString("showIspy");
            _statusBarToolStripMenuItem.Text = LocRM.GetString("Statusbar");
            _switchAllOffToolStripMenuItem.Text = LocRM.GetString("SwitchAllOff");
            _switchAllOnToolStripMenuItem.Text = LocRM.GetString("SwitchAllOn");
            _takePhotoToolStripMenuItem.Text = LocRM.GetString("TakePhoto");

            _toolStripButton4.Text = LocRM.GetString("settings");

            _toolStripDropDownButton2.Text = LocRM.GetString("Add");

            _toolStripToolStripMenuItem.Text = LocRM.GetString("toolStrip");
            _tsslStats.Text = LocRM.GetString("Loading");
            _unlockToolstripMenuItem.Text = LocRM.GetString("unlock");
            _websiteToolstripMenuItem.Text = LocRM.GetString("website");
        }

        int _pingCounter = 0;
        private void HouseKeepingTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!MWS.Running && MWS.numErr >= 5)
                StopAndStartServer();

            _pingCounter++;
            if (_pingCounter == 301)
            {
                _pingCounter = 0;
                //auto save
                try
                {
                    SaveObjects("");
                }
                catch (Exception ex)
                {
                    LogExceptionToFile(ex);
                }
            }
        }


        private void UpdateTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _updateTimer.Stop();

            foreach (Control c in _pnlCameras.Controls)
            {
                try
                {
                    switch (c.GetType().ToString())
                    {
                        case "iSpyServer.CameraWindow":
                            ((CameraWindow) c).Tick();
                            break;
                        case "iSpyServer.VolumeLevel":
                            ((VolumeLevel) c).Tick();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogExceptionToFile(ex);
                }
            }
            if (!_shuttingDown)
                _updateTimer.Start();
        }

        private void FswChanged(object sender, FileSystemEventArgs e)
        {
            _fsw.EnableRaisingEvents = false;
            string txt = "";
            bool err = true;
            int i = 0;
            while (err && i < 5)
            {
                try
                {
                    using (var fs = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var sr = new StreamReader(fs))
                        {
                            while (sr.EndOfStream == false)
                            {
                                txt = sr.ReadLine();
                                err = false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogExceptionToFile(ex);
                    i++;
                    Thread.Sleep(500);
                }
            }
            if (txt != null)
            {
                if (txt.ToLower().StartsWith("open "))
                {
                    Invoke(new LoadObjectListDelegate(LoadObjectList), txt.Substring(5).Trim('"'));
                }
                if (txt.ToLower().StartsWith("commands "))
                {
                    string cmd = txt.Substring(9).Trim('"');
                    string[] commands = cmd.Split('|');
                    foreach (string command in commands)
                    {
                        if (command != "")
                            Invoke(new ProcessCommandInternalDelegate(ProcessCommandInternal), command);
                    }
                }
            }
            _fsw.EnableRaisingEvents = true;
        }

        private void ProcessCommandInternal(string command)
        {
        }

        public void SetBackground()
        {
            _pnlCameras.BackColor = iSpyServer.Default.MainColor;
        }


        protected override void OnClosed(EventArgs e)
        {
            try
            {
                _notifyIcon1.Visible = false;

                _notifyIcon1.Icon.Dispose();
                _notifyIcon1.Dispose();
            }
            catch (Exception)
            {
            }
            base.OnClosed(e);
        }

        private void MenuItem2Click(object sender, EventArgs e)
        {
            StartBrowser("http://www.ispyconnect.com/ispyserver.aspx");
        }

        internal static void StopAndStartServer()
        {
            try
            {
                MWS.StopServer();
                Application.DoEvents();
                MWS.StartServer();
            }
            catch (Exception ex)
            {
                LogExceptionToFile(ex);
            }
        }

        private void MenuItem5Click(object sender, EventArgs e)
        {
            StartBrowser("http://www.ispyconnect.com/");
        }

        private void MenuItem10Click(object sender, EventArgs e)
        {
            CheckForUpdates(false);
        }

        private void CheckForUpdates(bool suppressMessages)
        {
            string version = "";
            try
            {
                var ispy = new iSpy();
                version = ispy.ProductLatestVersionGet(12);
            }
            catch (Exception ex)
            {
                LogExceptionToFile(ex);
                if (!suppressMessages)
                    MessageBox.Show(LocRM.GetString("CheckUpdateError"), LocRM.GetString("Error"));
            }
            if (version != "")
            {
                var verThis = new Version(Application.ProductVersion);
                var verLatest = new Version(version);
                if (verThis < verLatest)
                {
                    var nv = new NewVersion();
                    nv.ShowDialog(this);
                    nv.Dispose();
                }
                else
                {
                    if (!suppressMessages)
                        MessageBox.Show(LocRM.GetString("HaveLatest"), LocRM.GetString("Note"), MessageBoxButtons.OK);
                }
            }
        }

        private void MenuItem8Click(object sender, EventArgs e)
        {
            ShowSettings(0);
        }

        public void ShowSettings(int tabIndex)
        {
            var settings = new Settings();
            settings.Owner = this;
            settings.InitialTab = tabIndex;
            if (settings.ShowDialog(this) == DialogResult.OK)
            {
                _pnlCameras.BackColor = iSpyServer.Default.MainColor;
                _notifyIcon1.Text = iSpyServer.Default.TrayIconText;
            }

            if (settings.ReloadResources)
                RenderResources();

            AddressIPv4 = "";//forces reload
            settings.Dispose();
        }

        private void MenuItem11Click(object sender, EventArgs e)
        {
            var fb = new Feedback();
            fb.ShowDialog(this);
            fb.Dispose();
        }

        private void MainFormResize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                if (iSpyServer.Default.BalloonTips)
                {
                    if (iSpyServer.Default.BalloonTips)
                    {
                        _notifyIcon1.BalloonTipText = LocRM.GetString("RunningInTaskBar");
                        _notifyIcon1.ShowBalloonTip(3000);
                    }
                }
            }
            else
            {
                if (iSpyServer.Default.AutoLayout)
                    LayoutObjects(0, 0);
            }
        }

        private void NotifyIcon1DoubleClick(object sender, EventArgs e)
        {
            if (Visible == false || WindowState == FormWindowState.Minimized)
            {
                if (iSpyServer.Default.Enable_Password_Protect)
                {
                    var cp = new CheckPassword();
                    cp.ShowDialog(this);
                    if (cp.DialogResult == DialogResult.OK)
                    {
                        ShowForm(-1);
                    }
                    cp.Dispose();
                }
                else
                {
                    ShowForm(-1);
                }
            }
            else
            {
                ShowForm(-1);
            }
        }

        private void MainFormFormClosing1(object sender, FormClosingEventArgs e)
        {
            Exit();
        }

        private void Exit()
        {
            _houseKeepingTimer.Stop();
            _updateTimer.Stop();
            _shuttingDown = true;
            MWS.StopServer();
            if (iSpyServer.Default.BalloonTips)
            {
                if (iSpyServer.Default.BalloonTips)
                {
                    _notifyIcon1.BalloonTipText = LocRM.GetString("ShuttingDown");
                    _notifyIcon1.ShowBalloonTip(3000);
                }
            }
            try
            {
                iSpyServer.Default.Save();
            }
            catch (Exception ex)
            {
                LogExceptionToFile(ex);
            }
            _closing = true;
            try
            {
                SaveObjects("");
            }
            catch (Exception ex)
            {
                LogExceptionToFile(ex);
            }
            try
            {
                RemoveObjects();
            }
            catch (Exception ex)
            {
                LogExceptionToFile(ex);
            }
            try
            {
                MWS.StopServer();
            }
            catch (Exception ex)
            {
                LogExceptionToFile(ex);
            }
            Application.DoEvents();
            //restore screensaver
            try
            {
                _regkeyScreenSaver.SetValue("ScreenSaveActive", _origScreenSaveSetting);
            }
            catch (Exception ex)
            {
                LogExceptionToFile(ex);
            }
            
            try
            {
                File.WriteAllText(Program.AppDataPath + "exit.txt", "OK");
            }
            catch (Exception ex)
            {
                LogExceptionToFile(ex);
            }
            
            WriteLog();
        }

        private void WriteLog()
        {
            string fc = LogTemplate.Replace("<!--CONTENT-->", LogFile.ToString());
            File.WriteAllText(Program.AppPath + @"log_" + NextLog + ".htm", fc);
        }

        private void RemoveObjects()
        {
            bool removed = true;
            while (removed)
            {
                removed = false;
                foreach (Control c in _pnlCameras.Controls)
                {
                    if (c.GetType() == typeof (CameraWindow))
                    {
                        var cameraControl = (CameraWindow) c;
                        RemoveCamera(cameraControl);
                        removed = true;
                        break;
                    }
                    if (c.GetType() == typeof (VolumeLevel))
                    {
                        var volumeControl = (VolumeLevel) c;
                        RemoveMicrophone(volumeControl);
                        removed = true;
                        break;
                    }
                }
            }
        }


        private void RenderObjects()
        {
            foreach (Control c in _pnlCameras.Controls)
            {
                if (c.GetType() == typeof (CameraWindow))
                {
                    var cameraControl = (CameraWindow) c;
                    RemoveCamera(cameraControl);
                }

                if (c.GetType() == typeof (VolumeLevel))
                {
                    var volumeControl = (VolumeLevel) c;
                    RemoveMicrophone(volumeControl);
                }
            }

            foreach (objectsCamera oc in Cameras)
            {
                DisplayCamera(oc);
            }
            foreach (objectsMicrophone om in Microphones)
            {
                DisplayMicrophone(om);
            }
            ApplySchedule();
            NeedsSync = true;
        }

        private void SetCameraEvents(CameraWindow cameraControl)
        {
            cameraControl.MouseEnter += CameraControl_MouseEnter;
            cameraControl.MouseDown += CameraControlMouseDown;
            cameraControl.MouseUp += CameraControl_MouseUp;
            cameraControl.MouseLeave += CameraControl_MouseLeave;
            cameraControl.MouseMove += CameraControlMouseMove;
            cameraControl.DoubleClick += CameraControlDoubleClick;
            cameraControl.KeyDown += CameraControl_KeyDown;
        }

        private void SetMicrophoneEvents(VolumeLevel vw)
        {
            vw.DoubleClick += VolumeControlDoubleClick;
            vw.KeyDown += VolumeControl_KeyDown;
            vw.MouseEnter += VolumeControl_MouseEnter;
            vw.MouseDown += VolumeControlMouseDown;
            vw.MouseUp += VolumeControl_MouseUp;
            vw.MouseLeave += VolumeControl_MouseLeave;
            vw.MouseMove += VolumeControlMouseMove;
        }

        internal void DisplayMicrophone(objectsMicrophone mic)
        {
            var micControl = new VolumeLevel(mic);
            SetMicrophoneEvents(micControl);
            micControl.BackColor = iSpyServer.Default.BackColor;
            _pnlCameras.Controls.Add(micControl);
            micControl.Location = new Point(mic.x, mic.y);
            micControl.Size = new Size(mic.width, mic.height);
            micControl.BringToFront();
            if (mic.settings.active)
                micControl.Enable();

        }


        internal void EditCamera(objectsCamera cr)
        {
            int cameraId = Convert.ToInt32(cr.id);
            CameraWindow cw = null;

            foreach (Control c in _pnlCameras.Controls)
            {
                if (c.GetType() == typeof (CameraWindow))
                {
                    var cameraControl = (CameraWindow) c;
                    if (cameraControl.Camobject.id == cameraId)
                    {
                        cw = cameraControl;
                        break;
                    }
                }
            }

            if (cw != null)
            {
                var ac = new AddCamera {CameraControl = cw};
                ac.ShowDialog(this);
                ac.Dispose();
            }
        }

        internal void EditMicrophone(objectsMicrophone om)
        {
            VolumeLevel vlf = null;

            foreach (Control c in _pnlCameras.Controls)
            {
                if (c.GetType() == typeof (VolumeLevel))
                {
                    var vl = (VolumeLevel) c;
                    if (vl.Micobject.id == om.id)
                    {
                        vlf = vl;
                        break;
                    }
                }
            }

            if (vlf != null)
            {
                var am = new AddMicrophone {VolumeLevel = vlf};
                am.ShowDialog(this);
                am.Dispose();
            }
        }

        public CameraWindow GetCamera(int cameraId)
        {
            foreach (Control c in _pnlCameras.Controls)
            {
                if (c.GetType() == typeof (CameraWindow))
                {
                    var cw = (CameraWindow) c;
                    if (cw.Camobject.id == cameraId)
                    {
                        return cw;
                    }
                }
            }
            return null;
        }

        public VolumeLevel GetMicrophone(int microphoneId)
        {
            foreach (Control c in _pnlCameras.Controls)
            {
                if (c.GetType() == typeof (VolumeLevel))
                {
                    var vw = (VolumeLevel) c;
                    if (vw.Micobject.id == microphoneId)
                    {
                        return vw;
                    }
                }
            }
            return null;
        }

        private void RemoveCamera(CameraWindow cameraControl)
        {
            cameraControl.ShuttingDown = true;
            cameraControl.Disable();
            cameraControl.MouseEnter -= CameraControl_MouseEnter;
            cameraControl.MouseDown -= CameraControlMouseDown;
            cameraControl.MouseUp -= CameraControl_MouseUp;
            cameraControl.MouseLeave -= CameraControl_MouseLeave;
            cameraControl.MouseMove -= CameraControlMouseMove;
            cameraControl.DoubleClick -= CameraControlDoubleClick;
            cameraControl.KeyDown -= CameraControl_KeyDown;


            _pnlCameras.Controls.Remove(cameraControl);

            if (!_closing)
            {
                CameraWindow control = cameraControl;
                objectsCamera oc = Cameras.SingleOrDefault(p => p.id == control.Camobject.id);
                if (oc != null)
                    Cameras.Remove(oc);

                NeedsSync = true;
                SetNewStartPosition();
            }
            Application.DoEvents();
            cameraControl.Dispose();
        }

        private void RemoveMicrophone(VolumeLevel volumeControl)
        {
            volumeControl.Disable();
            _pnlCameras.Controls.Remove(volumeControl);

            if (!_closing)
            {
                VolumeLevel control = volumeControl;
                objectsMicrophone om = Microphones.SingleOrDefault(p => p.id == control.Micobject.id);
                foreach (objectsCamera oc in Cameras.Where(p => p.settings.micpair == om.id).ToList())
                {
                    oc.settings.micpair = -1;
                }
                if (om != null)
                    Microphones.Remove(om);
                SetNewStartPosition();
                NeedsSync = true;
            }
            Application.DoEvents();
            volumeControl.Dispose();
        }

        private void AddCamera(int videoSourceIndex)
        {
            var oc = new objectsCamera
                         {
                             alerts = new objectsCameraAlerts(),
                             detector = new objectsCameraDetector
                                            {
                                                motionzones =
                                                    new objectsCameraDetectorZone
                                                    [0]
                                            }
                         };
            oc.notifications = new objectsCameraNotifications();
            oc.recorder = new objectsCameraRecorder();
            oc.schedule = new objectsCameraSchedule {entries = new objectsCameraScheduleEntry[0]};
            oc.settings = new objectsCameraSettings();
            oc.ftp = new objectsCameraFtp();

            oc.id = -1;
            oc.directory = RandomString(5);
            oc.ptz = -1;
            oc.x = 0;
            oc.y = 0;
            oc.flipx = oc.flipy = false;
            oc.width = 320;
            oc.height = 240;
            oc.name = "";
            oc.description = "";
            oc.resolution = "320x240";
            oc.newrecordingcount = 0;

            oc.alerts.active = false;
            oc.alerts.mode = "movement";
            oc.alerts.alertoptions = "false,false";
            oc.alerts.objectcountalert = 1;
            oc.alerts.minimuminterval = 180;

            oc.notifications.sendemail = false;
            oc.notifications.sendsms = false;
            oc.notifications.sendmms = false;
            oc.notifications.emailgrabinterval = 0;

            oc.ftp.enabled = false;
            oc.ftp.port = 21;
            oc.ftp.server = "ftp://";
            oc.ftp.interval = 10;
            oc.ftp.filename = "mylivecamerafeed.jpg";
            oc.ftp.ready = true;
            oc.ftp.text = "www.ispyconnect.com";

            oc.schedule.active = false;

            oc.settings.active = false;
            oc.settings.deleteavi = true;
            oc.settings.ffmpeg = iSpyServer.Default.FFMPEG_Camera;
            oc.settings.emailaddress = EmailAddress;
            oc.settings.smsnumber = MobileNumber;
            oc.settings.suppressnoise = true;
            oc.settings.login = "";
            oc.settings.password = "";
            oc.settings.useragent = "Mozilla/5.0";
            oc.settings.frameinterval = 10;
            oc.settings.sourceindex = videoSourceIndex;
            oc.settings.micpair = -1;
            oc.settings.frameinterval = 200;
            oc.settings.framerate = 10;
            oc.settings.timestamplocation = 0;
            oc.settings.timestampformatter = "FPS: {FPS} {0:G} ";
            oc.settings.notifyondisconnect = false;

            oc.settings.youtube = new objectsCameraSettingsYoutube();
            oc.settings.youtube.autoupload = false;
            oc.settings.youtube.category = iSpyServer.Default.YouTubeDefaultCategory;
            oc.settings.youtube.tags = "iSpy, Motion Detection, Surveillance";
            oc.settings.youtube.@public = true;
            oc.settings.desktopresizeheight = 480;
            oc.settings.desktopresizewidth = 640;

            oc.detector.recordondetect = true;
            oc.detector.keepobjectedges = false;
            oc.detector.processeveryframe = 1;
            oc.detector.nomovementinterval = 30;
            oc.detector.movementinterval = 0;
            oc.detector.calibrationdelay = 15;
            oc.detector.color = ColorTranslator.ToHtml(iSpyServer.Default.TrackingColor);
            oc.detector.type = "None";
            oc.detector.postprocessor = "None";
            oc.detector.sensitivity = 80;

            oc.recorder.bufferframes = 30;
            oc.recorder.inactiverecord = 5;
            oc.recorder.timelapse = 0;
            oc.recorder.timelapseframes = 0;
            oc.recorder.maxrecordtime = 900;


            var cameraControl = new CameraWindow(oc) {BackColor = iSpyServer.Default.BackColor};
            _pnlCameras.Controls.Add(cameraControl);

            cameraControl.Location = new Point(oc.x, oc.y);
            cameraControl.Size = new Size(320, 240);
            cameraControl.AutoSize = true;
            cameraControl.BringToFront();
            SetCameraEvents(cameraControl);

            var ac = new AddCamera {CameraControl = cameraControl};
            ac.ShowDialog(this);
            if (ac.DialogResult == DialogResult.OK)
            {
                ac.CameraControl.Camobject.id = iSpyServer.Default.NextCameraID;
                Cameras.Add(oc);
                iSpyServer.Default.NextCameraID++;

                SetNewStartPosition();
                NeedsSync = true;
            }
            else
            {
                cameraControl.Disable();
                _pnlCameras.Controls.Remove(cameraControl);
                cameraControl.Dispose();
            }
            ac.Dispose();
        }

        private void AddMicrophone(int audioSourceIndex)
        {
            var om = new objectsMicrophone
                         {
                             alerts = new objectsMicrophoneAlerts(),
                             detector = new objectsMicrophoneDetector(),
                             notifications = new objectsMicrophoneNotifications(),
                             recorder = new objectsMicrophoneRecorder(),
                             schedule = new objectsMicrophoneSchedule
                                            {
                                                entries
                                                    =
                                                    new objectsMicrophoneScheduleEntry
                                                    [
                                                    0
                                                    ]
                                            }
                         };
            om.settings = new objectsMicrophoneSettings();

            om.id = -1;
            om.directory = RandomString(5);
            om.x = 0;
            om.y = 0;
            om.width = 160;
            om.height = 40;
            om.name = "";
            om.description = "";
            om.newrecordingcount = 0;

            int port = 257;
            foreach (objectsMicrophone om2 in Microphones)
            {
                if (om2.port > port)
                    port = om2.port + 1;
            }
            om.port = port;

            om.settings.typeindex = 0;
            if (audioSourceIndex == 2)
                om.settings.typeindex = 1;
            om.settings.deletewav = true;
            om.settings.ffmpeg = iSpyServer.Default.FFMPEG_Microphone;
            om.settings.buffer = 4;
            om.settings.samples = 22050;
            om.settings.bits = 16;
            om.settings.channels = 1;
            om.settings.decompress = true;
            om.settings.smsnumber = MobileNumber;
            om.settings.emailaddress = EmailAddress;
            om.settings.active = false;
            om.settings.notifyondisconnect = false;

            om.detector.sensitivity = 60;
            om.detector.nosoundinterval = 30;
            om.detector.soundinterval = 0;
            om.detector.recordondetect = true;

            om.alerts.mode = "sound";
            om.alerts.minimuminterval = 60;
            om.alerts.executefile = "";
            om.alerts.active = false;
            om.alerts.alertoptions = "false,false";

            om.recorder.inactiverecord = 5;
            om.recorder.maxrecordtime = 900;

            om.notifications.sendemail = false;
            om.notifications.sendsms = false;

            om.schedule.active = false;

            var volumeControl = new VolumeLevel(om) {BackColor = iSpyServer.Default.BackColor};
            _pnlCameras.Controls.Add(volumeControl);

            volumeControl.Location = new Point(om.x, om.y);
            volumeControl.Size = new Size(160, 40);
            volumeControl.BringToFront();
            SetMicrophoneEvents(volumeControl);

            var am = new AddMicrophone {VolumeLevel = volumeControl};
            am.ShowDialog(this);


            if (am.DialogResult == DialogResult.OK)
            {
                am.VolumeLevel.Micobject.id = iSpyServer.Default.NextMicrophoneID;
                iSpyServer.Default.NextMicrophoneID++;
                Microphones.Add(om);
                SetNewStartPosition();
                NeedsSync = true;
            }
            else
            {
                volumeControl.Disable();
                _pnlCameras.Controls.Remove(volumeControl);
                volumeControl.Dispose();
            }
            am.Dispose();
        }

        private static string RandomString(int length)
        {
            var builder = new StringBuilder();

            char ch;
            for (int i = 0; i < length; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26*Random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }

        private void SetNewStartPosition()
        {
            if (iSpyServer.Default.AutoLayout)
                LayoutObjects(0, 0);
        }

        private void TmrStartupTick(object sender, EventArgs e)
        {
            _tmrStartup.Stop();
            StopAndStartServer();
            iSpyServer.Default.Subscribed = false;

            if (iSpyServer.Default.Enable_Update_Check && !SilentStartup)
            {
                CheckForUpdates(true);
            }
            if (SilentStartup)
            {
                _mWindowState = new PersistWindowState {Parent = this, RegistryPath = @"Software\ispyserver\startup"};
                // set registry path in HKEY_CURRENT_USER
            }
            SilentStartup = false;
            _tsslStats.Text = "OK";
            _tmrStartup.Dispose();
        }

        public static IPAddress[] AddressListIPv4
        {
            get
            {
                if (_ipv4Addresses != null)
                    return _ipv4Addresses;
                _ipv4Addresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                return _ipv4Addresses;
            }
        }

        public CameraWindow GetCameraWindow(int cameraId)
        {
            foreach (Control c in _pnlCameras.Controls)
            {
                if (c.GetType() == typeof (CameraWindow))
                {
                    var cw = (CameraWindow) c;
                    if (cw.Camobject.id == cameraId)
                        return cw;
                }
            }
            return null;
        }

        public VolumeLevel GetVolumeLevel(int microphoneId)
        {
            foreach (Control c in _pnlCameras.Controls)
            {
                if (c.GetType() == typeof (VolumeLevel))
                {
                    var vw = (VolumeLevel) c;
                    if (vw.Micobject.id == microphoneId)
                        return vw;
                }
            }
            return null;
        }

        private void SetInactiveToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (ContextTarget.GetType() == typeof (CameraWindow))
            {
                var cameraControl = ((CameraWindow) ContextTarget);
                cameraControl.Disable();
            }
            else
            {
                if (ContextTarget.GetType() == typeof (VolumeLevel))
                {
                    var vf = ((VolumeLevel) ContextTarget);
                    vf.Disable();
                }
            }
        }

        private void EditToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (ContextTarget.GetType() == typeof (CameraWindow))
            {
                EditCamera(((CameraWindow) ContextTarget).Camobject);
            }
            if (ContextTarget.GetType() == typeof (VolumeLevel))
            {
                EditMicrophone(((VolumeLevel) ContextTarget).Micobject);
            }
        }

        private void DeleteToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (ContextTarget.GetType() == typeof (CameraWindow))
            {
                RemoveCamera((CameraWindow) ContextTarget);
            }
            if (ContextTarget.GetType() == typeof (VolumeLevel))
            {
                RemoveMicrophone((VolumeLevel) ContextTarget);
            }
        }

        private void ToolStripButton4Click(object sender, EventArgs e)
        {
            ShowSettings(0);
        }

        internal static void LogExceptionToFile(Exception ex, string info)
        {
            ex.HelpLink = info + ": " + ex.Message;
            LogExceptionToFile(ex);
        }

        internal static void LogExceptionToFile(Exception ex)
        {
            if (!_logging)
                return;
            try
            {
                string em = ex.HelpLink + "<br/>" + ex.Message + "<br/>" + ex.Source + "<br/>" + ex.StackTrace +
                             "<br/>" + ex.InnerException + "<br/>" + ex.Data;
                LogFile.Append("<tr><td style=\"color:red\" valign=\"top\">Exception</td><td valign=\"top\">" +
                               DateTime.Now.ToLongTimeString() + "</td><td valign=\"top\">" + em + "</td></tr>");
            }
            catch
            {
                //do nothing
            }
        }

        internal static void LogMessageToFile(String message)
        {
            if (!_logging)
                return;

            try
            {
                LogFile.Append("<tr><td style=\"color:green\" valign=\"top\">Message</td><td valign=\"top\">" +
                               DateTime.Now.ToLongTimeString() + "</td><td valign=\"top\">" + message + "</td></tr>");
            }
            catch
            {
                //do nothing
            }
        }

        internal static void LogErrorToFile(String message)
        {
            if (!_logging)
                return;

            try
            {
                LogFile.Append("<tr><td style=\"color:red\" valign=\"top\">Error</td><td valign=\"top\">" +
                               DateTime.Now.ToLongTimeString() + "</td><td valign=\"top\">" + message + "</td></tr>");
            }
            catch
            {
                //do nothing
            }
        }

        public static void GoSubscribe()
        {
            Help.ShowHelp(null, "http://www.ispyconnect.com/subscribe.aspx");
        }

        private void ActivateToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (ContextTarget.GetType() == typeof (CameraWindow))
            {
                var cameraControl = ((CameraWindow) ContextTarget);
                cameraControl.Enable();
            }
            else
            {
                if (ContextTarget.GetType() == typeof (VolumeLevel))
                {
                    var vf = ((VolumeLevel) ContextTarget);
                    vf.Enable();
                }
            }
        }

        private void WebsiteToolstripMenuItemClick(object sender, EventArgs e)
        {
            StartBrowser("http://www.ispyconnect.com/");
        }

        private void HelpToolstripMenuItemClick(object sender, EventArgs e)
        {
            StartBrowser("http://www.ispyconnect.com/help.aspx");
        }

        private void ShowToolstripMenuItemClick(object sender, EventArgs e)
        {
            ShowForm(-1);
        }

        private void ShowForm(double opacity)
        {
            Activate();
            Visible = true;
            if (WindowState == FormWindowState.Minimized)
            {
                Show();
                WindowState = FormWindowState.Normal;
            }
            if (opacity > -1)
                Opacity = opacity;
            TopMost = true;
            TopMost = false;
            BringToFront();
            Focus();
        }

        private void UnlockToolstripMenuItemClick(object sender, EventArgs e)
        {
            ShowUnlock();
        }

        private void ShowUnlock()
        {
            var cp = new CheckPassword();
            cp.ShowDialog(this);
            if (cp.DialogResult == DialogResult.OK)
            {
                Activate();
                Visible = true;
                if (WindowState == FormWindowState.Minimized)
                {
                    Show();
                    WindowState = FormWindowState.Normal;
                }
                Focus();
            }
            cp.Dispose();
        }

        private void NotifyIcon1Click(object sender, EventArgs e)
        {
        }

        private void AddCameraToolStripMenuItemClick(object sender, EventArgs e)
        {
            AddCamera(3);
        }

        private void AddMicrophoneToolStripMenuItemClick(object sender, EventArgs e)
        {
            AddMicrophone(1);
        }

        private void CtxtMainFormOpening(object sender, CancelEventArgs e)
        {
            if (_ctxtMnu.Visible)
                e.Cancel = true;
        }


        public static void StartBrowser(string url)
        {
            if (url != "")
                Help.ShowHelp(null, url);
        }

        private void ExitToolStripMenuItemClick(object sender, EventArgs e)
        {
            Close();
        }

        private void MenuItem20Click(object sender, EventArgs e)
        {
            WriteLog();
            Process.Start(Program.AppPath + "log_" + NextLog + ".htm");
        }

        private void ResetSizeToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (ContextTarget.GetType() == typeof (CameraWindow))
            {
                var cw = (CameraWindow) ContextTarget;
                if (cw.Camera != null && cw.Camera.LastFrameUnmanaged != null)
                {
                    cw.Width = cw.Camera.LastFrameUnmanaged.Width;
                    cw.Height = cw.Camera.LastFrameUnmanaged.Height;
                    cw.Camobject.width = cw.Width;
                    cw.Camobject.height = cw.Height;
                    cw.Invalidate();
                }
                else
                {
                    cw.Width = 320;
                    cw.Height = 240;
                    cw.Camobject.width = cw.Width;
                    cw.Camobject.height = cw.Height;
                    cw.Invalidate();
                }
            }
            else
            {
                if (ContextTarget.GetType() == typeof (VolumeLevel))
                {
                    var vl = (VolumeLevel) ContextTarget;
                    vl.Width = 200;
                    vl.Height = 40;
                    vl.Micobject.width = vl.Width;
                    vl.Micobject.height = vl.Height;
                }
            }
        }

        private void SettingsToolStripMenuItemClick(object sender, EventArgs e)
        {
            ShowSettings(0);
        }

        private void FullScreenToolStripMenuItemClick(object sender, EventArgs e)
        {
            _fullScreenToolStripMenuItem.Checked = !_fullScreenToolStripMenuItem.Checked;
            if (_fullScreenToolStripMenuItem.Checked)
            {
                WindowState = FormWindowState.Maximized;
                FormBorderStyle = FormBorderStyle.None;
                WinApi.SetWinFullScreen(Handle);
            }
            else
            {
                WindowState = FormWindowState.Maximized;
                FormBorderStyle = FormBorderStyle.Sizable;
            }
        }

        private void MenuItem19Click(object sender, EventArgs e)
        {
            if (Cameras.Count == 0 && Microphones.Count == 0)
            {
                MessageBox.Show(LocRM.GetString("NothingToExport"), LocRM.GetString("Error"));
                return;
            }

            var saveFileDialog = new SaveFileDialog
                                     {
                                         InitialDirectory = Program.AppPath,
                                         Filter = "iSpy Files (*.ispy)|*.ispy|XML Files (*.xml)|*.xml"
                                     };

            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                string fileName = saveFileDialog.FileName;

                if (fileName.Trim() != "")
                {
                    SaveObjects(fileName);
                }
            }
            saveFileDialog.Dispose();
        }


        private void MenuItem21Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
                          {
                              InitialDirectory = Program.AppPath,
                              Filter = "iSpy Files (*.ispy)|*.ispy|XML Files (*.xml)|*.xml"
                          };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                string fileName = ofd.FileName;

                if (fileName.Trim() != "")
                {
                    LoadObjectList(fileName.Trim());
                }
            }
            ofd.Dispose();
        }

        private void LoadObjectList(string fileName)
        {
            _houseKeepingTimer.Stop();
            _tsslStats.Text = "Loading Object List...";
            Application.DoEvents();
            RemoveObjects();
            LoadObjects(fileName);
            RenderObjects();
            _tsslStats.Text = "Loaded Objects";
            Application.DoEvents();
            _houseKeepingTimer.Start();
        }



        private void MainFormHelpButtonClicked(object sender, CancelEventArgs e)
        {
            Help.ShowHelp(this, "http://www.ispyconnect.com/help.aspx");
        }

       

        private void ShowISpy10PercentOpacityToolStripMenuItemClick(object sender, EventArgs e)
        {
            ShowForm(.1);
        }

        private void ShowISpy30OpacityToolStripMenuItemClick(object sender, EventArgs e)
        {
            ShowForm(.3);
        }

        private void ShowISpy100PercentOpacityToolStripMenuItemClick(object sender, EventArgs e)
        {
            ShowForm(1);
        }

        private void CtxtTaskbarOpening(object sender, CancelEventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                if (iSpyServer.Default.Enable_Password_Protect)
                {
                    _unlockToolstripMenuItem.Visible = true;
                    _showToolstripMenuItem.Visible =
                        _showISpy10PercentOpacityToolStripMenuItem.Visible =
                        _showISpy30OpacityToolStripMenuItem.Visible =
                        _showISpy100PercentOpacityToolStripMenuItem.Visible = false;
                    _exitToolStripMenuItem.Visible = false;
                    _websiteToolstripMenuItem.Visible = false;
                    _helpToolstripMenuItem.Visible = false;
                    _switchAllOffToolStripMenuItem.Visible = false;
                    _switchAllOnToolStripMenuItem.Visible = false;
                }
                else
                {
                    _unlockToolstripMenuItem.Visible = false;
                    _showToolstripMenuItem.Visible =
                        _showISpy10PercentOpacityToolStripMenuItem.Visible =
                        _showISpy30OpacityToolStripMenuItem.Visible =
                        _showISpy100PercentOpacityToolStripMenuItem.Visible = true;
                    _exitToolStripMenuItem.Visible = true;
                    _websiteToolstripMenuItem.Visible = true;
                    _helpToolstripMenuItem.Visible = true;
                    _switchAllOffToolStripMenuItem.Visible = true;
                    _switchAllOnToolStripMenuItem.Visible = true;
                }
            }
            else
            {
                _showToolstripMenuItem.Visible = false;
                _showISpy10PercentOpacityToolStripMenuItem.Visible =
                    _showISpy30OpacityToolStripMenuItem.Visible =
                    _showISpy100PercentOpacityToolStripMenuItem.Visible = true;
                _unlockToolstripMenuItem.Visible = false;
                _exitToolStripMenuItem.Visible = true;
                _websiteToolstripMenuItem.Visible = true;
                _helpToolstripMenuItem.Visible = true;
                _switchAllOffToolStripMenuItem.Visible = true;
                _switchAllOnToolStripMenuItem.Visible = true;
            }
        }

        private void SaveObjects(string fileName)
        {
            if (fileName == "")
                fileName = Program.AppPath + @"XML\objects.xml";
            var c = new objects();
            foreach (objectsCamera oc in Cameras)
            {
                CameraWindow occ = GetCameraWindow(oc.id);
                if (occ != null)
                {
                    oc.width = occ.Width;
                    oc.height = occ.Height;
                    oc.x = occ.Location.X;
                    oc.y = occ.Location.Y;
                }
            }
            c.cameras = Cameras.ToArray();
            foreach (objectsMicrophone om in Microphones)
            {
                VolumeLevel omc = GetMicrophone(om.id);
                if (omc != null)
                {
                    om.width = omc.Width;
                    om.height = omc.Height;
                    om.x = omc.Location.X;
                    om.y = omc.Location.Y;
                }
            }
            c.microphones = Microphones.ToArray();

            var s = new XmlSerializer(typeof (objects));
            var fs = new FileStream(fileName, FileMode.Create);
            TextWriter writer = new StreamWriter(fs);
            fs.Position = 0;
            s.Serialize(writer, c);
            fs.Close();
        }

        private void StatusBarToolStripMenuItemClick(object sender, EventArgs e)
        {
            _statusBarToolStripMenuItem.Checked = !_statusBarToolStripMenuItem.Checked;
            _statusStrip1.Visible = _statusBarToolStripMenuItem.Checked;
        }

        private void FileMenuToolStripMenuItemClick(object sender, EventArgs e)
        {
            _fileMenuToolStripMenuItem.Checked = !_fileMenuToolStripMenuItem.Checked;
            Menu = !_fileMenuToolStripMenuItem.Checked ? null : _mainMenu;
        }

        private void ToolStripToolStripMenuItemClick(object sender, EventArgs e)
        {
            _toolStripToolStripMenuItem.Checked = !_toolStripToolStripMenuItem.Checked;
            _toolStrip1.Visible = _toolStripToolStripMenuItem.Checked;
        }

        private void MenuItem26Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this, "http://www.ispyconnect.com/donate.aspx");
        }


        private void PnlCamerasPaint(object sender, PaintEventArgs pe)
        {
        }

        private void ListenToolStripMenuItemClick(object sender, EventArgs e)
        {
            
        }

        private void PnlCamerasMouseUp(object sender, MouseEventArgs e)
        {
        }

        private void OpacityToolStripMenuItemClick(object sender, EventArgs e)
        {
            ShowForm(.1);
        }

        private void OpacityToolStripMenuItem1Click(object sender, EventArgs e)
        {
            ShowForm(.3);
        }

        private void OpacityToolStripMenuItem2Click(object sender, EventArgs e)
        {
            ShowForm(1);
        }

        private void MenuItem31Click(object sender, EventArgs e)
        {
            RemoveObjects();
        }

        private void MenuItem34Click(object sender, EventArgs e)
        {
            SwitchObjects(true);
        }

        public void SwitchObjects(bool on)
        {
            foreach (Control c in _pnlCameras.Controls)
            {
                if (c.GetType() == typeof (CameraWindow))
                {
                    var cameraControl = (CameraWindow) c;
                    if (on && !cameraControl.Camobject.settings.active)
                        cameraControl.Enable();
                    if (!on && cameraControl.Camobject.settings.active)
                        cameraControl.Disable();
                }
                if (c.GetType() == typeof (VolumeLevel))
                {
                    var volumeControl = (VolumeLevel) c;
                    if (on && !volumeControl.Micobject.settings.active)
                        volumeControl.Enable();
                    if (!on && volumeControl.Micobject.settings.active)
                        volumeControl.Disable();
                }
            }
        }

        private void MenuItem33Click(object sender, EventArgs e)
        {
            SwitchObjects(false);
        }


        private void ToolStrip1ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
        }


        private void MenuItem37Click(object sender, EventArgs e)
        {
            MessageBox.Show(LocRM.GetString("EditInstruct"), LocRM.GetString("Note"));
        }

        private void PositionToolStripMenuItemClick(object sender, EventArgs e)
        {
            int x, y, w, h;

            var p = (PictureBox) ContextTarget;
            w = p.Width;
            h = p.Height;
            x = p.Location.X;
            y = p.Location.Y;

            var le = new LayoutEditor {X = x, Y = y, W = w, H = h};


            if (le.ShowDialog() == DialogResult.OK)
            {
                PositionPanel(p, new Point(le.X, le.Y), le.W, le.H);
            }
            le.Dispose();
        }

        private void PositionPanel(PictureBox p, Point xy, int w, int h)
        {
            p.Width = w;
            p.Height = h;
            p.Location = new Point(xy.X, xy.Y);
        }

        private void MenuItem38Click(object sender, EventArgs e)
        {
            StartBrowser("http://www.ispyconnect.com/producthistory.aspx?productid=12");
        }

        private void AutoLayoutToolStripMenuItemClick(object sender, EventArgs e)
        {
            _autoLayoutToolStripMenuItem.Checked = !_autoLayoutToolStripMenuItem.Checked;
            iSpyServer.Default.AutoLayout = _autoLayoutToolStripMenuItem.Checked;
            if (iSpyServer.Default.AutoLayout)
                LayoutObjects(0, 0);
        }

        private void LayoutObjects(int w, int h)
        {
            _pnlCameras.HorizontalScroll.Value = 0;
            _pnlCameras.VerticalScroll.Value = 0;
            _pnlCameras.Refresh();
            // Get data.
            var rectslist = new List<Rectangle>();
            foreach (Control c in _pnlCameras.Controls)
            {
                if (!(c is PictureBox)) continue;
                var p = (PictureBox) c;
                if (w > 0)
                {
                    p.Width = w;
                    p.Height = h;
                }
                rectslist.Add(new Rectangle(0, 0, p.Width, p.Height));
            }
            // Arrange the rectangles.
            Rectangle[] rects = rectslist.ToArray();
            int binWidth = _pnlCameras.Width;
            var proc = new C2BPProcessor();
            proc.SubAlgFillOneColumn(binWidth, rects);
            rectslist = rects.ToList();
            bool assigned = true;
            var indexesassigned = new List<int>();
            while (assigned)
            {
                assigned = false;
                foreach (Rectangle r in rectslist)
                {
                    for (int i = 0; i < _pnlCameras.Controls.Count; i++)
                    {
                        Control c = _pnlCameras.Controls[i];
                        if (!indexesassigned.Contains(i) && c is PictureBox)
                        {
                            if (c.Width == r.Width && c.Height == r.Height)
                            {
                                PositionPanel((PictureBox) c, new Point(r.X, r.Y), r.Width, r.Height);
                                rectslist.Remove(r);
                                assigned = true;
                                indexesassigned.Add(i);
                                break;
                            }
                        }
                    }
                    if (assigned)
                        break;
                }
            }
        }

        private void MenuItem39Click(object sender, EventArgs e)
        {
        }

        private void TakePhotoToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (ContextTarget.GetType() == typeof (CameraWindow))
            {
                var cameraControl = ((CameraWindow) ContextTarget);
                Help.ShowHelp(this,
                              "http://" + IPAddress + ":" + iSpyServer.Default.LANPort + "/?c=" +
                              cameraControl.Camobject.id + "&r=" + Random.NextDouble());
            }
        }

        private void LocalCameraToolStripMenuItemClick(object sender, EventArgs e)
        {
            AddCamera(3);
        }

        private void IPCameraToolStripMenuItemClick(object sender, EventArgs e)
        {
            AddCamera(1);
        }

        private void MicrophoneToolStripMenuItemClick(object sender, EventArgs e)
        {
            AddMicrophone(1);
        }

        private void MenuItem12Click(object sender, EventArgs e)
        {
            //+26 height for control bar
            LayoutObjects(164, 146);
        }

        private void MenuItem14Click(object sender, EventArgs e)
        {
            LayoutObjects(324, 266);
        }

        private void MenuItem29Click1(object sender, EventArgs e)
        {
            LayoutObjects(0, 0);
        }


        private void SwitchAllOnToolStripMenuItemClick(object sender, EventArgs e)
        {
            SwitchObjects(true);
        }

        private void SwitchAllOffToolStripMenuItemClick(object sender, EventArgs e)
        {
            SwitchObjects(false);
        }

        private void MenuItem22Click1(object sender, EventArgs e)
        {
            WriteLog();
            var ofd = new OpenFileDialog();
            ofd.InitialDirectory = Program.AppPath;

            ofd.Filter = "iSpy Log Files (*.htm)|*.htm";

            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                string fileName = ofd.FileName;

                if (fileName.Trim() != "")
                {
                    Process.Start(ofd.FileName);
                }
            }
        }

        public void ApplySchedule()
        {
            foreach (objectsCamera cam in _cameras)
            {
                if (cam.schedule.active)
                {
                    CameraWindow cw = GetCamera(cam.id);
                    cw.ApplySchedule();
                }
            }


            foreach (objectsMicrophone mic in _microphones)
            {
                if (mic.schedule.active)
                {
                    VolumeLevel vl = GetMicrophone(mic.id);
                    vl.ApplySchedule();
                }
            }
        }

        private void MenuItem3Click1(object sender, EventArgs e)
        {
            ApplySchedule();
        }

        #region CameraEvents

        private void CameraControl_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void CameraControlMouseMove(object sender, MouseEventArgs e)
        {
            var cameraControl = (CameraWindow) sender;
            if (e.Button == MouseButtons.Left)
            {
                int newLeft = cameraControl.Left + (e.X - cameraControl.Camobject.x);
                int newTop = cameraControl.Top + (e.Y - cameraControl.Camobject.y);
                if (newLeft + cameraControl.Width > 5 && newLeft < ClientRectangle.Width - 5)
                {
                    cameraControl.Left = newLeft;
                }
                if (newTop + cameraControl.Height > 5 && newTop < ClientRectangle.Height - 50)
                {
                    cameraControl.Top = newTop;
                }
            }
            cameraControl.Focus();
        }

        private void CameraControlMouseDown(object sender, MouseEventArgs e)
        {
            var cameraControl = (CameraWindow) sender;
            if (e.Button == MouseButtons.Left)
            {
                cameraControl.Camobject.x = e.X;
                cameraControl.Camobject.y = e.Y;
                cameraControl.BringToFront();
            }
            else
            {
                if (e.Button == MouseButtons.Right)
                {
                    ContextTarget = cameraControl;
                    _setInactiveToolStripMenuItem.Visible = false;
                    _activateToolStripMenuItem.Visible = false;

                    if (cameraControl.Camobject.settings.active)
                    {
                        _setInactiveToolStripMenuItem.Visible = true;

                        _takePhotoToolStripMenuItem.Visible = true;
                    }
                    else
                    {
                        _activateToolStripMenuItem.Visible = true;

                        _takePhotoToolStripMenuItem.Visible = false;
                    }

                    _ctxtMnu.Show(cameraControl, new Point(e.X, e.Y));
                }
            }
        }

        private void CameraControl_MouseUp(object sender, MouseEventArgs e)
        {
            var cameraControl = (CameraWindow) sender;
            if (e.Button == MouseButtons.Left)
            {
                cameraControl.Camobject.x = cameraControl.Left;
                cameraControl.Camobject.y = cameraControl.Top;
            }
        }

        private void CameraControl_MouseLeave(object sender, EventArgs e)
        {
            var cameraControl = (CameraWindow) sender;
            cameraControl.Cursor = Cursors.Default;
        }

        private void CameraControl_MouseEnter(object sender, EventArgs e)
        {
            var cameraControl = (CameraWindow) sender;
            cameraControl.Cursor = Cursors.Hand;
        }

        private void CameraControlDoubleClick(object sender, EventArgs e)
        {
            EditCamera(((CameraWindow) sender).Camobject);
        }

        #endregion

        #region VolumeEvents

        private void VolumeControlMouseDown(object sender, MouseEventArgs e)
        {
            var volumeControl = (VolumeLevel) sender;
            if (e.Button == MouseButtons.Left)
            {
                volumeControl.Micobject.x = e.X;
                volumeControl.Micobject.y = e.Y;
                volumeControl.BringToFront();
            }
            else
            {
                if (e.Button == MouseButtons.Right)
                {
                    ContextTarget = volumeControl;
                    _setInactiveToolStripMenuItem.Visible = false;
                    _activateToolStripMenuItem.Visible = false;
                    _takePhotoToolStripMenuItem.Visible = false;

                    if (volumeControl.Micobject.settings.active)
                    {
                        _setInactiveToolStripMenuItem.Visible = true;
                    }
                    else
                    {
                        _activateToolStripMenuItem.Visible = true;
                    }


                    _ctxtMnu.Show(volumeControl, new Point(e.X, e.Y));
                }
            }
        }

        private void VolumeControl_MouseUp(object sender, MouseEventArgs e)
        {
            var volumeControl = (VolumeLevel) sender;
            if (e.Button == MouseButtons.Left)
            {
                volumeControl.Micobject.x = volumeControl.Left;
                volumeControl.Micobject.y = volumeControl.Top;
            }
        }

        private void VolumeControl_MouseLeave(object sender, EventArgs e)
        {
            var volumeControl = (VolumeLevel) sender;
            volumeControl.Cursor = Cursors.Default;
        }

        private void VolumeControl_MouseEnter(object sender, EventArgs e)
        {
            var volumeControl = (VolumeLevel) sender;
            volumeControl.Cursor = Cursors.Hand;
        }


        private void VolumeControl_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void VolumeControlMouseMove(object sender, MouseEventArgs e)
        {
            var volumeControl = (VolumeLevel) sender;
            if (e.Button == MouseButtons.Left)
            {
                int newLeft = volumeControl.Left + (e.X - Convert.ToInt32(volumeControl.Micobject.x));
                int newTop = volumeControl.Top + (e.Y - Convert.ToInt32(volumeControl.Micobject.y));
                if (newLeft + volumeControl.Width > 5 && newLeft < ClientRectangle.Width - 5)
                {
                    volumeControl.Left = newLeft;
                }
                if (newTop + volumeControl.Height > 5 && newTop < ClientRectangle.Height - 50)
                {
                    volumeControl.Top = newTop;
                }
            }
            volumeControl.Focus();
        }

        #endregion

        #region RestoreSavedCameras

        internal void DisplayCamera(objectsCamera cam)
        {
            var cameraControl = new CameraWindow(cam);
            SetCameraEvents(cameraControl);
            cameraControl.BackColor = iSpyServer.Default.BackColor;
            _pnlCameras.Controls.Add(cameraControl);
            cameraControl.Location = new Point(cam.x, cam.y);
            cameraControl.Size = new Size(cam.width, cam.height);
            cameraControl.BringToFront();
            if (cam.settings.active)
                cameraControl.Enable();
        }

        #endregion

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this._mainMenu = new System.Windows.Forms.MainMenu(this.components);
            this._fileItem = new System.Windows.Forms.MenuItem();
            this._menuItem19 = new System.Windows.Forms.MenuItem();
            this._menuItem21 = new System.Windows.Forms.MenuItem();
            this._menuItem1 = new System.Windows.Forms.MenuItem();
            this._exitFileItem = new System.Windows.Forms.MenuItem();
            this._menuItem36 = new System.Windows.Forms.MenuItem();
            this._menuItem37 = new System.Windows.Forms.MenuItem();
            this._menuItem16 = new System.Windows.Forms.MenuItem();
            this._menuItem39 = new System.Windows.Forms.MenuItem();
            this._menuItem12 = new System.Windows.Forms.MenuItem();
            this._menuItem14 = new System.Windows.Forms.MenuItem();
            this._menuItem29 = new System.Windows.Forms.MenuItem();
            this._menuItem20 = new System.Windows.Forms.MenuItem();
            this._menuItem22 = new System.Windows.Forms.MenuItem();
            this._menuItem9 = new System.Windows.Forms.MenuItem();
            this._menuItem8 = new System.Windows.Forms.MenuItem();
            this._menuItem34 = new System.Windows.Forms.MenuItem();
            this._menuItem33 = new System.Windows.Forms.MenuItem();
            this._miApplySchedule = new System.Windows.Forms.MenuItem();
            this._menuItem31 = new System.Windows.Forms.MenuItem();
            this._helpItem = new System.Windows.Forms.MenuItem();
            this._aboutHelpItem = new System.Windows.Forms.MenuItem();
            this._menuItem30 = new System.Windows.Forms.MenuItem();
            this._menuItem2 = new System.Windows.Forms.MenuItem();
            this._menuItem10 = new System.Windows.Forms.MenuItem();
            this._menuItem38 = new System.Windows.Forms.MenuItem();
            this._menuItem11 = new System.Windows.Forms.MenuItem();
            this._menuItem5 = new System.Windows.Forms.MenuItem();
            this._menuItem27 = new System.Windows.Forms.MenuItem();
            this._menuItem26 = new System.Windows.Forms.MenuItem();
            this._pnlCameras = new System.Windows.Forms.Panel();
            this._ctxtMainForm = new System.Windows.Forms.ContextMenuStrip(this.components);
            this._addCameraToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._addMicrophoneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._fullScreenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._opacityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._opacityToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this._opacityToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this._autoLayoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._statusBarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._fileMenuToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStrip1 = new System.Windows.Forms.ToolStrip();
            this._toolStripDropDownButton2 = new System.Windows.Forms.ToolStripDropDownButton();
            this._localCameraToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._iPCameraToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._microphoneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolStripButton4 = new System.Windows.Forms.ToolStripButton();
            this._notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this._tmrStartup = new System.Windows.Forms.Timer(this.components);
            this._ctxtMnu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this._activateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._setInactiveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._takePhotoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._positionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._resetSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._ctxtTaskbar = new System.Windows.Forms.ContextMenuStrip(this.components);
            this._unlockToolstripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._switchAllOnToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._switchAllOffToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._showToolstripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._showISpy10PercentOpacityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._showISpy30OpacityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._showISpy100PercentOpacityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._helpToolstripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._websiteToolstripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._statusStrip1 = new System.Windows.Forms.StatusStrip();
            this._tsslStats = new System.Windows.Forms.ToolStripStatusLabel();
            this._ctxtMainForm.SuspendLayout();
            this._toolStrip1.SuspendLayout();
            this._ctxtMnu.SuspendLayout();
            this._ctxtTaskbar.SuspendLayout();
            this._statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // _mainMenu
            // 
            this._mainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this._fileItem,
            this._menuItem36,
            this._menuItem16,
            this._menuItem9,
            this._helpItem});
            // 
            // _fileItem
            // 
            this._fileItem.Index = 0;
            this._fileItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this._menuItem19,
            this._menuItem21,
            this._menuItem1,
            this._exitFileItem});
            this._fileItem.Text = "&File";
            // 
            // _menuItem19
            // 
            this._menuItem19.Index = 0;
            this._menuItem19.Text = "&Save Object List";
            this._menuItem19.Click += new System.EventHandler(this.MenuItem19Click);
            // 
            // _menuItem21
            // 
            this._menuItem21.Index = 1;
            this._menuItem21.Text = "&Open Object List";
            this._menuItem21.Click += new System.EventHandler(this.MenuItem21Click);
            // 
            // _menuItem1
            // 
            this._menuItem1.Index = 2;
            this._menuItem1.Text = "-";
            // 
            // _exitFileItem
            // 
            this._exitFileItem.Index = 3;
            this._exitFileItem.Text = "E&xit";
            this._exitFileItem.Click += new System.EventHandler(this.ExitFileItemClick);
            // 
            // _menuItem36
            // 
            this._menuItem36.Index = 1;
            this._menuItem36.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this._menuItem37});
            this._menuItem36.Text = "Edit";
            // 
            // _menuItem37
            // 
            this._menuItem37.Index = 0;
            this._menuItem37.Text = "Cameras and Microphones";
            this._menuItem37.Click += new System.EventHandler(this.MenuItem37Click);
            // 
            // _menuItem16
            // 
            this._menuItem16.Index = 2;
            this._menuItem16.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this._menuItem39,
            this._menuItem20,
            this._menuItem22});
            this._menuItem16.Text = "View";
            // 
            // _menuItem39
            // 
            this._menuItem39.Index = 0;
            this._menuItem39.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this._menuItem12,
            this._menuItem14,
            this._menuItem29});
            this._menuItem39.Text = "Auto Layout Objects";
            this._menuItem39.Click += new System.EventHandler(this.MenuItem39Click);
            // 
            // _menuItem12
            // 
            this._menuItem12.Index = 0;
            this._menuItem12.Text = "160 x 120";
            this._menuItem12.Click += new System.EventHandler(this.MenuItem12Click);
            // 
            // _menuItem14
            // 
            this._menuItem14.Index = 1;
            this._menuItem14.Text = "320 x 240";
            this._menuItem14.Click += new System.EventHandler(this.MenuItem14Click);
            // 
            // _menuItem29
            // 
            this._menuItem29.Index = 2;
            this._menuItem29.Text = "Current";
            this._menuItem29.Click += new System.EventHandler(this.MenuItem29Click1);
            // 
            // _menuItem20
            // 
            this._menuItem20.Index = 1;
            this._menuItem20.Text = "Log &File";
            this._menuItem20.Click += new System.EventHandler(this.MenuItem20Click);
            // 
            // _menuItem22
            // 
            this._menuItem22.Index = 2;
            this._menuItem22.Text = "Log F&iles";
            this._menuItem22.Click += new System.EventHandler(this.MenuItem22Click1);
            // 
            // _menuItem9
            // 
            this._menuItem9.Index = 3;
            this._menuItem9.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this._menuItem8,
            this._menuItem34,
            this._menuItem33,
            this._miApplySchedule,
            this._menuItem31});
            this._menuItem9.Text = "&Options";
            // 
            // _menuItem8
            // 
            this._menuItem8.Index = 0;
            this._menuItem8.Text = "&Settings";
            this._menuItem8.Click += new System.EventHandler(this.MenuItem8Click);
            // 
            // _menuItem34
            // 
            this._menuItem34.Index = 1;
            this._menuItem34.Text = "Switch On All Objects";
            this._menuItem34.Click += new System.EventHandler(this.MenuItem34Click);
            // 
            // _menuItem33
            // 
            this._menuItem33.Index = 2;
            this._menuItem33.Text = "Switch Off All Objects";
            this._menuItem33.Click += new System.EventHandler(this.MenuItem33Click);
            // 
            // _miApplySchedule
            // 
            this._miApplySchedule.Index = 3;
            this._miApplySchedule.Text = "Apply Schedule";
            this._miApplySchedule.Click += new System.EventHandler(this.MenuItem3Click1);
            // 
            // _menuItem31
            // 
            this._menuItem31.Index = 4;
            this._menuItem31.Text = "&Remove All Objects";
            this._menuItem31.Click += new System.EventHandler(this.MenuItem31Click);
            // 
            // _helpItem
            // 
            this._helpItem.Index = 4;
            this._helpItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this._aboutHelpItem,
            this._menuItem30,
            this._menuItem2,
            this._menuItem10,
            this._menuItem38,
            this._menuItem11,
            this._menuItem5,
            this._menuItem27,
            this._menuItem26});
            this._helpItem.Text = "&Help";
            // 
            // _aboutHelpItem
            // 
            this._aboutHelpItem.Index = 0;
            this._aboutHelpItem.Text = "&About";
            this._aboutHelpItem.Click += new System.EventHandler(this.AboutHelpItemClick);
            // 
            // _menuItem30
            // 
            this._menuItem30.Index = 1;
            this._menuItem30.Text = "-";
            // 
            // _menuItem2
            // 
            this._menuItem2.Index = 2;
            this._menuItem2.Text = "&Help";
            this._menuItem2.Click += new System.EventHandler(this.MenuItem2Click);
            // 
            // _menuItem10
            // 
            this._menuItem10.Index = 3;
            this._menuItem10.Text = "&Check For Updates";
            this._menuItem10.Click += new System.EventHandler(this.MenuItem10Click);
            // 
            // _menuItem38
            // 
            this._menuItem38.Index = 4;
            this._menuItem38.Text = "View Update Information";
            this._menuItem38.Click += new System.EventHandler(this.MenuItem38Click);
            // 
            // _menuItem11
            // 
            this._menuItem11.Index = 5;
            this._menuItem11.Text = "&Report Bug/ Feedback";
            this._menuItem11.Click += new System.EventHandler(this.MenuItem11Click);
            // 
            // _menuItem5
            // 
            this._menuItem5.Index = 6;
            this._menuItem5.Text = "Go to &Website";
            this._menuItem5.Click += new System.EventHandler(this.MenuItem5Click);
            // 
            // _menuItem27
            // 
            this._menuItem27.Index = 7;
            this._menuItem27.Text = "-";
            // 
            // _menuItem26
            // 
            this._menuItem26.Index = 8;
            this._menuItem26.Text = "&Support iSpy With a Donation";
            this._menuItem26.Click += new System.EventHandler(this.MenuItem26Click);
            // 
            // _pnlCameras
            // 
            this._pnlCameras.AutoScroll = true;
            this._pnlCameras.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._pnlCameras.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(66)))), ((int)(((byte)(66)))), ((int)(((byte)(66)))));
            this._pnlCameras.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this._pnlCameras.ContextMenuStrip = this._ctxtMainForm;
            this._pnlCameras.Dock = System.Windows.Forms.DockStyle.Fill;
            this._pnlCameras.Location = new System.Drawing.Point(0, 39);
            this._pnlCameras.Name = "_pnlCameras";
            this._pnlCameras.Size = new System.Drawing.Size(1015, 785);
            this._pnlCameras.TabIndex = 18;
            this._pnlCameras.Paint += new System.Windows.Forms.PaintEventHandler(this.PnlCamerasPaint);
            this._pnlCameras.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PnlCamerasMouseUp);
            // 
            // _ctxtMainForm
            // 
            this._ctxtMainForm.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._addCameraToolStripMenuItem,
            this._addMicrophoneToolStripMenuItem,
            this._settingsToolStripMenuItem,
            this._fullScreenToolStripMenuItem,
            this._opacityToolStripMenuItem,
            this._opacityToolStripMenuItem1,
            this._opacityToolStripMenuItem2,
            this._autoLayoutToolStripMenuItem,
            this._statusBarToolStripMenuItem,
            this._fileMenuToolStripMenuItem,
            this._toolStripToolStripMenuItem});
            this._ctxtMainForm.Name = "_ctxtMainForm";
            this._ctxtMainForm.Size = new System.Drawing.Size(165, 246);
            this._ctxtMainForm.Opening += new System.ComponentModel.CancelEventHandler(this.CtxtMainFormOpening);
            // 
            // _addCameraToolStripMenuItem
            // 
            this._addCameraToolStripMenuItem.Image = global::iSpyServer.Properties.Resources.camera;
            this._addCameraToolStripMenuItem.Name = "_addCameraToolStripMenuItem";
            this._addCameraToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this._addCameraToolStripMenuItem.Text = "Add &Camera";
            this._addCameraToolStripMenuItem.Click += new System.EventHandler(this.AddCameraToolStripMenuItemClick);
            // 
            // _addMicrophoneToolStripMenuItem
            // 
            this._addMicrophoneToolStripMenuItem.Image = global::iSpyServer.Properties.Resources.Mic;
            this._addMicrophoneToolStripMenuItem.Name = "_addMicrophoneToolStripMenuItem";
            this._addMicrophoneToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this._addMicrophoneToolStripMenuItem.Text = "Add &Microphone";
            this._addMicrophoneToolStripMenuItem.Click += new System.EventHandler(this.AddMicrophoneToolStripMenuItemClick);
            // 
            // _settingsToolStripMenuItem
            // 
            this._settingsToolStripMenuItem.Image = global::iSpyServer.Properties.Resources.settings;
            this._settingsToolStripMenuItem.Name = "_settingsToolStripMenuItem";
            this._settingsToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this._settingsToolStripMenuItem.Text = "&Settings";
            this._settingsToolStripMenuItem.Click += new System.EventHandler(this.SettingsToolStripMenuItemClick);
            // 
            // _fullScreenToolStripMenuItem
            // 
            this._fullScreenToolStripMenuItem.Name = "_fullScreenToolStripMenuItem";
            this._fullScreenToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this._fullScreenToolStripMenuItem.Text = "&Full Screen";
            this._fullScreenToolStripMenuItem.Click += new System.EventHandler(this.FullScreenToolStripMenuItemClick);
            // 
            // _opacityToolStripMenuItem
            // 
            this._opacityToolStripMenuItem.Name = "_opacityToolStripMenuItem";
            this._opacityToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this._opacityToolStripMenuItem.Text = "10% Opacity";
            this._opacityToolStripMenuItem.Click += new System.EventHandler(this.OpacityToolStripMenuItemClick);
            // 
            // _opacityToolStripMenuItem1
            // 
            this._opacityToolStripMenuItem1.Name = "_opacityToolStripMenuItem1";
            this._opacityToolStripMenuItem1.Size = new System.Drawing.Size(164, 22);
            this._opacityToolStripMenuItem1.Text = "30% Opacity";
            this._opacityToolStripMenuItem1.Click += new System.EventHandler(this.OpacityToolStripMenuItem1Click);
            // 
            // _opacityToolStripMenuItem2
            // 
            this._opacityToolStripMenuItem2.Name = "_opacityToolStripMenuItem2";
            this._opacityToolStripMenuItem2.Size = new System.Drawing.Size(164, 22);
            this._opacityToolStripMenuItem2.Text = "100% Opacity";
            this._opacityToolStripMenuItem2.Click += new System.EventHandler(this.OpacityToolStripMenuItem2Click);
            // 
            // _autoLayoutToolStripMenuItem
            // 
            this._autoLayoutToolStripMenuItem.Checked = true;
            this._autoLayoutToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this._autoLayoutToolStripMenuItem.Name = "_autoLayoutToolStripMenuItem";
            this._autoLayoutToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this._autoLayoutToolStripMenuItem.Text = "Auto Layout";
            this._autoLayoutToolStripMenuItem.Click += new System.EventHandler(this.AutoLayoutToolStripMenuItemClick);
            // 
            // _statusBarToolStripMenuItem
            // 
            this._statusBarToolStripMenuItem.Checked = true;
            this._statusBarToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this._statusBarToolStripMenuItem.Name = "_statusBarToolStripMenuItem";
            this._statusBarToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this._statusBarToolStripMenuItem.Text = "Status &Bar";
            this._statusBarToolStripMenuItem.Click += new System.EventHandler(this.StatusBarToolStripMenuItemClick);
            // 
            // _fileMenuToolStripMenuItem
            // 
            this._fileMenuToolStripMenuItem.Checked = true;
            this._fileMenuToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this._fileMenuToolStripMenuItem.Name = "_fileMenuToolStripMenuItem";
            this._fileMenuToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this._fileMenuToolStripMenuItem.Text = "File &Menu";
            this._fileMenuToolStripMenuItem.Click += new System.EventHandler(this.FileMenuToolStripMenuItemClick);
            // 
            // _toolStripToolStripMenuItem
            // 
            this._toolStripToolStripMenuItem.Checked = true;
            this._toolStripToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this._toolStripToolStripMenuItem.Name = "_toolStripToolStripMenuItem";
            this._toolStripToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this._toolStripToolStripMenuItem.Text = "&Tool Strip";
            this._toolStripToolStripMenuItem.Click += new System.EventHandler(this.ToolStripToolStripMenuItemClick);
            // 
            // _toolStrip1
            // 
            this._toolStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this._toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripDropDownButton2,
            this._toolStripButton4});
            this._toolStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this._toolStrip1.Location = new System.Drawing.Point(0, 0);
            this._toolStrip1.Name = "_toolStrip1";
            this._toolStrip1.Size = new System.Drawing.Size(1015, 39);
            this._toolStrip1.TabIndex = 0;
            this._toolStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.ToolStrip1ItemClicked);
            // 
            // _toolStripDropDownButton2
            // 
            this._toolStripDropDownButton2.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._localCameraToolStripMenuItem,
            this._iPCameraToolStripMenuItem,
            this._microphoneToolStripMenuItem});
            this._toolStripDropDownButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._toolStripDropDownButton2.Name = "_toolStripDropDownButton2";
            this._toolStripDropDownButton2.Size = new System.Drawing.Size(51, 36);
            this._toolStripDropDownButton2.Text = "Add...";
            // 
            // _localCameraToolStripMenuItem
            // 
            this._localCameraToolStripMenuItem.Image = global::iSpyServer.Properties.Resources.addcam;
            this._localCameraToolStripMenuItem.Name = "_localCameraToolStripMenuItem";
            this._localCameraToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this._localCameraToolStripMenuItem.Text = "Local Camera";
            this._localCameraToolStripMenuItem.Click += new System.EventHandler(this.LocalCameraToolStripMenuItemClick);
            // 
            // _iPCameraToolStripMenuItem
            // 
            this._iPCameraToolStripMenuItem.Image = global::iSpyServer.Properties.Resources.ipcam;
            this._iPCameraToolStripMenuItem.Name = "_iPCameraToolStripMenuItem";
            this._iPCameraToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this._iPCameraToolStripMenuItem.Text = "IP Camera";
            this._iPCameraToolStripMenuItem.Click += new System.EventHandler(this.IPCameraToolStripMenuItemClick);
            // 
            // _microphoneToolStripMenuItem
            // 
            this._microphoneToolStripMenuItem.Image = global::iSpyServer.Properties.Resources.Mic;
            this._microphoneToolStripMenuItem.Name = "_microphoneToolStripMenuItem";
            this._microphoneToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this._microphoneToolStripMenuItem.Text = "Microphone";
            this._microphoneToolStripMenuItem.Click += new System.EventHandler(this.MicrophoneToolStripMenuItemClick);
            // 
            // _toolStripButton4
            // 
            this._toolStripButton4.Image = global::iSpyServer.Properties.Resources.settings;
            this._toolStripButton4.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._toolStripButton4.Name = "_toolStripButton4";
            this._toolStripButton4.Size = new System.Drawing.Size(85, 36);
            this._toolStripButton4.Text = "Settings";
            this._toolStripButton4.Click += new System.EventHandler(this.ToolStripButton4Click);
            // 
            // _notifyIcon1
            // 
            this._notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("_notifyIcon1.Icon")));
            this._notifyIcon1.Text = "iSpy";
            this._notifyIcon1.Visible = true;
            this._notifyIcon1.Click += new System.EventHandler(this.NotifyIcon1Click);
            this._notifyIcon1.DoubleClick += new System.EventHandler(this.NotifyIcon1DoubleClick);
            // 
            // _tmrStartup
            // 
            this._tmrStartup.Interval = 1000;
            this._tmrStartup.Tick += new System.EventHandler(this.TmrStartupTick);
            // 
            // _ctxtMnu
            // 
            this._ctxtMnu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._activateToolStripMenuItem,
            this._setInactiveToolStripMenuItem,
            this._takePhotoToolStripMenuItem,
            this._editToolStripMenuItem,
            this._positionToolStripMenuItem,
            this._resetSizeToolStripMenuItem,
            this._deleteToolStripMenuItem});
            this._ctxtMnu.Name = "_ctxtMnu";
            this._ctxtMnu.Size = new System.Drawing.Size(153, 180);
            // 
            // _activateToolStripMenuItem
            // 
            this._activateToolStripMenuItem.Image = global::iSpyServer.Properties.Resources.active;
            this._activateToolStripMenuItem.Name = "_activateToolStripMenuItem";
            this._activateToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this._activateToolStripMenuItem.Text = "Switch &On";
            this._activateToolStripMenuItem.Click += new System.EventHandler(this.ActivateToolStripMenuItemClick);
            // 
            // _setInactiveToolStripMenuItem
            // 
            this._setInactiveToolStripMenuItem.Name = "_setInactiveToolStripMenuItem";
            this._setInactiveToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this._setInactiveToolStripMenuItem.Text = "&Switch Off";
            this._setInactiveToolStripMenuItem.Click += new System.EventHandler(this.SetInactiveToolStripMenuItemClick);
            // 
            // _takePhotoToolStripMenuItem
            // 
            this._takePhotoToolStripMenuItem.Image = global::iSpyServer.Properties.Resources.snapshot;
            this._takePhotoToolStripMenuItem.Name = "_takePhotoToolStripMenuItem";
            this._takePhotoToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this._takePhotoToolStripMenuItem.Text = "Take Photo";
            this._takePhotoToolStripMenuItem.Click += new System.EventHandler(this.TakePhotoToolStripMenuItemClick);
            // 
            // _editToolStripMenuItem
            // 
            this._editToolStripMenuItem.Name = "_editToolStripMenuItem";
            this._editToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this._editToolStripMenuItem.Text = "&Edit";
            this._editToolStripMenuItem.Click += new System.EventHandler(this.EditToolStripMenuItemClick);
            // 
            // _positionToolStripMenuItem
            // 
            this._positionToolStripMenuItem.Name = "_positionToolStripMenuItem";
            this._positionToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this._positionToolStripMenuItem.Text = "Position";
            this._positionToolStripMenuItem.Click += new System.EventHandler(this.PositionToolStripMenuItemClick);
            // 
            // _resetSizeToolStripMenuItem
            // 
            this._resetSizeToolStripMenuItem.Name = "_resetSizeToolStripMenuItem";
            this._resetSizeToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this._resetSizeToolStripMenuItem.Text = "Reset Si&ze";
            this._resetSizeToolStripMenuItem.Click += new System.EventHandler(this.ResetSizeToolStripMenuItemClick);
            // 
            // _deleteToolStripMenuItem
            // 
            this._deleteToolStripMenuItem.Name = "_deleteToolStripMenuItem";
            this._deleteToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this._deleteToolStripMenuItem.Text = "&Remove";
            this._deleteToolStripMenuItem.Click += new System.EventHandler(this.DeleteToolStripMenuItemClick);
            // 
            // _ctxtTaskbar
            // 
            this._ctxtTaskbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._unlockToolstripMenuItem,
            this._switchAllOnToolStripMenuItem,
            this._switchAllOffToolStripMenuItem,
            this._showToolstripMenuItem,
            this._showISpy10PercentOpacityToolStripMenuItem,
            this._showISpy30OpacityToolStripMenuItem,
            this._showISpy100PercentOpacityToolStripMenuItem,
            this._helpToolstripMenuItem,
            this._websiteToolstripMenuItem,
            this._exitToolStripMenuItem});
            this._ctxtTaskbar.Name = "_ctxtMnu";
            this._ctxtTaskbar.Size = new System.Drawing.Size(219, 224);
            this._ctxtTaskbar.Opening += new System.ComponentModel.CancelEventHandler(this.CtxtTaskbarOpening);
            // 
            // _unlockToolstripMenuItem
            // 
            this._unlockToolstripMenuItem.Image = global::iSpyServer.Properties.Resources.unlock;
            this._unlockToolstripMenuItem.Name = "_unlockToolstripMenuItem";
            this._unlockToolstripMenuItem.Size = new System.Drawing.Size(218, 22);
            this._unlockToolstripMenuItem.Text = "&Unlock";
            this._unlockToolstripMenuItem.Click += new System.EventHandler(this.UnlockToolstripMenuItemClick);
            // 
            // _switchAllOnToolStripMenuItem
            // 
            this._switchAllOnToolStripMenuItem.Name = "_switchAllOnToolStripMenuItem";
            this._switchAllOnToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this._switchAllOnToolStripMenuItem.Text = "Switch All On";
            this._switchAllOnToolStripMenuItem.Click += new System.EventHandler(this.SwitchAllOnToolStripMenuItemClick);
            // 
            // _switchAllOffToolStripMenuItem
            // 
            this._switchAllOffToolStripMenuItem.Name = "_switchAllOffToolStripMenuItem";
            this._switchAllOffToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this._switchAllOffToolStripMenuItem.Text = "Switch All Off";
            this._switchAllOffToolStripMenuItem.Click += new System.EventHandler(this.SwitchAllOffToolStripMenuItemClick);
            // 
            // _showToolstripMenuItem
            // 
            this._showToolstripMenuItem.Image = global::iSpyServer.Properties.Resources.active;
            this._showToolstripMenuItem.Name = "_showToolstripMenuItem";
            this._showToolstripMenuItem.Size = new System.Drawing.Size(218, 22);
            this._showToolstripMenuItem.Text = "&Show iSpy";
            this._showToolstripMenuItem.Click += new System.EventHandler(this.ShowToolstripMenuItemClick);
            // 
            // _showISpy10PercentOpacityToolStripMenuItem
            // 
            this._showISpy10PercentOpacityToolStripMenuItem.Name = "_showISpy10PercentOpacityToolStripMenuItem";
            this._showISpy10PercentOpacityToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this._showISpy10PercentOpacityToolStripMenuItem.Text = "Show iSpy @ 10% opacity";
            this._showISpy10PercentOpacityToolStripMenuItem.Click += new System.EventHandler(this.ShowISpy10PercentOpacityToolStripMenuItemClick);
            // 
            // _showISpy30OpacityToolStripMenuItem
            // 
            this._showISpy30OpacityToolStripMenuItem.Name = "_showISpy30OpacityToolStripMenuItem";
            this._showISpy30OpacityToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this._showISpy30OpacityToolStripMenuItem.Text = "Show iSpy @ 30% opacity";
            this._showISpy30OpacityToolStripMenuItem.Click += new System.EventHandler(this.ShowISpy30OpacityToolStripMenuItemClick);
            // 
            // _showISpy100PercentOpacityToolStripMenuItem
            // 
            this._showISpy100PercentOpacityToolStripMenuItem.Name = "_showISpy100PercentOpacityToolStripMenuItem";
            this._showISpy100PercentOpacityToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this._showISpy100PercentOpacityToolStripMenuItem.Text = "Show iSpy @ 100 % opacity";
            // 
            // _helpToolstripMenuItem
            // 
            this._helpToolstripMenuItem.Name = "_helpToolstripMenuItem";
            this._helpToolstripMenuItem.Size = new System.Drawing.Size(218, 22);
            this._helpToolstripMenuItem.Text = "&Help";
            this._helpToolstripMenuItem.Click += new System.EventHandler(this.HelpToolstripMenuItemClick);
            // 
            // _websiteToolstripMenuItem
            // 
            this._websiteToolstripMenuItem.Image = global::iSpyServer.Properties.Resources.web;
            this._websiteToolstripMenuItem.Name = "_websiteToolstripMenuItem";
            this._websiteToolstripMenuItem.Size = new System.Drawing.Size(218, 22);
            this._websiteToolstripMenuItem.Text = "&Website";
            this._websiteToolstripMenuItem.Click += new System.EventHandler(this.WebsiteToolstripMenuItemClick);
            // 
            // _exitToolStripMenuItem
            // 
            this._exitToolStripMenuItem.Name = "_exitToolStripMenuItem";
            this._exitToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this._exitToolStripMenuItem.Text = "Exit";
            this._exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItemClick);
            // 
            // _statusStrip1
            // 
            this._statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._tsslStats});
            this._statusStrip1.Location = new System.Drawing.Point(0, 824);
            this._statusStrip1.Name = "_statusStrip1";
            this._statusStrip1.Size = new System.Drawing.Size(1015, 22);
            this._statusStrip1.TabIndex = 0;
            // 
            // _tsslStats
            // 
            this._tsslStats.Name = "_tsslStats";
            this._tsslStats.Size = new System.Drawing.Size(59, 17);
            this._tsslStats.Text = "Loading...";
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(1015, 846);
            this.ContextMenuStrip = this._ctxtTaskbar;
            this.Controls.Add(this._pnlCameras);
            this.Controls.Add(this._toolStrip1);
            this.Controls.Add(this._statusStrip1);
            this.HelpButton = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(0, 180);
            this.Name = "MainForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "iSpyServer";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.MainFormHelpButtonClicked);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFormFormClosing1);
            this.Load += new System.EventHandler(this.MainFormLoad);
            this.Resize += new System.EventHandler(this.MainFormResize);
            this._ctxtMainForm.ResumeLayout(false);
            this._toolStrip1.ResumeLayout(false);
            this._toolStrip1.PerformLayout();
            this._ctxtMnu.ResumeLayout(false);
            this._ctxtTaskbar.ResumeLayout(false);
            this._statusStrip1.ResumeLayout(false);
            this._statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        #region Nested type: LoadObjectListDelegate

        private delegate void LoadObjectListDelegate(string fileName);

        #endregion

        #region Nested type: ProcessCommandInternalDelegate

        private delegate void ProcessCommandInternalDelegate(string command);

        #endregion
    }
}