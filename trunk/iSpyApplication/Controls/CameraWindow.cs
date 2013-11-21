using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Video.DirectShow.Internals;
using iSpy.Video.FFMPEG;
using AForge.Video.Ximea;
using AForge.Vision.Motion;
using iSpyApplication.Video;
using xiApi.NET;
using Encoder = System.Drawing.Imaging.Encoder;
using PictureBox = AForge.Controls.PictureBox;

namespace iSpyApplication.Controls
{
    /// <summary>
    /// Summary description for CameraWindow.
    /// </summary>
    public sealed class CameraWindow : PictureBox
    {
        #region Private
        internal DateTime LastAutoTrackSent = DateTime.MinValue;
        private Color _customColor = Color.Black;
        private DateTime _lastRedraw = DateTime.MinValue;
        private double _recordingTime;
        private readonly ManualResetEvent _stopWrite = new ManualResetEvent(false);

        private double _timeLapse;
        private double _timeLapseFrames;
        private double _timeLapseTotal;
        private double _timeLapseFrameCount;
        private double _secondCount;
        private Point _mouseLoc;
        private List<FrameAction> _videoBuffer = new List<FrameAction>();
        private QueueWithEvents<FrameAction> _writerBuffer;
        private DateTime _errorTime = DateTime.MinValue;
        private DateTime _reconnectTime = DateTime.MinValue;
        private bool _firstFrame = true;
        private readonly AutoResetEvent _newRecordingFrame = new AutoResetEvent(false);
        private Thread _recordingThread;
        private bool _isTrigger;
        private int _calibrateTarget;
        private Camera _camera;
        private double _intervalCount;
        private DateTime _lastFrameUploaded = DateTime.Now;
        private DateTime _lastScheduleCheck = DateTime.MinValue;
        private DateTime _dtPTZLastCheck = DateTime.Now;
        private long _lastRun = DateTime.Now.Ticks;
        private DateTime _movementLastDetected = DateTime.Now;
        private DateTime _lastAlertCheck = DateTime.MinValue;
        private DateTime _mouseMove = DateTime.MinValue;
        private List<FilesFile> _filelist = new List<FilesFile>();
        private VideoFileWriter _timeLapseWriter;
        private readonly ToolTip _toolTipCam;
        private int _ttind = -1;
        public volatile bool IsReconnect;
        private bool _suspendPTZSchedule;
        private bool _requestRefresh;
        private readonly StringBuilder _motionData = new StringBuilder(100000);
        private const int ButtonOffset = 4, ButtonCount = 6;
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

        private string _pluginMessage = "";
        private Bitmap _lastFrame;
        private readonly object _lockobject = new object();
        #endregion

        internal bool Ptzneedsstop;
        internal bool IsClone;
        private VideoFileWriter _writer;
        internal bool LoadedFiles;
        #region Public

        #region Delegates

        public delegate void NotificationEventHandler(object sender, NotificationType e);
        public delegate void SwitchDelegate();
        public delegate void FileListUpdatedEventHandler(object sender);
        public delegate void RemoteCommandEventHandler(object sender, ThreadSafeCommand e);

        #endregion

        #region Events
        public event RemoteCommandEventHandler RemoteCommand;
        public event NotificationEventHandler Notification;
        public event FileListUpdatedEventHandler FileListUpdated;
        #endregion

        private event EventHandler CloneCameraReconnect, CloneCameraDisabled, CloneCameraEnabled, CloneCameraReConnected;
        public bool Talking;
        public bool ForcedRecording;
        public bool NeedMotionZones = true;
        public XimeaVideoSource XimeaSource;
        public bool Alerted;
        public double MovementCount;
        public double CalibrateCount, ReconnectCount;
        public Rectangle RestoreRect = Rectangle.Empty;
        private bool _calibrating;
        public bool Calibrating
        {
            get { return _calibrating; }
            set
            {
                _calibrating = value;
                if (value)
                {
                    CalibrateCount = 0;
                    _calibrateTarget = Camobject.detector.calibrationdelay;
                }
            }
        }
        public Graphics CurrentFrame;
        public PTZController PTZ;
        public int FlashCounter;
        public double InactiveRecord;
        public bool IsEdit;
        public bool MovementDetected;
        public bool PTZNavigate;
        public Point PTZReference;
        public bool NeedSizeUpdate;
        public bool ResizeParent;
        public bool ShuttingDown;
        public string TimeLapseVideoFileName = "";
        public string VideoFileName = "";
        public string VideoSourceErrorMessage = "";
        public bool VideoSourceErrorState;
        public DateTime TimelapseStart = DateTime.MinValue;
        public objectsCamera Camobject;
        public volatile bool IsEnabled;
        internal Color BackgroundColor = MainForm.BackgroundColor;
        public bool Seekable;

        public VolumeLevel VolumeControl
        {
            get
            {
                if (Camobject != null && Camobject.settings.micpair > -1)
                {
                    if (TopLevelControl != null)
                    {
                        VolumeLevel vl = ((MainForm)TopLevelControl).GetVolumeLevel(Camobject.settings.micpair);
                        if (vl != null && vl.Micobject != null)
                            return vl;
                    }
                }
                return null;
            }
        }
        public bool Recording
        {
            get
            {
                return _recordingThread != null && _recordingThread.IsAlive;
            }
        }
        public bool SavingTimeLapse
        {
            get { return _timeLapseWriter != null; }
        }


        private string CodecExtension
        {
            get
            {
                switch (Camobject.recorder.profile)
                {
                    default:
                        return ".mp4";
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                        return ".avi";
                }
            }
        }

        private int CodecFramerate
        {
            get
            {
                switch (Camobject.recorder.profile)
                {
                    default:
                        return 0;
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                        int i = Convert.ToInt32(Camera.Framerate);
                        if (i == 0)
                            return 1;
                        return i;
                }
            }
        }

        private VideoCodec Codec
        {
            get
            {
                switch (Camobject.recorder.profile)
                {
                    default:
                        return VideoCodec.H264;
                    case 3:
                        return VideoCodec.WMV1;
                    case 4:
                        return VideoCodec.WMV2;
                    case 5:
                        return VideoCodec.MPEG4;
                    case 6:
                        return VideoCodec.MSMPEG4v3;
                    case 7:
                        return VideoCodec.Raw;
                    case 8:
                        return VideoCodec.MJPEG;
                }
            }
        }

        private AudioCodec CodecAudio
        {
            get
            {
                switch (Camobject.recorder.profile)
                {
                    default:
                        return AudioCodec.AAC;
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                        return AudioCodec.MP3;
                }
            }
        }


        private void GenerateFileList()
        {
            string dir = MainForm.Conf.MediaDirectory + "video\\" +
                         Camobject.directory + "\\";

            if (!Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    _filelist = new List<FilesFile>();
                    return;
                }
            }

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
                var dirinfo = new DirectoryInfo(MainForm.Conf.MediaDirectory + "video\\" +
                                                Camobject.directory + "\\");

