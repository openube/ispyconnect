using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Video.FFMPEG;
using AForge.Video.Ximea;
using AForge.Vision.Motion;
using iSpyApplication.Video;
using xiApi.NET;
using Encoder = System.Drawing.Imaging.Encoder;
using PictureBox = AForge.Controls.PictureBox;

namespace iSpyApplication
{
    /// <summary>
    /// Summary description for CameraWindow.
    /// </summary>
    public sealed class CameraWindow : PictureBox
    {
        #region Private
        internal DateTime LastAutoTrackSent = DateTime.MinValue;
        private DateTime _lastRedraw = DateTime.MinValue;
        private bool _processing;
        private double _recordingTime;
        private bool _stopWrite;
        private double _timeLapse;
        private double _timeLapseFrames;
        private double _timeLapseTotal;
        private double _timeLapseFrameCount;
        private Point _mouseLoc;
        private List<FrameAction> _videoBuffer = new List<FrameAction>();
        private QueueWithEvents<FrameAction> _writerBuffer;
        private DateTime _errorTime = DateTime.MinValue;
        private DateTime _reconnectTime = DateTime.MinValue;
        
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
        private int _milliCount;
        private DateTime _movementLastDetected = DateTime.MinValue;
        private DateTime _lastAlertCheck = DateTime.MinValue;
        private DateTime _mouseMove = DateTime.MinValue;
        private List<FilesFile> _filelist;
        private VideoFileWriter _timeLapseWriter;
        private readonly ToolTip _toolTipCam;
        private int _ttind = -1;
        public volatile bool IsReconnect;
        private bool _suspendPTZSchedule;
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
            get { return (ButtonWidth + ButtonOffset*2); }
        }

        private string _pluginMessage = "";
        #endregion
        
        internal bool Ptzneedsstop;
        internal VideoFileWriter Writer;

        #region Public

        #region Delegates

        public delegate void NotificationEventHandler(object sender, NotificationType e);
        private delegate void SwitchDelegate();
        public delegate void RemoteCommandEventHandler(object sender, ThreadSafeCommand e);

        #endregion

        public bool Talking;
        public bool ForcedRecording;
        public bool NeedMotionZones = true;
        public XimeaVideoSource XimeaSource;
        public bool Alerted;    
        public double MovementCount;
        public double CalibrateCount, ReconnectCount;
        public Rectangle RestoreRect = Rectangle.Empty;
        public bool Calibrating;
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


       

