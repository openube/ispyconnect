using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using AForge.Video;
using iSpyApplication.Audio;
using iSpyApplication.Audio.streams;
using iSpyApplication.MP3Stream;
using iSpyApplication.Video;
using NAudio.Wave;
using iSpy.Video.FFMPEG;
using PictureBox = AForge.Controls.PictureBox;
using WaveFormat = NAudio.Wave.WaveFormat;

namespace iSpyApplication.Controls
{
    public sealed partial class VolumeLevel : PictureBox
    {
        #region Private

        
        private AudioFileWriter _writer;
        private DateTime _mouseMove = DateTime.MinValue;

        private double _intervalCount;
        private long _lastRun = DateTime.Now.Ticks;
        private double _secondCount;
        private bool _processing;
        private double _recordingTime;
        private Point _mouseLoc;        
        private bool _stopWrite;
        private volatile float[] _levels = new float[] {0};
        private readonly ToolTip _toolTipMic;
        private int _ttind = -1;
        private DateTime _errorTime = DateTime.MinValue;
        private DateTime _reconnectTime = DateTime.MinValue;
        private DateTime _soundLastDetected = DateTime.MinValue;
        private DateTime _lastAlertCheck = DateTime.MinValue;
        private bool _isTrigger;
        private readonly DateTime _lastScheduleCheck = DateTime.MinValue;
        private List<FilesFile> _filelist = new List<FilesFile>();
        private readonly AutoResetEvent _newRecordingFrame = new AutoResetEvent(false);
        private readonly object _obj = new object();
        private Thread _recordingThread;
        private const int ButtonOffset = 4, ButtonCount = 5;
        public bool ShuttingDown;
        private static int ButtonWidth
        {
            get { return MainForm.ButtonWidth; }
        }
        private static int ButtonPanelWidth
        {
            get
            {
                return ((ButtonWidth + ButtonOffset) * ButtonCount + ButtonOffset);
            }
        }
        private static int ButtonPanelHeight
        {
            get { return (ButtonWidth + ButtonOffset * 2); }
        }
        private bool _pairedRecording;
        private volatile bool _isReconnect;
        private delegate void SwitchDelegate();
        private readonly StringBuilder _soundData = new StringBuilder(100000);

        //private AudioStreamer _as = null;
        private WaveFormat _audioStreamFormat;
        private Mp3Writer _mp3Writer;
        private readonly MemoryStream _outStream = new MemoryStream();
        private readonly byte[] _bResampled = new byte[25000];

        #endregion

        #region Public
        public volatile bool IsEnabled;
        private bool _audioSourceErrorState;
        public bool AudioSourceErrorState
        {
            get { return _audioSourceErrorState; }
            set { _audioSourceErrorState = value;
                Invalidate();
            }
        }
        public bool Paired
        {
            get { return Micobject != null && MainForm.Cameras.FirstOrDefault(p => p.settings.micpair == Micobject.id) != null; }
        }
        public CameraWindow CameraControl
        {
            get
            {
                if (Micobject != null && Micobject.id>-1)
                {
                    var oc = MainForm.Cameras.FirstOrDefault(p => p.settings.micpair == Micobject.id);
                    if (oc != null && TopLevelControl != null)
                    {
                        return ((MainForm)TopLevelControl).GetCameraWindow(oc.id);
                    }
                }
                return null;
            }
        }

        #region Delegates
        public delegate void NewDataAvailable(object sender, NewDataAvailableArgs eventArgs);
        public delegate void NotificationEventHandler(object sender, NotificationType e);
        public delegate void RemoteCommandEventHandler(object sender, ThreadSafeCommand e);
        public delegate void FileListUpdatedEventHandler(object sender);
        #endregion

        #region Events
        public event RemoteCommandEventHandler RemoteCommand;
        public event NotificationEventHandler Notification;
        public event NewDataAvailable DataAvailable;
        public event FileListUpdatedEventHandler FileListUpdated;
        #endregion

        public List<AudioAction> AudioBuffer;
        public QueueWithEvents<AudioAction> WriterBuffer;
        public bool Alerted;
        public string AudioFileName = "";
        public Enums.AudioStreamMode AudioStreamMode;
       
        public Rectangle RestoreRect = Rectangle.Empty;
        public int FlashCounter;
        public bool ForcedRecording;
        public double InactiveRecord;
        public bool IsEdit;
        public bool NoSource;
        public bool ResizeParent;
        public bool SoundDetected;
        public objectsMicrophone Micobject;
        public double ReconnectCount;
        public double SoundCount;
        public IWavePlayer WaveOut;
        public IAudioSource AudioSource;

        public List<Socket> OutSockets = new List<Socket>();

        private Thread _tFiles;
        public void GetFiles()
        {
            if (_tFiles == null || !_tFiles.IsAlive)
            {
                _tFiles = new Thread(GenerateFileList);
                _tFiles.Start();
            }
        }