                var lFi = new List<FileInfo>();
                lFi.AddRange(dirinfo.GetFiles());
                lFi = lFi.FindAll(f => f.Extension.ToLower() == ".avi" || f.Extension.ToLower() == ".mp4");
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
                            IsTimelapse =
                                fi.Name.ToLower().IndexOf("timelapse", StringComparison.Ordinal) !=
                                -1
                        });
                    }
                }

                for (int index = 0; index < _filelist.Count; index++)
                {
                    FilesFile ff = _filelist[index];
                    if (ff != null && lFi.All(p => p.Name != ff.Filename))
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

        public void ClearFileList()
        {
            lock (_filelist)
            {
                _filelist.Clear();
            }
            MainForm.MasterFileRemoveAll(2, Camobject.id);
        }

        public void RemoveFile(string filename)
        {
            lock (_filelist)
            {
                _filelist.RemoveAll(p => p.Filename == filename);
            }
            MainForm.MasterFileRemove(filename);
        }

        public void SaveFileList()
        {
            try
            {
                lock (_filelist)
                {
                    var fl = new Files { File = _filelist.ToArray() };
                    string dir = MainForm.Conf.MediaDirectory + "video\\" +
                                 Camobject.directory + "\\";
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    var s = new XmlSerializer(typeof (Files));
                    using (var fs = new FileStream(dir + "data.xml", FileMode.Create))
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
                var dir = MainForm.Conf.MediaDirectory + "video\\" +
                                                  Camobject.directory + "\\";
                var dirinfo = new DirectoryInfo(dir);

                var lFi = new List<FileInfo>();
                lFi.AddRange(dirinfo.GetFiles());
                lFi = lFi.FindAll(f => f.Extension.ToLower() == ".avi" || f.Extension.ToLower() == ".mp4");
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
        #endregion

        #region SizingControls

        public void UpdatePosition()
        {
            Monitor.Enter(this);

            if (Parent != null && Camera != null)
            {
                if (!LastFrameNull)
                {
                    int width = Camera.Width;
                    int height = Camera.Height;
                    Camobject.resolution = width + "x" + height;
                    SuspendLayout();

                    //resize to max 640xh
                    if (width > 640)
                    {
                        double d = width / 640d;
                        width = 640;
                        height = Convert.ToInt32(Convert.ToDouble(height) / d);
                    }
                    Camobject.width = width;
                    Camobject.height = height;
                    Size = new Size(width + 2, height + 26);
                    ResumeLayout();
                    NeedSizeUpdate = false;
                }
            }
            Monitor.Exit(this);
        }
        #endregion



        public CameraWindow(objectsCamera cam)
        {
            InitializeComponent();
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint, true);
            Margin = new Padding(0, 0, 0, 0);
            Padding = new Padding(0, 0, 5, 5);
            BorderStyle = BorderStyle.None;
            BackgroundColor = MainForm.BackgroundColor;
            Camobject = cam;
            PTZ = new PTZController(this);

            _toolTipCam = new ToolTip { AutomaticDelay = 500, AutoPopDelay = 1500 };
        }

        private Thread _tFiles;
        public void GetFiles()
        {
            if (_tFiles == null || !_tFiles.IsAlive)
            {
                _tFiles = new Thread(GenerateFileList);
                _tFiles.Start();
            }
        }

        public Camera Camera
        {
            get { return _camera; }
            set
            {
                lock (_lockobject)
                {
                    if (_camera != null)
                    {
                        _camera.Dispose();
                    }

                    _camera = value;
                    if (_camera != null)
                    {
                        _camera.CW = this;

                        _videoBuffer.Clear();
                        if (value == null)
                        {
                            _videoBuffer = null;
                        }

                    }
                }
            }
        }

        public string[] ScheduleDetails
        {
            get
            {
                var entries = new List<string>();
                foreach (objectsCameraScheduleEntry sched in Camobject.schedule.entries)
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
                    if (sched.timelapseenabled)
                        s += " " + LocRm.GetString("TIMELAPSE_UC");
                    if (!sched.active)
                        s += " (" + LocRm.GetString("INACTIVE_UC") + ")";

                    entries.Add(s);
                }
                return entries.ToArray();
            }
        }

        #region MouseEvents

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

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                case Keys.Up:
                case Keys.Left:
                case Keys.Right:
                    e.IsInputKey = true;
                    break;
            }
            base.OnPreviewKeyDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Camera == null)
                return;
            Rectangle r = Camera.ViewRectangle;
            if (r != Rectangle.Empty)
            {
                switch (e.KeyCode)
                {
                    case Keys.Add:
                    case Keys.Subtract:
                        Camera.ZPoint = new Point(r.Left + r.Width / 2, r.Top + r.Height / 2);
                        if (e.KeyCode == Keys.Add)
                        {
                            Camera.ZFactor += 0.2f;
                        }
                        if (e.KeyCode == Keys.Subtract)
                        {
                            Camera.ZFactor -= 0.2f;
                        }
                        if (Camera.ZFactor < 1)
                            Camera.ZFactor = 1;
                        break;
                    case Keys.Left:
                        Camera.ZPoint.X -= 10;
                        break;
                    case Keys.Right:
                        Camera.ZPoint.X += 10;
                        break;
                    case Keys.Up:
                        Camera.ZPoint.Y -= Convert.ToInt32(10);
                        break;
                    case Keys.Down:
                        Camera.ZPoint.Y += 10;
                        break;
                }
            }


            base.OnKeyDown(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if ((e.Location.X >= 0) && (e.Location.X <= Size.Width) &&
            (e.Location.Y >= 0) && (e.Location.Y <= Size.Height))
            {
                base.OnMouseWheel(e);
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {

            base.OnMouseUp(e);

            switch (e.Button)
            {
                case MouseButtons.Left:
                    MousePos mousePos = GetMousePos(e.Location);

                    if (mousePos == MousePos.NoWhere)
                    {
                        if (MainForm.Conf.ShowOverlayControls)
                        {
                            if (Seekable && _seeking && Camera != null && Camera.VideoSource != null)
                            {
                                //seek video bar
                                var pc = (float)(Convert.ToDouble(_newSeek) / Convert.ToDouble(ButtonPanelWidth));
                                var vlc = Camera.VideoSource as VlcStream;
                                if (vlc != null)
                                {
                                    vlc.Seek(pc);
                                }
                                else
                                {
                                    var ffmpeg = Camera.VideoSource as FFMPEGStream;
                                    if (ffmpeg != null)
                                    {
                                        ffmpeg.Seek(pc);
                                    }
                                }
                            }
                            _seeking = false;
                            _newSeek = 0;
                        }
                    }
                    break;
                case MouseButtons.Middle:
                    PTZNavigate = false;
                    PTZSettings2Camera ptz = MainForm.PTZs.SingleOrDefault(p => p.id == Camobject.ptz);
                    if (ptz != null && !String.IsNullOrEmpty(ptz.Commands.Stop))
                        PTZ.SendPTZCommand(ptz.Commands.Stop, true);

                    if (PTZ.IsContinuous)
                        PTZ.SendPTZCommand(Enums.PtzCommand.Stop);
                    break;
            }

        }

        private float _newSeek;
        private bool _seeking;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _seeking = false;
            base.OnMouseDown(e);
            Select();
            IntPtr hwnd = Handle;
            if (ResizeParent && Parent != null && Parent.IsHandleCreated)
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
                        int leftpoint = Width / 2 - ButtonPanelWidth / 2;
                        int ypoint = Height - 24 - ButtonPanelHeight;
                        if (Seekable && Camera != null && Camera.VideoSource != null)
                        {
                            if (e.Location.X > leftpoint && e.Location.X < leftpoint + ButtonPanelWidth &&
                                e.Location.Y > ypoint - 20 && e.Location.Y < ypoint + 4)
                            {
                                _seeking = true;
                                return;
                            }
                        }
                        if (e.Location.X > leftpoint && e.Location.X < leftpoint + ButtonPanelWidth &&
                            e.Location.Y > ypoint && e.Location.Y < ypoint + ButtonPanelHeight)
                        {
                            int x = e.Location.X - leftpoint;
                            if (x < ButtonWidth + ButtonOffset)
                            {
                                //power
                                if (Camobject.settings.active)
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
                                if (x < (ButtonWidth + ButtonOffset) * 2)
                                {
                                    //record
                                    if (Camobject.settings.active)
                                    {
                                        RecordSwitch(!Recording);
                                    }
                                }
                                else
                                {
                                    if (TopLevelControl != null)
                                    {
                                        if (x < (ButtonWidth + ButtonOffset) * 3)
                                            ((MainForm)TopLevelControl).EditCamera(Camobject);
                                        else
                                        {
                                            if (x < (ButtonWidth + ButtonOffset) * 4)
                                            {
                                                string url = MainForm.Webserver + "/watch_new.aspx";
                                                if (WsWrapper.WebsiteLive && MainForm.Conf.ServicesEnabled)
                                                {
                                                    MainForm.OpenUrl(url);
                                                }
                                                else
                                                    ((MainForm)TopLevelControl).Connect(url, false);
                                            }
                                            else
                                            {
                                                if (x < (ButtonWidth + ButtonOffset) * 5)
                                                {
                                                    if (Camobject.settings.active)
                                                    {
                                                        string fn = SaveFrame();
                                                        if (fn != "")
                                                            MainForm.OpenUrl(fn);
                                                    }
                                                }
                                                else
                                                {
                                                    if (Camobject.settings.audiomodel == "None")
                                                    {
                                                        MessageBox.Show(this, "You need to configure talk settings for this camera first");
                                                        ((MainForm)TopLevelControl).EditCamera(Camobject);
                                                    }
                                                    else
                                                    {
                                                        Talking = !Talking;
                                                        ((MainForm)TopLevelControl).TalkTo(this, Talking);
                                                    }

                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (MainForm.Conf.LockLayout) return;
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
                }
            }
        }

        protected override void OnLostFocus(EventArgs e)
        {
            _requestRefresh = true;
            base.OnLostFocus(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            if (TopLevelControl != null) ((MainForm)TopLevelControl).PTZToolUpdate(this);
            _requestRefresh = true;
            base.OnGotFocus(e);
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
                    if (_toolTipCam.Active)
                    {
                        _toolTipCam.Hide(this);
                        _ttind = -1;
                    }
                    if (e.Location.X < 30 && e.Location.Y > Height - 24)
                    {
                        string m = "";
                        if (Camobject.alerts.active)
                            m = "Alerts Active";

                        if (ForcedRecording)
                            m = "Forced Recording, " + m;

                        if (Camobject.detector.recordondetect)
                            m = "Record on Detect, " + m;
                        else
                        {
                            if (Camobject.detector.recordonalert)
                                m = "Record on Alert, " + m;
                            else
                            {
                                m = "No Recording, " + m;
                            }
                        }
                        m = m.Trim().Trim(',');
                        var toolTipLocation = new Point(5, Height - 24);
                        _toolTipCam.Show(m, this, toolTipLocation, 1000);
                    }
                    if (MainForm.Conf.ShowOverlayControls)
                    {
                        int leftpoint = Width / 2 - ButtonPanelWidth / 2;
                        int ypoint = Height - 24 - ButtonPanelHeight;
                        if (_seeking && Seekable && Camera != null && Camera.VideoSource != null)
                        {
                            _newSeek = e.Location.X - leftpoint;
                            if (_newSeek < 0.0001) _newSeek = 0.0001f;
                            if (_newSeek > ButtonPanelWidth)
                                _newSeek = ButtonPanelWidth;
                            return;
                        }

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
                                    _toolTipCam.Show(
                                        Camobject.settings.active
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
                                        _toolTipCam.Show(LocRm.GetString("RecordNow"), this, toolTipLocation, 1000);
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
                                                _toolTipCam.Show(LocRm.GetString("Edit"), this, toolTipLocation, 1000);
                                                _ttind = 2;
                                            }
                                        }
                                        else
                                        {
                                            if (x < (ButtonWidth + ButtonOffset) * 4)
                                            {
                                                if (_ttind != 3)
                                                {
                                                    _toolTipCam.Show(LocRm.GetString("MediaoverTheWeb"), this, toolTipLocation, 1000);
                                                    _ttind = 3;
                                                }
                                            }
                                            else
                                            {
                                                if (x < (ButtonWidth + ButtonOffset) * 5)
                                                {
                                                    if (_ttind != 4)
                                                    {
                                                        _toolTipCam.Show(LocRm.GetString("TakePhoto"), this,
                                                                        toolTipLocation, 1000);
                                                        _ttind = 4;
                                                    }
                                                }
                                                else
                                                {
                                                    if (_ttind != 5)
                                                    {
                                                        _toolTipCam.Show("Talk", this,
                                                                        toolTipLocation, 1000);
                                                        _ttind = 5;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                    break;
            }

            base.OnMouseMove(e);

            _requestRefresh = true;

        }

        protected override void OnResize(EventArgs eventargs)
        {

            if ((ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                if (Camera != null && !LastFrameNull)
                {
                    double arW = Convert.ToDouble(Camera.Width) / Convert.ToDouble(Camera.Height);
                    Width = Convert.ToInt32(arW * Height);
                }
            }
            base.OnResize(eventargs);

            if (Width < MinimumSize.Width) Width = MinimumSize.Width;
            if (Height < MinimumSize.Height) Height = MinimumSize.Height;
            if (VolumeControl != null)
                MainForm.NeedsRedraw = true;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Cursor = Cursors.Default;
            _mouseMove = DateTime.MinValue;
            _requestRefresh = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Cursor = Cursors.Hand;
            _requestRefresh = true;
        }

        private enum MousePos
        {
            NoWhere,
            Right,
            Bottom,
            BottomRight
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Invalidate();
            }
            LocationChanged -= CameraWindow_LocationChanged;
            Resize-=CameraWindow_Resize;

            _toolTipCam.RemoveAll();
            _toolTipCam.Dispose();
            _timeLapseWriter = null;
            _writer = null;
            base.Dispose(disposing);
        }

        public void Tick()
        {
            //reset custom border color
            if (Camobject.settings.bordertimeout != 0 && _customSet != DateTime.MinValue &&
                _customSet < DateTime.Now.AddSeconds(0 - Camobject.settings.bordertimeout))
            {
                Custom = false;
            }


            try
            {
                //time since last tick
                var ts = new TimeSpan(DateTime.Now.Ticks - _lastRun);
                _lastRun = DateTime.Now.Ticks;
                _secondCount += ts.TotalMilliseconds / 1000.0;

                if (Camobject.schedule.active)
                {
                    if (CheckSchedule()) goto skip;
                }

                if (!Camobject.settings.active) goto skip;

                if (FlashCounter <= 5)
                {
                    MovementDetected = false;
                    if (_suspendPTZSchedule)
                    {
                        _suspendPTZSchedule = false;
                        _dtPTZLastCheck = DateTime.Now;
                    }

                }
                if (FlashCounter > 5)
                {
                    if (Camobject.ftp.mode == 0)
                    {
                        if (Camobject.ftp.enabled && Camobject.ftp.ready)
                        {
                            //ftp frame on motion detection
                            FtpFrame();
                        }
                        if (Camobject.ftp.savelocal && Camera.MotionDetected)
                        {
                            SaveFrame();
                        }
                    }

                    InactiveRecord = 0;
                    if (Camobject.alerts.mode != "nomovement" && VolumeControl != null)
                        VolumeControl.InactiveRecord = 0;
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

                if (Camobject.alerts.active && Camobject.settings.active)
                {
                    reset = FlashBackground();
                }
                if (reset)
                    BackgroundColor = MainForm.BackgroundColor;

               
                if (_secondCount > 1 && Camobject.settings.active) //every second
                {

                    DoPTZTracking();

                    if (CheckReconnect()) goto skip;


                    if (Calibrating)
                    {
                        DoCalibrate();
                    }
                    
                    CheckReconnectInterval();

                    CheckDisconnect();

                    if (Recording && !MovementDetected && !ForcedRecording)
                    {
                        InactiveRecord += _secondCount;
                    }
                        
                    if (Camobject.ptzschedule.active && !_suspendPTZSchedule)
                    {
                        CheckPTZSchedule();
                    }
                    if (Camera != null && !LastFrameNull)
                    {
                        CheckVLCTimeStamp();

                        CheckFTP();

                        CheckRecord();

                        CheckTimeLapse();
                    }
                    
                }

                if (!Calibrating)
                {
                    CheckAlert();
                }

            skip:
                if (_secondCount > 1)
                    _secondCount = 0;
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex, "Camera " + Camobject.id);
            }
            if (_requestRefresh)
            {
                _requestRefresh = false;
                Invalidate();
            }
        }

        private bool FlashBackground()
        {
            bool reset = true;
            if (FlashCounter > 0 && _isTrigger)
            {
                BackgroundColor = (BackgroundColor == MainForm.ActivityColor)
                                      ? MainForm.BackgroundColor
                                      : MainForm.ActivityColor;
                reset = false;
            }
            else
            {
                switch (Camobject.alerts.mode.ToLower())
                {
                    case "movement":
                        if (MovementDetected)
                        {
                            BackgroundColor = (BackgroundColor == MainForm.ActivityColor)
                                                  ? MainForm.BackgroundColor
                                                  : MainForm.ActivityColor;
                            reset = false;
                        }

                        break;
                    case "nomovement":
                        if (!MovementDetected)
                        {
                            BackgroundColor = (BackgroundColor == MainForm.NoActivityColor)
                                                  ? MainForm.BackgroundColor
                                                  : MainForm.NoActivityColor;
                            reset = false;
                        }

                        break;
                    default:
                        if (FlashCounter > 0)
                        {
                            BackgroundColor = (BackgroundColor == MainForm.ActivityColor)
                                                  ? MainForm.BackgroundColor
                                                  : MainForm.ActivityColor;
                            reset = false;
                        }

                        break;
                }
            }
            return reset;
        }

        private void CheckVLCTimeStamp()
        {
            if (Camobject.settings.sourceindex == 5)
            {
                var vlc = Camera.VideoSource as VlcStream;
                if (vlc != null)
                {
                    vlc.CheckTimestamp();
                }
            }
        }

        private void CheckRecord()
        {
            if (((Camobject.detector.recordondetect && MovementDetected && !Calibrating) || ForcedRecording) && !Recording)
            {
                StartSaving();
            }
            else
            {
                if (Recording)
                {
                    if (_recordingTime > Camobject.recorder.maxrecordtime ||
                        ((!MovementDetected && InactiveRecord > Camobject.recorder.inactiverecord) && !ForcedRecording))
                        StopSaving();
                }
            }
        }

        private void CheckTimeLapse()
        {
            if (Camobject.recorder.timelapseenabled)
            {
                bool err = false;
                if (Camobject.recorder.timelapse > 0)
                {
                    _timeLapseTotal += _secondCount;
                    _timeLapse += _secondCount;

                    if (_timeLapse >= Camobject.recorder.timelapse)
                    {
                        if (!SavingTimeLapse)
                        {
                            if (!OpenTimeLapseWriter())
                                return;
                        }

                        Bitmap bm = LastFrame;
                        try
                        {
                            var pts = (long)TimeSpan.FromSeconds(_timeLapseFrameCount *
                                                                  (1d / Camobject.recorder.timelapseframerate)).
                                                 TotalMilliseconds;
                            _timeLapseWriter.WriteVideoFrame(ResizeBitmap(bm), pts);
                            _timeLapseFrameCount++;
                        }
                        catch (Exception ex)
                        {
                            MainForm.LogExceptionToFile(ex);
                            err = true;
                        }
                        finally
                        {
                            bm.Dispose();
                        }
                        _timeLapse = 0;
                    }
                    if (_timeLapseTotal >= 60 * Camobject.recorder.timelapsesave || err)
                    {
                        CloseTimeLapseWriter();
                    }
                }
                if (err)
                {
                    _timeLapseFrames = 0;
                }
                else
                {
                    if (Camobject.recorder.timelapseframes > 0 && Camera != null)
                    {
                        _timeLapseFrames += _secondCount;
                        if (_timeLapseFrames >= Camobject.recorder.timelapseframes)
                        {
                            Image frame = LastFrame;
                            string dir = MainForm.Conf.MediaDirectory + "video\\" +
                                         Camobject.directory + "\\";
                            dir += @"timelapseframes\";

                            DateTime date = DateTime.Now;
                            string filename = String.Format("Frame_{0}-{1}-{2}_{3}-{4}-{5}.jpg",
                                                            date.Year, Helper.ZeroPad(date.Month),
                                                            Helper.ZeroPad(date.Day),
                                                            Helper.ZeroPad(date.Hour),
                                                            Helper.ZeroPad(date.Minute),
                                                            Helper.ZeroPad(date.Second));
                            if (!Directory.Exists(dir))
                                Directory.CreateDirectory(dir);
                            frame.Save(dir + filename, MainForm.Encoder, MainForm.EncoderParams);
                            frame.Dispose();
                            _timeLapseFrames = 0;
                        }
                    }
                }
            }
        }

        private void CheckAlert()
        {
            if (Camobject.settings.active && Camera != null && !LastFrameNull)
            {
                if (Alerted)
                {
                    _intervalCount += _secondCount;
                    if (_intervalCount > Camobject.alerts.minimuminterval)
                    {
                        Alerted = false;
                        _intervalCount = 0;
                        UpdateFloorplans(false);
                        _lastAlertCheck = DateTime.Now;
                    }
                }
                else
                {
                    //Check new Alert
                    if (Camobject.alerts.active && Camera != null)
                    {
                        switch (Camobject.alerts.mode)
                        {
                            case "movement":
                                if (_movementLastDetected > _lastAlertCheck)
                                {
                                    MovementCount += _secondCount;
                                    if (_isTrigger ||
                                        (MovementCount > Camobject.detector.movementintervalnew))
                                    {
                                        DoAlert();
                                        MovementCount = 0;
                                        
                                    }
                                    _lastAlertCheck = DateTime.Now;
                                }
                                else
                                    MovementCount = 0;

                                break;
                            case "objectcount":
                                if (Camera.MotionDetector != null)
                                {
                                    var blobalg = Camera.MotionDetector.MotionProcessingAlgorithm as BlobCountingObjectsProcessing;
                                        
                                    if (blobalg!=null)
                                    {
                                        if (blobalg.ObjectsCount >= Camobject.alerts.objectcountalert)
                                        {
                                            DoAlert();
                                            MovementCount = 0;
                                        }
                                    }
                                }
                                break;
                            case "nomovement":
                                if ((DateTime.Now - _movementLastDetected).TotalSeconds >
                                    Camobject.detector.nomovementintervalnew)
                                {
                                    DoAlert();
                                }
                                break;
                            default:
                                if (_pluginMessage != "")
                                {
                                    DoAlert(_pluginMessage);
                                    MovementCount = 0;
                                    //reset plugin message
                                    _pluginMessage = "";
                                }
                                break;
                        }
                    }
                }
            }
        }

        private void DoPTZTracking()
        {
            if (Camobject.settings.ptzautotrack && !Calibrating && Camobject.ptz != -1)
            {
                if (Ptzneedsstop && LastAutoTrackSent < DateTime.Now.AddMilliseconds(-1000))
                {
                    PTZ.SendPTZCommand(Enums.PtzCommand.Stop);
                    Ptzneedsstop = false;
                }
                if (Camobject.settings.ptzautohome && LastAutoTrackSent > DateTime.MinValue &&
                    LastAutoTrackSent < DateTime.Now.AddSeconds(0 - Camobject.settings.ptzautohomedelay))
                {
                    LastAutoTrackSent = DateTime.MinValue;
                    Calibrating = true;
                    _calibrateTarget = Camobject.settings.ptztimetohome;
                    if (String.IsNullOrEmpty(Camobject.settings.ptzautohomecommand) ||
                        Camobject.settings.ptzautohomecommand == "Center")
                        PTZ.SendPTZCommand(Enums.PtzCommand.Center);
                    else
                    {
                        PTZ.SendPTZCommand(Camobject.settings.ptzautohomecommand);
                    }
                }
            }
        }

        private void CheckFTP()
        {
            if (Camobject.ftp.mode == 2 && Camobject.ftp.interval != 0)
            {
                var tsFg = new TimeSpan(DateTime.Now.Ticks - _lastFrameUploaded.Ticks);
                if (Camobject.ftp.enabled && Camobject.ftp.ready)
                {
                    if (tsFg.TotalSeconds >= Camobject.ftp.interval)
                    {
                        FtpFrame();
                    }
                }
                if (Camobject.ftp.savelocal)
                {
                    if (tsFg.TotalSeconds >= Camobject.ftp.interval)
                    {
                        SaveFrame();
                    }
                }
            }
        }

        private void CheckPTZSchedule()
        {
            if (Camobject.ptz == -1)
                return;
            DateTime dtnow = DateTime.Now;
            foreach (
                var entry in Camobject.ptzschedule.entries)
            {
                if (entry != null && entry.time.TimeOfDay < dtnow.TimeOfDay &&
                    entry.time.TimeOfDay > _dtPTZLastCheck.TimeOfDay)
                {
                    PTZSettings2Camera ptz = MainForm.PTZs.Single(p => p.id == Camobject.ptz);
                    objectsCameraPtzscheduleEntry entry1 = entry;
                    if (ptz.ExtendedCommands != null && ptz.ExtendedCommands.Command != null)
                    {
                        var extcmd =
                            ptz.ExtendedCommands.Command.FirstOrDefault(p => p.Name == entry1.command);
                        if (extcmd != null)
                        {
                            Calibrating = true;
                            PTZ.SendPTZCommand(extcmd.Value);
                        }
                    }
                }
            }
            _dtPTZLastCheck = DateTime.Now;
        }

        private bool CheckSchedule()
        {
            DateTime dtnow = DateTime.Now;

            foreach (var entry in Camobject.schedule.entries.Where(p => p.active))
            {
                if (
                    entry.daysofweek.IndexOf(((int)dtnow.DayOfWeek).ToString(CultureInfo.InvariantCulture),
                                             StringComparison.Ordinal) == -1) continue;
                var stop = entry.stop.Split(':');
                if (stop[0] != "-")
                {
                    if (Convert.ToInt32(stop[0]) == dtnow.Hour)
                    {
                        if (Convert.ToInt32(stop[1]) == dtnow.Minute && dtnow.Second < 2)
                        {
                            Camobject.detector.recordondetect = entry.recordondetect;
                            Camobject.detector.recordonalert = entry.recordonalert;
                            Camobject.ftp.enabled = entry.ftpenabled;
                            Camobject.ftp.savelocal = entry.savelocalenabled;
                            Camobject.alerts.active = entry.alerts;

                            if (Camobject.settings.active)
                                Disable();
                            return true;
                        }
                    }
                }


                var start = entry.start.Split(':');
                if (start[0] != "-")
                {
                    if (Convert.ToInt32(start[0]) == dtnow.Hour)
                    {
                        if (Convert.ToInt32(start[1]) == dtnow.Minute && dtnow.Second < 3)
                        {
                            if ((dtnow - _lastScheduleCheck).TotalSeconds > 60) //only want to do this once per schedule
                            {
                                if (!Camobject.settings.active)
                                    Enable();

                                Camobject.detector.recordondetect = entry.recordondetect;
                                Camobject.detector.recordonalert = entry.recordonalert;
                                Camobject.ftp.enabled = entry.ftpenabled;
                                Camobject.ftp.savelocal = entry.savelocalenabled;
                                Camobject.alerts.active = entry.alerts;
                                if (Camobject.recorder.timelapseenabled && !entry.timelapseenabled)
                                {
                                    CloseTimeLapseWriter();
                                }
                                Camobject.recorder.timelapseenabled = entry.timelapseenabled;
                                if (entry.recordonstart)
                                {
                                    ForcedRecording = true;
                                }
                                _lastScheduleCheck = dtnow;
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void CheckDisconnect()
        {
            if (!String.IsNullOrEmpty(Camobject.settings.emailondisconnect) && _errorTime != DateTime.MinValue)
            {
                int sec = Convert.ToInt32((DateTime.Now - _errorTime).TotalSeconds);
                if (sec > MainForm.Conf.DisconnectNotificationDelay)
                {
                    //camera has been down for 30 seconds - send notification
                    string subject = LocRm.GetString("CameraNotifyDisconnectMailSubject").Replace("[OBJECTNAME]", Camobject.name);
                    string message = LocRm.GetString("CameraNotifyDisconnectMailBody");
                    message = message.Replace("[NAME]", Camobject.name);
                    message = message.Replace("[TIME]", DateTime.Now.AddHours(Convert.ToDouble(Camobject.settings.timestampoffset)).ToLongTimeString());

                    WsWrapper.SendContent(Camobject.settings.emailondisconnect, subject, message);

                    _errorTime = DateTime.MinValue;
                }
            }
        }

        private bool CheckReconnect()
        {
            if (_reconnectTime != DateTime.MinValue && !IsClone && !IsReconnect)
            {
                if (Camera != null && Camera.VideoSource != null)
                {
                    int sec = Convert.ToInt32((DateTime.Now - _reconnectTime).TotalSeconds);
                    if (sec > 10)
                    {
                        //try to reconnect every 10 seconds
                        if (!Camera.VideoSource.IsRunning)
                        {
                            Calibrating = true;
                            if (Camera.VideoSource != null)
                            {
                                Camera.Start();
                            }
                            
                            
                        }
                        _reconnectTime = DateTime.Now;
                        return true;
                    }
                }
            }
            return false;
        }


        private void CheckReconnectInterval()
        {
            if (Camera != null && Camera.VideoSource != null && Camobject.settings.active && !IsClone && !IsReconnect)
            {
                if (Camobject.settings.reconnectinterval > 0)
                {

                    ReconnectCount += _secondCount;
                    if (ReconnectCount > Camobject.settings.reconnectinterval)
                    {
                        IsReconnect = true;

                        if (VolumeControl != null)
                            VolumeControl.IsReconnect = true;

                        if (CloneCameraReconnect != null)
                            CloneCameraReconnect(this, EventArgs.Empty);

                        try
                        {
                            Camera.SignalToStop();
                            Camera.VideoSource.WaitForStop();
                        }
                        catch (Exception ex)
                        {
                            MainForm.LogExceptionToFile(ex);
                        }

                        Application.DoEvents();

                        if (Camobject.settings.calibrateonreconnect)
                        {
                            Calibrating = true;
                        }

                        try
                        {
                            Camera.Start();
                        }
                        catch (Exception ex)
                        {
                            MainForm.LogExceptionToFile(ex);
                        }
                        

                        if (CloneCameraReConnected != null)
                            CloneCameraReConnected(this, EventArgs.Empty);

                        if (VolumeControl != null)
                            VolumeControl.IsReconnect = false;

                        IsReconnect = false;
                        ReconnectCount = 0;
                    }
                }
            }
        }

        private void DoCalibrate()
        {
            if (Camera != null && !LastFrameNull)
            {
                if (Camera.MotionDetector != null)
                {
                    if (Camera.MotionDetector.MotionDetectionAlgorithm is CustomFrameColorDifferenceDetector)
                    {
                        ((CustomFrameColorDifferenceDetector)Camera.MotionDetector.MotionDetectionAlgorithm).
                            SetBackgroundFrame(LastFrame);
                    }
                    else
                    {
                        if (Camera.MotionDetector.MotionDetectionAlgorithm is CustomFrameDifferenceDetector)
                        {
                            ((CustomFrameDifferenceDetector)Camera.MotionDetector.MotionDetectionAlgorithm)
                                .
                                SetBackgroundFrame(LastFrame);
                        }
                        else
                        {
                            if (
                                Camera.MotionDetector.MotionDetectionAlgorithm is
                                SimpleBackgroundModelingDetector)
                            {
                                ((SimpleBackgroundModelingDetector)
                                 Camera.MotionDetector.MotionDetectionAlgorithm).Reset();
                            }
                            else
                            {
                                if (Camera.MotionDetector.MotionDetectionAlgorithm is
                                    SimpleColorBackgroundModelingDetector)
                                {
                                    ((SimpleColorBackgroundModelingDetector)
                                     Camera.MotionDetector.MotionDetectionAlgorithm).Reset();
                                }
                            }
                        }
                    }
                }

                CalibrateCount += _secondCount;
                if (CalibrateCount > _calibrateTarget)
                {
                    Calibrating = false;
                    _lastAlertCheck = DateTime.Now;
                    CalibrateCount = 0;
                }
            }
            _movementLastDetected = DateTime.Now;
        }

        private Bitmap ResizeBitmap(Bitmap frame)
        {
            if (Camobject.recorder.profile == 0)
                return frame;

            if (frame.Width == _videoWidth && frame.Height == _videoHeight)
                return frame;

            var b = new Bitmap(_videoWidth, _videoHeight);
            var r = new Rectangle(0, 0, _videoWidth, _videoHeight);
            using (var g = Graphics.FromImage(b))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighSpeed;
                g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                g.SmoothingMode = SmoothingMode.None;
                g.InterpolationMode = InterpolationMode.Default;

                //g.GdiDrawImage(LastFrame, r);
                g.DrawImage(LastFrame, r);
            }

            frame.Dispose();
            frame = null;
            return b;
        }

        private int _videoWidth, _videoHeight;

        public void SetVideoSize()
        {
            switch (Camobject.recorder.profile)
            {
                default:
                    if (Camera != null && Camera.Width > -1)
                    {
                        _videoWidth = Camera.Width;
                        _videoHeight = Camera.Height;
                    }
                    else
                    {
                        string[] wh = Camobject.resolution.Split('x');
                        _videoWidth = Convert.ToInt32(wh[0]);
                        _videoHeight = Convert.ToInt32(wh[1]);
                    }
                    break;
                case 1:
                    _videoWidth = 320; _videoHeight = 240;
                    break;
                case 2:
                    _videoWidth = 480; _videoHeight = 320;
                    break;
            }
        }

        internal string SaveFrame(Bitmap bmp = null)
        {
            if (Camera == null || !Camera.IsRunning)
                return "";

            Image myThumbnail = bmp;
            Graphics g = null;
            string fullpath = "";
            var strFormat = new StringFormat();
            try
            {
                if (myThumbnail == null)
                    myThumbnail = LastFrame;
                g = Graphics.FromImage(myThumbnail);
                strFormat.Alignment = StringAlignment.Center;
                strFormat.LineAlignment = StringAlignment.Far;
                g.DrawString(Camobject.ftp.text, MainForm.Drawfont, MainForm.OverlayBrush,
                             new RectangleF(0, 0, myThumbnail.Width, myThumbnail.Height), strFormat);


                if (MainForm.Encoder != null)
                {
                    string folder = MainForm.Conf.MediaDirectory + "video\\" + Camobject.directory + "\\";

                    if (!Directory.Exists(folder + @"grabs\"))
                        Directory.CreateDirectory(folder + @"grabs\");

                    int i = 0;
                    string filename = Camobject.ftp.localfilename;
                    filename = filename.Replace("{C}", ZeroPad(Camobject.ftp.counter, Camobject.ftp.countermax));
                    Camobject.ftp.counter++;
                    if (Camobject.ftp.counter > Camobject.ftp.countermax)
                        Camobject.ftp.counter = 0;

                    while (filename.IndexOf("{", StringComparison.Ordinal) != -1 && i < 20)
                    {
                        filename = String.Format(CultureInfo.InvariantCulture, filename, DateTime.Now);
                        i++;
                    }

                    //  Set the quality
                    fullpath = folder + @"grabs\" + filename;

                    var parameters = new EncoderParameters(1);
                    parameters.Param[0] = new EncoderParameter(Encoder.Quality, Camobject.ftp.quality);
                    myThumbnail.Save(fullpath, MainForm.Encoder, MainForm.EncoderParams);
                }

            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            _lastFrameUploaded = DateTime.Now;
            if (g != null)
                g.Dispose();
            strFormat.Dispose();
            if (myThumbnail != null)
                myThumbnail.Dispose();
            return fullpath;
        }

        private string ZeroPad(int counter, int countermax)
        {
            string r = counter.ToString(CultureInfo.InvariantCulture);
            int i = countermax.ToString(CultureInfo.InvariantCulture).Length;
            while (r.Length < i)
            {
                r = "0" + r;
            }
            return r;

        }

        private void FtpFrame(Bitmap bmp = null)
        {
            using (var imageStream = new MemoryStream())
            {
                Image myThumbnail = bmp;
                Graphics g = null;
                var strFormat = new StringFormat();
                try
                {
                    if (myThumbnail==null)
                        myThumbnail = LastFrame;
                    g = Graphics.FromImage(myThumbnail);
                    strFormat.Alignment = StringAlignment.Center;
                    strFormat.LineAlignment = StringAlignment.Far;
                    g.DrawString(Camobject.ftp.text, MainForm.Drawfont, MainForm.OverlayBrush,
                                 new RectangleF(0, 0, myThumbnail.Width, myThumbnail.Height), strFormat);

                    int i = 0;
                    string filename = Camobject.ftp.filename;
                    filename = filename.Replace("{C}", ZeroPad(Camobject.ftp.ftpcounter, Camobject.ftp.countermax));
                    Camobject.ftp.ftpcounter++;
                    if (Camobject.ftp.ftpcounter > Camobject.ftp.countermax)
                        Camobject.ftp.ftpcounter = 0;

                    while (filename.IndexOf("{", StringComparison.Ordinal) != -1 && i < 20)
                    {
                        filename = String.Format(CultureInfo.InvariantCulture, filename, DateTime.Now);
                        i++;
                    }


                    if (MainForm.Encoder != null)
                    {
                        //  Set the quality
                        var parameters = new EncoderParameters(1);
                        parameters.Param[0] = new EncoderParameter(Encoder.Quality, Camobject.ftp.quality);
                        myThumbnail.Save(imageStream, MainForm.Encoder, parameters);
                    }



                    Camobject.ftp.ready = false;

                    ThreadPool.QueueUserWorkItem((new AsynchronousFtpUpLoader()).FTP,
                                                 new FTPTask(Camobject.ftp.server + ":" + Camobject.ftp.port,
                                                             Camobject.ftp.usepassive, Camobject.ftp.username,
                                                             Camobject.ftp.password, filename,
                                                             imageStream.ToArray(), Camobject.id, Camobject.ftp.counter, Camobject.ftp.rename));
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    Camobject.ftp.ready = true;
                }
                _lastFrameUploaded = DateTime.Now;
                if (g != null)
                    g.Dispose();
                strFormat.Dispose();
                if (myThumbnail != null)
                    myThumbnail.Dispose();
            }

        }

        private bool OpenTimeLapseWriter()
        {
            DateTime date = DateTime.Now;
            String filename = String.Format("TimeLapse_{0}-{1}-{2}_{3}-{4}-{5}",
                                             date.Year, Helper.ZeroPad(date.Month), Helper.ZeroPad(date.Day),
                                             Helper.ZeroPad(date.Hour), Helper.ZeroPad(date.Minute),
                                             Helper.ZeroPad(date.Second));
            TimeLapseVideoFileName = Camobject.id + "_" + filename;
            string folder = MainForm.Conf.MediaDirectory + "video\\" + Camobject.directory + "\\";

            if (!Directory.Exists(folder + @"thumbs\"))
                Directory.CreateDirectory(folder + @"thumbs\");

            filename = folder + TimeLapseVideoFileName;


            Bitmap bmpPreview = LastFrame;


            bmpPreview.Save(folder + @"thumbs/" + TimeLapseVideoFileName + "_large.jpg", MainForm.Encoder, MainForm.EncoderParams);
            Image.GetThumbnailImageAbort myCallback = ThumbnailCallback;
            Image myThumbnail = bmpPreview.GetThumbnailImage(96, 72, myCallback, IntPtr.Zero);
            bmpPreview.Dispose();

            Graphics g = Graphics.FromImage(myThumbnail);
            var strFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far };
            var rect = new RectangleF(0, 0, 96, 72);

            g.DrawString(LocRm.GetString("Timelapse"), MainForm.Drawfont, MainForm.OverlayBrush,
                         rect, strFormat);
            strFormat.Dispose();

            myThumbnail.Save(folder + @"thumbs/" + TimeLapseVideoFileName + ".jpg", MainForm.Encoder, MainForm.EncoderParams);

            g.Dispose();
            myThumbnail.Dispose();


            _timeLapseWriter = null;
            bool success = false;

            try
            {
                try
                {
                    Program.WriterMutex.WaitOne();
                    _timeLapseWriter = new VideoFileWriter();
                    _timeLapseWriter.Open(filename + CodecExtension, _videoWidth, _videoHeight, Codec,
                                          CalcBitRate(Camobject.recorder.quality), Camobject.recorder.timelapseframerate);

                    success = true;
                    TimelapseStart = DateTime.Now;
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex, "Camera " + Camobject.id);
                    _timeLapseWriter = null;
                    Camobject.recorder.timelapse = 0;
                }
                finally
                {
                    Program.WriterMutex.ReleaseMutex();
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex, "Camera " + Camobject.id);
            }
            return success;
        }

        private void CloseTimeLapseWriter()
        {
            _timeLapseTotal = 0;
            _timeLapseFrameCount = 0;

            if (_timeLapseWriter == null)
                return;

            try
            {
                Program.WriterMutex.WaitOne();
                _timeLapseWriter.Dispose();
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            finally
            {
                Program.WriterMutex.ReleaseMutex();
            }

            _timeLapseWriter = null;

            var fpath = MainForm.Conf.MediaDirectory + "video\\" + Camobject.directory + "\\" + TimeLapseVideoFileName + CodecExtension;

            var fi = new FileInfo(fpath);
            var dSeconds = Convert.ToInt32((DateTime.Now - TimelapseStart).TotalSeconds);

            FilesFile ff = _filelist.FirstOrDefault(p => p.Filename.EndsWith(TimeLapseVideoFileName + CodecExtension));
            bool newfile = false;
            if (ff == null)
            {
                ff = new FilesFile();
                newfile = true;
            }

            ff.CreatedDateTicks = DateTime.Now.Ticks;
            ff.Filename = TimeLapseVideoFileName + CodecExtension;
            ff.MaxAlarm = 0;
            ff.SizeBytes = fi.Length;
            ff.DurationSeconds = dSeconds;
            ff.IsTimelapse = true;
            ff.AlertData = "";
            ff.TriggerLevel = 0;
            ff.TriggerLevelMax = 0;


            if (newfile)
            {
                lock(_filelist)
                    _filelist.Insert(0, ff);

                MainForm.MasterFileAdd(new FilePreview(TimeLapseVideoFileName + CodecExtension, dSeconds,
                                                            Camobject.name, DateTime.Now.Ticks, 2, Camobject.id,
                                                            ff.MaxAlarm));
                
                if (TopLevelControl != null)
                {
                    ((MainForm)TopLevelControl).NeedsMediaRefresh = DateTime.Now;
                }
            }
        }

        private static bool ThumbnailCallback()
        {
            return false;
        }

        public bool Highlighted;


        private bool _custom;
        private DateTime _customSet = DateTime.MinValue;

        internal bool Custom
        {
            get { return _custom; }
            set
            {
                _custom = value;
                if (value)
                    _customSet = DateTime.Now;
            }
        }

        public Color BorderColor
        {
            get
            {

                if (Custom)
                    return _customColor;

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
                return (Highlighted || Focused || Custom) ? 2 : 1;
            }
        }

        private bool _lastframenull = true;
        public bool LastFrameNull
        {
            get { return _lastframenull; }
        }
        
        public Bitmap LastFrame
        {
            get
            {
                lock (_lockobject)
                {
                    if (_lastFrame == null)
                    {
                        return null;
                    }
                    Bitmap bmp = null;
                    try
                    {
                        //bmp = new Bitmap(_lastFrame);
                        bmp = (Bitmap)_lastFrame.Clone();//bit faster
                    }
                    catch (ArgumentException)
                    {
                        //bitmap has been disposed
                        _lastFrame = null;
                        _lastframenull = true;
                    }
                    catch (Exception e)
                    {
                        MainForm.LogExceptionToFile(e);
                        _lastFrame = null;
                        _lastframenull = true;
                    }

                    return bmp;
                }
            }
            set
            {
                //setting the frame when not enabled risks a deadlock
                if (IsEnabled)
                {
                    lock (_lockobject)
                    {
                        if (_lastFrame != null)
                            _lastFrame.Dispose();
                        _lastFrame = value;
                    }
                }
                else
                {
                    if (_lastFrame != null)
                        _lastFrame.Dispose();
                    _lastFrame = value;
                }
                _lastframenull = value == null;
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            if (NeedSizeUpdate && Camera != null)
            {
                AutoSize = true;
                UpdatePosition();
            }
            else
                AutoSize = false;
            
            Monitor.Enter(this);
            Graphics gCam = pe.Graphics;

            var grabBrush = new SolidBrush(BorderColor);
            var borderPen = new Pen(grabBrush, BorderWidth);
            var volBrush = new SolidBrush(MainForm.VolumeLevelColor);

            var overlayBackgroundBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 0));

            string m = "";
            try
            {
                Rectangle rc = ClientRectangle;
                int textpos = rc.Height - 15;


                bool message = false;

                if (Camobject.settings.active)
                {
                    if (Camera != null && !LastFrameNull)
                    {
                        m = string.Format("FPS: {0:F2}", Camera.Framerate) + ", ";

                        if (Camera.ZFactor > 1)
                        {
                            m = "Z: " + String.Format("{0:0.0}", Camera.ZFactor) + " " + m;
                        }

                        gCam.CompositingMode = CompositingMode.SourceCopy;
                        gCam.CompositingQuality = CompositingQuality.HighSpeed;
                        gCam.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                        gCam.SmoothingMode = SmoothingMode.None;
                        gCam.InterpolationMode = InterpolationMode.Default;
                        gCam.Clear(BackgroundColor);

                        gCam.DrawImage(LastFrame, rc.X + 1, rc.Y + 1, rc.Width - 2, rc.Height - 26);

                        gCam.CompositingMode = CompositingMode.SourceOver;
                        gCam.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;


                        if (Calibrating)
                        {
                            int remaining = _calibrateTarget - Convert.ToInt32(CalibrateCount);
                            if (remaining < 0) remaining = 0;

                            gCam.DrawString(
                                LocRm.GetString("Calibrating") + " (" + remaining + "): " + Camobject.name,
                                MainForm.Drawfont, MainForm.CameraDrawBrush, new PointF(5, textpos));
                            message = true;
                        }
                        if (Recording)
                        {
                            gCam.FillEllipse(MainForm.RecordBrush, new Rectangle(rc.Width - 12, 4, 8, 8));
                        }
                        if (PTZNavigate)
                        {
                            RunPTZ(gCam);
                        }

                        if (Camobject.alerts.active)
                            m = "!: " + m;
                        else
                        {
                            m = ": " + m;
                        }

                        if (ForcedRecording)
                            m = "F" + m;
                        else
                        {
                            if (Camobject.detector.recordondetect)
                                m = "D" + m;
                            else
                            {
                                if (Camobject.detector.recordonalert)
                                    m = "A" + m;
                                else
                                {
                                    m = "N" + m;
                                }
                            }

                        }

                    }
                    else
                    {
                        if (VideoSourceErrorState)
                        {
                            gCam.DrawString(
                                VideoSourceErrorMessage,
                                MainForm.Drawfont, MainForm.CameraDrawBrush, new PointF(5, 5));
                            gCam.DrawString(
                                LocRm.GetString("Error") + ": " + Camobject.name,
                                MainForm.Drawfont, MainForm.CameraDrawBrush, new PointF(5, textpos));
                            message = true;
                        }
                        else
                        {
                            gCam.DrawString(
                                LocRm.GetString("Connecting") + ": " + Camobject.name,
                                MainForm.Drawfont, MainForm.CameraDrawBrush, new PointF(5, textpos));
                            message = true;
                        }
                    }

                    if (Camera != null && Camera.IsRunning)
                    {
                        DrawDetectionGraph(gCam, volBrush, MainForm.CameraLine, rc);
                    }
                }
                else
                {
                    string txt = Camobject.schedule.active ? LocRm.GetString("Scheduled") : LocRm.GetString("Offline");
                    txt += ": " + Camobject.name;
                    if (Camobject.schedule.active)
                        m = "S: " + m;
                    gCam.DrawString(txt, MainForm.Drawfont, MainForm.CameraDrawBrush, new PointF(5, 5));
                    gCam.DrawString(SourceType, MainForm.Drawfont, MainForm.CameraDrawBrush, new PointF(5, 20));
                }

                if (Camera != null && Camera.MotionDetector != null && !Calibrating)
                {

                    var blobcounter = Camera.MotionDetector.MotionProcessingAlgorithm as BlobCountingObjectsProcessing;
                    if (blobcounter != null)
                    {
                        m += blobcounter.ObjectsCount + " " + LocRm.GetString("Objects") + ", ";
                    }
                }

                if (!message)
                {
                    gCam.DrawString(m + Camobject.name,
                                     MainForm.Drawfont, MainForm.CameraDrawBrush, new PointF(5, textpos));
                }

                if (_mouseMove > DateTime.Now.AddSeconds(-3) && MainForm.Conf.ShowOverlayControls && !PTZNavigate)
                {
                    DrawOverlay(gCam, overlayBackgroundBrush);
                }

                gCam.DrawRectangle(borderPen, 0, 0, rc.Width - 1, rc.Height - 1);
                var borderPoints = new[]
                {
                    new Point(rc.Width - 15, rc.Height), new Point(rc.Width, rc.Height - 15),
                    new Point(rc.Width, rc.Height)
                };

                gCam.FillPolygon(grabBrush, borderPoints);
            }
            catch (Exception e)
            {
                MainForm.LogExceptionToFile(e, "Camera " + Camobject.id);
            }


            overlayBackgroundBrush.Dispose();
            borderPen.Dispose();
            grabBrush.Dispose();
            volBrush.Dispose();

            Monitor.Exit(this);

            base.OnPaint(pe);
            _lastRedraw = DateTime.Now;
        }

        private void DrawDetectionGraph(Graphics gCam, SolidBrush sb, Pen pline, Rectangle rc)
        {
            //draw detection graph
            double d = (Convert.ToDouble(rc.Width - 4) / 100.0);
            int w = 2 + Convert.ToInt32(d * (Camera.MotionLevel * 1000));
            int ax = 2 + Convert.ToInt32(d * Camobject.detector.minsensitivity);
            int axmax = 2 + Convert.ToInt32(d * Camobject.detector.maxsensitivity);

            var grabPoints = new[]
                                 {
                                     new Point(2, rc.Height - 22), new Point(w, rc.Height - 22),
                                     new Point(w, rc.Height - 15), new Point(2, rc.Height - 15)
                                 };

            gCam.FillPolygon(sb, grabPoints);

            gCam.DrawLine(pline, new Point(ax, rc.Height - 22), new Point(ax, rc.Height - 15));
            gCam.DrawLine(pline, new Point(axmax, rc.Height - 22), new Point(axmax, rc.Height - 15));
        }

        private void DrawOverlay(Graphics gCam, SolidBrush sbTs)
        {
            int leftpoint = Width / 2 - ButtonPanelWidth / 2;
            int ypoint = Height - 25 - ButtonPanelHeight;

            if (Camera != null && Seekable)
            {
                AddSeekOverlay(ypoint, sbTs, leftpoint, gCam);
            }
            if (!Seekable)
            {
                gCam.FillRectangle(sbTs, leftpoint, ypoint, ButtonPanelWidth, ButtonPanelHeight);
            }


            gCam.DrawString(">", MainForm.Iconfont, Camobject.settings.active ? MainForm.IconBrushActive : MainForm.IconBrush,
                            leftpoint + ButtonOffset, ypoint + ButtonOffset);

            var b = MainForm.IconBrushOff;
            if (Camobject.settings.active)
            {
                b = MainForm.IconBrush;
            }
            gCam.DrawString("R", MainForm.Iconfont,
                            Recording ? MainForm.IconBrushActive : b,
                            leftpoint + (ButtonOffset * 2) + ButtonWidth,
                            ypoint + ButtonOffset);

            gCam.DrawString("E", MainForm.Iconfont, b, leftpoint + (ButtonOffset * 3) + (ButtonWidth * 2),
                            ypoint + ButtonOffset);
            gCam.DrawString("C", MainForm.Iconfont, b, leftpoint + (ButtonOffset * 4) + (ButtonWidth * 3),
                            ypoint + ButtonOffset);

            gCam.DrawString("P", MainForm.Iconfont, b, leftpoint + (ButtonOffset * 5) + (ButtonWidth * 4),
                            ypoint + ButtonOffset);

            gCam.DrawString("T", MainForm.Iconfont, Talking ? MainForm.IconBrushActive : b,
                            leftpoint + (ButtonOffset * 6) + (ButtonWidth * 5),
                            ypoint + ButtonOffset);
        }

        private void AddSeekOverlay(int ypoint, SolidBrush sbTs, int leftpoint, Graphics gCam)
        {
            var vs = Camera.VideoSource as VlcStream;
            long time = 0, duration = 0;
            if (vs != null)
            {
                time = vs.Time;
                duration = vs.Duration;
            }
            else
            {
                var ff = Camera.VideoSource as FFMPEGStream;
                if (ff != null)
                {
                    time = ff.Time;
                    duration = ff.Duration;

                }
            }
            if (duration > 0)
            {
                string timedisplay = String.Format("{0} / {1}",
                                                    TimeSpan.FromMilliseconds(time).ToString().Substring(0, 8),
                                                    TimeSpan.FromMilliseconds(duration).ToString().Substring(0, 8));

                gCam.FillRectangle(sbTs, leftpoint, ypoint - 25, ButtonPanelWidth, ButtonPanelHeight + 25);
                //draw seek bar
                gCam.DrawLine(Pens.White, leftpoint, ypoint - 2, ButtonPanelWidth + leftpoint, ypoint - 2);
                var xpos = (Convert.ToDouble(time) / Convert.ToDouble(duration)) * ButtonPanelWidth;
                if (_newSeek > 0)
                    xpos = _newSeek;

                int x = leftpoint + Convert.ToInt32(xpos);
                var navPoints = new[]
                {
                    new Point(x-4,ypoint-8), 
                    new Point(x+4,ypoint-2),
                    new Point(x-4, ypoint+4)
                };

                gCam.FillPolygon(Brushes.White, navPoints);
                gCam.DrawPolygon(Pens.Black, navPoints);
                var s = gCam.MeasureString(timedisplay, MainForm.Drawfont);
                gCam.DrawString(timedisplay, MainForm.Drawfont, MainForm.OverlayBrush, Width / 2 - s.Width / 2,
                                ypoint - s.Height - 6);
            }
        }

        private void RunPTZ(Graphics gCam)
        {
            var overlayBackgroundBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 0));
            gCam.FillEllipse(overlayBackgroundBrush, PTZReference.X - 40, PTZReference.Y - 40, 80, 80);
            overlayBackgroundBrush.Dispose();

            gCam.DrawEllipse(MainForm.CameraNav, PTZReference.X - 10, PTZReference.Y - 10, 20, 20);
            double angle = Math.Atan2(PTZReference.Y - _mouseLoc.Y, PTZReference.X - _mouseLoc.X);

            var x = PTZReference.X - 30 * Math.Cos(angle);
            var y = PTZReference.Y - 30 * Math.Sin(angle);
            gCam.DrawLine(MainForm.CameraNav, PTZReference, new Point((int)x, (int)y));

            if (Camobject.ptz != -1 && Math.Abs(Camera.ZFactor - 1) < double.Epsilon)
            {
                Calibrating = true;
                PTZ.SendPTZDirection(angle);
            }
            else
            {
                if (Camera.ZFactor > 1)
                {
                    var d =
                        (Math.Sqrt(Math.Pow(PTZReference.X - _mouseLoc.X, 2) +
                                   Math.Pow(PTZReference.Y - _mouseLoc.Y, 2))) / 5;

                    Camera.ZPoint.X -= Convert.ToInt32(d * Math.Cos(angle));
                    Camera.ZPoint.Y -= Convert.ToInt32(d * Math.Sin(angle));
                    gCam.DrawString("DIGITAL", MainForm.Drawfont, MainForm.CameraDrawBrush, PTZReference.X - 21, PTZReference.Y - 25);
                }
            }
        }

        public ReadOnlyCollection<FilesFile> FileList
        {
            get { return _filelist.AsReadOnly(); }
        }


        private void CameraNewFrame(object sender, NewFrameEventArgs e)
        {
            try
            {
                if (_firstFrame)
                {
                    Camobject.resolution = e.Frame.Width + "x" + e.Frame.Height;
                    SetVideoSize();
                    _firstFrame = false;
                    var vlc = Camera.VideoSource as VlcStream;
                    if (vlc != null)
                        Seekable = vlc.Seekable;
                    else
                    {
                        var ffmpeg = Camera.VideoSource as FFMPEGStream;
                        if (ffmpeg != null)
                        {
                            Seekable = ffmpeg.Seekable;
                        }
                    }
                }

                if (VideoSourceErrorState)
                {
                    UpdateFloorplans(false);
                    var vl = VolumeControl;
                    if (vl != null && vl.AudioSource != null && vl.IsEnabled)
                    {
                        if (vl.AudioSource == Camera.VideoSource || vl.IsClone)
                        {
                            vl.AudioSource.LevelChanged += vl.AudioDeviceLevelChanged;
                            vl.AudioSource.DataAvailable += vl.AudioDeviceDataAvailable;
                            vl.AudioSource.AudioFinished += vl.AudioDeviceAudioFinished;
                        }
                    }
                    VideoSourceErrorState = false;
                }

                

                if (_writerBuffer == null)
                {
                    if (Camobject.recorder.bufferseconds > 0)
                    {
                        var dt = DateTime.Now.AddSeconds(0 - Camobject.recorder.bufferseconds);
                        while (_videoBuffer.Count > 0 && _videoBuffer[0].Timestamp < dt)
                        {
                            _videoBuffer[0].Nullify();
                            _videoBuffer.RemoveAt(0);
                        }

                        _videoBuffer.Add(new FrameAction(e.Frame, Camera.MotionLevel, DateTime.Now));
                    }
                }
                else
                {
                    _writerBuffer.Enqueue(new FrameAction(e.Frame, Camera.MotionLevel, DateTime.Now));
                }

                if (_lastRedraw < DateTime.Now.AddMilliseconds(0 - 1000 / MainForm.Conf.MaxRedrawRate))
                {
                    LastFrame = e.Frame;
                    Invalidate();
                }

                if (_reconnectTime != DateTime.MinValue)
                {
                    Camobject.settings.active = true;
                    _errorTime = _reconnectTime = DateTime.MinValue;
                }
                _errorTime = DateTime.MinValue;

            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);

            }
        }

        public void StartSaving()
        {
            if (Recording || MainForm.StopRecordingFlag || IsEdit)
                return;

            if (MainForm.RecordingThreads >= MainForm.Conf.MaxRecordingThreads)
            {
                MainForm.LogMessageToFile("Skipped recording - maximum recording thread limit hit. See settings to modify the limit.");
                return;
            }
            _recordingThread = new Thread(Record) { Name = "Recording Thread (" + Camobject.id + ")", IsBackground = false, Priority = ThreadPriority.Normal };
            _recordingThread.Start();
        }

        [HandleProcessCorruptedStateExceptions]
        private void Record()
        {
            _stopWrite.Reset();
            MainForm.RecordingThreads++;
            string previewImage = "";
            DateTime recordingStart = DateTime.Now;

            if (!String.IsNullOrEmpty(Camobject.recorder.trigger) && TopLevelControl != null)
            {
                string[] tid = Camobject.recorder.trigger.Split(',');
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
            
            ClearWriterBuffer();
            _writerBuffer = new QueueWithEvents<FrameAction>();
            _writerBuffer.Changed += WriterBufferChanged;
            try
            {

                DateTime date = DateTime.Now.AddHours(Convert.ToDouble(Camobject.settings.timestampoffset));

                string filename = String.Format("{0}-{1}-{2}_{3}-{4}-{5}",
                                                date.Year, Helper.ZeroPad(date.Month), Helper.ZeroPad(date.Day),
                                                Helper.ZeroPad(date.Hour), Helper.ZeroPad(date.Minute),
                                                Helper.ZeroPad(date.Second));

                var vc = VolumeControl;
                if (vc != null && vc.Micobject.settings.active)
                {
                    vc.ForcedRecording = ForcedRecording;
                    vc.StartSaving();
                }

                VideoFileName = Camobject.id + "_" + filename;
                string folder = MainForm.Conf.MediaDirectory + "video\\" + Camobject.directory + "\\";
                string videopath = folder + VideoFileName + CodecExtension;
                bool error = false;
                double maxAlarm = 0;
                LogPluginMessage("record", videopath);
                try
                {
                    try
                    {
                        
                        Program.WriterMutex.WaitOne();
                        _writer = new VideoFileWriter();
                        if (vc == null || vc.AudioSource == null)
                            _writer.Open(videopath, _videoWidth, _videoHeight, Codec,
                                        CalcBitRate(Camobject.recorder.quality), CodecFramerate);
                        else
                        {

                            _writer.Open(videopath, _videoWidth, _videoHeight, Codec,
                                        CalcBitRate(Camobject.recorder.quality), CodecAudio, CodecFramerate,
                                        vc.AudioSource.RecordingFormat.BitsPerSample * vc.AudioSource.RecordingFormat.SampleRate * vc.AudioSource.RecordingFormat.Channels,
                                        vc.AudioSource.RecordingFormat.SampleRate, vc.AudioSource.RecordingFormat.Channels);
                        }
                    }
                    catch
                    {
                        ForcedRecording = false;
                        if (vc != null)
                        {
                            vc.ForcedRecording = false;
                            vc.StopSaving();
                        }
                        throw;
                    }
                    finally
                    {
                        Program.WriterMutex.ReleaseMutex();
                    }

                    FrameAction? peakFrame = null;
                    bool first = true;
                    if (_videoBuffer != null)
                    {
                        foreach (FrameAction fa in _videoBuffer.OrderBy(p => p.Timestamp))
                        {
                            using (var ms = new MemoryStream(fa.Frame))
                            {
                                using (var bmp = (Bitmap) Image.FromStream(ms))
                                {
                                    if (first)
                                    {
                                        recordingStart = fa.Timestamp;
                                        first = false;
                                    }
                                    var pts = (long) (fa.Timestamp - recordingStart).TotalMilliseconds;
                                    _writer.WriteVideoFrame(ResizeBitmap(bmp), pts);
                                }


                                if (fa.MotionLevel > maxAlarm || peakFrame == null)
                                {
                                    maxAlarm = fa.MotionLevel;
                                    peakFrame = fa;
                                }
                                _motionData.Append(String.Format(CultureInfo.InvariantCulture,
                                                                 "{0:0.000}", Math.Min(fa.MotionLevel*1000, 100)));
                                _motionData.Append(",");
                                ms.Close();
                            }
                        }
                        
                        ClearVideoBuffer();
                    }

                    if (vc != null && vc.AudioBuffer != null)
                    {
                        foreach (VolumeLevel.AudioAction aa in vc.AudioBuffer.OrderBy(p => p.TimeStamp))
                        {
                            unsafe
                            {
                                fixed (byte* p = aa.Decoded)
                                {
                                    var pts = (long)(aa.TimeStamp - recordingStart).TotalMilliseconds;
                                    if (pts >= 0)
                                        _writer.WriteAudio(p, aa.Decoded.Length, pts);
                                }
                            }
                        }
                        vc.ClearAudioBuffer();
                    }

                    while (!_stopWrite.WaitOne(0))
                    {
                        int iBuffer = _writerBuffer.Count;
                        while (_writerBuffer.Count > 0 && !_stopWrite.WaitOne(0))
                        {
                            var fa = _writerBuffer.Dequeue();

                            using (var ms = new MemoryStream(fa.Frame))
                            {
                                if (first)
                                {
                                    recordingStart = fa.Timestamp;
                                    first = false;
                                }
                                var bmp = (Bitmap)Image.FromStream(ms);
                                var pts = (long)(fa.Timestamp - recordingStart).TotalMilliseconds;
                                try
                                {
                                    _writer.WriteVideoFrame(ResizeBitmap(bmp), pts);
                                }
                                catch
                                {
                                    bmp.Dispose();
                                    bmp = null;
                                    throw;
                                }

                                bmp.Dispose();
                                bmp = null;

                                if (fa.MotionLevel > maxAlarm || peakFrame == null)
                                {
                                    maxAlarm = fa.MotionLevel;
                                    peakFrame = fa;
                                }
                                _motionData.Append(String.Format(CultureInfo.InvariantCulture,
                                                                "{0:0.000}",
                                                                Math.Min(fa.MotionLevel * 1000, 100)));
                                _motionData.Append(",");
                                ms.Close();

                            }
                            fa.Nullify();

                            while (_writerBuffer.Count > iBuffer)
                            {
                                //drop enqueued frames since last write to prevent buffer overrun
                                _writerBuffer.Dequeue().Nullify();
                            }
                            iBuffer = _writerBuffer.Count;

                            if (vc != null && vc.WriterBuffer != null)
                            {
                                while (vc.WriterBuffer.Count > 0)
                                {

                                    var b = vc.WriterBuffer.Dequeue();
                                    if (first)
                                    {
                                        recordingStart = b.TimeStamp;
                                        first = false;
                                    }
                                    unsafe
                                    {
                                        fixed (byte* p = b.Decoded)
                                        {
                                            var pts = (long)(b.TimeStamp - recordingStart).TotalMilliseconds;
                                            if (pts >= 0)
                                                _writer.WriteAudio(p, b.Decoded.Length, pts);
                                        }
                                    }
                                    b.Nullify();
                                }

                            }

                        }
                        _newRecordingFrame.WaitOne(20);
                    }

                    if (!Directory.Exists(folder + @"thumbs\"))
                        Directory.CreateDirectory(folder + @"thumbs\");

                    if (peakFrame != null && peakFrame.Value.Frame!=null)
                    {
                        using (var ms = new MemoryStream(peakFrame.Value.Frame))
                        {
                            using (var bmp = (Bitmap)Image.FromStream(ms))
                            {
                                bmp.Save(folder + @"thumbs\" + VideoFileName + "_large.jpg", MainForm.Encoder, MainForm.EncoderParams);
                                Image.GetThumbnailImageAbort myCallback = ThumbnailCallback;
                                using (var myThumbnail = bmp.GetThumbnailImage(96, 72, myCallback, IntPtr.Zero))
                                {
                                    myThumbnail.Save(folder + @"thumbs\" + VideoFileName + ".jpg", MainForm.Encoder, MainForm.EncoderParams);
                                }
                            }
                            previewImage = folder + @"thumbs\" + VideoFileName + ".jpg";
                            ms.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    error = true;
                    MainForm.LogExceptionToFile(ex, "Camera " + Camobject.id);
                }
                finally
                {
                    if (_writer != null && _writer.IsOpen)
                    {
                        try
                        {
                            Program.WriterMutex.WaitOne();
                            //Console.WriteLine("closing video writer " + Camobject.name);
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
                    }

                    
                    ClearWriterBuffer();
                    _writerBuffer.Changed -= WriterBufferChanged;
                    _writerBuffer = null;
                    _recordingTime = 0;
                    if (vc != null && vc.Micobject.settings.active)
                        vc.StopSaving();
                }
                if (error)
                {
                    try
                    {
                        FileOperations.Delete(filename + CodecExtension);
                    }
                    catch
                    {
                    }
                    MainForm.RecordingThreads--;
                    return;
                }

                string path = MainForm.Conf.MediaDirectory + "video\\" + Camobject.directory + "\\" +
                              VideoFileName;

                string[] fnpath = (path + CodecExtension).Split('\\');
                string fn = fnpath[fnpath.Length - 1];
                //var fpath = MainForm.Conf.MediaDirectory + "video\\" + Camobject.directory + "\\thumbs\\";
                var fi = new FileInfo(path + CodecExtension);
                var dSeconds = Convert.ToInt32((DateTime.Now - recordingStart).TotalSeconds);

                var ff = _filelist.FirstOrDefault(p => p.Filename.EndsWith(fn));
                bool newfile = false;
                if (ff == null)
                {
                    ff = new FilesFile();
                    newfile = true;
                }

                ff.CreatedDateTicks = DateTime.Now.Ticks;
                ff.Filename = fn;
                ff.MaxAlarm = Math.Min(maxAlarm * 1000, 100);
                ff.SizeBytes = fi.Length;
                ff.DurationSeconds = dSeconds;
                ff.IsTimelapse = false;
                ff.AlertData = Helper.GetMotionDataPoints(_motionData);
                _motionData.Clear();
                ff.TriggerLevel = (100 - Camobject.detector.minsensitivity); //adjusted
                ff.TriggerLevelMax = (100 - Camobject.detector.maxsensitivity);

                if (newfile)
                {
                    lock (_filelist)
                        _filelist.Insert(0, ff);

                    MainForm.MasterFileAdd(new FilePreview(fn, dSeconds, Camobject.name, DateTime.Now.Ticks, 2,
                                                                    Camobject.id, ff.MaxAlarm));
                    if (TopLevelControl != null)
                    {
                        ((MainForm)TopLevelControl).NeedsMediaRefresh = DateTime.Now;
                    }
                }

            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
           

            if (!String.IsNullOrEmpty(Camobject.recorder.trigger) && TopLevelControl != null)
            {
                string[] tid = Camobject.recorder.trigger.Split(',');
                switch (tid[0])
                {
                    case "1":
                        VolumeLevel vl = ((MainForm)TopLevelControl).GetVolumeLevel(Convert.ToInt32(tid[1]));
                        if (vl != null)
                            vl.ForcedRecording = false;
                        break;
                    case "2":
                        CameraWindow cw = ((MainForm)TopLevelControl).GetCameraWindow(Convert.ToInt32(tid[1]));
                        if (cw != null)
                            cw.ForcedRecording = false;
                        break;
                }
            }

            MainForm.RecordingThreads--;
            Camobject.newrecordingcount++;

            if (Notification != null)
                Notification(this, new NotificationType("NewRecording", Camobject.name, previewImage));
        }

        void WriterBufferChanged(object sender, EventArgs e)
        {
            _newRecordingFrame.Set();
        }

        public void CameraAlarm(object sender, EventArgs e)
        {
            _movementLastDetected = DateTime.Now;

            if (!Calibrating)
            {
                if (Camobject.ptzschedule.active && Camobject.ptzschedule.suspend)
                {
                    _suspendPTZSchedule = true;
                }
            }
            if (sender is Camera)
            {
                FlashCounter = 10;
                MovementDetected = true;
                _isTrigger = false;
                return;
            }

            if (sender is LocalServer || sender is VolumeLevel || sender is CameraWindow)
            {
                FlashCounter = 10;
                _isTrigger = true;
                return;
            }


            if (sender is String || sender is IVideoSource)
            {
                if (Camobject.alerts.active && Camera != null)
                {
                    FlashCounter = 10;
                    _isTrigger = true;
                    if (sender is String)
                        _pluginMessage = (String)sender;
                    else
                    {
                        if (sender is KinectStream)
                            _pluginMessage = "Trip Wire";
                    }
                }
                else
                {
                    //ignore - plugin alert
                    _pluginMessage = "";
                }
            }
            else
            {
                _pluginMessage = "";

            }
        }

        private void DoAlert(string msg = "")
        {
            if (IsEdit)
                return;

            RemoteCommand(this, new ThreadSafeCommand("bringtofrontcam," + Camobject.id));
            Alerted = true;
            UpdateFloorplans(true);
            var t = new Thread(AlertThread) { Name = "Alert (" + Camobject.id + ")", IsBackground = false };
            t.Start(msg);
            
            if (Camobject.detector.recordonalert && !Recording)
            {
                StartSaving();
            }
        }

        private void AlertThread(object omsg)
        {
            string msg = omsg.ToString();

            if (Notification != null)
            {
                if (omsg is String)
                    Notification(this, new NotificationType("ALERT_UC", Camobject.name, "", omsg.ToString()));
                else
                    Notification(this, new NotificationType("ALERT_UC", Camobject.name, ""));
            }
            
            if (MainForm.Conf.ScreensaverWakeup)
                ScreenSaver.KillScreenSaver();


            var bmp = LastFrame;
            using (var imageStream = new MemoryStream())
            {

                if (MainForm.Encoder != null && bmp!=null)
                {
                    //  Set the quality
                    var parameters = new EncoderParameters(1);
                    parameters.Param[0] = new EncoderParameter(Encoder.Quality, Camobject.ftp.quality);
                    bmp.Save(imageStream, MainForm.Encoder, parameters);
                }
                byte[] rawgrab = imageStream.ToArray();

                foreach (var ev in Camobject.alertevents.entries)
                {
                    ProcessAlertEvent(rawgrab, msg, ev.type, ev.param1, ev.param2, ev.param3, ev.param4);
                }

            }

            if (Camobject.ftp.mode == 1)
            {
                if (Camobject.ftp.savelocal && !String.IsNullOrEmpty(Camobject.ftp.localfilename))
                {
                    SaveFrame(bmp);
                }

                if (Camobject.ftp.enabled && Camobject.ftp.ready)
                    FtpFrame(bmp);
            }

            if (bmp!=null)
                bmp.Dispose();

        }

        private void ProcessAlertEvent(byte[] rawgrab, string pluginmessage, string type, string param1, string param2, string param3, string param4)
        {
            string id = Camobject.id.ToString(CultureInfo.InvariantCulture);

            param1 = param1.Replace("{ID}", id).Replace("{NAME}", Camobject.name).Replace("{MSG}", pluginmessage);
            param2 = param2.Replace("{ID}", id).Replace("{NAME}", Camobject.name).Replace("{MSG}", pluginmessage);
            param3 = param3.Replace("{ID}", id).Replace("{NAME}", Camobject.name).Replace("{MSG}", pluginmessage);
            param4 = param4.Replace("{ID}", id).Replace("{NAME}", Camobject.name).Replace("{MSG}", pluginmessage);

            try
            {
                switch (type)
                {
                    case "Exe":
                        {
                            if (param1.ToLower() == "ispy.exe" || param1.ToLower() == "ispy")
                            {
                                var topLevelControl = (MainForm) TopLevelControl;
                                if (topLevelControl != null) topLevelControl.ProcessCommandString(param2);
                            }
                            else
                            {
                                try
                                {

                                    var startInfo = new ProcessStartInfo
                                                        {
                                                            UseShellExecute = true,
                                                            FileName = param1,
                                                            Arguments = param2
                                                        };
                                    try
                                    {
                                        var fi = new FileInfo(param1);
                                        if (fi.DirectoryName != null)
                                            startInfo.WorkingDirectory = fi.DirectoryName;
                                    }
                                    catch
                                    {
                                    }
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
                        }
                        break;
                    case "URL":
                        {
                            bool postgrab = Convert.ToBoolean(param2) && rawgrab.Length > 0;
                            if (postgrab)
                            {
                                const string file = "grab.jpg";
                                const string paramName = "file";
                                const string contentType = "image/jpeg";
                                string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
                                byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

                                var wr = (HttpWebRequest) WebRequest.Create(param1);
                                wr.ContentType = "multipart/form-data; boundary=" + boundary;
                                wr.Method = "POST";
                                wr.KeepAlive = true;
                                wr.Credentials = CredentialCache.DefaultCredentials;


                                using (Stream rs = wr.GetRequestStream())
                                {
                                    rs.Write(boundarybytes, 0, boundarybytes.Length);

                                    const string headerTemplate =
                                        "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
                                    string header = string.Format(headerTemplate, paramName, file, contentType);
                                    byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                                    rs.Write(headerbytes, 0, headerbytes.Length);

                                    rs.Write(rawgrab, 0, rawgrab.Length);

                                    byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                                    rs.Write(trailer, 0, trailer.Length);
                                    rs.Close();

                                    WebResponse wresp = null;
                                    try
                                    {
                                        wresp = wr.GetResponse();
                                        Stream stream2 = wresp.GetResponseStream();
                                        if (stream2 != null)
                                        {
                                            var reader2 = new StreamReader(stream2);
                                            string res = reader2.ReadToEnd();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        if (wresp != null)
                                        {
                                            wresp.Close();
                                            wresp = null;
                                        }
                                    }
                                    finally
                                    {
                                        wr = null;
                                    }
                                }

                            }
                            else
                            {
                                var request = (HttpWebRequest) WebRequest.Create(param1);
                                request.Credentials = CredentialCache.DefaultCredentials;
                                var response = (HttpWebResponse) request.GetResponse();

                                // Get the stream associated with the response.
                                Stream receiveStream = response.GetResponseStream();

                                // Pipes the stream to a higher level stream reader with the required encoding format. 
                                if (receiveStream != null)
                                {
                                    var readStream = new StreamReader(receiveStream, Encoding.UTF8);
                                    readStream.ReadToEnd();
                                    response.Close();
                                    readStream.Close();
                                    receiveStream.Close();
                                }
                                response.Close();
                            }
                        }
                        break;
                    case "NM": //network message
                        switch (param1)
                        {
                            case "TCP":
                                {
                                    IPAddress ip;
                                    if (IPAddress.TryParse(param2, out ip))
                                    {
                                        int port;
                                        if (int.TryParse(param3, out port))
                                        {
                                            using (var tcpClient = new TcpClient())
                                            {
                                                try
                                                {
                                                    tcpClient.Connect(ip, port);
                                                    using (var networkStream = tcpClient.GetStream())
                                                    {
                                                        using (var clientStreamWriter = new StreamWriter(networkStream))
                                                        {
                                                            clientStreamWriter.Write(param4);
                                                            clientStreamWriter.Flush();
                                                            clientStreamWriter.Close();
                                                        }
                                                        networkStream.Close();
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    MainForm.LogExceptionToFile(ex);
                                                }
                                                tcpClient.Close();
                                            }

                                        }
                                    }
                                }

                                break;
                            case "UDP":
                                {
                                    IPAddress ip;
                                    if (IPAddress.TryParse(param2, out ip))
                                    {
                                        int port;
                                        if (int.TryParse(param3, out port))
                                        {
                                            using (var udpClient = new UdpClient())
                                            {
                                                try
                                                {

                                                    udpClient.Connect(ip, port);
                                                    var cmd = Encoding.ASCII.GetBytes(param4);
                                                    udpClient.Send(cmd, cmd.Length);

                                                }
                                                catch (Exception ex)
                                                {
                                                    MainForm.LogExceptionToFile(ex);
                                                }
                                                finally
                                                {
                                                    udpClient.Close();
                                                }
                                            }

                                        }
                                    }
                                }

                                break;
                        }

                        break;
                    case "S":
                        try
                        {
                            using (var sp = new SoundPlayer(param1))
                            {
                                sp.Play();
                            }
                        }
                        catch (Exception ex)
                        {
                            MainForm.LogExceptionToFile(ex);
                        }
                        break;
                    case "SW":
                        RemoteCommand(this, new ThreadSafeCommand("show"));
                        break;
                    case "B":
                        Console.Beep();
                        break;
                    case "M":
                        if (TopLevelControl != null)
                        {
                            var mf = ((MainForm) TopLevelControl);
                            mf.Maximise(this, false);
                        }
                        break;
                    case "TA":
                        {
                            if (TopLevelControl != null)
                            {
                                string[] tid = param1.Split(',');
                                switch (tid[0])
                                {
                                    case "1":
                                        VolumeLevel vl =
                                            ((MainForm) TopLevelControl).GetVolumeLevel(Convert.ToInt32(tid[1]));
                                        if (vl != null)
                                            vl.MicrophoneAlarm(this, EventArgs.Empty);
                                        break;
                                    case "2":
                                        CameraWindow cw =
                                            ((MainForm) TopLevelControl).GetCameraWindow(Convert.ToInt32(tid[1]));
                                        if (cw != null && cw!=this)
                                            cw.CameraAlarm(this, EventArgs.Empty);
                                        break;
                                }
                            }
                        }
                        break;
                    case "E":
                        {
                            bool includeGrab = Convert.ToBoolean(param2);
                            string subject =
                                LocRm.GetString("CameraAlertMailSubject").Replace("[OBJECTNAME]", Camobject.name).Trim()
                                    .Replace(Environment.NewLine, " ");
                            string message = LocRm.GetString("CameraAlertMailBody").Trim();
                            message = message.Replace("[OBJECTNAME]", Camobject.name + " " + pluginmessage);

                            string body;
                            switch (Camobject.alerts.mode)
                            {

                                case "nomovement":
                                    int minutes = Convert.ToInt32(Camobject.detector.nomovementintervalnew/60);
                                    int seconds = (Convert.ToInt32(Camobject.detector.nomovementintervalnew)%60);

                                    body =
                                        LocRm.GetString("CameraAlertBodyNoMovement").Replace("[TIME]",
                                                                                             DateTime.Now.AddHours(Convert.ToDouble(Camobject.settings.timestampoffset)).
                                                                                                 ToLongTimeString()).
                                            Replace("[MINUTES]", minutes.ToString(CultureInfo.InvariantCulture)).Replace
                                            ("[SECONDS]", seconds.ToString(CultureInfo.InvariantCulture));

                                    break;
                                default:
                                    body = LocRm.GetString("CameraAlertBodyMovement").Replace("[TIME]",
                                                                                              DateTime.Now.AddHours(Convert.ToDouble(Camobject.settings.timestampoffset)).
                                                                                                  ToLongTimeString());

                                    if (Recording)
                                    {
                                        body += " " + LocRm.GetString("VideoCaptured");
                                    }
                                    else
                                        body += " " + LocRm.GetString("VideoNotCaptured");
                                    break;
                            }

                            message = message.Replace("[BODY]", body + "<br/>" + MainForm.Conf.AppendLinkText);


                            if (includeGrab)
                                WsWrapper.SendAlertWithImage(param1, subject, message, rawgrab);
                            else
                                WsWrapper.SendAlert(param1, subject, message);
                            
                        }
                        break;
                    case "SMS":
                        {
                            string message =
                                LocRm.GetString("SMSMovementAlert").Replace("[OBJECTNAME]", Camobject.name) + " ";
                            switch (Camobject.alerts.mode)
                            {

                                case "nomovement":
                                    int minutes = Convert.ToInt32(Camobject.detector.nomovementintervalnew/60);
                                    int seconds = Convert.ToInt32(Camobject.detector.nomovementintervalnew)%60;

                                    message +=
                                        LocRm.GetString("SMSNoMovementDetected").Replace("[MINUTES]",
                                                                                         minutes.ToString(
                                                                                             CultureInfo.
                                                                                                 InvariantCulture)).
                                            Replace("[SECONDS]", seconds.ToString(CultureInfo.InvariantCulture));
                                    break;
                                default:
                                    message += LocRm.GetString("SMSMovementDetected");
                                    message = message.Replace("[RECORDED]",
                                                              Recording
                                                                  ? LocRm.GetString("VideoCaptured")
                                                                  : LocRm.GetString("VideoNotCaptured"));
                                    break;
                            }
                            if (pluginmessage != "")
                                message = pluginmessage + ": " + message;
                            if (message.Length > 160)
                                message = message.Substring(0, 159);

                            WsWrapper.SendSms(param1, message);
                        }
                        break;
                    case "TM":
                        {
                            string message =
                                LocRm.GetString("SMSMovementAlert").Replace("[OBJECTNAME]", Camobject.name) +
                                " ";
                            switch (Camobject.alerts.mode)
                            {

                                case "nomovement":
                                    int minutes = Convert.ToInt32(Camobject.detector.nomovementintervalnew/60);
                                    int seconds = Convert.ToInt32(Camobject.detector.nomovementintervalnew)%60;

                                    message +=
                                        LocRm.GetString("SMSNoMovementDetected").Replace("[MINUTES]",
                                                                                         minutes.ToString(
                                                                                             CultureInfo.
                                                                                                 InvariantCulture)).
                                            Replace("[SECONDS]", seconds.ToString(CultureInfo.InvariantCulture));
                                    break;
                                default:
                                    message += LocRm.GetString("SMSMovementDetected");
                                    message = message.Replace("[RECORDED]",
                                                              Recording
                                                                  ? LocRm.GetString("VideoCaptured")
                                                                  : LocRm.GetString("VideoNotCaptured"));
                                    break;
                            }
                            if (pluginmessage != "")
                                message = pluginmessage + ": " + message;
                            if (message.Length > 160)
                                message = message.Substring(0, 159);

                            WsWrapper.SendTweet(message + " " + MainForm.Webserver + "/mobile/");
                        }
                        break;

                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
        }

        private void VideoDeviceVideoFinished(object sender, ReasonToFinishPlaying reason)
        {
            if (IsReconnect)
                return;

            switch (reason)
            {
                case ReasonToFinishPlaying.DeviceLost:
                case ReasonToFinishPlaying.EndOfStreamReached:
                case ReasonToFinishPlaying.VideoSourceError:
                    SetErrorState(reason.ToString());
                    break;
                case ReasonToFinishPlaying.StoppedByUser:
                    if (!IsClone)
                        SetDisabled(false);
                    break;
            }

            LastFrame = null;
        }

        private void SetErrorState(string reason)
        {
            VideoSourceErrorMessage = reason;
            if (!VideoSourceErrorState)
            {
                
                VideoSourceErrorState = true;
                MainForm.LogErrorToFile(reason, "Camera " + Camobject.id);

                if (_reconnectTime == DateTime.MinValue)
                {
                    _reconnectTime = DateTime.Now;
                }
                if (_errorTime == DateTime.MinValue)
                    _errorTime = DateTime.Now;
                var vl = VolumeControl;
                if (vl != null && vl.AudioSource != null && vl.IsEnabled)
                {
                    if ((Camera!=null && vl.AudioSource == Camera.VideoSource) || vl.IsClone)
                    {
                        vl.AudioSourceErrorMessage = reason;
                        vl.AudioSourceErrorState = true;

                        vl.AudioSource.LevelChanged -= vl.AudioDeviceLevelChanged;
                        vl.AudioSource.DataAvailable -= vl.AudioDeviceDataAvailable;
                        vl.AudioSource.AudioFinished -= vl.AudioDeviceAudioFinished;
                    }
                }
            }
            _requestRefresh = true;
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
            IsEnabled = false;
            IsReconnect = false;

            if (_recordingThread != null)
                RecordSwitch(false);

            if (VolumeControl != null && VolumeControl.IsEnabled)
                VolumeControl.Disable();

            if (TopLevelControl != null)
            {
                if (((MainForm)TopLevelControl).TalkCamera == this)
                    ((MainForm)TopLevelControl).TalkTo(this, false);
            }

            Application.DoEvents();

            if (SavingTimeLapse)
            {
                CloseTimeLapseWriter();
            }

            StopSaving();
            if (Camera != null)
            {
                
                Calibrating = false;
                
                Camera.NewFrame -= CameraNewFrame;
                Camera.Alarm -= CameraAlarm;
                Camera.PlayingFinished -= VideoDeviceVideoFinished;

                if (Camera!=null && Camera.VideoSource != null)
                {   

                    if (Camera.Plugin != null)
                    {
                        //wait for plugin to exit
                        int i = 0;
                        while (Camera.PluginRunning && i < 10)
                        {
                            Thread.Sleep(100);
                            i++;
                        }
                    }

                    if (Camera.VideoSource is KinectStream)
                    {
                        ((KinectStream)Camera.VideoSource).TripWire -= CameraAlarm;
                    }

                    if (Camera.VideoSource is KinectNetworkStream)
                    {
                        ((KinectNetworkStream)Camera.VideoSource).AlertHandler -= CameraWindow_AlertHandler;
                    }

                    var audiostream = Camera.VideoSource as ISupportsAudio;
                    if (audiostream != null)
                    {
                        audiostream.HasAudioStream -= VideoSourceHasAudioStream;
                    }

                    if (!IsClone && stopSource)
                    {
                        Application.DoEvents();

                        lock (_lockobject)
                        {
                            try
                            {
                                Camera.SignalToStop();
                                if (Camera.VideoSource is VideoCaptureDevice)
                                {
                                    Camera.VideoSource.WaitForStop();
                                }
                            }
                            catch (Exception ex)
                            {
                                MainForm.LogExceptionToFile(ex, "Camera " + Camobject.id);
                            }
                        }

                        if (Camera.VideoSource is XimeaVideoSource)
                        {
                            XimeaSource = null;
                        }
                    }
                }

                var vl = VolumeControl;
                if (vl != null && vl.AudioSource != null && vl.IsEnabled)
                {
                    if ((Camera != null && vl.AudioSource == Camera.VideoSource) || vl.IsClone)
                    {
                        vl.AudioSource.LevelChanged -= vl.AudioDeviceLevelChanged;
                        vl.AudioSource.DataAvailable -= vl.AudioDeviceDataAvailable;
                        vl.AudioSource.AudioFinished -= vl.AudioDeviceAudioFinished;
                    }
                }

                LastFrame = null;               

                Camera.Dispose();
                Camera = null;
            }
            BackgroundColor = MainForm.BackgroundColor;
            Camobject.settings.active = false;
            _recordingTime = 0;
            InactiveRecord = 0;
            _timeLapseTotal = _timeLapseFrameCount = 0;
            ForcedRecording = false;

            MovementDetected = false;
            Alerted = false;
            FlashCounter = 0;
            ReconnectCount = 0;
            PTZNavigate = false;
            UpdateFloorplans(false);
            MainForm.NeedsSync = true;
            _errorTime = _reconnectTime = DateTime.MinValue;

            ClearVideoBuffer();
            ClearWriterBuffer();

            if (!ShuttingDown)
                _requestRefresh = true;

            LastFrame = null;

            if (CloneCameraDisabled != null)
                CloneCameraDisabled(this, EventArgs.Empty);

            GC.Collect();
            
        }

        private void ClearVideoBuffer()
        {

            if (_videoBuffer != null)
            {
                lock (_videoBuffer)
                {
                    while (_videoBuffer.Count > 0)
                    {
                        _videoBuffer[0].Nullify();
                        _videoBuffer.RemoveAt(0);
                    }
                }
            }
        }

        private void ClearWriterBuffer()
        {
            if (_writerBuffer != null)
            {
                lock (_writerBuffer)
                {
                    while (_writerBuffer.Count > 0)
                    {
                        _writerBuffer.Dequeue().Nullify();
                    }
                }
            }
        }

        private string SourceType
        {
            get
            {
                switch (Camobject.settings.sourceindex)
                {
                    default:
                        return "JPEG Feed";
                    case 1:
                        return "MJPEG Feed";
                    case 2:
                        return "FFMPEG File/Stream";
                    case 3:
                        return "Local Device";
                    case 4:
                        return "Desktop";
                    case 5:
                        return "VLC File/Stream";
                    case 6:
                        return "XIMEA Device";
                    case 7:
                        return "Kinect Device";
                    case 8:
                        return "Custom Provider";
                    case 10:
                        return "Cloned Feed";
                }
            }

        }

        private void SetVideoSourceProperty(VideoCaptureDevice device, VideoProcAmpProperty prop, string n)
        {
            try
            {
                int v;
                if (Int32.TryParse(Nv(Camobject.settings.procAmpConfig, n), out v))
                {
                    if (v > Int32.MinValue)
                    {
                        int fv;
                        if (Int32.TryParse(Nv(Camobject.settings.procAmpConfig, "f" + n), out fv))
                        {
                            device.SetProperty(prop, v, (VideoProcAmpFlags)fv);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
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

            if (Camera != null && Camera.IsRunning)
            {
                Disable();
            }

            LastFrame = null;

            IsEnabled = true;
            IsReconnect = false;
            Seekable = false;
            IsClone = Camobject.settings.sourceindex == 10;
            VideoSourceErrorState = false;
            VideoSourceErrorMessage = "";

            string ckies, hdrs;
            switch (Camobject.settings.sourceindex)
            {
                case 0:
                    ckies = Camobject.settings.cookies ?? "";
                    ckies = ckies.Replace("[USERNAME]", Camobject.settings.login);
                    ckies = ckies.Replace("[PASSWORD]", Camobject.settings.password);
                    ckies = ckies.Replace("[CHANNEL]", Camobject.settings.ptzchannel);

                    hdrs = Camobject.settings.headers ?? "";
                    hdrs = hdrs.Replace("[USERNAME]", Camobject.settings.login);
                    hdrs = hdrs.Replace("[PASSWORD]", Camobject.settings.password);
                    hdrs = hdrs.Replace("[CHANNEL]", Camobject.settings.ptzchannel);

                    var jpegSource = new JPEGStream2(Camobject.settings.videosourcestring)
                    {
                        Login = Camobject.settings.login,
                        Password = Camobject.settings.password,
                        ForceBasicAuthentication = Camobject.settings.forcebasic,
                        RequestTimeout = Camobject.settings.timeout,
                        UseHTTP10 = Camobject.settings.usehttp10,
                        HttpUserAgent = Camobject.settings.useragent,
                        Cookies = ckies,
                        Headers = hdrs
                    };

                    OpenVideoSource(jpegSource, true);

                    if (Camobject.settings.frameinterval != 0)
                        jpegSource.FrameInterval = Camobject.settings.frameinterval;

                    break;
                case 1:
                    ckies = Camobject.settings.cookies ?? "";
                    ckies = ckies.Replace("[USERNAME]", Camobject.settings.login);
                    ckies = ckies.Replace("[PASSWORD]", Camobject.settings.password);
                    ckies = ckies.Replace("[CHANNEL]", Camobject.settings.ptzchannel);

                    hdrs = Camobject.settings.headers ?? "";
                    hdrs = hdrs.Replace("[USERNAME]", Camobject.settings.login);
                    hdrs = hdrs.Replace("[PASSWORD]", Camobject.settings.password);
                    hdrs = hdrs.Replace("[CHANNEL]", Camobject.settings.ptzchannel);

                    var mjpegSource = new MJPEGStream2(Camobject.settings.videosourcestring)
                    {
                        Login = Camobject.settings.login,
                        Password = Camobject.settings.password,
                        ForceBasicAuthentication = Camobject.settings.forcebasic,
                        RequestTimeout = Camobject.settings.timeout,
                        HttpUserAgent = Camobject.settings.useragent,
                        DecodeKey = Camobject.decodekey,
                        Cookies = ckies,
                        Headers = hdrs
                    };
                    OpenVideoSource(mjpegSource, true);
                    break;
                case 2:
                    string url = Camobject.settings.videosourcestring;
                    var ffmpegSource = new FFMPEGStream(url)
                    {
                        Cookies = Camobject.settings.cookies,
                        AnalyzeDuration = Camobject.settings.analyseduration,
                        Timeout = Camobject.settings.timeout,
                        UserAgent = Camobject.settings.useragent,
                        Headers = Camobject.settings.headers,
                        NoBuffer = Camobject.settings.nobuffer,
                        RTSPMode = Camobject.settings.rtspmode
                    };
                    OpenVideoSource(ffmpegSource, true);
                    break;
                case 3:
                    string moniker = Camobject.settings.videosourcestring;

                    var videoSource = new VideoCaptureDevice(moniker);
                    string[] wh = Camobject.resolution.Split('x');
                    var sz = new Size(Convert.ToInt32(wh[0]), Convert.ToInt32(wh[1]));
                    var vc = videoSource.VideoCapabilities.Where(p => p.FrameSize == sz).ToList();
                    if (vc.Count > 0)
                    {
                        var vc2 = vc.FirstOrDefault(p => p.AverageFrameRate == Camobject.settings.framerate) ??
                                    vc.FirstOrDefault();
                        videoSource.VideoResolution = vc2;
                    }

                    if (Camobject.settings.crossbarindex != -1 && videoSource.CheckIfCrossbarAvailable())
                    {
                        var cbi =
                            videoSource.AvailableCrossbarVideoInputs.FirstOrDefault(
                                p => p.Index == Camobject.settings.crossbarindex);
                        if (cbi != null)
                        {
                            videoSource.CrossbarVideoInput = cbi;
                        }
                    }

                    OpenVideoSource(videoSource, true);


                    break;
                case 4:
                    Rectangle area = Rectangle.Empty;
                    if (!String.IsNullOrEmpty(Camobject.settings.desktoparea))
                    {
                        var i = System.Array.ConvertAll(Camobject.settings.desktoparea.Split(','), int.Parse);
                        area = new Rectangle(i[0], i[1], i[2], i[3]);
                    }
                    var desktopSource = new DesktopStream(Convert.ToInt32(Camobject.settings.videosourcestring),
                                                            area) { MousePointer = Camobject.settings.desktopmouse };
                    if (Camobject.settings.frameinterval != 0)
                        desktopSource.FrameInterval = Camobject.settings.frameinterval;
                    OpenVideoSource(desktopSource, true);

                    break;
                case 5:
                    List<String> inargs = Camobject.settings.vlcargs.Split(Environment.NewLine.ToCharArray(),
                                                                            StringSplitOptions.RemoveEmptyEntries)
                        .
                        ToList();
                    var vlcSource = new VlcStream(Camobject.settings.videosourcestring, inargs.ToArray()) { TimeOut = Camobject.settings.timeout };

                    OpenVideoSource(vlcSource, true);
                    break;
                case 6:
                    if (XimeaSource == null || !XimeaSource.IsRunning)
                        XimeaSource = new XimeaVideoSource(Convert.ToInt32(Nv(Camobject.settings.namevaluesettings, "device")));
                    OpenVideoSource(XimeaSource, true);
                    break;
                case 7:
                    var tw = false;
                    if (!String.IsNullOrEmpty(Nv(Camobject.settings.namevaluesettings, "TripWires")))
                        tw = Convert.ToBoolean(Nv(Camobject.settings.namevaluesettings, "TripWires"));
                    var ks = new KinectStream(Nv(Camobject.settings.namevaluesettings, "UniqueKinectId"), Convert.ToBoolean(Nv(Camobject.settings.namevaluesettings, "KinectSkeleton")), tw);
                    if (Nv(Camobject.settings.namevaluesettings, "StreamMode") != "")
                        ks.StreamMode = Convert.ToInt32(Nv(Camobject.settings.namevaluesettings, "StreamMode"));
                    OpenVideoSource(ks, true);
                    break;
                case 8:
                    switch (Nv(Camobject.settings.namevaluesettings, "custom"))
                    {
                        case "Network Kinect":
                            OpenVideoSource(new KinectNetworkStream(Camobject.settings.videosourcestring), true);
                            break;
                        default:
                            IsEnabled = false;
                            throw new Exception("No custom provider found for " + Nv(Camobject.settings.namevaluesettings, "custom"));
                    }
                    break;
                case 9:
                    //there is no 9, spooky hey?
                    break;
                case 10:
                    int icam;
                    if (Int32.TryParse(Camobject.settings.videosourcestring, out icam))
                    {
                        var topLevelControl = (MainForm)TopLevelControl;
                        if (topLevelControl != null)
                        {
                            var cw = topLevelControl.GetCameraWindow(icam);
                            if (cw != null)
                            {

                                OpenVideoSource(cw);
                            }
                        }

                    }
                    break;
            }



            if (Camera != null)
            {
                IMotionDetector motionDetector = null;
                IMotionProcessing motionProcessor = null;

                switch (Camobject.detector.type)
                {
                    case "Two Frames":
                        motionDetector = new TwoFramesDifferenceDetector(Camobject.settings.suppressnoise);
                        break;
                    case "Custom Frame":
                        motionDetector = new CustomFrameDifferenceDetector(Camobject.settings.suppressnoise,
                                                                            Camobject.detector.keepobjectedges);
                        break;
                    case "Background Modelling":
                        motionDetector = new SimpleBackgroundModelingDetector(Camobject.settings.suppressnoise,
                                                                                Camobject.detector.keepobjectedges);
                        break;
                    case "Two Frames (Color)":
                        motionDetector = new TwoFramesColorDifferenceDetector(Camobject.settings.suppressnoise);
                        break;
                    case "Custom Frame (Color)":
                        motionDetector = new CustomFrameColorDifferenceDetector(
                            Camobject.settings.suppressnoise,
                            Camobject.detector.keepobjectedges);
                        break;
                    case "Background Modelling (Color)":
                        motionDetector =
                            new SimpleColorBackgroundModelingDetector(Camobject.settings.suppressnoise,
                                                                        Camobject.detector.
                                                                            keepobjectedges);
                        break;
                    case "None":
                        break;
                }

                if (motionDetector != null)
                {
                    switch (Camobject.detector.postprocessor)
                    {
                        case "Grid Processing":
                            motionProcessor = new GridMotionAreaProcessing
                            {
                                HighlightColor =
                                    ColorTranslator.FromHtml(Camobject.detector.color),
                                HighlightMotionGrid = Camobject.detector.highlight
                            };
                            break;
                        case "Object Tracking":
                            motionProcessor = new BlobCountingObjectsProcessing
                            {
                                HighlightColor =
                                    ColorTranslator.FromHtml(Camobject.detector.color),
                                HighlightMotionRegions = Camobject.detector.highlight,
                                MinObjectsHeight = Camobject.detector.minheight,
                                MinObjectsWidth = Camobject.detector.minwidth
                            };

                            break;
                        case "Border Highlighting":
                            motionProcessor = new MotionBorderHighlighting
                            {
                                HighlightColor =
                                    ColorTranslator.FromHtml(Camobject.detector.color)
                            };
                            break;
                        case "Area Highlighting":
                            motionProcessor = new MotionAreaHighlighting
                            {
                                HighlightColor =
                                    ColorTranslator.FromHtml(Camobject.detector.color)
                            };
                            break;
                        case "None":
                            break;
                    }

                    if (Camera.MotionDetector != null)
                    {
                        Camera.MotionDetector.Reset();
                        Camera.MotionDetector = null;
                    }

                    Camera.MotionDetector = motionProcessor == null
                                                ? new MotionDetector(motionDetector)
                                                : new MotionDetector(motionDetector, motionProcessor);

                    Camera.AlarmLevel = Helper.CalculateTrigger(Camobject.detector.minsensitivity);
                    Camera.AlarmLevelMax = Helper.CalculateTrigger(Camobject.detector.maxsensitivity);
                    NeedMotionZones = true;
                }
                else
                {
                    Camera.MotionDetector = null;
                }

                if (!Camera.IsRunning)
                {
                    Calibrating = true;
                    _lastRun = DateTime.Now.Ticks;
                    Camera.Start();
                }
                if (Camera.VideoSource is XimeaVideoSource)
                {
                    //need to set these after the camera starts
                    try
                    {
                        XimeaSource.SetParam(PRM.IMAGE_DATA_FORMAT, IMG_FORMAT.RGB24);
                    }
                    catch (ApplicationException)
                    {
                        XimeaSource.SetParam(PRM.IMAGE_DATA_FORMAT, IMG_FORMAT.MONO8);
                    }
                    XimeaSource.SetParam(CameraParameter.OffsetX, Convert.ToInt32(Nv(Camobject.settings.namevaluesettings, "x")));
                    XimeaSource.SetParam(CameraParameter.OffsetY, Convert.ToInt32(Nv(Camobject.settings.namevaluesettings, "y")));
                    float gain;
                    float.TryParse(Nv(Camobject.settings.namevaluesettings, "gain"), out gain);
                    XimeaSource.SetParam(CameraParameter.Gain, gain);
                    float exp;
                    float.TryParse(Nv(Camobject.settings.namevaluesettings, "exposure"), out exp);
                    XimeaSource.SetParam(CameraParameter.Exposure, exp * 1000);
                    XimeaSource.SetParam(CameraParameter.Downsampling, Convert.ToInt32(Nv(Camobject.settings.namevaluesettings, "downsampling")));
                    XimeaSource.SetParam(CameraParameter.Width, Convert.ToInt32(Nv(Camobject.settings.namevaluesettings, "width")));
                    XimeaSource.SetParam(CameraParameter.Height, Convert.ToInt32(Nv(Camobject.settings.namevaluesettings, "height")));
                    XimeaSource.FrameInterval =
                        (int)(1000.0f / XimeaSource.GetParamFloat(CameraParameter.FramerateMax));
                }



                if (File.Exists(Camobject.settings.maskimage))
                {
                    Camera.Mask = (Bitmap)Image.FromFile(Camobject.settings.maskimage);
                }


            }
            Camobject.settings.active = true;
            UpdateFloorplans(false);

            _recordingTime = 0;
            _timeLapseTotal = _timeLapseFrameCount = 0;
            InactiveRecord = 0;
            MovementDetected = false;

            Alerted = false;
            PTZNavigate = false;
            Camobject.ftp.ready = true;
            _lastRun = DateTime.Now.Ticks;
            MainForm.NeedsSync = true;
            ReconnectCount = 0;
            _dtPTZLastCheck = DateTime.Now;
            _movementLastDetected = DateTime.Now;
            _firstFrame = true;

            if (_videoBuffer != null)
            {
                _videoBuffer.Clear();
            }

            if (Camera != null)
            {
                Camera.ZFactor = 1;
                Camera.ZPoint = Point.Empty;
            }
            _requestRefresh = true;



            if (VolumeControl != null)
                VolumeControl.Enable();


            SetVideoSize();

            //cloned initialisation goes here
            if (CloneCameraEnabled != null)
                CloneCameraEnabled(this, EventArgs.Empty);
        }

        public void SetVideoSourceProperties()
        {
            var videoSource = Camera.VideoSource as VideoCaptureDevice;
            if (videoSource != null && !String.IsNullOrEmpty(Camobject.settings.procAmpConfig) && videoSource.SupportsProperties)
            {
                try
                {
                    SetVideoSourceProperty(videoSource, VideoProcAmpProperty.Brightness, "b");
                    SetVideoSourceProperty(videoSource, VideoProcAmpProperty.Contrast, "c");
                    SetVideoSourceProperty(videoSource, VideoProcAmpProperty.Hue, "h");
                    SetVideoSourceProperty(videoSource, VideoProcAmpProperty.Saturation, "s");
                    SetVideoSourceProperty(videoSource, VideoProcAmpProperty.Sharpness, "sh");
                    SetVideoSourceProperty(videoSource, VideoProcAmpProperty.Gamma, "gam");
                    SetVideoSourceProperty(videoSource, VideoProcAmpProperty.ColorEnable, "ce");
                    SetVideoSourceProperty(videoSource, VideoProcAmpProperty.WhiteBalance, "wb");
                    SetVideoSourceProperty(videoSource, VideoProcAmpProperty.BacklightCompensation, "bc");
                    SetVideoSourceProperty(videoSource, VideoProcAmpProperty.Gain, "g");
                }
                catch
                {
                    //ignore - not supported on the device
                }
            }
        }

        void VideoSourceHasAudioStream(object sender, EventArgs eventArgs)
        {
            if (InvokeRequired)
            {
                Invoke(new SwitchDelegate(AddAudioStream));
                return;
            }
            AddAudioStream();
        }

        private void AddAudioStream()
        {
            if (Camera != null && TopLevelControl != null && !Camobject.settings.ignoreaudio)
            {
                var vl = VolumeControl;
                if (vl == null)
                {
                    vl = ((MainForm)TopLevelControl).AddCameraMicrophone(Camobject.id, Camobject.name + " mic");
                    Camobject.settings.micpair = vl.Micobject.id;
                }

                var m = vl.Micobject;
                if (m != null)
                {
                    var c = Camera.VideoSource as ISupportsAudio;
                    if (c != null)
                    {
                        if (c.RecordingFormat != null)
                        {
                            m.settings.samples = c.RecordingFormat.SampleRate;
                            m.settings.channels = c.RecordingFormat.Channels;
                        }
                    }

                    m.settings.buffer = Camobject.recorder.bufferseconds;
                    m.settings.bits = 16;
                    m.alerts.active = false;
                    m.detector.recordonalert = false;
                    m.detector.recordondetect = false;
                }

                vl.Enable();
            }
        }

        public string Nv(string name)
        {
            return Nv(Camobject.settings.namevaluesettings, name);
        }

        public string Nv(string csv, string name)
        {
            if (String.IsNullOrEmpty(csv))
                return "";
            name = name.ToLower().Trim();
            string[] settings = csv.Split(',');
            foreach (string[] nv in settings.Select(s => s.Split('=')).Where(nv => nv[0].ToLower().Trim() == name))
            {
                return nv[1];
            }
            return "";
        }

        private static int CalcBitRate(int q)
        {
            return 8000 * (Convert.ToInt32(Math.Pow(2, (q - 1))));
        }

        public void UpdateFloorplans(bool isAlert)
        {
            foreach (
                objectsFloorplan ofp in
                    MainForm.FloorPlans.Where(
                        p => p.objects.@object.Any(q => q.type == "camera" && q.id == Camobject.id)).
                        ToList())
            {
                ofp.needsupdate = true;
                if (!isAlert || TopLevelControl == null) continue;
                FloorPlanControl fpc = ((MainForm)TopLevelControl).GetFloorPlan(ofp.id);
                fpc.LastAlertTimestamp = DateTime.Now.UnixTicks();
                fpc.LastOid = Camobject.id;
                fpc.LastOtid = 2;
            }
        }

        public string RecordSwitch(bool record)
        {
            if (record)
            {
                if (!Camobject.settings.active)
                {
                    Enable();
                }
                ForcedRecording = true;
                return "recording," + LocRm.GetString("RecordingStarted");
            }

            ForcedRecording = false;
            try {
                if (VolumeControl != null)
                    VolumeControl.ForcedRecording = false;
            }
            catch
            {
            }

            StopSaving();           

            return "notrecording," + LocRm.GetString("RecordingStopped");
        }

        private void OpenVideoSource(CameraWindow cw)
        {
            if (Camera != null)
            {
                Camera.WaitForStop();
                Camera.Dispose();
                Camera = null;
            }

            cw.CloneCameraReconnect += CWCameraReconnect;
            cw.CloneCameraDisabled += CWCameraDisabled;
            cw.CloneCameraReConnected += CWCameraReconnected;
            cw.CloneCameraEnabled += CWCameraEnabled;

            bool opened = false;
            if (cw.Camera != null)
            {
                if (cw.Camera.VideoSource != null)
                {
                    var source = cw.Camera.VideoSource;
                    Camera = new Camera(source);

                    Camera.NewFrame += CameraNewFrame;
                    Camera.PlayingFinished += VideoDeviceVideoFinished;
                    Camera.Alarm += CameraAlarm;

                    Calibrating = true;
                    _lastRun = DateTime.Now.Ticks;
                    Camera.Start();
                    if (cw.VolumeControl != null && !Camobject.settings.ignoreaudio)
                    {
                        if (Camobject.id == -1)
                        {
                            Camobject.id = MainForm.NextCameraId;
                            MainForm.Cameras.Add(Camobject);
                        }
                        var vl = VolumeControl;
                        if (vl == null && TopLevelControl != null)
                        {
                            vl = ((MainForm)TopLevelControl).AddCameraMicrophone(Camobject.id, Camobject.name + " mic");
                            Camobject.settings.micpair = vl.Micobject.id;
                        }
                        if (vl != null)
                        {
                            var m = vl.Micobject;
                            if (m != null)
                            {
                                m.settings.samples = cw.VolumeControl.Micobject.settings.samples;
                                m.settings.channels = cw.VolumeControl.Micobject.settings.channels;
                                m.settings.typeindex = 4;
                                m.settings.buffer = Camobject.recorder.bufferseconds;
                                m.settings.bits = 16;
                                m.alerts.active = false;
                                m.detector.recordonalert = false;
                                m.detector.recordondetect = false;
                            }

                            vl.Disable();
                            vl.Enable();

                            cw.VolumeControl.AudioDeviceEnabled += VLAudioDeviceEnabled;
                        }
                    }
                    opened = true;
                }

            }
            if (!opened)
            {
                SetErrorState("Source camera offline");
                VideoSourceErrorState = true;
                _requestRefresh = true;
            }
        }

        void CWCameraReconnect(object sender, EventArgs e)
        {
            if (Camera != null)
            {
                if (Camera.VideoSource != null)
                {
                    Camera.PlayingFinished -= VideoDeviceVideoFinished;
                }
                Camera.NewFrame -= CameraNewFrame;
                Camera.Alarm -= CameraAlarm;

                if (VolumeControl != null)
                    VolumeControl.IsReconnect = true;
            }



        }

        void CWCameraDisabled(object sender, EventArgs e)
        {
            if (IsEnabled)
            {
                SetErrorState("Source camera offline");

                if (Camera != null)
                {
                    Camera.PlayingFinished -= VideoDeviceVideoFinished;
                    Camera.NewFrame -= CameraNewFrame;
                    Camera.Alarm -= CameraAlarm;

                    StopSaving();
                    Camera.Dispose();
                    Camera = null;
                }
                
                
            }

        }

        void CWCameraEnabled(object sender, EventArgs e)
        {
            if (IsEnabled)
            {
                OpenVideoSource((CameraWindow)sender);
            }
        }

        void VLAudioDeviceEnabled(object sender, EventArgs e)
        {
            if (IsClone)
            {
                if (VolumeControl != null)
                {
                    if (VolumeControl.IsEnabled)
                    {
                        VolumeControl.Disable();
                        VolumeControl.Enable();
                    }

                }
            }
        }



        void CWCameraReconnected(object sender, EventArgs e)
        {
            if (Camera != null)
            {
                if (Camera.VideoSource != null)
                {
                    Camera.PlayingFinished += VideoDeviceVideoFinished;
                }

                Camera.NewFrame += CameraNewFrame;
                Camera.Alarm += CameraAlarm;

                if (Camobject.settings.calibrateonreconnect)
                {
                    Calibrating = true;

                }

                if (VolumeControl != null)
                    VolumeControl.IsReconnect = false;
            }

        }



        private void OpenVideoSource(IVideoSource source, bool @override)
        {
            if (!@override && Camera != null && Camera.VideoSource != null && Camera.VideoSource.Source == source.Source)
            {
                return;
            }
            if (Camera != null && Camera.IsRunning)
            {
                Disable();
            }
            var vlcStream = source as VlcStream;
            if (vlcStream != null)
            {
                vlcStream.FormatWidth = Camobject.settings.desktopresizewidth;
                vlcStream.FormatHeight = Camobject.settings.desktopresizeheight;
            }

            var kinectStream = source as KinectStream;
            if (kinectStream != null)
            {
                kinectStream.InitTripWires(Camobject.alerts.pluginconfig);
                kinectStream.TripWire += CameraAlarm;
            }

            var kinectNetworkStream = source as KinectNetworkStream;
            if (kinectNetworkStream != null)
            {
                kinectNetworkStream.AlertHandler += CameraWindow_AlertHandler;
            }

            var audiostream = source as ISupportsAudio;
            if (audiostream != null)
            {
                audiostream.HasAudioStream += VideoSourceHasAudioStream;
            }

            Camera = new Camera(source);

            Camera.NewFrame += CameraNewFrame;
            Camera.PlayingFinished += VideoDeviceVideoFinished;
            Camera.Alarm += CameraAlarm;

            RotateFlipType rft;
            if (Enum.TryParse(Camobject.rotateMode, out rft))
            {
                Camera.RotateFlipType = rft;
            }


            SetVideoSourceProperties();
        }

        public List<string> PluginCommands
        {
            get
            {
                if (Camera.Plugin!=null)
                {
                    var c = Camera.Plugin.GetType().GetProperty("Commands");
                    if (c!=null)
                    {
                        string commands = c.GetValue(Camera.Plugin, null).ToString();
                        if (!String.IsNullOrEmpty(commands))
                        {
                            return commands.Split(',').ToList();
                        }
                    }
                }
                return null;
            }
        } 

        public void ExecutePluginCommand(string command)
        {
            if (Camera.Plugin != null)
            {
                var a = (String)Camera.Plugin.GetType().GetMethod("ExecuteCommand").Invoke(Camera.Plugin, new object[] { command });
                if (a!="OK")
                {
                    MainForm.LogErrorToFile("Plugin response: "+a);
                }
            }
        }

        void CameraWindow_AlertHandler(object sender, AlertEventArgs eventArgs)
        {
            if (Camera.Plugin != null)
            {
                var a = (String)Camera.Plugin.GetType().GetMethod("ProcessAlert").Invoke(Camera.Plugin, new object[] { eventArgs.Description });
                if (!String.IsNullOrEmpty(a))
                {
                    string[] actions = a.ToLower().Split(',');
                    foreach (var action in actions)
                    {
                        if (!String.IsNullOrEmpty(action))
                        {
                            switch (action)
                            {
                                case "alarm":
                                    CameraAlarm(eventArgs.Description, EventArgs.Empty);
                                    break;
                                case "flash":
                                    FlashCounter = 10;
                                    break;
                                default:
                                    if (action.StartsWith("border:") && action.Length > 7)
                                    {
                                        string col = action.Substring(7);
                                        try
                                        {
                                            _customColor = Color.FromArgb(Convert.ToInt32(col));
                                            Custom = true;
                                        }
                                        catch (Exception e)
                                        {
                                            MainForm.LogExceptionToFile(e);
                                        }

                                    }
                                    if (action.StartsWith("log:") && action.Length > 4)
                                    {
                                        string[] log = action.Substring(4).Split('|');
                                        if (log.Length >= 2)
                                        {
                                            LogPluginMessage(log[0], log[1]);
                                        }

                                    }
                                    break;
                            }

                        }
                    }
                }
            }

        }

        private void LogPluginMessage(string action, string detail)
        {
            if (Camobject.alerts.mode == "KinectPlugin")
                MainForm.LogPluginToFile(Camobject.name, Camobject.id, action, detail);
        }

        private void StopSaving()
        {
            if (Recording)
            {
                _stopWrite.Set();
                _recordingThread.Join();
            }
        }

        public void ApplySchedule()
        {
            //find most recent schedule entry
            if (!Camobject.schedule.active || Camobject.schedule == null || Camobject.schedule.entries == null ||
                !Camobject.schedule.entries.Any())
                return;

            DateTime dNow = DateTime.Now;
            TimeSpan shortest = TimeSpan.MaxValue;
            objectsCameraScheduleEntry mostrecent = null;
            bool isstart = true;

            for (int index = 0; index < Camobject.schedule.entries.Length; index++)
            {
                objectsCameraScheduleEntry entry = Camobject.schedule.entries[index];
                if (entry.active)
                {
                    string[] dows = entry.daysofweek.Split(',');
                    foreach (int dow in dows.Select(dayofweek => Convert.ToInt32(dayofweek)))
                    {
                        //when did this last fire?
                        if (entry.start.IndexOf("-", StringComparison.Ordinal) == -1)
                        {
                            string[] start = entry.start.Split(':');
                            var dtstart = new DateTime(dNow.Year, dNow.Month, dNow.Day, Convert.ToInt32(start[0]),
                                                       Convert.ToInt32(start[1]), 0);
                            while ((int)dtstart.DayOfWeek != dow || dtstart > dNow)
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
                            while ((int)dtstop.DayOfWeek != dow || dtstop > dNow)
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
                    if (!Camobject.settings.active)
                        Enable();

                    Camobject.detector.recordondetect = mostrecent.recordondetect;
                    Camobject.detector.recordonalert = mostrecent.recordonalert;
                    Camobject.ftp.enabled = mostrecent.ftpenabled;
                    Camobject.ftp.savelocal = mostrecent.savelocalenabled;
                    Camobject.alerts.active = mostrecent.alerts;
                    if (Camobject.recorder.timelapseenabled && !mostrecent.timelapseenabled)
                    {
                        CloseTimeLapseWriter();
                    }
                    Camobject.recorder.timelapseenabled = mostrecent.timelapseenabled;
                    if (mostrecent.recordonstart)
                    {
                        ForcedRecording = true;
                    }
                }
                else
                {
                    if (Camobject.settings.active)
                        Disable();
                }
            }
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // CameraWindow
            // 
            this.Cursor = System.Windows.Forms.Cursors.Hand;
            this.MinimumSize = new System.Drawing.Size(160, 120);
            this.Size = new System.Drawing.Size(160, 120);
            this.LocationChanged += new System.EventHandler(this.CameraWindow_LocationChanged);
            this.Resize += new System.EventHandler(this.CameraWindow_Resize);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        #region Nested type: FrameAction

        private struct FrameAction
        {
            public byte[] Frame;
            public readonly double MotionLevel;
            public readonly DateTime Timestamp;

            public FrameAction(Bitmap frame, double motionLevel, DateTime timeStamp)
            {
                using (var ms = new MemoryStream())
                {
                    frame.Save(ms, MainForm.Encoder, MainForm.EncoderParams);
                    Frame = ms.GetBuffer();
                }
                //frame = null;
                MotionLevel = motionLevel;
                Timestamp = timeStamp;
            }

            public void Nullify()
            {
                Frame = null;
            }

        }
        #endregion

        private void CameraWindow_Resize(object sender, EventArgs e)
        {
            SetVolumeLevelLocation();
        }

        private void CameraWindow_LocationChanged(object sender, EventArgs e)
        {
            SetVolumeLevelLocation();
        }

        public void SetVolumeLevelLocation()
        {
            if (VolumeControl != null)
            {
                VolumeControl.Location = new Point(Location.X, Location.Y + Height);
                VolumeControl.Width = Width;
                VolumeControl.Height = 40;
            }
        }
    }


    public class ThreadSafeCommand : EventArgs
    {
        public string Command;
        // Constructor
        public ThreadSafeCommand(string command)
        {
            Command = command;
        }
    }

    public class NotificationType : EventArgs
    {
        public int Objectid;
        public int Objecttypeid;
        public string Text;
        public string Type;
        public string PreviewImage;
        public string OverrideMessage;

        public NotificationType(string type, string text, string previewimage, string overrideMessage = "")
        {
            Type = type;
            Text = text;
            PreviewImage = previewimage;
            OverrideMessage = overrideMessage;
        }
    }
}