        public VolumeLevel VolumeControl
        {
         get
         {
            if (Camobject!=null && Camobject.settings.micpair>-1)
            {
                if (TopLevelControl != null)
                {
                    VolumeLevel vl = ((MainForm) TopLevelControl).GetVolumeLevel(Camobject.settings.micpair);
                    if (vl != null && vl.Micobject!=null)
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
                return _recordingThread!=null && _recordingThread.IsAlive;
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
                        return AudioCodec.MP3;
                    case 4:
                        return AudioCodec.MP3;
                    case 5:
                        return AudioCodec.MP3;
                    case 6:
                        return AudioCodec.MP3;
                    case 7:
                        return AudioCodec.MP3;
                    case 8:
                        return AudioCodec.MP3;
                }
            }
        }

        public List<FilesFile> FileList
        {
            get
            {
                if (_filelist != null)
                    return _filelist;
                string dir = MainForm.Conf.MediaDirectory + "video\\" +
                                                      Camobject.directory + "\\";

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                bool failed = false;
                if (File.Exists(dir + "data.xml"))
                {
                    var s = new XmlSerializer(typeof(Files));
                    using (var fs = new FileStream(dir + "data.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        try
                        {
                            using (TextReader reader = new StreamReader(fs))
                            {
                                fs.Position = 0;
                                _filelist = ((Files) s.Deserialize(reader)).File.ToList();
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
                    if (!failed)
                        return _filelist;
                }
                
                //else build from directory contents
                
                _filelist = new List<FilesFile>();
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
                    if (_filelist.Where(p => p.Filename == fi1.Name).Count() == 0)
                    {
                        _filelist.Add(new FilesFile
                                          {
                                              CreatedDateTicks = fi.CreationTime.Ticks,
                                              Filename = fi.Name,
                                              SizeBytes = fi.Length,
                                              MaxAlarm = 0,
                                              AlertData = "0",
                                              DurationSeconds = 0,
                                              IsTimelapse = fi.Name.ToLower().IndexOf("timelapse")!=-1
                                          });
                    }
                }
                for (int index = 0; index < _filelist.Count; index++)
                {
                    FilesFile ff = _filelist[index];
                    if (lFi.Where(p => p.Name == ff.Filename).Count() == 0)
                    {
                        _filelist.Remove(ff);
                        index--;
                    }
                }
                _filelist = _filelist.OrderByDescending(p => p.CreatedDateTicks).ToList();
                return _filelist;
            }
            set { lock (_filelist) {_filelist = value;} }
        }
        public void SaveFileList()
        {
            try
            {
                if (FileList != null)
                    lock (FileList)
                    {
                        var fl = new Files {File = FileList.ToArray()};
                        string fn = MainForm.Conf.MediaDirectory + "video\\" +
                                    Camobject.directory + "\\data.xml";
                        var s = new XmlSerializer(typeof (Files));
                        using (var fs = new FileStream(fn, FileMode.Create))
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
        public void ScanForMissingFiles()
        {
            try
            {
                //check files exist
                string dir = MainForm.Conf.MediaDirectory + "video\\" +
                             Camobject.directory + "\\";
                var farr = FileList.ToArray();
                int j = 0;
                foreach (FilesFile t in farr)
                {
                    if (!File.Exists(dir + t.Filename))
                    {
                        _filelist.RemoveAt(j);
                        continue;
                    }
                    j++;
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
        }

        public event RemoteCommandEventHandler RemoteCommand;
        public event NotificationEventHandler Notification;

        #endregion

        #region SizingControls

        public void UpdatePosition()
        {
            Monitor.Enter(this);

            if (Parent != null && _camera != null)
            {
                if (!_camera.LastFrameNull)
                {
                    int width = _camera.Width;
                    int height = _camera.Height;
                    Camobject.resolution = width + "x" + height;
                    SuspendLayout();
                    Size = new Size(width + 2, height + 26);
                    Camobject.width = width;
                    Camobject.height = height;
                    ResumeLayout();
                    NeedSizeUpdate = false;
                }
                else
                {
                    Monitor.Exit(this);
                    return;
                }               
            }
            Monitor.Exit(this);
        }

        private void CameraWindowResize(object sender, EventArgs e)
        {
        }
        #endregion

        private double _secondCount;

        public CameraWindow(objectsCamera cam)
        {
            InitializeComponent();
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint, true);
            Margin = new Padding(0, 0, 0, 0);
            Padding = new Padding(0, 0, 5, 5);
            BorderStyle = BorderStyle.None;
            BackColor = MainForm.Conf.BackColor.ToColor();
            Camobject = cam;
            PTZ = new PTZController(this);

            _toolTipCam = new ToolTip {AutomaticDelay = 500, AutoPopDelay = 1500};
        }

        public Camera Camera
        {
            get { return _camera; }
            set
            {
                Monitor.Enter(this);
                bool newCamera = (value != _camera && value != null);

                if (value == null && _camera != null && _camera.IsRunning)
                {
                    Disable();
                }

                _camera = value;
                if (_camera != null)
                {
                    _camera.CW = this;
                    
                    _videoBuffer.Clear();
                    if (value != null)
                    {
                        if (newCamera)
                        {
                            _camera.NewFrame += CameraNewFrame;
                            _camera.Alarm += CameraAlarm;
                        }
                    }
                    else
                    {
                        _videoBuffer = null;
                    }
                    
                }
                Monitor.Exit(this);
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
            MousePos result = MousePos.NoWhere;
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
                        Camera.ZPoint = new Point(r.Left + r.Width/2, r.Top + r.Height/2);
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

        /// zoom camera only if mouse is positioned on camera while turning the wheel
        /// from Marco@BlueOceanLtd.asia
        /// <param name="e"></param>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if ((e.Location.X >= 0) && (e.Location.X <= Size.Width) &&
            (e.Location.Y >= 0) && (e.Location.Y <= Size.Height))
            {
                base.OnMouseWheel(e);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
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

                if (mousePos== MousePos.NoWhere)
                {
                    if (MainForm.Conf.ShowOverlayControls)
                    {
                        int leftpoint = Width / 2 - ButtonPanelWidth / 2;
                        int ypoint = Height - 24 - ButtonPanelHeight;
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
                                                string url = MainForm.Webserver + "/watch.aspx?tab=1&obj=2_" +
                                                             Camobject.id +
                                                             "_" +
                                                             MainForm.Conf.ServerPort;
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
                                                        var r = new Random();
                                                        MainForm.OpenUrl("http://" + MainForm.IPAddress + ":" +
                                                                         MainForm.Conf.LANPort +
                                                                         "/livefeed?oid=" +
                                                                         Camobject.id + "&r=" + r.NextDouble() +
                                                                         "&full=1&auth=" + MainForm.Identifier);
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
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            if (TopLevelControl != null) ((MainForm) TopLevelControl).PTZToolUpdate(this);
            Invalidate();
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
                    if (MainForm.Conf.ShowOverlayControls)
                    {
                        int leftpoint = Width/2 - ButtonPanelWidth/2;
                        int ypoint = Height - 24 - ButtonPanelHeight;
                        var toolTipLocation = new Point(e.Location.X, ypoint+ButtonPanelHeight+1);
                        if (e.Location.X > leftpoint && e.Location.X < leftpoint + ButtonPanelWidth &&
                            e.Location.Y > ypoint && e.Location.Y < ypoint + ButtonPanelHeight)
                        {
                            int x = e.Location.X - leftpoint;
                            if (x < ButtonWidth + ButtonOffset)
                            {
                                //power
                                if (_ttind != 0)
                                {
                                    if (Camobject.settings.active)
                                        _toolTipCam.Show(LocRm.GetString("switchOff"), this, toolTipLocation, 1000);
                                    else
                                    {
                                        _toolTipCam.Show(LocRm.GetString("Switchon"), this, toolTipLocation, 1000);
                                    }
                                    _ttind = 0;
                                }
                            }
                            else
                            {
                                if (x < (ButtonWidth + ButtonOffset)*2)
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
                        else
                        {
                            _toolTipCam.Hide(this);
                            _ttind = -1;
                        }
                    }
                    break;
            }

            base.OnMouseMove(e);

        }

        protected override void OnResize(EventArgs eventargs)
        {
            if ((ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                if (_camera != null && !_camera.LastFrameNull)
                {
                    double arW = Convert.ToDouble(_camera.Width)/Convert.ToDouble(_camera.Height);
                    Width = Convert.ToInt32(arW*Height);
                }
            }
            base.OnResize(eventargs);
            if (Width < MinimumSize.Width) Width = MinimumSize.Width;
            if (Height < MinimumSize.Height) Height = MinimumSize.Height;
            if (VolumeControl!=null)
                MainForm.NeedsRedraw = true;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Cursor = Cursors.Default;
            _mouseMove = DateTime.MinValue;
            Invalidate();
        }

        protected override void  OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Cursor = Cursors.Hand;
            Invalidate();
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
            _toolTipCam.RemoveAll();
            _toolTipCam.Dispose();
            _timeLapseWriter = null;
            Writer = null;
            base.Dispose(disposing);
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

                _milliCount += Convert.ToInt32(ts.TotalMilliseconds);
                _lastRun = DateTime.Now.Ticks;

                _secondCount += ts.TotalMilliseconds/1000.0;
                
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
                    _recordingTime += Convert.ToDouble(ts.TotalMilliseconds)/1000.0;

                if (Camobject.alerts.active && Camobject.settings.active)
                {
                    if (FlashCounter > 0 && _isTrigger)
                    {
                        BackColor = (BackColor == MainForm.Conf.ActivityColor.ToColor())
                                        ? MainForm.Conf.BackColor.ToColor()
                                        : MainForm.Conf.ActivityColor.ToColor();
                    }
                    else
                    {

                        switch (Camobject.alerts.mode.ToLower())
                        {
                            case "movement":
                                if (MovementDetected)
                                {
                                    BackColor = (BackColor == MainForm.Conf.ActivityColor.ToColor())
                                                    ? MainForm.Conf.BackColor.ToColor()
                                                    : MainForm.Conf.ActivityColor.ToColor();
                                }
                                else
                                    BackColor = MainForm.Conf.BackColor.ToColor();
                                break;
                            case "nomovement":
                                if (!MovementDetected)
                                {
                                    BackColor = (BackColor == MainForm.Conf.NoActivityColor.ToColor())
                                                    ? MainForm.Conf.BackColor.ToColor()
                                                    : MainForm.Conf.NoActivityColor.ToColor();
                                }
                                else
                                    BackColor = MainForm.Conf.BackColor.ToColor();
                                break;
                            default:
                                if (FlashCounter > 0)
                                {
                                    BackColor = (BackColor == MainForm.Conf.ActivityColor.ToColor())
                                                    ? MainForm.Conf.BackColor.ToColor()
                                                    : MainForm.Conf.ActivityColor.ToColor();
                                }
                                else
                                    BackColor = MainForm.Conf.BackColor.ToColor();
                                break;
                        }
                    }
                }
                else
                {
                    BackColor = MainForm.Conf.BackColor.ToColor();
                }

                if (_secondCount > 1) //every second
                {
                    if (Camobject.settings.ptzautotrack && !Calibrating)
                    {
                        if (Ptzneedsstop && LastAutoTrackSent < DateTime.Now.AddMilliseconds(-1000))
                        {
                            PTZ.SendPTZCommand(Enums.PtzCommand.Stop);
                            Ptzneedsstop = false;
                        }
                        if (Camobject.settings.ptzautohome && LastAutoTrackSent > DateTime.MinValue && LastAutoTrackSent < DateTime.Now.AddSeconds(0 - Camobject.settings.ptzautohomedelay))
                        {
                            LastAutoTrackSent = DateTime.MinValue;                           
                            Calibrating = true;
                            CalibrateCount = 0;
                            _calibrateTarget = Camobject.settings.ptztimetohome;
                            if (String.IsNullOrEmpty(Camobject.settings.ptzautohomecommand) || Camobject.settings.ptzautohomecommand=="Center")
                                PTZ.SendPTZCommand(Enums.PtzCommand.Center);
                            else
                            {
                                PTZ.SendPTZCommand(Camobject.settings.ptzautohomecommand);
                            }
                        }
                    }

                    if (_camera != null && !Camera.LastFrameNull && ForcedRecording && !Recording)
                    {
                        StartSaving();
                    }


                    if (_reconnectTime != DateTime.MinValue)
                    {
                        if (_camera != null && _camera.VideoSource != null)
                        {
                            int sec = Convert.ToInt32((DateTime.Now - _reconnectTime).TotalSeconds);
                            if (sec > 10)
                            {
                                //try to reconnect every 10 seconds
                                if (!_camera.VideoSource.IsRunning)
                                {
                                    Calibrating = true;
                                    CalibrateCount = 0;
                                    _calibrateTarget = Camobject.detector.calibrationdelay;
                                    _camera.Start();
                                }
                                _reconnectTime = DateTime.Now;
                                goto skip;
                            }

                        }
                    }


                    if (Calibrating)
                    {
                        if (_camera!=null && !Camera.LastFrameNull)
                        {
                            if (_camera.MotionDetector != null)
                            {
                                if (_camera.MotionDetector.MotionDetectionAlgorithm is CustomFrameDifferenceDetector)
                                {
                                    ((CustomFrameDifferenceDetector)_camera.MotionDetector.MotionDetectionAlgorithm).
                                        SetBackgroundFrame(_camera.LastFrame);
                                }
                            }

                            CalibrateCount += _secondCount;
                            if (CalibrateCount > _calibrateTarget)
                            {
                                Calibrating = false;
                                CalibrateCount = 0;
                            }
                        }
                        _movementLastDetected = DateTime.MinValue;
                    }
                    else
                    {
                        if (_camera != null && _camera.VideoSource!=null && Camobject.settings.active)
                        {
                            if (Camobject.settings.reconnectinterval > 0)
                            {
                                ReconnectCount += _secondCount;
                                if (ReconnectCount > Camobject.settings.reconnectinterval)
                                {
                                    IsReconnect = true;
                                    _camera.Stop();
                                    if (_camera.VideoSource is VideoCaptureDevice)
                                    {
                                        //need to calibrate as most do auto brightness on enable
                                        Calibrating = true;
                                        CalibrateCount = 0;
                                        _calibrateTarget = Camobject.detector.calibrationdelay;
                                    }
                                    while (_camera.IsRunning)
                                    {
                                        Thread.Sleep(100);
                                    }
                                    _camera.Start();
                                    IsReconnect = false;
                                    ReconnectCount = 0;
                                }
                            }
                        }
                        if (Camobject.settings.notifyondisconnect &&
                            _errorTime != DateTime.MinValue)
                        {
                            int sec = Convert.ToInt32((DateTime.Now - _errorTime).TotalSeconds);
                            if (sec > 30 && sec < 35)
                            {
                                //camera has been down for 30 seconds - send notification
                                string subject =
                                    LocRm.GetString("CameraNotifyDisconnectMailSubject").Replace("[OBJECTNAME]",
                                                                                                 Camobject.name);
                                string message = LocRm.GetString("CameraNotifyDisconnectMailBody");
                                message = message.Replace("[NAME]", Camobject.name);
                                message = message.Replace("[TIME]", DateTime.Now.ToLongTimeString());

                                if (MainForm.Conf.ServicesEnabled && MainForm.Conf.Subscribed)
                                        WsWrapper.SendAlert(Camobject.settings.emailaddress, subject, message);
                               
                                _errorTime = DateTime.MinValue;
                            }
                        }

                        if (Recording && !MovementDetected && !ForcedRecording)
                        {
                            InactiveRecord += _secondCount;
                        }

                        if (Camobject.schedule.active)
                        {
                            DateTime dtnow = DateTime.Now;
                            
                            foreach (var entry in Camobject.schedule.entries.Where(p => p.active))
                            {
                                
                                if (entry.daysofweek.IndexOf(((int) dtnow.DayOfWeek).ToString()) == -1) continue;
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
                                            goto skip;
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
                                            goto skip;
                                        }
                                    }
                                }
                            }
                        }
                        if (Camobject.ptzschedule.active && !_suspendPTZSchedule)
                        {
                            DateTime dtnow = DateTime.Now;
                            foreach (
                                var entry in Camobject.ptzschedule.entries)
                            {
                                if (entry!=null && entry.time.TimeOfDay < dtnow.TimeOfDay &&
                                    entry.time.TimeOfDay > _dtPTZLastCheck.TimeOfDay)
                                {
                                    PTZSettings2Camera ptz = MainForm.PTZs.Single(p => p.id == Camobject.ptz);
                                    objectsCameraPtzscheduleEntry entry1 = entry;
                                    if (ptz.ExtendedCommands != null && ptz.ExtendedCommands.Command!=null)
                                    {
                                        var extcmd =
                                            ptz.ExtendedCommands.Command.FirstOrDefault(p => p.Name == entry1.command);
                                        if (extcmd != null)
                                        {
                                            Calibrating = true;
                                            CalibrateCount = 0;
                                            _calibrateTarget = Camobject.detector.calibrationdelay;
                                            PTZ.SendPTZCommand(extcmd.Value);
                                        }
                                    }

                                }
                            }
                            _dtPTZLastCheck = DateTime.Now;
                        }
                        if (Camobject.settings.active && !Camera.LastFrameNull)
                        {
                            //FTP Interval
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

                            //Check Alert Interval
                            if (Alerted)
                            {
                                _intervalCount += _secondCount;
                                if (_intervalCount > Camobject.alerts.minimuminterval)
                                {
                                    Alerted = false;
                                    _intervalCount = 0;
                                    UpdateFloorplans(false);
                                }
                            }
                            else
                            {
                                //Check new Alert
                                if (Camobject.alerts.active && _camera != null)
                                {
                                    switch (Camobject.alerts.mode)
                                    {

                                        case "movement":
                                            if (_movementLastDetected>_lastAlertCheck)
                                            {
                                                MovementCount += _secondCount;
                                                if (_isTrigger ||
                                                    (Math.Floor(MovementCount) >= Camobject.detector.movementinterval))
                                                {
                                                    RemoteCommand(this,
                                                                    new ThreadSafeCommand("bringtofrontcam," +
                                                                                        Camobject.id));
                                                    DoAlert();
                                                    MovementCount = 0;
                                                    if (Camobject.detector.recordonalert && !Recording)
                                                    {
                                                        StartSaving();
                                                    }
                                                }
                                                _lastAlertCheck = DateTime.Now;
                                            }
                                            else
                                            {
                                                MovementCount = 0;
                                            }

                                            break;
                                        case "objectcount":
                                            if (Camera.MotionDetector != null &&
                                                Camera.MotionDetector.MotionProcessingAlgorithm is
                                                BlobCountingObjectsProcessing)
                                            {
                                                var blobalg =
                                                    (BlobCountingObjectsProcessing)
                                                    Camera.MotionDetector.MotionProcessingAlgorithm;
                                                if (blobalg.ObjectsCount >=
                                                    Camobject.alerts.objectcountalert)
                                                {
                                                    RemoteCommand(this,
                                                                    new ThreadSafeCommand("bringtofrontcam," +
                                                                                        Camobject.id));
                                                    DoAlert();
                                                    MovementCount = 0;
                                                    if (Camobject.detector.recordonalert && !Recording)
                                                    {
                                                        StartSaving();
                                                    }
                                                }

                                            }
                                            break;
                                        case "nomovement":                                            
                                            if ((DateTime.Now - _movementLastDetected).TotalSeconds>Camobject.detector.nomovementinterval)
                                            {
                                                RemoteCommand(this,
                                                                new ThreadSafeCommand("bringtofrontcam," +
                                                                                    Camobject.id));
                                                DoAlert();
                                                if (Camobject.detector.recordonalert && !Recording)
                                                {
                                                    StartSaving();
                                                }
                                            }
                                            break;
                                        default:
                                            if (_pluginMessage != "")
                                            {
                                                DoAlert(_pluginMessage);
                                                MovementCount = 0;
                                                if (Camobject.detector.recordonalert && !Recording)
                                                {
                                                    StartSaving();
                                                }
                                                //reset plugin message
                                                _pluginMessage = "";
                                            }
                                            break;
                                    }
                                }
                            }
                            //Check record
                            if ((Camobject.detector.recordondetect && MovementDetected) && !Recording)
                            {
                                StartSaving();
                            }
                            else
                            {
                                if (!_stopWrite && Recording)
                                {
                                    if (_recordingTime > Camobject.recorder.maxrecordtime ||
                                        ((!MovementDetected && InactiveRecord > Camobject.recorder.inactiverecord) &&
                                            !ForcedRecording))
                                        StopSaving();
                                }
                            }

                            //Check TimeLapse
                            if (Camobject.recorder.timelapseenabled)
                            {
                                if (Camobject.recorder.timelapse > 0)
                                {
                                    _timeLapseTotal += _secondCount;
                                    _timeLapse += _secondCount;
                                    if (_timeLapse >= Camobject.recorder.timelapse)
                                    {
                                        if (!SavingTimeLapse)
                                        {
                                            if (!OpenTimeLapseWriter())
                                                goto skip;
                                        }

                                        Bitmap bm = Camera.LastFrame;
                                        try
                                        {
                                            _timeLapseWriter.WriteVideoFrame(ResizeBitmap(bm),
                                                                                TimeSpan.FromSeconds(_timeLapseFrameCount*
                                                                                                    (1d/
                                                                                                    Camobject.recorder.
                                                                                                        timelapseframerate)));
                                            _timeLapseFrameCount++;
                                        }
                                        catch (Exception ex)
                                        {
                                            MainForm.LogExceptionToFile(ex);
                                        }
                                        finally
                                        {
                                            bm.Dispose();
                                        }
                                        _timeLapse = 0;
                                    }
                                    if (_timeLapseTotal >= 60*Camobject.recorder.timelapsesave)
                                    {
                                        CloseTimeLapseWriter();
                                    }
                                }
                                if (Camobject.recorder.timelapseframes > 0 && _camera != null)
                                {
                                    _timeLapseFrames += _secondCount;
                                    if (_timeLapseFrames >= Camobject.recorder.timelapseframes)
                                    {
                                        Image frame = _camera.LastFrame;
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
                                        frame.Save(dir + filename, MainForm.Encoder,MainForm.EncoderParams);
                                        frame.Dispose();
                                        _timeLapseFrames = 0;
                                    }
                                }
                            }
                        }
                    }
                }
                skip:
                if (_secondCount > 1)
                    _secondCount = 0;
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex, "Camera " + Camobject.id);
            }
            _processing = false;
        }

        private Bitmap ResizeBitmap(Bitmap frame)
        {
            if (Camobject.recorder.profile == 0)
                return frame;
            int w,h;
            GetVideoSize(out w, out h);
            if (frame.Width == w && frame.Height == h)
                return frame;
            var r = new Bitmap(w, h);
            using (var g = Graphics.FromImage(r))
                g.DrawImage(frame, 0, 0, w, h);
            frame.Dispose();
            frame = null;
            return r;
        }

        private void GetVideoSize(out int w, out int h)
        {
            switch (Camobject.recorder.profile)
            {
                default:
                    w = _camera.Width;
                    h = _camera.Height;
                    break;
                case 1:
                    w = 320; h = 240;
                    break;
                case 2:
                    w = 480; h = 320;
                    break;
            }
        }

        private void SaveFrame()
        {
            Image myThumbnail = null;
            Graphics g = null;
            var strFormat = new StringFormat();
            try
            {
                myThumbnail = Camera.LastFrame;
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
                    while (filename.IndexOf("{") != -1 && i < 20)
                    {
                        filename = String.Format(System.Globalization.CultureInfo.InvariantCulture, filename, DateTime.Now);
                        i++;
                    }


                    //  Set the quality
                    myThumbnail.Save(folder + @"grabs\" + filename, MainForm.Encoder, MainForm.EncoderParams);
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
        }

        private void FtpFrame()
        {
            using (var imageStream = new MemoryStream())
            {
                Image myThumbnail = null;
                Graphics g = null;
                var strFormat = new StringFormat();
                try
                {
                    myThumbnail = Camera.LastFrame;
                    g = Graphics.FromImage(myThumbnail);
                    strFormat.Alignment = StringAlignment.Center;
                    strFormat.LineAlignment = StringAlignment.Far;
                    g.DrawString(Camobject.ftp.text, MainForm.Drawfont, MainForm.OverlayBrush,
                                 new RectangleF(0, 0, myThumbnail.Width, myThumbnail.Height), strFormat);


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
                                                             Camobject.ftp.password, Camobject.ftp.filename,
                                                             imageStream.ToArray(), Camobject.id));
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
                imageStream.Close();
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

            filename = folder+TimeLapseVideoFileName;


            Bitmap bmpPreview = Camera.LastFrame;


            bmpPreview.Save(folder + @"thumbs/" + TimeLapseVideoFileName + "_large.jpg", MainForm.Encoder, MainForm.EncoderParams);
            Image.GetThumbnailImageAbort myCallback = ThumbnailCallback;
            Image myThumbnail = bmpPreview.GetThumbnailImage(96, 72, myCallback, IntPtr.Zero);
            bmpPreview.Dispose();

            Graphics g = Graphics.FromImage(myThumbnail);
            var strFormat = new StringFormat {Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far};
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
                

                int w, h;
                GetVideoSize(out w, out h);

                Program.WriterMutex.WaitOne();
                try
                {
                    _timeLapseWriter = new VideoFileWriter();
                    _timeLapseWriter.Open(filename + CodecExtension, w, h, Camobject.recorder.crf, Codec,
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

            Program.WriterMutex.WaitOne();
            try
            {

                _timeLapseWriter.Close();
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

            var fpath = MainForm.Conf.MediaDirectory + "video\\" + Camobject.directory + "\\"+TimeLapseVideoFileName+CodecExtension;

            bool yt = false;
            if (Camobject.settings.youtube.autoupload && MainForm.Conf.Subscribed)
            {
                yt = true;
            }

            var fi = new FileInfo(fpath);
            var dSeconds = Convert.ToInt32((DateTime.Now - TimelapseStart).TotalSeconds);

            FilesFile ff = FileList.FirstOrDefault(p => p.Filename.EndsWith(fpath));
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

            if (newfile)
            {
                FileList.Insert(0, ff);

                if (MainForm.MasterFileList.Where(p => p.Filename.EndsWith(TimeLapseVideoFileName + CodecExtension)).Count() == 0)
                {
                    MainForm.MasterFileList.Add(new FilePreview(TimeLapseVideoFileName + CodecExtension, dSeconds, Camobject.name, DateTime.Now.Ticks, 2,
                                                                Camobject.id));
                    if (TopLevelControl != null)
                    {
                        string thumbname = MainForm.Conf.MediaDirectory + "video\\" + Camobject.directory + "\\thumbs\\" +
                                           TimeLapseVideoFileName + ".jpg";
                        ((MainForm)TopLevelControl).AddPreviewControl(thumbname, fpath, dSeconds,
                                                                       DateTime.Now, true);
                    }
                }

                if (yt && CodecExtension==".mp4")
                {
                    YouTubeUploader.AddUpload(Camobject.id,fpath, Camobject.settings.youtube.@public, "",
                                              "");
                }
            }
        }
        
        private static bool ThumbnailCallback()
        {
            return false;
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            if (NeedSizeUpdate && _camera != null && !_camera.LastFrameNull)
            {
                AutoSize = true;
                UpdatePosition();
            }
            else
                AutoSize = false;
            Monitor.Enter(this);
            Graphics gCam = pe.Graphics;
            Color border = (!Focused) ? Color.Black : MainForm.Conf.BorderHighlightColor.ToColor();
            var grabBrush = new SolidBrush(border);
            var borderPen = new Pen(grabBrush);
            var drawBrush = new SolidBrush(Color.White);
            var sb = new SolidBrush(MainForm.Conf.VolumeLevelColor.ToColor());
            var sbTs = new SolidBrush(Color.FromArgb(128, 0, 0, 0));
            var pline = new Pen(Color.Green, 2);
            var pNav = new Pen(Color.White, 1);
            Bitmap bm = null;
            var recBrush = new SolidBrush(Color.Red);
            string m = "";
            try
            {
                Rectangle rc = ClientRectangle;
                gCam.DrawRectangle(borderPen, 0, 0, rc.Width - 1, rc.Height - 1);
               
                var grabPoints = new[]
                                     {
                                         new Point(rc.Width - 15, rc.Height), new Point(rc.Width, rc.Height - 15),
                                         new Point(rc.Width, rc.Height)
                                     };
                int textpos = rc.Height - 15;


                gCam.FillPolygon(grabBrush, grabPoints);

                if (_camera != null && _camera.IsRunning)
                {
                    //draw detection graph
                    int w = 2 + Convert.ToInt32((Convert.ToDouble(rc.Width - 4)/100.0)*(_camera.MotionLevel*1000));
                    int ax = 2 + Convert.ToInt32((Convert.ToDouble(rc.Width - 4)/100.0)*(_camera.AlarmLevel*1000));

                    grabPoints = new[]
                                     {
                                         new Point(2, rc.Height - 22), new Point(w, rc.Height - 22),
                                         new Point(w, rc.Height - 15), new Point(2, rc.Height - 15)
                                     };

                    gCam.FillPolygon(sb, grabPoints);

                    gCam.DrawLine(pline, new Point(ax, rc.Height - 22), new Point(ax, rc.Height - 15));
                    pline.Dispose();
                }
                bool message = false;

                if (Camobject.settings.active)
                {
                    if (_camera != null && !_camera.LastFrameNull)
                    {
                        m = string.Format("FPS: {0:F2}", _camera.Framerate) + ", ";
                        bm = _camera.LastFrame;
                        if (bm==null)
                        {
                            Camera.LastFrameNull = true;
                            throw new Exception("last frame is null");
                        }

                        if (Camera.ZFactor > 1)
                        {
                            m = "Z: " + String.Format("{0:0.0}", Camera.ZFactor) + " " + m;
                        }

                        gCam.DrawImage(bm, rc.X + 1, rc.Y + 1, rc.Width - 2, rc.Height - 26);

                        if (Calibrating)
                        {
                            int remaining = _calibrateTarget - Convert.ToInt32(CalibrateCount);
                            if (remaining < 0) remaining = 0;

                            gCam.DrawString(
                                LocRm.GetString("Calibrating") + " (" + remaining + "): " + Camobject.name,
                                MainForm.Drawfont, drawBrush, new PointF(5, textpos));
                            message = true;
                        }
                        if (Recording)
                        {
                            gCam.FillEllipse(recBrush, new Rectangle(rc.Width - 10, 2, 8, 8));
                        }
                        if (PTZNavigate)
                        {
                            gCam.FillEllipse(sbTs, PTZReference.X - 40, PTZReference.Y - 40, 80, 80);
                            gCam.DrawEllipse(pNav, PTZReference.X - 10, PTZReference.Y - 10, 20, 20);
                            double angle = Math.Atan2(PTZReference.Y - _mouseLoc.Y, PTZReference.X - _mouseLoc.X);
                            
                            var x = PTZReference.X - 30*Math.Cos(angle);
                            var y = PTZReference.Y - 30*Math.Sin(angle);
                            gCam.DrawLine(pNav, PTZReference, new Point((int) x, (int) y));

                            if (Camobject.ptz > -1 && Camera.ZFactor== 1)
                            {
                                CalibrateCount = 0;
                                Calibrating = true;
                                PTZ.SendPTZDirection(angle);
                            }
                            else
                            {
                                if (Camera.ZFactor > 1)
                                {
                                    var d =
                                        (Math.Sqrt(Math.Pow(PTZReference.X - _mouseLoc.X, 2) +
                                                   Math.Pow(PTZReference.Y - _mouseLoc.Y, 2)))/5;

                                    Camera.ZPoint.X -= Convert.ToInt32(d * Math.Cos(angle));
                                    Camera.ZPoint.Y -= Convert.ToInt32(d * Math.Sin(angle));
                                    gCam.DrawString("DIGITAL",MainForm.Drawfont, drawBrush,PTZReference.X - 21, PTZReference.Y-25);

                                }
                            }
                        }
                        if (VideoSourceErrorState)
                            UpdateFloorplans(false);
                        VideoSourceErrorState = false;
                        
                        if (Camobject.alerts.active)
                            m = "!: " + m;
                        else
                        {
                            m = ": " + m;
                        }

                        if (ForcedRecording)
                            m = "F"+m;
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
                                MainForm.Drawfont, drawBrush, new PointF(5, 5));
                            gCam.DrawString(
                                LocRm.GetString("Error") + ": " + Camobject.name,
                                MainForm.Drawfont, drawBrush, new PointF(5, textpos));
                            message = true;
                        }
                        else
                        {
                            gCam.DrawString(
                                LocRm.GetString("Connecting") + ": " + Camobject.name,
                                MainForm.Drawfont, drawBrush, new PointF(5, textpos));
                            message = true;
                        }
                    }
                }
                else
                {
                    string txt = Camobject.schedule.active ? LocRm.GetString("Scheduled") : LocRm.GetString("Offline");
                    txt += ": " + Camobject.name;
                    if (Camobject.schedule.active)
                        m = "S: " + m;
                    gCam.DrawString(txt,MainForm.Drawfont, drawBrush, new PointF(5, 5));
                    gCam.DrawString(SourceType, MainForm.Drawfont, drawBrush, new PointF(5, 20));
                }

                if (Camera != null && Camera.MotionDetector != null && !Calibrating &&
                    Camera.MotionDetector.MotionProcessingAlgorithm is BlobCountingObjectsProcessing)
                {
                    var blobcounter =
                        (BlobCountingObjectsProcessing)Camera.MotionDetector.MotionProcessingAlgorithm;

                    m += blobcounter.ObjectsCount + " " + LocRm.GetString("Objects") + ", ";
                }

                if (!message)
                {
                    gCam.DrawString(m + Camobject.name,
                                     MainForm.Drawfont, drawBrush, new PointF(5, textpos));
                }


                if (_mouseMove > DateTime.Now.AddSeconds(-3) && MainForm.Conf.ShowOverlayControls && !PTZNavigate)
                {
                    int leftpoint = Width/2 - ButtonPanelWidth/2;
                    int ypoint = Height - 25 - ButtonPanelHeight;
                    
                    gCam.FillRectangle(sbTs, leftpoint, ypoint, ButtonPanelWidth, ButtonPanelHeight);

                    
                        
                    gCam.DrawString(">", MainForm.Iconfont, Camobject.settings.active? MainForm.IconBrushActive: MainForm.IconBrush, leftpoint + ButtonOffset, ypoint + ButtonOffset);

                    var b = MainForm.IconBrushOff;
                    if (Camobject.settings.active)
                    {
                        b = MainForm.IconBrush;
                    }
                    gCam.DrawString("R", MainForm.Iconfont,
                                        Recording ? MainForm.IconBrushActive : b,
                                        leftpoint + (ButtonOffset*2) + ButtonWidth,
                                        ypoint + ButtonOffset);

                    gCam.DrawString("E",MainForm.Iconfont,b, leftpoint + (ButtonOffset*3) + (ButtonWidth*2),
                                    ypoint + ButtonOffset);
                    gCam.DrawString("C",MainForm.Iconfont,b, leftpoint + (ButtonOffset*4) + (ButtonWidth*3),
                                    ypoint + ButtonOffset);

                    gCam.DrawString("P",MainForm.Iconfont,b,  leftpoint + (ButtonOffset*5) + (ButtonWidth*4),
                                    ypoint + ButtonOffset);

                    gCam.DrawString("T",MainForm.Iconfont, Talking?MainForm.IconBrushActive:b, leftpoint + (ButtonOffset*6) + (ButtonWidth*5),
                                        ypoint + ButtonOffset);
                    
                    

                }
            }
            catch (Exception e)
            {
                MainForm.LogExceptionToFile(e, "Camera " + Camobject.id);
            }

            borderPen.Dispose();
            grabBrush.Dispose();
            if (bm != null)
                bm.Dispose();
            drawBrush.Dispose();
            recBrush.Dispose();
            sb.Dispose();
            sbTs.Dispose();
            pline.Dispose();
            pNav.Dispose();
            Monitor.Exit(this);

            base.OnPaint(pe);
        }


        private void CameraNewFrame(object sender, EventArgs e)
        {            
            try
            {
                if (_writerBuffer == null)
                {
                    var dt = DateTime.Now.AddSeconds(0 - Camobject.recorder.bufferseconds);
                    while(_videoBuffer.Count>0 && _videoBuffer[0].Timestamp<dt)
                    {
                        _videoBuffer.RemoveAt(0);
                    }

                    _videoBuffer.Add(new FrameAction(_camera.LastFrame, _camera.MotionLevel, DateTime.Now));
                }
                else
                {
                    _writerBuffer.Enqueue(new FrameAction(_camera.LastFrame, _camera.MotionLevel, DateTime.Now));
                }

                if (_lastRedraw < DateTime.Now.AddMilliseconds(0 - 1000/MainForm.Conf.MaxRedrawRate))
                {
                    Invalidate();
                    _lastRedraw = DateTime.Now;
                }

                if (_reconnectTime != DateTime.MinValue)
                {
                    Camobject.settings.active = true;
                }
                _errorTime = _reconnectTime = DateTime.MinValue;
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);

            }
        }

        public void StartSaving()
        {
            Console.WriteLine("StartSaving");

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
       
        private void Record()
        {
            _stopWrite = false;
            MainForm.RecordingThreads++;
            string previewImage = "";
            DateTime recordingStart = DateTime.MinValue;

            if (!String.IsNullOrEmpty(Camobject.recorder.trigger) && TopLevelControl != null)
            {
                string[] tid = Camobject.recorder.trigger.Split(',');
                switch (tid[0])
                {
                    case "1":
                        VolumeLevel vl = ((MainForm)TopLevelControl).GetVolumeLevel(Convert.ToInt32(tid[1]));
                        if (vl != null)
                            vl.RecordSwitch(true);
                        break;
                    case "2":
                        CameraWindow cw = ((MainForm)TopLevelControl).GetCameraWindow(Convert.ToInt32(tid[1]));
                        if (cw != null)
                            cw.RecordSwitch(true);
                        break;
                }
            }

            try {
                if (_writerBuffer != null)
                    _writerBuffer.Clear();
                _writerBuffer = new QueueWithEvents<FrameAction>();
                _writerBuffer.Changed += WriterBufferChanged;
                DateTime date = DateTime.Now;

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
                string avifilename = folder + VideoFileName + CodecExtension;
                bool error = false;
                double maxAlarm = 0;
                
                try
                {
                    
                    
                    int w, h;
                    GetVideoSize(out w, out h);
                    Program.WriterMutex.WaitOne();

                    try
                    {
                        Writer = new VideoFileWriter();
                        if (vc == null || vc.AudioSource==null)
                            Writer.Open(avifilename, w, h, Camobject.recorder.crf, Codec,
                                        CalcBitRate(Camobject.recorder.quality), CodecFramerate);
                        else
                        {

                            Writer.Open(avifilename, w, h, Camobject.recorder.crf, Codec,
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

                    foreach(FrameAction fa in _videoBuffer.OrderBy(p=>p.Timestamp))
                    {
                        try
                        {
                            using (var ms = new MemoryStream(fa.Frame))
                            {
                                using (var bmp = (Bitmap)System.Drawing.Image.FromStream(ms))
                                {
                                    if (recordingStart == DateTime.MinValue)
                                    {
                                        recordingStart = fa.Timestamp;
                                    }
                                    Writer.WriteVideoFrame(ResizeBitmap(bmp), fa.Timestamp - recordingStart);
                                }


                                if (fa.MotionLevel > maxAlarm || peakFrame == null)
                                {
                                    maxAlarm = fa.MotionLevel;
                                    peakFrame = fa;
                                }
                                _motionData.Append(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                                "{0:0.000}", Math.Min(fa.MotionLevel*1000, 100)));
                                _motionData.Append(",");
                                ms.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            MainForm.LogExceptionToFile(ex);
                        }
                        
                    }
                    _videoBuffer.Clear();

                    if (vc != null && vc.AudioBuffer != null)
                    {
                        foreach (VolumeLevel.AudioAction aa in vc.AudioBuffer.OrderBy(p=>p.TimeStamp))
                        {
                            unsafe
                            {
                                fixed (byte* p = aa.Decoded)
                                {
                                    if ((aa.TimeStamp - recordingStart).TotalMilliseconds>=0)
                                        Writer.WriteAudio(p, aa.Decoded.Length);
                                }
                            }
                        }
                        vc.AudioBuffer.Clear();
                    }
                    

                    if (recordingStart == DateTime.MinValue)
                        recordingStart = DateTime.Now;
                    
                    while (!_stopWrite)
                    {
                        while (_writerBuffer.Count > 0)
                        {
                            var fa = _writerBuffer.Dequeue();
                            try
                            {
                                using (var ms = new MemoryStream(fa.Frame))
                                {

                                    var bmp = (Bitmap) System.Drawing.Image.FromStream(ms);
                                    Writer.WriteVideoFrame(ResizeBitmap(bmp), fa.Timestamp - recordingStart);
                                    bmp.Dispose();
                                    bmp = null;



                                    if (fa.MotionLevel > maxAlarm || peakFrame == null)
                                    {
                                        maxAlarm = fa.MotionLevel;
                                        peakFrame = fa;
                                    }
                                    _motionData.Append(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                                    "{0:0.000}",
                                                                    Math.Min(fa.MotionLevel*1000, 100)));
                                    _motionData.Append(",");
                                    ms.Close();
                                }
                            }
                            catch (Exception ex)
                            {
                                MainForm.LogExceptionToFile(ex);
                            }
                            if (vc != null && vc.WriterBuffer != null)
                            {
                                try
                                {
                                    while (vc.WriterBuffer.Count > 0)
                                    {

                                        var b = vc.WriterBuffer.Dequeue();
                                        unsafe
                                        {
                                            fixed (byte* p = b.Decoded)
                                            {
                                                Writer.WriteAudio(p, b.Decoded.Length);
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    //can fail if the control is switched off/removed whilst recording

                                }
                            }
                            
                        }
                        _newRecordingFrame.WaitOne(200);
                    }

                    if (!Directory.Exists(folder + @"thumbs\"))
                        Directory.CreateDirectory(folder + @"thumbs\");

                    if (peakFrame != null)
                    {
                        using (var ms = new MemoryStream(peakFrame.Value.Frame))
                        {
                            using (var bmp = (Bitmap)System.Drawing.Image.FromStream(ms))
                            {
                                bmp.Save(folder + @"thumbs\" + VideoFileName + "_large.jpg", MainForm.Encoder,
                                         MainForm.EncoderParams);
                                Image.GetThumbnailImageAbort myCallback = ThumbnailCallback;
                                using (var myThumbnail = bmp.GetThumbnailImage(96, 72, myCallback, IntPtr.Zero))
                                {
                                    myThumbnail.Save(folder + @"thumbs\" + VideoFileName + ".jpg", MainForm.Encoder,
                                                     MainForm.EncoderParams);
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
                    _stopWrite = false;
                    if (Writer != null)
                    {
                        Program.WriterMutex.WaitOne();
                        try
                        {
                            
                            Writer.Close();
                            Writer.Dispose();
                        }
                        catch (Exception ex)
                        {
                            MainForm.LogExceptionToFile(ex);
                        }
                        finally
                        {
                            Program.WriterMutex.ReleaseMutex();
                        }
                        
                        Writer = null;
                    }

                    try
                    {
                        _writerBuffer.Clear();
                    }
                    catch
                    {
                    }

                    _writerBuffer = null;
                    _recordingTime = 0;
                    if (vc != null && vc.Micobject.settings.active)
                        VolumeControl.StopSaving();
                }
                if (error)
                {
                    try
                    {
                        File.Delete(filename + CodecExtension);
                    }
                    catch
                    {
                    }
                    MainForm.RecordingThreads--;
                    return;
                }

                string path = MainForm.Conf.MediaDirectory + "video\\" + Camobject.directory + "\\" +
                              VideoFileName;               
                
                bool yt = false;
                if (Camobject.settings.youtube.autoupload && MainForm.Conf.Subscribed)
                {
                    yt = true;
                }


                string[] fnpath = (path + CodecExtension).Split('\\');
                string fn = fnpath[fnpath.Length - 1];
                var fpath = MainForm.Conf.MediaDirectory + "video\\" + Camobject.directory + "\\thumbs\\";
                var fi = new FileInfo(path + CodecExtension);
                var dSeconds = Convert.ToInt32((DateTime.Now - recordingStart).TotalSeconds);

                FilesFile ff = FileList.FirstOrDefault(p => p.Filename.EndsWith(path + CodecExtension));
                bool newfile = false;
                if (ff == null)
                {
                    ff = new FilesFile();
                    newfile = true;
                }

                ff.CreatedDateTicks = DateTime.Now.Ticks;
                ff.Filename = fnpath[fnpath.Length - 1];
                ff.MaxAlarm = Math.Min(maxAlarm * 1000, 100);
                ff.SizeBytes = fi.Length;
                ff.DurationSeconds = dSeconds;
                ff.IsTimelapse = false;
                ff.AlertData = Helper.GetMotionDataPoints(_motionData);
                _motionData.Clear();
                ff.TriggerLevel = Camobject.detector.sensitivity;

                if (newfile)
                {
                    FileList.Insert(0, ff);

                    if (MainForm.MasterFileList.Where(p => p.Filename.EndsWith(fn)).Count() == 0)
                    {
                        MainForm.MasterFileList.Add(new FilePreview(fn, dSeconds, Camobject.name, DateTime.Now.Ticks, 2,
                                                                    Camobject.id));
                        if (TopLevelControl != null)
                        {
                            string thumb = fpath + fn.Replace(CodecExtension, ".jpg");

                            ((MainForm)TopLevelControl).AddPreviewControl(thumb, path + CodecExtension, dSeconds,
                                                                           DateTime.Now, true);
                        }
                    }

                    if (yt)
                    {
                        if (CodecExtension!=".mp4")
                            MainForm.LogMessageToFile("Skipped youtube upload (only upload mp4 files).");
                        else
                        {
                            try
                            {
                                YouTubeUploader.AddUpload(Camobject.id, fn, Camobject.settings.youtube.@public, "", "");
                            }
                            catch (Exception ex)
                            {
                                MainForm.LogExceptionToFile(ex);
                            }    
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            MainForm.RecordingThreads--;
            Camobject.newrecordingcount++;

            if (!String.IsNullOrEmpty(Camobject.recorder.trigger) && TopLevelControl != null)
            {
                string[] tid = Camobject.recorder.trigger.Split(',');
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


            if (sender is String)
            {
                if (Camobject.alerts.active && _camera != null)
                {
                    FlashCounter = 10;
                    _isTrigger = true;
                    _pluginMessage = (String)sender;
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

            if (Camobject.alerts.maximise && TopLevelControl!=null)
                ((MainForm)TopLevelControl).Maximise(this,false);
            Alerted = true;
            UpdateFloorplans(true);
            var start = new ParameterizedThreadStart(AlertThread);
            var t = new Thread(start) {Name = "Alert (" + Camobject.id + ")", IsBackground = false};
            t.Start(msg);
        }

        private void AlertThread(object omsg)
        {
            string msg = omsg.ToString();

            if (!String.IsNullOrEmpty(Camobject.alerts.trigger) && TopLevelControl != null)
            {
                string[] tid = Camobject.alerts.trigger.Split(',');
                switch (tid[0])
                {
                    case "1":
                        VolumeLevel vl = ((MainForm)TopLevelControl).GetVolumeLevel(Convert.ToInt32(tid[1]));
                        if (vl != null)
                            vl.MicrophoneAlarm(this, EventArgs.Empty);
                        break;
                    case "2":
                        CameraWindow cw = ((MainForm)TopLevelControl).GetCameraWindow(Convert.ToInt32(tid[1]));
                        if (cw != null)
                            cw.CameraAlarm(this, EventArgs.Empty);
                        break;
                }
            }

            if (Notification != null)
            {
                if (omsg is String)
                    Notification(this, new NotificationType("ALERT_UC", Camobject.name, "",omsg.ToString()));
                else
                    Notification(this, new NotificationType("ALERT_UC", Camobject.name, ""));
            }

            if (Camobject.alerts.executefile != "")
            {
                try
                {
                    
                    var startInfo = new ProcessStartInfo
                        {
                            UseShellExecute = true,
                            FileName = Camobject.alerts.executefile,
                            Arguments = Camobject.alerts.arguments
                        };
                    try
                    {
                        var fi = new FileInfo(Camobject.alerts.executefile);
                        startInfo.WorkingDirectory = fi.DirectoryName;
                    }
                    catch {}
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

            string[] alertOptions = Camobject.alerts.alertoptions.Split(','); //beep,restore
            if (Convert.ToBoolean(alertOptions[0]))
            {
                Console.Beep();
            }
            if (Convert.ToBoolean(alertOptions[1]))
                RemoteCommand(this, new ThreadSafeCommand("show"));

            if (!String.IsNullOrEmpty(Camobject.alerts.playsound))
            {
                try
                {
                    using (var sp = new SoundPlayer(Camobject.alerts.playsound))
                    {
                        sp.Play();
                    }
                }
                catch (System.Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                }
            }

            using (var imageStream = new MemoryStream())    {
                Image screengrab = null;

                try
                {
                    screengrab = Camera.LastFrame;
                    screengrab.Save(imageStream, MainForm.Encoder, MainForm.EncoderParams);
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                }

                if ( Camobject.ftp.mode == 1)
                {
                    if (screengrab != null && Camobject.ftp.savelocal && !String.IsNullOrEmpty(Camobject.ftp.localfilename))
                    {
                        SaveFrame();
                    }

                    if (screengrab != null && Camobject.ftp.enabled && Camobject.ftp.ready)
                        FtpFrame();
                }

           

                if (MainForm.Conf.ServicesEnabled && MainForm.Conf.Subscribed)
                {
                    //get image array
                    if (Camobject.notifications.sendemail)
                    {
                        string subject = LocRm.GetString("CameraAlertMailSubject").Replace("[OBJECTNAME]", Camobject.name);
                        string message = LocRm.GetString("CameraAlertMailBody");
                        message = message.Replace("[OBJECTNAME]", Camobject.name + " " + msg);

                        string body;
                        switch (Camobject.alerts.mode)
                        {

                            case "nomovement":
                                int minutes = Convert.ToInt32(Camobject.detector.nomovementinterval/60);
                                int seconds = (Camobject.detector.nomovementinterval%60);

                                body =
                                    LocRm.GetString("CameraAlertBodyNoMovement").Replace("[TIME]",
                                                                                         DateTime.Now.ToLongTimeString()).
                                        Replace("[MINUTES]", minutes.ToString()).Replace("[SECONDS]", seconds.ToString());

                                break;
                            default:
                                body = LocRm.GetString("CameraAlertBodyMovement").Replace("[TIME]",
                                                                                          DateTime.Now.ToLongTimeString());

                                if (Recording)
                                {
                                    body += " " + LocRm.GetString("VideoCaptured");
                                }
                                else
                                    body += " " + LocRm.GetString("VideoNotCaptured");
                                break;
                        }

                        message = message.Replace("[BODY]", body + "<br/><a href=\"http://www.ispyconnect.com\">http://www.ispyconnect.com</a>");


                        if (MainForm.Conf.ServicesEnabled && MainForm.Conf.Subscribed)
                        {
                            WsWrapper.SendAlertWithImage(Camobject.settings.emailaddress, subject, message,
                                                         imageStream.ToArray());
                        }


                    }

                    if (Camobject.notifications.sendsms || Camobject.notifications.sendmms ||
                        Camobject.notifications.sendtwitter)
                    {
                        string message = LocRm.GetString("SMSMovementAlert").Replace("[OBJECTNAME]", Camobject.name) + " ";
                        switch (Camobject.alerts.mode)
                        {

                            case "nomovement":
                                int minutes = Convert.ToInt32(Camobject.detector.nomovementinterval/60);
                                int seconds = (Camobject.detector.nomovementinterval%60);

                                message +=
                                    LocRm.GetString("SMSNoMovementDetected").Replace("[MINUTES]", minutes.ToString()).
                                        Replace("[SECONDS]", seconds.ToString());
                                break;
                            default:
                                message += LocRm.GetString("SMSMovementDetected");
                                message = message.Replace("[RECORDED]", Recording ? LocRm.GetString("VideoCaptured") : LocRm.GetString("VideoNotCaptured"));
                                break;
                        }
                        if (msg != "")
                            message = msg + ": " + message;
                        if (message.Length > 160)
                            message = message.Substring(0, 159);


                        if (Camobject.notifications.sendmms)
                        {
                            WsWrapper.SendMms(Camobject.settings.smsnumber, message, imageStream.ToArray());
                        }

                        if (Camobject.notifications.sendsms)
                        {
                            WsWrapper.SendSms(Camobject.settings.smsnumber, message);
                        }

                        if (Camobject.notifications.sendtwitter)
                        {
                            WsWrapper.SendTweet(message + " "+MainForm.Webserver+"/mobile/");
                        }
                    }

                    if (screengrab != null)
                        screengrab.Dispose();

                }
                imageStream.Close();
            }
        }

        private void SourceVideoSourceError(object sender, VideoSourceErrorEventArgs eventArgs)
        {
            VideoSourceErrorMessage = eventArgs.Description;
            if (!VideoSourceErrorState)
            {
                VideoSourceErrorState = true;
                MainForm.LogExceptionToFile(new Exception("VideoSourceError: " + eventArgs.Description),
                                            "Camera " + Camobject.id);
                _reconnectTime = DateTime.Now;
                if (_errorTime == DateTime.MinValue)
                    _errorTime = DateTime.Now;
                
                _camera.LastFrameNull = true;

                if (VolumeControl != null && VolumeControl.AudioSource != null)
                {
                    VolumeControl.AudioSource.Stop();
                    VolumeControl.AudioSourceErrorState = true;
                }
            }
            if (!ShuttingDown)
                Invalidate();
        }

        private void SourcePlayingFinished(object sender, ReasonToFinishPlaying reason)
        {
            if (IsReconnect)
                return;
            switch (reason)
            {
                case ReasonToFinishPlaying.DeviceLost:
                case ReasonToFinishPlaying.EndOfStreamReached:
                case ReasonToFinishPlaying.VideoSourceError:
                    if (!VideoSourceErrorState)
                    {
                        VideoSourceErrorState = true;
                        MainForm.LogExceptionToFile(new Exception("VideoSourceFinished: " + reason), "Camera " + Camobject.id);
                        _reconnectTime = DateTime.Now;
                        if (_errorTime == DateTime.MinValue)
                            _errorTime = DateTime.Now;
                        _camera.LastFrameNull = true;

                        if (VolumeControl != null && VolumeControl.AudioSource != null)
                        {
                            VolumeControl.AudioSource.Stop();
                            VolumeControl.AudioSourceErrorState = true;
                        }
                    }
                    break;
                case ReasonToFinishPlaying.StoppedByUser:
                    Camobject.settings.active = false;
                    break;
            }
            if (!ShuttingDown)
                Invalidate();
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

            IsEnabled = false; 
            _processing = true;
            
            if (_recordingThread != null)
                RecordSwitch(false);
            
            if (VolumeControl != null && VolumeControl.IsEnabled)
                VolumeControl.Disable();

            if (TopLevelControl != null)
            {
                if (((MainForm) TopLevelControl)._talkCamera == this)
                    ((MainForm) TopLevelControl).TalkTo(this, false);
            }
           
            Application.DoEvents();

            

            if (SavingTimeLapse)
            {
                CloseTimeLapseWriter();
            }
            if (_camera != null)
            {
                _camera.ClearMotionZones();
                Calibrating = false;
                if (Camera.MotionDetector != null)
                {
                    try
                    {
                        Camera.MotionDetector.Reset();
                    }
                    catch (Exception ex)
                    {
                        MainForm.LogExceptionToFile(ex, "Camera " + Camobject.id);
                    }
                    Camera.MotionDetector = null;
                }
                if (_camera.VideoSource != null)
                {
                    _camera.VideoSource.PlayingFinished -= SourcePlayingFinished;
                    _camera.VideoSource.VideoSourceError -= SourceVideoSourceError;
                    _camera.NewFrame -= CameraNewFrame;
                    _camera.Alarm -= CameraAlarm;
                    try
                    {
                        _camera.SignalToStop();
                        if (_camera.VideoSource is VideoCaptureDevice)
                        {
                            _camera.VideoSource.WaitForStop();
                        }
                    }
                    catch (Exception ex)
                    {
                        MainForm.LogExceptionToFile(ex, "Camera " + Camobject.id);
                    }


                    if (_camera.VideoSource is XimeaVideoSource)
                    {
                        XimeaSource = null;
                        _camera.VideoSource = null;
                    }
                    _camera.Plugin = null;
                }
                try
                {
                    if (_camera.LastFrameUnmanaged != null)
                    {
                        _camera.LastFrameUnmanaged.Dispose();
                        _camera.LastFrameUnmanaged = null;
                    }
                }
                catch
                {
                }

                if (_camera.Mask != null)
                {
                    _camera.Mask.Dispose();
                    _camera.Mask = null;
                }
                _camera = null;
                BackColor = MainForm.Conf.BackColor.ToColor();
            }
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

            if (!ShuttingDown)
                Invalidate();

            if (_videoBuffer != null)
            {
                _videoBuffer.Clear();                
            }


            _processing = false;
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
                }
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

            _processing = true;
            if (Camera != null && Camera.IsRunning)
            {
                Disable();
            }
            
            IsEnabled = true;
            string ckies;
            switch (Camobject.settings.sourceindex)
            {
                case 0:
                    ckies = Camobject.settings.cookies ?? "";
                    ckies = ckies.Replace("[USERNAME]", Camobject.settings.login);
                    ckies = ckies.Replace("[PASSWORD]", Camobject.settings.password);
                    ckies = ckies.Replace("[CHANNEL]", Camobject.settings.ptzchannel);
                    var jpegSource = new JPEGStream2(Camobject.settings.videosourcestring)
                                         {
                                             Login = Camobject.settings.login,
                                             Password = Camobject.settings.password,
                                             ForceBasicAuthentication = Camobject.settings.forcebasic,
                                             RequestTimeout = MainForm.Conf.IPCameraTimeout,
                                             UseHTTP10 = Camobject.settings.usehttp10,
                                             HttpUserAgent = Camobject.settings.useragent,
                                             Cookies = ckies
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

                    var mjpegSource = new MJPEGStream2(Camobject.settings.videosourcestring)
                                            {
                                                Login = Camobject.settings.login,
                                                Password = Camobject.settings.password,
                                                ForceBasicAuthentication = Camobject.settings.forcebasic,
                                                RequestTimeout = MainForm.Conf.IPCameraTimeout,
                                                HttpUserAgent = Camobject.settings.useragent,
                                                DecodeKey = Camobject.decodekey,
                                                Cookies = ckies
                                            };
                    OpenVideoSource(mjpegSource, true);
                    break;
                case 2:
                    string url = Camobject.settings.videosourcestring;
                    var ffmpegSource = new FFMPEGStream(url);
                    OpenVideoSource(ffmpegSource, true);
                    break;
                case 3:
                    string moniker = Camobject.settings.videosourcestring;

                    var videoSource = new VideoCaptureDevice(moniker);
                    string[] wh = Camobject.resolution.Split('x');
                    videoSource.DesiredFrameSize = new Size(Convert.ToInt32(wh[0]), Convert.ToInt32(wh[1]));
                    videoSource.DesiredFrameRate = Camobject.settings.framerate;

                    if (Camobject.settings.crossbarindex!=-1 && videoSource.CheckIfCrossbarAvailable())
                    {
                        var availableVideoInputs = videoSource.AvailableCrossbarVideoInputs;

                        foreach (VideoInput input in availableVideoInputs)
                        {
                            if (input.Index == Camobject.settings.crossbarindex)
                            {
                                videoSource.CrossbarVideoInput = input;
                                break;
                            }
                        }
                    }

                    OpenVideoSource(videoSource, true);
                   
                   
                    break;
                case 4:
                    Rectangle area = Rectangle.Empty;
                    if (!String.IsNullOrEmpty(Camobject.settings.desktoparea))
                    {
                        var i = Array.ConvertAll(Camobject.settings.desktoparea.Split(','), int.Parse);
                        area = new Rectangle(i[0],i[1],i[2],i[3]);
                    }
                    var desktopSource = new DesktopStream(Convert.ToInt32(Camobject.settings.videosourcestring), area)
                                            {MousePointer = Camobject.settings.desktopmouse};
                    if (Camobject.settings.frameinterval != 0)
                        desktopSource.FrameInterval = Camobject.settings.frameinterval;
                    OpenVideoSource(desktopSource, true);
                    
                    break;
                case 5:
                    List<String> inargs = Camobject.settings.vlcargs.Split(Environment.NewLine.ToCharArray(),
                                                                           StringSplitOptions.RemoveEmptyEntries).ToList();                  
                    var vlcSource = new VlcStream(Camobject.settings.videosourcestring, inargs.ToArray());
                    OpenVideoSource(vlcSource, true);
                    break;
                case 6:
                    if (XimeaSource == null || !XimeaSource.IsRunning)
                        XimeaSource = new XimeaVideoSource(Convert.ToInt32(NV("device")));
                    OpenVideoSource(XimeaSource, true);
                    break;
                case 7:
                    var ks = new KinectStream(NV("UniqueKinectId"), Convert.ToBoolean(NV("KinectSkeleton")));
                    OpenVideoSource(ks, true);
                    break;
                case 8:
                    switch (NV("custom"))
                    {
                        case "Network Kinect":
                            OpenVideoSource(new KinectNetworkStream(Camobject.settings.videosourcestring), true);
                            break;
                        default:
                            throw new Exception("No custom provider found for "+NV("custom"));
                    }
                    break;
            }



            if (Camera != null)
            {
                Camera.LastFrameNull = true;

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

                    if (motionProcessor == null)
                        Camera.MotionDetector = new MotionDetector(motionDetector);
                    else
                        Camera.MotionDetector = new MotionDetector(motionDetector, motionProcessor);

                    Camera.AlarmLevel = Helper.CalculateSensitivity(Camobject.detector.sensitivity);
                    NeedMotionZones = true;
                }
                else
                {
                    Camera.MotionDetector = null;
                }

                if (!Camera.IsRunning)
                {
                    Calibrating = true;
                    CalibrateCount = 0;
                    _calibrateTarget = Camobject.detector.calibrationdelay;
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
                    XimeaSource.SetParam(CameraParameter.OffsetX, Convert.ToInt32(NV("x")));
                    XimeaSource.SetParam(CameraParameter.OffsetY, Convert.ToInt32(NV("y")));
                    float gain;
                    float.TryParse(NV("gain"), out gain);
                    XimeaSource.SetParam(CameraParameter.Gain, gain);
                    float exp;
                    float.TryParse(NV("exposure"), out exp);
                    XimeaSource.SetParam(CameraParameter.Exposure, exp*1000);
                    XimeaSource.SetParam(CameraParameter.Downsampling, Convert.ToInt32(NV("downsampling")));
                    XimeaSource.SetParam(CameraParameter.Width, Convert.ToInt32(NV("width")));
                    XimeaSource.SetParam(CameraParameter.Height, Convert.ToInt32(NV("height")));
                    XimeaSource.FrameInterval =
                        (int) (1000.0f/XimeaSource.GetParamFloat(CameraParameter.FramerateMax));
                }

                Camobject.settings.active = true;


                if (File.Exists(Camobject.settings.maskimage))
                {
                    Camera.Mask = System.Drawing.Image.FromFile(Camobject.settings.maskimage);
                }

                UpdateFloorplans(false);
            }
            _recordingTime = 0;
            _timeLapseTotal = _timeLapseFrameCount = 0;
            InactiveRecord = 0;
            MovementDetected = false;
            VideoSourceErrorState = false;
            VideoSourceErrorMessage = "";
            Alerted = false;
            PTZNavigate = false;
            Camobject.ftp.ready = true;
            _lastRun = DateTime.Now.Ticks;
            MainForm.NeedsSync = true;
            ReconnectCount = 0;
            _dtPTZLastCheck = DateTime.Now;
            _movementLastDetected = DateTime.MinValue;
            if (Camera != null)
            {
                Camera.ZFactor = 1;
                Camera.ZPoint = Point.Empty;
            }
            Invalidate();

            if (_videoBuffer != null)
            {
                _videoBuffer.Clear();
            }

            if (VolumeControl != null)
                VolumeControl.Enable();
            _processing = false;
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
            if (TopLevelControl != null && Camobject.settings.micpair==-1)
            {
                var vl = ((MainForm)TopLevelControl).AddCameraMicrophone(Camobject.id, Camobject.name + " mic");
                Camobject.settings.micpair = vl.Micobject.id;

                var m = vl.Micobject;
                if (m != null)
                {
                    if (Camera.VideoSource is VlcStream)
                    {
                        var s = ((VlcStream)Camera.VideoSource);
                        m.settings.samples = s.RecordingFormat.SampleRate;
                        m.settings.channels = s.RecordingFormat.Channels;
                    }
                    if (Camera.VideoSource is FFMPEGStream)
                    {
                        var s = ((FFMPEGStream)Camera.VideoSource);
                        m.settings.samples = s.RecordingFormat.SampleRate;
                        m.settings.channels = s.RecordingFormat.Channels;
                    }
                    if (Camera.VideoSource is KinectStream)
                    {
                        var s = ((KinectStream)Camera.VideoSource);
                        m.settings.samples = s.RecordingFormat.SampleRate;
                        m.settings.channels = s.RecordingFormat.Channels;
                    }
                    if (Camera.VideoSource is KinectNetworkStream)
                    {
                        var s = ((KinectNetworkStream)Camera.VideoSource);
                        m.settings.samples = s.RecordingFormat.SampleRate;
                        m.settings.channels = s.RecordingFormat.Channels;
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

        public string NV(string name)
        {
            if (String.IsNullOrEmpty(Camobject.settings.namevaluesettings))
                return "";
            name = name.ToLower().Trim();
            string[] settings = Camobject.settings.namevaluesettings.Split(',');
            foreach (string[] nv in settings.Select(s => s.Split('=')).Where(nv => nv[0].ToLower().Trim() == name))
            {
                return nv[1];
            }
            return "";
        }

        private static int CalcBitRate(int q)
        {
            return 8000*( Convert.ToInt32(Math.Pow(2, (q-1))));
        }

        public void UpdateFloorplans(bool isAlert)
        {
            foreach (
                objectsFloorplan ofp in
                    MainForm.FloorPlans.Where(
                        p => p.objects.@object.Where(q => q.type == "camera" && q.id == Camobject.id).Count() > 0).
                        ToList())
            {
                ofp.needsupdate = true;
                if (isAlert && TopLevelControl!=null)
                {
                    FloorPlanControl fpc = ((MainForm) TopLevelControl).GetFloorPlan(ofp.id);
                    fpc.LastAlertTimestamp = DateTime.Now.UnixTicks();
                    fpc.LastOid = Camobject.id;
                    fpc.LastOtid = 2;
                }
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
            StopSaving();
            var vc = VolumeControl;
            if (vc != null && vc.Micobject.settings.active)
            {
                vc.ForcedRecording = false;
                vc.StopSaving();
            }
           
            return "notrecording," + LocRm.GetString("RecordingStopped");            
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
            if (source is VlcStream)
            {
                ((VlcStream) source).FormatWidth = Camobject.settings.desktopresizewidth;
                ((VlcStream) source).FormatHeight = Camobject.settings.desktopresizeheight;
            }
            Camera = new Camera(source);
            if (source is FFMPEGStream)
            {
                ((FFMPEGStream)source).HasAudioStream += VideoSourceHasAudioStream;
            }
            if (source is VlcStream)
            {
                ((VlcStream)source).HasAudioStream += VideoSourceHasAudioStream;
            }
            if (source is KinectStream)
            {
                ((KinectStream)source).HasAudioStream += VideoSourceHasAudioStream;
            }
            if (source is KinectNetworkStream)
            {
                ((KinectNetworkStream)source).HasAudioStream += VideoSourceHasAudioStream;
                ((KinectNetworkStream)source).AlertHandler += CameraWindow_AlertHandler;
            }
            source.PlayingFinished += SourcePlayingFinished;
            source.VideoSourceError += SourceVideoSourceError;
            return;
        }

        void CameraWindow_AlertHandler(object sender, AlertEventArgs eventArgs)
        {
            if (Camera.Plugin != null)
            {
                var a = (String) Camera.Plugin.GetType().GetMethod("ProcessAlert").Invoke(Camera.Plugin, new object[] { eventArgs.Description });
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
                            }
                        }
                    }
                }
            }
            
        }

        public void StopSaving()
        {
            _stopWrite = true;
            if (_recordingThread != null)
                _recordingThread.Join(2000);
            if (_recordingThread!=null && _recordingThread.Join(0))
            {
                _recordingThread.Abort();
                _recordingThread.Join(2000);
                _stopWrite = false;
                if (Writer != null)
                {
                    Program.WriterMutex.WaitOne();
                    try
                    {

                        Writer.Close();
                    }
                    catch (Exception ex)
                    {
                        MainForm.LogExceptionToFile(ex);
                    }
                    finally
                    {
                        Program.WriterMutex.ReleaseMutex();
                    }
                    Writer = null;
                }
                
                _writerBuffer = null;
                _recordingTime = 0;
                var vc = VolumeControl;
                if (vc != null && vc.Micobject.settings.active)
                    vc.StopSaving();
            }
        }

        public void ApplySchedule()
        {
            //find most recent schedule entry
            if (!Camobject.schedule.active || Camobject.schedule == null || Camobject.schedule.entries == null ||
                Camobject.schedule.entries.Count() == 0)
                return;

            DateTime dNow = DateTime.Now;
            TimeSpan shortest = TimeSpan.MaxValue;
            objectsCameraScheduleEntry mostrecent = null;
            bool isstart = true;

            for (int index = 0; index < Camobject.schedule.entries.Length; index++)
            {
                objectsCameraScheduleEntry entry = Camobject.schedule.entries[index];
                string[] dows = entry.daysofweek.Split(',');
                foreach (int dow in dows.Select(dayofweek => Convert.ToInt32(dayofweek)))
                {
                    //when did this last fire?
                    if (entry.start.IndexOf("-") == -1)
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
                    if (entry.stop.IndexOf("-") == -1)
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
            this.SuspendLayout();
            // 
            // CameraWindow
            // 
            this.BackColor = System.Drawing.Color.Black;
            this.Cursor = System.Windows.Forms.Cursors.Hand;
            this.MinimumSize = new System.Drawing.Size(160, 120);
            this.Size = new System.Drawing.Size(160, 120);
            this.Resize += this.CameraWindowResize;
            this.ResumeLayout(false);
        }

        #endregion

        #region Nested type: FrameAction

        private struct FrameAction
        {
            public readonly byte[] Frame;
            public readonly double MotionLevel;
            public readonly DateTime Timestamp;

            public FrameAction(Bitmap frame, double motionLevel, DateTime timeStamp)
            {
                using (var ms = new MemoryStream())
                {
                    using (frame)
                    {
                        frame.Save(ms, MainForm.Encoder, MainForm.EncoderParams);
                    }

                    Frame = ms.GetBuffer();
                    ms.Close();
                }
                frame = null;
                MotionLevel = motionLevel;
                Timestamp = timeStamp;
            }

        }
        #endregion
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