        private void GenerateFileList()
        {
            string dir = MainForm.Conf.MediaDirectory + "audio\\" +Micobject.directory + "\\";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            bool failed = false;
            if (File.Exists(dir + "data.xml"))
            {
                var s = new XmlSerializer(typeof(Files));
                try
                {
                    using (var fs = new FileStream(dir + "data.xml", FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        try
                        {
                            using (TextReader reader = new StreamReader(fs))
                            {
                                fs.Position = 0;
                                lock (_filelist)
                                {
                                    var t = ((Files)s.Deserialize(reader));
                                    if (t.File == null || !t.File.Any())
                                    {
                                        _filelist = new List<FilesFile>();
                                    }
                                    else
                                        _filelist = t.File.ToList();
                                }
                                reader.Close();
                            }
                            ScanForMissingFiles();
                        }
                        catch (Exception ex)
                        {
                            MainForm.LogExceptionToFile(ex);
                            failed = true;
                        }
                        fs.Close();
                    }
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    failed = true;
                }
                if (!failed)
                {
                    return;
                }
                    

            }

            //else build from directory contents

            _filelist = new List<FilesFile>();
            lock (_filelist)
            {
                var dirinfo = new DirectoryInfo(MainForm.Conf.MediaDirectory + "audio\\" + Micobject.directory + "\\");

                var lFi = new List<FileInfo>();
                lFi.AddRange(dirinfo.GetFiles());
                lFi = lFi.FindAll(f => f.Extension.ToLower() == ".mp3");
                lFi = lFi.OrderByDescending(f => f.CreationTime).ToList();

                //sanity check existing data
                foreach (FileInfo fi in lFi)
                {
                    FileInfo fi1 = fi;
                    if (_filelist.Count(p => p.Filename == fi1.Name) == 0)
                    {
                        _filelist.Add(new FilesFile
                                          {
                                              CreatedDateTicks = fi.CreationTime.Ticks,
                                              Filename = fi.Name,
                                              SizeBytes = fi.Length,
                                              MaxAlarm = 0,
                                              AlertData = "0",
                                              DurationSeconds = 0,
                                              IsTimelapse = false
                                          });
                    }
                }
                for (int index = 0; index < _filelist.Count; index++)
                {
                    FilesFile ff = _filelist[index];
                    if (lFi.All(p => p.Name != ff.Filename))
                    {
                        _filelist.Remove(ff);
                        index--;
                    }
                }
                _filelist = _filelist.OrderByDescending(p => p.CreatedDateTicks).ToList();
            }
            if (FileListUpdated != null)
                FileListUpdated(this);
        }
        public List<FilesFile> FileList
        {
            get
            {
                return _filelist;
            }
        }

        public void ClearFileList()
        {
            lock(_filelist)
            {
                _filelist.Clear();
            }
            lock (MainForm.MasterFileList)
            {
                MainForm.MasterFileList.RemoveAll(p=>p.ObjectTypeId==1 && p.ObjectId==Micobject.id);
            }

        }

        public void RemoveFile(string filename)
        {
            lock (_filelist)
            {
                FileList.RemoveAll(p => p.Filename == filename);
            }
            lock (MainForm.MasterFileList)
            {
                MainForm.MasterFileList.RemoveAll(p => p.Filename == filename);
            }
        }

        public void SaveFileList()
        {
            try
            {
                if (FileList != null)
                    lock (FileList)
                    {
                        var fl = new Files {File = FileList.ToArray()};
                        string dir = MainForm.Conf.MediaDirectory + "audio\\" +
                                    Micobject.directory + "\\";
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        var s = new XmlSerializer(typeof (Files));
                        using (var fs = new FileStream(dir+"data.xml", FileMode.Create))
                        {
                            using (TextWriter writer = new StreamWriter(fs))
                            {
                                fs.Position = 0;
                                s.Serialize(writer, fl);
                                writer.Close();
                            }
                            fs.Close();
                        }
                    }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
        }

        private Thread _tScan;
        private void ScanForMissingFiles()
        {
            if (_tScan == null || !_tScan.IsAlive)
            {
                _tScan = new Thread(ScanFiles);
                _tScan.Start();
            }
        }

        private void ScanFiles()
        {
            try
            {
                //check files exist
                var dirinfo = new DirectoryInfo(MainForm.Conf.MediaDirectory + "audio\\" + Micobject.directory + "\\");

                var lFi = new List<FileInfo>();
                lFi.AddRange(dirinfo.GetFiles());
                lFi = lFi.FindAll(f => f.Extension.ToLower() == ".mp3" || f.Extension.ToLower() == ".mp4");
                lFi = lFi.OrderByDescending(f => f.CreationTime).ToList();

                //var farr = _filelist.ToArray();
                lock (_filelist)
                {
                    for (int j = 0; j < _filelist.Count; j++)
                    {
                        var t = _filelist[j];
                        if (t != null)
                        {
                            var fe = lFi.FirstOrDefault(p => p.Name == t.Filename);
                            if (fe == null)
                            {
                                //file not found
                                _filelist.RemoveAt(j);
                                j--;
                                continue;
                            }
                            lFi.Remove(fe);
                        }
                    }
                    //add missing files
                    foreach (var fi in lFi)
                    {
                        _filelist.Add(new FilesFile
                                            {
                                                CreatedDateTicks = fi.CreationTime.Ticks,
                                                Filename = fi.Name,
                                                SizeBytes = fi.Length,
                                                MaxAlarm = 0,
                                                AlertData = "0",
                                                DurationSeconds = 0,
                                                IsTimelapse =
                                                    fi.Name.ToLower().IndexOf("timelapse", StringComparison.Ordinal) !=
                                                    -1
                                            });
                    }
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            if (FileListUpdated != null)
                FileListUpdated(this);
        }

        public bool Recording
        {
            get
            {
                return _recordingThread != null && _recordingThread.IsAlive;       
            }
        }

        public bool Listening
        {
            get
            {
                if (WaveOut != null && WaveOut.PlaybackState == PlaybackState.Playing)
                    return true;
                return false;
            }
            set
            {
                if (WaveOut != null)
                {
                    if (value && AudioSource!=null)
                    {
                        AudioSource.Listening = true; //(creates the waveoutprovider referenced below)
                        WaveOut.Init(AudioSource.WaveOutProvider);
                        WaveOut.Play();
                        
                    }
                    else
                    {
                        if (AudioSource != null) AudioSource.Listening = false;
                        WaveOut.Stop();
                        WaveOut.Dispose();
                    }
                }
            }
        }

        public float Volume
        {
            get
            {
                return Micobject.settings.volume;
            }
            set
            {
                if (AudioSource != null)
                    AudioSource.Volume = value;
                Micobject.settings.gain = value;
            }
        }
        #endregion

        #region SizingControls

        private MousePos GetMousePos(Point location)
        {
            var result = MousePos.NoWhere;
            int rightSize = Padding.Right;
            int bottomSize = Padding.Bottom;
            var testRect = new Rectangle(Width - rightSize, 0, Width - rightSize, Height - bottomSize);
            if (testRect.Contains(location)) result = MousePos.Right;
            testRect = new Rectangle(0, Height - bottomSize, Width - rightSize, Height);
            if (testRect.Contains(location)) result = MousePos.Bottom;
            testRect = new Rectangle(Width - rightSize, Height - bottomSize, Width, Height);
            if (testRect.Contains(location)) result = MousePos.BottomRight;
            return result;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Select();
            IntPtr hwnd = Handle;
            if ((ResizeParent) && (Parent != null) && (Parent.IsHandleCreated))
            {
                hwnd = Parent.Handle;
            }
            if (e.Button == MouseButtons.Left)
            {
                MousePos mousePos = GetMousePos(e.Location);

                if (mousePos == MousePos.NoWhere)
                {
                    if (MainForm.Conf.ShowOverlayControls)
                    {
                        int leftpoint = Width - ButtonPanelWidth-1;
                        const int ypoint = 1;

                        if (e.Location.X > leftpoint && e.Location.X < leftpoint + ButtonPanelWidth &&
                                    e.Location.Y > ypoint && e.Location.Y < ypoint + ButtonPanelHeight)
                        {
                            int x = e.Location.X - leftpoint;
                            if (x < ButtonWidth + ButtonOffset)
                            {
                                //power
                                if (Micobject.settings.active)
                                {
                                    Disable();
                                }
                                else
                                {
                                    Enable();
                                }
                            }
                            else
                            {
                                if (x < (ButtonWidth + ButtonOffset)*2)
                                {
                                    //record
                                    if (Micobject.settings.active)
                                    {
                                        RecordSwitch(!Recording);
                                    }
                                }
                                else
                                {
                                    if (TopLevelControl != null)
                                    {
                                        if (x < (ButtonWidth + ButtonOffset)*3)
                                            ((MainForm) TopLevelControl).EditMicrophone(Micobject);
                                        else
                                        {
                                            if (x < (ButtonWidth + ButtonOffset)*4)
                                            {
                                                string url = MainForm.Webserver + "/watch_new.aspx";// "?tab=1&obj=1_" +Micobject.id +"_" +MainForm.Conf.ServerPort;
                                                if (WsWrapper.WebsiteLive && MainForm.Conf.ServicesEnabled)
                                                {
                                                    MainForm.OpenUrl(url);
                                                }
                                                else
                                                    ((MainForm) TopLevelControl).Connect(url, false);
                                            }
                                            else
                                            {
                                                if (Micobject.settings.active)
                                                {
                                                    Listening = !Listening;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return;
                }
                if (CameraControl!=null ||  MainForm.Conf.LockLayout) return;
                switch (mousePos)
                {
                    case MousePos.Right:
                        {
                            NativeCalls.ReleaseCapture(hwnd);
                            NativeCalls.SendMessage(hwnd, NativeCalls.WmSyscommand, NativeCalls.ScDragsizeE, IntPtr.Zero);
                        }
                        break;
                    case MousePos.Bottom:
                        {
                            NativeCalls.ReleaseCapture(hwnd);
                            NativeCalls.SendMessage(hwnd, NativeCalls.WmSyscommand, NativeCalls.ScDragsizeS, IntPtr.Zero);
                        }
                        break;
                    case MousePos.BottomRight:
                        {
                            NativeCalls.ReleaseCapture(hwnd);
                            NativeCalls.SendMessage(hwnd, NativeCalls.WmSyscommand, NativeCalls.ScDragsizeSe,
                                                    IntPtr.Zero);
                        }
                        break;
                    default:
                        Cursor = Cursors.Hand;
                        break;

                }
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_mouseLoc.X == e.X && _mouseLoc.Y == e.Y)
                return;
            _mouseLoc.X = e.X;
            _mouseLoc.Y = e.Y;

            _mouseMove = DateTime.Now;
            MousePos mousePos = GetMousePos(e.Location);
            switch (mousePos)
            {
                case MousePos.Right:
                    Cursor = Cursors.SizeWE;
                    break;
                case MousePos.Bottom:
                    Cursor = Cursors.SizeNS;
                    break;
                case MousePos.BottomRight:
                    Cursor = Cursors.SizeNWSE;
                    break;
                default:
                    Cursor = Cursors.Hand;
                    if (MainForm.Conf.ShowOverlayControls)
                    {
                        int leftpoint = Width - ButtonPanelWidth - 1;
                        const int ypoint = 1;
                        var toolTipLocation = new Point(e.Location.X, ypoint + ButtonPanelHeight + 1);
                        if (e.Location.X > leftpoint && e.Location.X < leftpoint + ButtonPanelWidth &&
                            e.Location.Y > ypoint && e.Location.Y < ypoint + ButtonPanelHeight)
                        {
                            int x = e.Location.X - leftpoint;
                            if (x < ButtonWidth + ButtonOffset)
                            {
                                //power
                                if (_ttind != 0)
                                {
                                    _toolTipMic.Show(
                                        Micobject.settings.active
                                            ? LocRm.GetString("switchOff")
                                            : LocRm.GetString("Switchon"), this, toolTipLocation, 1000);
                                    _ttind = 0;
                                }
                            }
                            else
                            {
                                if (x < (ButtonWidth + ButtonOffset) * 2)
                                {
                                    //record
                                    if (_ttind != 1)
                                    {
                                        _toolTipMic.Show(LocRm.GetString("RecordNow"), this, toolTipLocation, 1000);
                                        _ttind = 1;
                                    }
                                }
                                else
                                {
                                    if (TopLevelControl != null)
                                    {
                                        if (x < (ButtonWidth + ButtonOffset) * 3)
                                        {
                                            if (_ttind != 2)
                                            {
                                                _toolTipMic.Show(LocRm.GetString("Edit"), this, toolTipLocation, 1000);
                                                _ttind = 2;
                                            }
                                        }
                                        else
                                        {
                                            if (x < (ButtonWidth + ButtonOffset) * 4)
                                            {
                                                if (_ttind != 3)
                                                {
                                                    _toolTipMic.Show(LocRm.GetString("MediaoverTheWeb"), this,
                                                                    toolTipLocation, 1000);
                                                    _ttind = 3;
                                                }
                                            }
                                            else
                                            {
                                                if (_ttind != 4)
                                                {
                                                    _toolTipMic.Show(
                                                        Listening
                                                            ? LocRm.GetString("StopListening")
                                                            : LocRm.GetString("Listen"), this, toolTipLocation, 1000);
                                                    _ttind = 4;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            _toolTipMic.Hide(this);
                            _ttind = -1;
                        }
                    }
                    break;
            }
            base.OnMouseMove(e);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            if (CameraControl == null)
            {
                if ((ModifierKeys & Keys.Shift) == Keys.Shift)
                {
                    var ar = Convert.ToDouble(MinimumSize.Width)/Convert.ToDouble(MinimumSize.Height);
                    Width = Convert.ToInt32(ar*Height);
                }

               
                if (Width < MinimumSize.Width) Width = MinimumSize.Width;
                if (Height < MinimumSize.Height) Height = MinimumSize.Height;
            }
            base.OnResize(eventargs);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Cursor = Cursors.Default;
            _mouseMove = DateTime.MinValue;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            if (CameraControl==null)
                Cursor = Cursors.Hand;
            Invalidate();
        }


        #region Nested type: MousePos

        private enum MousePos
        {
            NoWhere,
            Right,
            Bottom,
            BottomRight
        }

        #endregion

        #endregion

        public VolumeLevel(objectsMicrophone om)
        {
            InitializeComponent();

            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint, true);
            Margin = new Padding(0, 0, 0, 0);
            Padding = new Padding(0, 0, 5, 5);
            BorderStyle = BorderStyle.None;
            BackColor = MainForm.BackgroundColor;
            Micobject = om;

            _toolTipMic = new ToolTip { AutomaticDelay = 500, AutoPopDelay = 1500 };
        }


        [DefaultValue(false)]
        public float[] Levels
        {
            get
            {
                return _levels;
            }
            set
            {
                if (value.Length!=_levels.Length)
                    _levels = new float[value.Length];
                value.CopyTo(_levels, 0);

                if (_levels.Length > 0 && AudioSourceErrorState)
                {
                    if (AudioSourceErrorState)
                        UpdateFloorplans(false);
                    AudioSourceErrorState = false;
                }

                Invalidate();
            }
        }

        public string[] ScheduleDetails
        {
            get
            {
                var entries = new List<string>();
                foreach (var sched in Micobject.schedule.entries)
                {
                    string daysofweek = sched.daysofweek;
                    daysofweek = daysofweek.Replace("0", LocRm.GetString("Sun"));
                    daysofweek = daysofweek.Replace("1", LocRm.GetString("Mon"));
                    daysofweek = daysofweek.Replace("2", LocRm.GetString("Tue"));
                    daysofweek = daysofweek.Replace("3", LocRm.GetString("Wed"));
                    daysofweek = daysofweek.Replace("4", LocRm.GetString("Thu"));
                    daysofweek = daysofweek.Replace("5", LocRm.GetString("Fri"));
                    daysofweek = daysofweek.Replace("6", LocRm.GetString("Sat"));

                    string s = sched.start + " -> " + sched.stop + " (" + daysofweek + ")";
                    if (sched.recordonstart)
                        s += " " + LocRm.GetString("RECORD_UC");
                    if (sched.alerts)
                        s += " " + LocRm.GetString("ALERT_UC");
                    if (sched.recordondetect)
                        s += " " + LocRm.GetString("DETECT_UC");
                    if (!sched.active)
                        s += " (" + LocRm.GetString("INACTIVE_UC") + ")";

                    entries.Add(s);
                }
                return entries.ToArray();
            }
        }

        public void Tick()
        {
            if (_processing)
                return;
            _processing = true;
            try
            {
                //time since last tick
                var ts = new TimeSpan(DateTime.Now.Ticks - _lastRun);
                _lastRun = DateTime.Now.Ticks;
                _secondCount += ts.Milliseconds / 1000.0;
                

                if (FlashCounter <= 5)
                {
                    SoundDetected = false;
                }

                if (FlashCounter > 5)
                {
                    InactiveRecord = 0;
                    if (Micobject.alerts.mode!="nosound" && CameraControl != null)
                        CameraControl.InactiveRecord = 0;
                }

                if (FlashCounter == 1)
                {
                    UpdateFloorplans(false);
                }

                if (FlashCounter > 0)
                    FlashCounter--;

                if (Recording)
                    _recordingTime += Convert.ToDouble(ts.TotalMilliseconds) / 1000.0;

                bool reset = true;              

                if (Micobject.alerts.active && Micobject.settings.active)
                {
                    reset = FlashBackground();
                }

                if (reset)
                    BackColor = MainForm.BackgroundColor;

                if (_secondCount > 1) //approx every second
                {
                    if (CheckReconnect()) goto skip;

                    CheckReconnectInterval();

                    CheckDisconnect();

                    if (Recording && !SoundDetected && !ForcedRecording)
                    {
                        InactiveRecord += _secondCount;
                    }

                    if (Micobject.schedule.active)
                    {
                        if (CheckSchedule()) goto skip;
                    }

                    CheckRecord();
                }

                CheckAlert();
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }

            skip:
            if (_secondCount > 1)
                _secondCount = 0;
            _processing = false;
        }

        private bool FlashBackground()
        {
            bool reset = true;
            if (FlashCounter > 0 && _isTrigger)
            {
                BackColor = (BackColor == MainForm.ActivityColor)
                                ? MainForm.BackgroundColor
                                : MainForm.ActivityColor;
                reset = false;
            }
            else
            {
                switch (Micobject.alerts.mode)
                {
                    case "sound":
                        if (FlashCounter > 0)
                        {
                            BackColor = (BackColor == MainForm.ActivityColor)
                                            ? MainForm.BackgroundColor
                                            : MainForm.ActivityColor;
                            reset = false;
                        }
                        break;
                    case "nosound":
                        if (!SoundDetected)
                        {
                            BackColor = (BackColor == MainForm.NoActivityColor)
                                            ? MainForm.BackgroundColor
                                            : MainForm.NoActivityColor;
                            reset = false;
                        }
                        break;
                }
            }
            return reset;
        }

        private void CheckReconnectInterval()
        {
            if (Micobject.settings.active && AudioSource != null)
            {
                if (Micobject.settings.reconnectinterval > 0 && !(AudioSource is IVideoSource))
                {
                    ReconnectCount += _secondCount;
                    if (ReconnectCount > Micobject.settings.reconnectinterval)
                    {
                        _isReconnect = true;
                        lock (this)
                        {
                            AudioSource.Stop();
                            while (AudioSource.IsRunning)
                            {
                                Thread.Sleep(100);
                            }
                            AudioSource.Start();
                        }
                        _isReconnect = false;
                        ReconnectCount = 0;
                    }
                    
                }
            }
        }

        private void CheckDisconnect()
        {
            if (Micobject.settings.notifyondisconnect && _errorTime != DateTime.MinValue)
            {
                int sec = Convert.ToInt32((DateTime.Now - _errorTime).TotalSeconds);
                if (sec > 30 && sec < 35)
                {
                    string subject =
                        LocRm.GetString("MicrophoneNotifyDisconnectMailSubject").Replace("[OBJECTNAME]",
                                                                                         Micobject.name);
                    string message = LocRm.GetString("MicrophoneNotifyDisconnectMailBody");
                    message = message.Replace("[NAME]", Micobject.name);
                    message = message.Replace("[TIME]", DateTime.Now.ToLongTimeString());


                    if (MainForm.Conf.ServicesEnabled && MainForm.Conf.Subscribed)
                        WsWrapper.SendAlert(Micobject.settings.emailaddress, subject, message);

                    _errorTime = DateTime.MinValue;
                }
            }
        }

        private bool CheckSchedule()
        {
            DateTime dtnow = DateTime.Now;
            foreach (objectsMicrophoneScheduleEntry entry in Micobject.schedule.entries.Where(p => p.active))
            {
                if (
                    entry.daysofweek.IndexOf(
                        ((int) dtnow.DayOfWeek).ToString(CultureInfo.InvariantCulture),
                        StringComparison.Ordinal) != -1)
                {
                    string[] stop = entry.stop.Split(':');
                    if (stop[0] != "-")
                    {
                        if (Convert.ToInt32(stop[0]) == dtnow.Hour)
                        {
                            if (Convert.ToInt32(stop[1]) == dtnow.Minute && dtnow.Second < 2)
                            {
                                Micobject.detector.recordondetect = entry.recordondetect;
                                Micobject.detector.recordonalert = entry.recordonalert;
                                Micobject.alerts.active = entry.alerts;

                                if (Micobject.settings.active)
                                    Disable();
                                return true;
                            }
                        }
                    }

                    string[] start = entry.start.Split(':');
                    if (start[0] != "-")
                    {
                        if (Convert.ToInt32(start[0]) == dtnow.Hour)
                        {
                            if (Convert.ToInt32(start[1]) == dtnow.Minute && dtnow.Second < 3)
                            {
                                if (!Micobject.settings.active)
                                    Enable();
                                if ((dtnow - _lastScheduleCheck).TotalSeconds > 60)
                                {
                                    Micobject.detector.recordondetect = entry.recordondetect;
                                    Micobject.detector.recordonalert = entry.recordonalert;
                                    Micobject.alerts.active = entry.alerts;
                                    if (entry.recordonstart)
                                    {
                                        ForcedRecording = true;
                                    }
                                }
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private void CheckAlert()
        {
            if (Micobject.settings.active && AudioSource != null)
            {
                if (Alerted)
                {
                    _intervalCount += _secondCount;
                    if (_intervalCount > Micobject.alerts.minimuminterval)
                    {
                        Alerted = false;
                        _intervalCount = 0;
                        UpdateFloorplans(false);
                        _lastAlertCheck = DateTime.Now;
                    }
                }
                else
                {
                    if (Micobject.alerts.active && AudioSource != null)
                    {
                        switch (Micobject.alerts.mode)
                        {
                            case "sound":

                                if (_soundLastDetected > _lastAlertCheck)
                                {
                                    SoundCount += _secondCount;
                                    if (_isTrigger ||
                                        (Math.Floor(SoundCount) >= Micobject.detector.soundinterval))
                                    {
                                        RemoteCommand(this, new ThreadSafeCommand("bringtofrontmic," + Micobject.id));
                                        DoAlert();
                                        SoundCount = 0;
                                        if (Micobject.detector.recordonalert && !Recording)
                                        {
                                            StartSaving();
                                        }
                                    }
                                    _lastAlertCheck = DateTime.Now;
                                }
                                else
                                    SoundCount = 0;

                                break;
                            case "nosound":
                                if ((DateTime.Now - _soundLastDetected).TotalSeconds >
                                    Micobject.detector.nosoundinterval)
                                {
                                    RemoteCommand(this, new ThreadSafeCommand("bringtofrontmic," + Micobject.id));
                                    DoAlert();
                                    if (Micobject.detector.recordonalert && !Recording)
                                    {
                                        StartSaving();
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }

        private void CheckRecord()
        {
            if (((Micobject.detector.recordondetect && SoundDetected) || ForcedRecording) && !Recording)
            {
                StartSaving();
            }
            else
            {
                if (!_stopWrite && Recording && !_pairedRecording)
                {
                    if (_recordingTime > Micobject.recorder.maxrecordtime ||
                        ((!SoundDetected && InactiveRecord > Micobject.recorder.inactiverecord) &&
                         !ForcedRecording))
                        StopSaving();
                }
            }
        }

        private bool CheckReconnect()
        {
            if (_reconnectTime != DateTime.MinValue)
            {
                if (AudioSource != null)
                {
                    int sec = Convert.ToInt32((DateTime.Now - _reconnectTime).TotalSeconds);
                    if (sec > 10)
                    {
                        //try to reconnect every 10 seconds
                        if (!AudioSource.IsRunning)
                        {
                            lock (this)
                            {
                                AudioSource.Start();
                            }
                        }
                        _reconnectTime = DateTime.Now;
                        return true;
                    }
                }
            }
            return false;
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        public bool Highlighted;

        public Color BorderColor
        {
            get
            {
                if (Highlighted)
                    return MainForm.FloorPlanHighlightColor;

                if (Focused)
                    return MainForm.BorderHighlightColor;

                return MainForm.BorderDefaultColor;

            }
        }

        public int BorderWidth
        {
            get
            {
                return (Highlighted || Focused) ? 2 : 1;
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            // lock
            Monitor.Enter(this);

            var gMic = pe.Graphics;
            var rc = ClientRectangle;

            
            var grabBrush = new SolidBrush(BorderColor);
            var borderPen = new Pen(grabBrush,BorderWidth);
            var lgb = new SolidBrush(MainForm.VolumeLevelColor);
            //var drawBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
            //var sbTs = new SolidBrush(Color.FromArgb(128, 0, 0, 0));
            //var drawPen = new Pen(drawBrush);
           
            if (Micobject.settings.active && _levels != null && !AudioSourceErrorState)
            {
                int bh = (rc.Height - 20) / Micobject.settings.channels - (Micobject.settings.channels - 1) * 2;
                if (bh <= 2)
                    bh = 2;
                for (int j = 0; j < Micobject.settings.channels; j++)
                {
                    float f = 0f;
                    if (j < _levels.Length)
                        f = _levels[j];
                    int drawW = Convert.ToInt32(Convert.ToDouble(rc.Width - 1.0)*f);
                    if (drawW < 1)
                        drawW = 1;

                    gMic.FillRectangle(lgb, rc.X + 2, rc.Y + 2 + j * bh + (j * 2), drawW - 4, bh);

                }
                var mx =
                    (float) ((Convert.ToDouble(rc.Width)/100.00)*Convert.ToDouble(Micobject.detector.sensitivity));
                var pline = new Pen(Color.Green, 2);
                gMic.DrawLine(pline, mx, 2, mx, rc.Height - 20);
                pline.Dispose();

                
                if (Listening)
                {
                    gMic.DrawString("LIVE", MainForm.Drawfont, MainForm.CameraDrawBrush, new PointF(5, 4));
                }    
                
                

                if (Recording)
                {
                    gMic.FillEllipse(MainForm.RecordBrush, new Rectangle(rc.Width - 14, 2, 8, 8));
                }
                
                string m = "";
                if (Micobject.alerts.active)
                    m = "!: " + m;
                else
                {
                    m = ": " + m;
                }

                if (ForcedRecording)
                    m = "F" + m;
                else
                {
                    if (Micobject.detector.recordondetect)
                        m = "D" + m;
                    else
                    {
                        if (Micobject.detector.recordonalert)
                            m = "A" + m;
                        else
                        {
                            m = "N" + m;
                        }
                    }

                }
                gMic.DrawString(m+ Micobject.name, MainForm.Drawfont, MainForm.CameraDrawBrush, new PointF(5, rc.Height - 18));

                lgb.Dispose();
            }
            else
            {
                if (NoSource || AudioSourceErrorState)
                {
                    gMic.DrawString(LocRm.GetString("NoSource") + ": " + Micobject.name,
                                     MainForm.Drawfont, MainForm.CameraDrawBrush, new PointF(5, 5));
                }
                else
                {
                    if (Micobject.schedule.active)
                    {
                        gMic.DrawString("S: " + Micobject.name,
                                         MainForm.Drawfont, MainForm.CameraDrawBrush, new PointF(5, 5));
                    }
                    else
                    {
                        gMic.DrawString(LocRm.GetString("Inactive") + ": " + Micobject.name,
                                         MainForm.Drawfont, MainForm.CameraDrawBrush, new PointF(5, 5));
                    }
                }
            }

            if (_mouseMove > DateTime.Now.AddSeconds(-3) && MainForm.Conf.ShowOverlayControls)
            {
                int leftpoint = Width - ButtonPanelWidth-1;
                const int ypoint = 0;

                gMic.FillRectangle(MainForm.TimestampBackgroundBrush, leftpoint, ypoint, ButtonPanelWidth, ButtonPanelHeight);



                gMic.DrawString(">", MainForm.Iconfont, Micobject.settings.active ? MainForm.IconBrushActive : MainForm.IconBrush, leftpoint + ButtonOffset, ypoint + ButtonOffset);

                var b = MainForm.IconBrushOff;
                if (Micobject.settings.active)
                {
                    b = MainForm.IconBrush;
                }
                gMic.DrawString("R", MainForm.Iconfont,
                                    Recording ? MainForm.IconBrushActive : b,
                                    leftpoint + (ButtonOffset * 2) + ButtonWidth,
                                    ypoint + ButtonOffset);

                gMic.DrawString("E", MainForm.Iconfont, b, leftpoint + (ButtonOffset * 3) + (ButtonWidth * 2),
                                ypoint + ButtonOffset);
                gMic.DrawString("C", MainForm.Iconfont, b, leftpoint + (ButtonOffset * 4) + (ButtonWidth * 3),
                                ypoint + ButtonOffset);

                gMic.DrawString("L", MainForm.Iconfont, Listening ? MainForm.IconBrushActive : b, leftpoint + (ButtonOffset * 5) + (ButtonWidth * 4),
                                ypoint + ButtonOffset);
            }


            if (!Paired)
            {
                var grabPoints = new[]
                                 {
                                     new Point(rc.Width - 15, rc.Height), new Point(rc.Width, rc.Height - 15),
                                     new Point(rc.Width, rc.Height)
                                 };
                gMic.FillPolygon(grabBrush, grabPoints);
            }

            if (!Paired)
                gMic.DrawRectangle(borderPen, 0, 0, rc.Width - 1, rc.Height - 1);
            else
            {
                gMic.DrawLine(borderPen, 0, 0, 0, rc.Height - 1);
                gMic.DrawLine(borderPen, 0, rc.Height - 1, rc.Width - 1, rc.Height - 1);
                gMic.DrawLine(borderPen, rc.Width - 1, rc.Height - 1, rc.Width - 1, 0);
            }



            borderPen.Dispose();
            grabBrush.Dispose();
            Monitor.Exit(this);

            base.OnPaint(pe);
        }

        public void StopSaving()
        {
            _stopWrite = true;
            if (_recordingThread != null)
                _recordingThread.Join();
        }

        public void StartSaving()
        {
            if (Recording || MainForm.StopRecordingFlag || IsEdit)
                return;

            _recordingThread = new Thread(Record) { Name = "Recording Thread (" + Micobject.id + ")", IsBackground = false, Priority = ThreadPriority.Normal };
            _recordingThread.Start();
        }

        private void Record()
        {
            _stopWrite = false;
            DateTime recordingStart = DateTime.MinValue;

            if (!String.IsNullOrEmpty(Micobject.recorder.trigger) && TopLevelControl != null)
            {
                string[] tid = Micobject.recorder.trigger.Split(',');
                switch (tid[0])
                {
                    case "1":
                        VolumeLevel vl = ((MainForm)TopLevelControl).GetVolumeLevel(Convert.ToInt32(tid[1]));
                        if (vl != null && !vl.Recording)
                            vl.RecordSwitch(true);
                        break;
                    case "2":
                        CameraWindow cw = ((MainForm)TopLevelControl).GetCameraWindow(Convert.ToInt32(tid[1]));
                        if (cw != null && !cw.Recording)
                            cw.RecordSwitch(true);
                        break;
                }
            }

            try
            {
                WriterBuffer = new QueueWithEvents<AudioAction>();
                WriterBuffer.Changed += WriterBufferChanged;

                _pairedRecording = false;
                if (CameraControl!=null && CameraControl.Camobject.settings.active)
                {
                    _pairedRecording = true;
                    CameraControl.StartSaving();
                    CameraControl.ForcedRecording = ForcedRecording;
                    while (!_stopWrite)
                    {
                        _newRecordingFrame.WaitOne(200);
                    }
                }
                else
                {
                    #region mp3writer

                    DateTime date = DateTime.Now;

                    string filename = String.Format("{0}-{1}-{2}_{3}-{4}-{5}",
                                                    date.Year, Helper.ZeroPad(date.Month), Helper.ZeroPad(date.Day),
                                                    Helper.ZeroPad(date.Hour), Helper.ZeroPad(date.Minute),
                                                    Helper.ZeroPad(date.Second));

                    AudioFileName = Micobject.id + "_" + filename;
                    filename = MainForm.Conf.MediaDirectory + "audio\\" + Micobject.directory + "\\";
                    filename += AudioFileName;

                    
                    
                    Program.WriterMutex.WaitOne();
                    try
                    {
                        _writer = new AudioFileWriter();
                        _writer.Open(filename + ".mp3", AudioCodec.MP3, AudioSource.RecordingFormat.BitsPerSample * AudioSource.RecordingFormat.SampleRate * AudioSource.RecordingFormat.Channels, AudioSource.RecordingFormat.SampleRate, AudioSource.RecordingFormat.Channels);
                    }
                    catch (Exception ex)
                    {
                        MainForm.LogExceptionToFile(ex);
                    }
                    finally
                    {
                        Program.WriterMutex.ReleaseMutex();
                    }

                    

                    double maxlevel = 0;
                    foreach (AudioAction aa in AudioBuffer.OrderBy(p=>p.TimeStamp))
                    {
                        if (recordingStart == DateTime.MinValue)
                        {
                            recordingStart = aa.TimeStamp;
                        }

                        unsafe
                        {
                            fixed (byte* p = aa.Decoded)
                            {
                                _writer.WriteAudio(p, aa.Decoded.Length);
                            }
                        }

                        _soundData.Append(String.Format(CultureInfo.InvariantCulture,
                                                        "{0:0.000}", aa.SoundLevel));
                        _soundData.Append(",");
                        if (aa.SoundLevel > maxlevel)
                            maxlevel = aa.SoundLevel;
                    }
                    
                    AudioBuffer.Clear();

                    if (recordingStart == DateTime.MinValue)
                        recordingStart = DateTime.Now;

                    try
                    {
                        while (!_stopWrite)
                        {
                            while (WriterBuffer.Count > 0)
                            {
                                AudioAction b;
                                lock (_obj)
                                {
                                    b = WriterBuffer.Dequeue();
                                }
                                unsafe
                                {
                                    fixed (byte* p = b.Decoded)
                                    {
                                        _writer.WriteAudio(p, b.Decoded.Length);
                                    }
                                }
                                float d = Levels.Max();
                                _soundData.Append(String.Format(CultureInfo.InvariantCulture,
                                                               "{0:0.000}", d));
                                _soundData.Append(",");
                                if (d > maxlevel)
                                    maxlevel = d;

                            }
                            _newRecordingFrame.WaitOne(200);
                        }


                        FilesFile ff = FileList.FirstOrDefault(p => p.Filename.EndsWith(AudioFileName + ".mp3"));
                        bool newfile = false;
                        if (ff == null)
                        {
                            ff = new FilesFile();
                            newfile = true;
                        }


                        string[] fnpath = (filename + ".mp3").Split('\\');
                        string fn = fnpath[fnpath.Length - 1];
                        var fi = new FileInfo(filename + ".mp3");
                        var dSeconds = Convert.ToInt32((DateTime.Now - recordingStart).TotalSeconds);

                        ff.CreatedDateTicks = DateTime.Now.Ticks;
                        ff.Filename = fnpath[fnpath.Length - 1];
                        ff.MaxAlarm = maxlevel;
                        ff.SizeBytes = fi.Length;
                        ff.DurationSeconds = dSeconds;
                        ff.IsTimelapse = false;
                        ff.AlertData = Helper.GetMotionDataPoints(_soundData);
                        _soundData.Clear();
                        ff.TriggerLevel = Micobject.detector.sensitivity;
                        ff.TriggerLevelMax = 100;

                        if (newfile)
                        {
                            FileList.Insert(0, ff);
                            lock (MainForm.MasterFileList)
                            {
                                MainForm.MasterFileList.Add(new FilePreview(fn, dSeconds, Micobject.name,
                                                                            DateTime.Now.Ticks, 1, Micobject.id,
                                                                            ff.MaxAlarm));
                            }
                        }


                    }
                    catch (Exception ex)
                    {
                        MainForm.LogExceptionToFile(ex);                       
                    }

                    Program.WriterMutex.WaitOne();
                    try
                    {
                        _writer.Close();
                        _writer.Dispose();
                    }
                    catch (Exception ex)
                    {
                        MainForm.LogExceptionToFile(ex);
                    }
                    finally
                    {
                        Program.WriterMutex.ReleaseMutex();
                    }
                    
                    _writer = null;
                    #endregion
                }
                _stopWrite = false;
                WriterBuffer = null;
                _recordingTime = 0;
                UpdateFloorplans(false);
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            
            if (!String.IsNullOrEmpty(Micobject.recorder.trigger) && TopLevelControl != null)
            {
                string[] tid = Micobject.recorder.trigger.Split(',');
                switch (tid[0])
                {
                    case "1":
                        VolumeLevel vl = ((MainForm)TopLevelControl).GetVolumeLevel(Convert.ToInt32(tid[1]));
                        if (vl != null)
                            vl.RecordSwitch(false);
                        break;
                    case "2":
                        CameraWindow cw = ((MainForm)TopLevelControl).GetCameraWindow(Convert.ToInt32(tid[1]));
                        if (cw != null)
                            cw.RecordSwitch(false);
                        break;
                }
            }

            if (!_pairedRecording)
            {
                Micobject.newrecordingcount++;
                if (Notification != null)
                    Notification(this, new NotificationType("NewRecording", Micobject.name, ""));
            }
        }

        void WriterBufferChanged(object sender, EventArgs e)
        {
            _newRecordingFrame.Set();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Invalidate();              
            }
            _processing = true;
            if (WaveOut != null)
                WaveOut.Dispose();
            _toolTipMic.RemoveAll();
            _toolTipMic.Dispose();            
            base.Dispose(disposing);
        }

        public void Disable()
        {
            if (!IsEnabled)
                return;
            if (InvokeRequired)
            {
                Invoke(new SwitchDelegate(Disable));
                return;
            }

            SetDisabled(true);
        }

        private void SetDisabled(bool stopSource)
        {
            _processing = true;

            if (_recordingThread != null)
                RecordSwitch(false);

            if (AudioSource != null)
            {
                AudioSource.AudioFinished -= AudioDeviceAudioFinished;
                AudioSource.AudioSourceError -= AudioDeviceAudioSourceError;
                AudioSource.DataAvailable -= AudioDeviceDataAvailable;
                AudioSource.LevelChanged -= AudioDeviceLevelChanged;

                if (!(AudioSource is IVideoSource) && stopSource)
                {
                    lock (this)
                    {
                        AudioSource.Stop();
                    }
                }

            }
            //allow operations to complete in other threads
            Thread.Sleep(250);
            IsEnabled = false;

            if (AudioBuffer != null)
            {
                AudioBuffer.Clear();
            }

            SoundDetected = false;
            ForcedRecording = false;
            Alerted = false;
            NoSource = false;
            FlashCounter = 0;
            _recordingTime = 0;
            Listening = false;
            ReconnectCount = 0;

            UpdateFloorplans(false);
            Micobject.settings.active = false;

            MainForm.NeedsSync = true;
            _errorTime = _reconnectTime = DateTime.MinValue;

            if (!ShuttingDown)
                Invalidate();
            _processing = false;
        }

        public void Enable()
        {
            if (IsEnabled)
                return;
            if (InvokeRequired)
            {
                Invoke(new SwitchDelegate(Enable));
                return;
            }

            
            if (CameraControl!=null)
            {
                Width = CameraControl.Width;
                Location = new Point(CameraControl.Location.X, CameraControl.Location.Y + CameraControl.Height);
                Width = Width;
                Height = 40;
                if (!CameraControl.IsEnabled)
                {
                    CameraControl.Enable(); //will then enable this
                    return;
                }
            }

            IsEnabled = true;
            _processing = true;

            int sampleRate = Micobject.settings.samples;
            int channels = Micobject.settings.channels;
            const int bitsPerSample = 16;

            if (channels < 1)
            {
                channels = Micobject.settings.channels = 1;
            }
            if (sampleRate<8000)
            {
                sampleRate = Micobject.settings.samples = 8000;
            }

            switch (Micobject.settings.typeindex)
            {
                case 0: //usb
                    AudioSource = new LocalDeviceStream(Micobject.settings.sourcename) { RecordingFormat = new WaveFormat(sampleRate, bitsPerSample, channels) };
                    break;
                case 1: //ispy server (fixed waveformat at the moment...)
                    AudioSource = new iSpyServerStream(Micobject.settings.sourcename) { RecordingFormat = new WaveFormat(8000,16,1) };
                    break;
                case 2: //VLC listener
                    List<String> inargs = Micobject.settings.vlcargs.Split(Environment.NewLine.ToCharArray(),
                                                                           StringSplitOptions.RemoveEmptyEntries).ToList();
                    //switch off video output
                    inargs.Add(":sout=#transcode{vcodec=none}:Display");

                    AudioSource = new VLCStream(Micobject.settings.sourcename, inargs.ToArray()) { RecordingFormat = new WaveFormat(sampleRate, bitsPerSample, channels), TimeOut = Micobject.settings.timeout };
                    break;
                case 3: //FFMPEG listener
                    AudioSource = new FFMPEGAudioStream(Micobject.settings.sourcename)
                                      {
                                          RecordingFormat = new WaveFormat(sampleRate, bitsPerSample, channels),
                                          AnalyseDuration =  Micobject.settings.analyseduration,
                                          Timeout =  Micobject.settings.timeout
                                      };                    
                    break;
                case 4: //From Camera Feed
                    if (CameraControl != null)
                    {
                        if (CameraControl.Camera != null)
                        {
                            switch (CameraControl.Camobject.settings.sourceindex)
                            {
                                case 2://ffmpeg
                                    AudioSource = (FFMPEGStream)CameraControl.Camera.VideoSource;
                                    break;
                                case 5://vlc
                                    AudioSource = (VlcStream)CameraControl.Camera.VideoSource;
                                    break;
                                case 7://kinect
                                    AudioSource = (KinectStream)CameraControl.Camera.VideoSource;
                                    break;
                                case 8://kinect
                                    switch (CameraControl.Nv("custom"))
                                    {
                                        case "Network Kinect":
                                            AudioSource = (KinectNetworkStream)CameraControl.Camera.VideoSource;
                                            break;
                                        default:
                                            throw new Exception("No custom provider found for " +CameraControl.Nv("custom"));
                                    }
                                    break;
                                default:
                                    AudioSource = null;
                                    break;
                            }
                        }
                        else
                            AudioSource = null;
                    }
                    else
                        AudioSource = null;
                    break;
            }

            if (AudioSource == null)
            {
                IsEnabled = false;
                return;
            }

            WaveOut = !String.IsNullOrEmpty(Micobject.settings.deviceout) ? new DirectSoundOut(new Guid(Micobject.settings.deviceout), 100) : new DirectSoundOut(100);

            AudioSource.AudioFinished += AudioDeviceAudioFinished;
            AudioSource.AudioSourceError += AudioDeviceAudioSourceError;
            AudioSource.DataAvailable += AudioDeviceDataAvailable;
            AudioSource.LevelChanged += AudioDeviceLevelChanged;

            var l = new float[Micobject.settings.channels];
            for (int i = 0; i < l.Length; i++)
            {
                l[i] = 0.0f;
            }
            AudioDeviceLevelChanged(this, new LevelChangedEventArgs(l));

            //AudioSource.Volume = Micobject.settings.gain;

            if (!AudioSource.IsRunning)
            {
                lock (this)
                {
                    AudioSource.Start();
                }
            }

            AudioBuffer = new List<AudioAction>();
            SoundDetected = false;
            Alerted = false;
            NoSource = false;
            FlashCounter = 0;
            _recordingTime = 0;
            ReconnectCount = 0;
            Listening = false;
            _soundLastDetected = DateTime.MinValue;
            UpdateFloorplans(false);
            Micobject.settings.active = true;

            MainForm.NeedsSync = true;
            Invalidate();
            _processing = false;
        }

        void AudioDeviceLevelChanged(object sender, LevelChangedEventArgs eventArgs)
        {
            if (Math.Abs(eventArgs.MaxSamples.Max() - 0) < float.Epsilon)
                return;
            Levels = eventArgs.MaxSamples;
            if (Levels.Max() * 100 > Micobject.detector.sensitivity)
            {
                SoundDetected = true;
                InactiveRecord = 0;
                FlashCounter = 10;
                MicrophoneAlarm(sender,EventArgs.Empty);
            }         

        }

        void AudioDeviceDataAvailable(object sender, DataAvailableEventArgs e)
        {
            if (Levels == null)
                return;
            try
            {
                if (WriterBuffer == null)
                {
                    if (Micobject.settings.buffer > 0)
                    {
                        var dt = DateTime.Now.AddSeconds(0 - Micobject.settings.buffer);
                        AudioBuffer.RemoveAll(p => p.TimeStamp < dt);
                        AudioBuffer.Add(new AudioAction(e.RawData, Levels.Max(), DateTime.Now));
                    }
                }
                else
                {
                    WriterBuffer.Enqueue(new AudioAction(e.RawData, Levels.Max(), DateTime.Now));
                }


                if (Micobject.settings.needsupdate)
                {
                    Micobject.settings.samples = AudioSource.RecordingFormat.SampleRate;
                    Micobject.settings.channels = AudioSource.RecordingFormat.Channels;
                    Micobject.settings.needsupdate = false;
                }

                OutSockets.RemoveAll(p => p.Connected == false);
                if (OutSockets.Count>0)
                {
                    if (_mp3Writer == null)
                    {
                        _audioStreamFormat = new WaveFormat(22050, 16, Micobject.settings.channels);
                        var wf = new MP3Stream.WaveFormat(_audioStreamFormat.SampleRate, _audioStreamFormat.BitsPerSample, _audioStreamFormat.Channels);
                        _mp3Writer = new Mp3Writer(_outStream, wf, false);
                    }

                    byte[] bSrc = e.RawData;
                    int totBytes = bSrc.Length;

                    var ws = new TalkHelperStream(bSrc, totBytes, AudioSource.RecordingFormat);
                    var helpStm = new WaveFormatConversionStream(_audioStreamFormat, ws);
                    totBytes = helpStm.Read(_bResampled, 0, 25000);

                    ws.Close();
                    ws.Dispose();
                    helpStm.Close();
                    helpStm.Dispose();

                    _mp3Writer.Write(_bResampled, 0, totBytes);


                    if (_outStream.Length > 0)
                    {
                        var bout = new byte[(int) _outStream.Length];

                        _outStream.Seek(0, SeekOrigin.Begin);
                        _outStream.Read(bout, 0, (int) _outStream.Length);

                        _outStream.SetLength(0);
                        _outStream.Seek(0, SeekOrigin.Begin);

                        foreach (Socket s in OutSockets)
                        {
                            s.Send(Encoding.ASCII.GetBytes(bout.Length.ToString("X") + "\r\n"));
                            s.Send(bout);
                            s.Send(Encoding.ASCII.GetBytes("\r\n"));
                        }
                    }

                }
                else
                {
                    if (_mp3Writer != null)
                    {
                        _mp3Writer.Close();
                        _mp3Writer = null;
                    }
                }


                if (DataAvailable != null)
                {
                    DataAvailable(this, new NewDataAvailableArgs((byte[])e.RawData.Clone()));
                }

            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
        }

        void AudioDeviceAudioSourceError(object sender, AudioSourceErrorEventArgs eventArgs)
        {
            if (eventArgs.Description=="not connected")
            {
                Micobject.settings.active = false;
                NoSource = true;
                _processing = false;
            }
            if (!AudioSourceErrorState)
            {
                AudioSourceErrorState = true;
                MainForm.LogExceptionToFile(new Exception("AudioSourceError: " + eventArgs.Description),
                                            "Mic " + Micobject.id);
                _reconnectTime = DateTime.Now;
                if (_errorTime == DateTime.MinValue)
                    _errorTime = DateTime.Now;
            }
            if (!ShuttingDown)
                Invalidate();

        }

        void AudioDeviceAudioFinished(object sender, ReasonToFinishPlaying reason)
        {
            if ((CameraControl!=null && CameraControl.IsReconnect) || _isReconnect)
                return;
            switch (reason)
            {
                case ReasonToFinishPlaying.DeviceLost:
                case ReasonToFinishPlaying.EndOfStreamReached:
                case ReasonToFinishPlaying.VideoSourceError:
                    if (!AudioSourceErrorState)
                    {
                        AudioSourceErrorState = true;
                        MainForm.LogExceptionToFile(new Exception("AudioSourceFinished: " + reason), "Mic " + Micobject.id);
                        _reconnectTime = DateTime.Now;
                        if (_errorTime == DateTime.MinValue)
                            _errorTime = DateTime.Now;
                    }
                    break;
                case ReasonToFinishPlaying.StoppedByUser:
                    SetDisabled(false);
                    break;
            }
            if (!ShuttingDown)
                Invalidate();
        }

        private void UpdateFloorplans(bool isAlert)
        {
            foreach (
                var ofp in
                    MainForm.FloorPlans.Where(
                        p => p.objects.@object.Count(q => q.type == "microphone" && q.id == Micobject.id) > 0).
                        ToList())
            {
                ofp.needsupdate = true;
                if (isAlert)
                {
                    if (TopLevelControl != null)
                    {
                        FloorPlanControl fpc = ((MainForm) TopLevelControl).GetFloorPlan(ofp.id);
                        fpc.LastAlertTimestamp = DateTime.Now.UnixTicks();
                        fpc.LastOid = Micobject.id;
                        fpc.LastOtid = 1;
                    }
                }
            }
        }

        #region Nested type: AudioAction

        public struct AudioAction
        {
            public byte[] Decoded;
            public readonly double SoundLevel;
            public readonly DateTime TimeStamp;

            public AudioAction(byte[] decoded, double soundLevel, DateTime timestamp)
            {
                Decoded = decoded;
                SoundLevel = soundLevel;
                TimeStamp = timestamp;
            }
        }

        #endregion

        public void MicrophoneAlarm(object sender, EventArgs e)
        {
            _soundLastDetected = DateTime.Now;
            if (sender is IAudioSource)
            {
                FlashCounter = 10;
                SoundDetected = true;
                _isTrigger = false;
                return;
            }

            if (sender is LocalServer || sender is VolumeLevel || sender is CameraWindow)
            {
                FlashCounter = 10;
                _isTrigger = true;
            }
        }

        private void DoAlert()
        {
            if (IsEdit)
                return;

            Alerted = true;
            UpdateFloorplans(true);

            var t = new Thread(AlertThread) { Name = "Alert (" + Micobject.id + ")", IsBackground = false };
            t.Start();
        }

        private void AlertThread()
        {
            if (!String.IsNullOrEmpty(Micobject.alerts.trigger) && TopLevelControl != null)
            {
                string[] tid = Micobject.alerts.trigger.Split(',');
                switch (tid[0])
                {
                    case "1":
                        VolumeLevel vl = ((MainForm)TopLevelControl).GetVolumeLevel(Convert.ToInt32(tid[1]));
                        if (vl != null)
                            vl.MicrophoneAlarm(this,EventArgs.Empty);
                        break;
                    case "2":
                        CameraWindow cw = ((MainForm)TopLevelControl).GetCameraWindow(Convert.ToInt32(tid[1]));
                        if (cw != null)
                            cw.CameraAlarm(this,EventArgs.Empty);
                        break;
                }
            }

            if (Notification != null)
                Notification(this, new NotificationType("ALERT_UC", Micobject.name,""));

            if (Micobject.alerts.executefile.Trim() != "")
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        FileName = Micobject.alerts.executefile,
                        Arguments = Micobject.alerts.arguments
                    };
                    try
                    {
                        var fi = new FileInfo(Micobject.alerts.executefile);
                        if (fi.DirectoryName!=null)
                            startInfo.WorkingDirectory = fi.DirectoryName;
                    }
                    catch { }
                    if (!MainForm.Conf.CreateAlertWindows)
                    {
                        startInfo.CreateNoWindow = true;
                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    }
                    Process.Start(startInfo);
                }
                catch (Exception e)
                {
                    MainForm.LogExceptionToFile(e);
                }
            }

            if (MainForm.Conf.ScreensaverWakeup)
                ScreenSaver.KillScreenSaver();

            string[] alertOptions = Micobject.alerts.alertoptions.Split(','); //beep,restore
            if (Convert.ToBoolean(alertOptions[0]))
                Console.Beep();
            if (Convert.ToBoolean(alertOptions[1]))
                RemoteCommand(this, new ThreadSafeCommand("show"));


            if (MainForm.Conf.ServicesEnabled && MainForm.Conf.Subscribed)
            {
                if (Micobject.notifications.sendemail)
                {
                    string subject = LocRm.GetString("MicrophoneAlertMailSubject").Replace("[OBJECTNAME]",
                                                                                            Micobject.name);
                    string message = LocRm.GetString("MicrophoneAlertMailBody");
                    message = message.Replace("[OBJECTNAME]", Micobject.name);

                    string body = "";
                    switch (Micobject.alerts.mode)
                    {
                        case "sound":
                            body = LocRm.GetString("MicrophoneAlertBodySound").Replace("[TIME]",
                                                                                        DateTime.Now.ToLongTimeString());

                            if (Recording)
                            {
                                body += " " + LocRm.GetString("AudioCaptured");
                            }
                            else
                                body += " " + LocRm.GetString("AudioNotCaptured");
                            break;
                        case "nosound":

                            int minutes = Convert.ToInt32(Micobject.detector.nosoundinterval / 60);
                            int seconds = (Micobject.detector.nosoundinterval % 60);

                            body =
                                LocRm.GetString("MicrophoneAlertBodyNoSound").Replace("[TIME]",
                                                                                      DateTime.Now.ToLongTimeString()).
                                    Replace("[MINUTES]", minutes.ToString(CultureInfo.InvariantCulture)).Replace("[SECONDS]", seconds.ToString(CultureInfo.InvariantCulture));
                            break;
                    }

                    message = message.Replace("[BODY]", body + "<br/>" + MainForm.Conf.AppendLinkText);


                    if (MainForm.Conf.ServicesEnabled && MainForm.Conf.Subscribed)
                    {
                        WsWrapper.SendAlert(Micobject.settings.emailaddress, subject, message);
                    }

                }

                if (Micobject.notifications.sendsms || Micobject.notifications.sendtwitter)
                {
                    string message = LocRm.GetString("SMSAudioAlert").Replace("[OBJECTNAME]", Micobject.name) + " ";
                    switch (Micobject.alerts.mode)
                    {
                        case "sound":
                            message += LocRm.GetString("SMSAudioDetected");
                            message = message.Replace("[RECORDED]", Recording ? LocRm.GetString("AudioCaptured") : LocRm.GetString("AudioNotCaptured"));
                            break;
                        case "nosound":
                            int minutes = Convert.ToInt32(Micobject.detector.nosoundinterval/60);
                            int seconds = (Micobject.detector.nosoundinterval%60);

                            message +=
                                LocRm.GetString("SMSNoAudioDetected").Replace("[MINUTES]", minutes.ToString(CultureInfo.InvariantCulture)).Replace(
                                    "[SECONDS]", seconds.ToString(CultureInfo.InvariantCulture));
                            break;
                    }

                    if (Micobject.notifications.sendsms)
                    {
                        WsWrapper.SendSms(Micobject.settings.smsnumber, message);
                    }

                    if (Micobject.notifications.sendtwitter)
                    {
                        WsWrapper.SendTweet(message + " "+MainForm.Webserver+"/mobile/");
                    }
                }
            }
        }

        public string RecordSwitch(bool record)
        {  
            if (record)
            {
                if (!Micobject.settings.active)
                {
                    Enable();
                }
                ForcedRecording = true;
                return "recording," + LocRm.GetString("RecordingStarted");
            }

            ForcedRecording = false;
            StopSaving();

            var cw = CameraControl;
            if (cw != null && CameraControl.Camobject.settings.active)
            {
                cw.RecordSwitch(false);
            }
           
            return "notrecording," + LocRm.GetString("RecordingStopped");
        }

        public void ApplySchedule()
        {
            if (!Micobject.schedule.active || Micobject.schedule == null || Micobject.schedule.entries == null ||
                !Micobject.schedule.entries.Any())
                return;
            //find most recent schedule entry
            DateTime dNow = DateTime.Now;
            TimeSpan shortest = TimeSpan.MaxValue;
            objectsMicrophoneScheduleEntry mostrecent = null;
            bool isstart = true;

            foreach (objectsMicrophoneScheduleEntry entry in Micobject.schedule.entries)
            {
                if (entry.active)
                {
                    string[] dows = entry.daysofweek.Split(',');
                    foreach (string dayofweek in dows)
                    {
                        int dow = Convert.ToInt32(dayofweek);
                        //when did this last fire?
                        if (entry.start.IndexOf("-", StringComparison.Ordinal) == -1)
                        {
                            string[] start = entry.start.Split(':');
                            var dtstart = new DateTime(dNow.Year, dNow.Month, dNow.Day, Convert.ToInt32(start[0]),
                                                       Convert.ToInt32(start[1]), 0);
                            while ((int) dtstart.DayOfWeek != dow || dtstart > dNow)
                                dtstart = dtstart.AddDays(-1);
                            if (dNow - dtstart < shortest)
                            {
                                shortest = dNow - dtstart;
                                mostrecent = entry;
                                isstart = true;
                            }
                        }
                        if (entry.stop.IndexOf("-", StringComparison.Ordinal) == -1)
                        {
                            string[] stop = entry.stop.Split(':');
                            var dtstop = new DateTime(dNow.Year, dNow.Month, dNow.Day, Convert.ToInt32(stop[0]),
                                                      Convert.ToInt32(stop[1]), 0);
                            while ((int) dtstop.DayOfWeek != dow || dtstop > dNow)
                                dtstop = dtstop.AddDays(-1);
                            if (dNow - dtstop < shortest)
                            {
                                shortest = dNow - dtstop;
                                mostrecent = entry;
                                isstart = false;
                            }
                        }
                    }
                }
            }
            if (mostrecent != null)
            {
                if (isstart)
                {
                    Micobject.detector.recordondetect = mostrecent.recordondetect;
                    Micobject.detector.recordonalert = mostrecent.recordonalert;
                    Micobject.alerts.active = mostrecent.alerts;
                    if (!Micobject.settings.active)
                        Enable();
                    if (mostrecent.recordonstart)
                    {
                        ForcedRecording = true;
                    }
                }
                else
                {
                    if (Micobject.settings.active)
                        Disable();
                }
            }
        }

        #region Nested type: ThreadSafeCommand

        public class ThreadSafeCommand : EventArgs
        {
            public string Command;
            // Constructor
            public ThreadSafeCommand(string command)
            {
                Command = command;
            }
        }

        #endregion

        #region Nested type: VolumeChangedEventArgs

        public class VolumeChangedEventArgs : EventArgs
        {
            public int NewLevel;

            public VolumeChangedEventArgs(int newLevel)
            {
                NewLevel = newLevel;
            }
        }

        #endregion
    }

    public class NewDataAvailableArgs : EventArgs
    {
        private readonly byte[] _decodedData;

        public NewDataAvailableArgs(byte[] decodedData)
        {
            _decodedData = decodedData;
        }

        public byte[] DecodedData
        {
            get { return _decodedData; }
        }
    }
}