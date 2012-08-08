using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using iSpyServer.Video;
using PictureBox = AForge.Controls.PictureBox;

namespace iSpyServer
{
    /// <summary>
    /// Summary description for CameraWindow.
    /// </summary>
    public sealed class CameraWindow : PictureBox
    {
        #region Private

        private Camera _camera;
        private int _frameCount;
        public double Framerate;

        private long _lastRun = DateTime.Now.Ticks;
        private int _milliCount;
        private bool _processing;

        #endregion

        #region Public

        #region Delegates

        public delegate void RemoteCommandEventHandler(object sender, ThreadSafeCommand e);

        #endregion

        public Graphics CurrentFrame;
        public bool IsEdit;

        public bool NeedSizeUpdate;
        public bool ResizeParent;
        public bool ShuttingDown;
        public string VideoSourceErrorMessage = "";
        public bool VideoSourceErrorState;
        public objectsCamera Camobject;
        public List<Socket> OutSockets = new List<Socket>();

        #endregion

        #region SizingControls

        public void UpdatePosition()
        {
            Monitor.Enter(this);

            if (Parent != null && _camera != null)
            {
                int width = 320;
                int height = 240;
                if (!_camera.LastFrameNull)
                {
                    width = _camera.Width;
                    height = _camera.Height;
                    Camobject.resolution = width + "x" + height;
                }

                SuspendLayout();
                Size = new Size(width + 2, height + 26);
                Camobject.width = width;
                Camobject.height = height;
                ResumeLayout();
                NeedSizeUpdate = false;
            }
            Monitor.Exit(this);
        }

        private void CameraWindowResize(object sender, EventArgs e)
        {
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
                foreach (string dayofweek in dows)
                {
                    int dow = Convert.ToInt32(dayofweek);
                    //when did this last fire?
                    if (entry.start.IndexOf("-") == -1)
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
                    Camobject.alerts.active = mostrecent.alerts;
                    if (!Camobject.settings.active)
                        Enable();
                }
                else
                {
                    if (Camobject.settings.active)
                        Disable();
                }
            }
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
            BackColor = iSpyServer.Default.BackColor;
            Camobject = cam;
        }

        public Camera Camera
        {
            get { return _camera; }
            set
            {
                Monitor.Enter(this);
                bool newCamera = (value != _camera && value != null);

                if (value == null && _camera != null)
                {
                    Disable();
                }

                _camera = value;
                if (_camera != null)
                {
                    _camera.CW = this;

                    if (newCamera)
                    {
                        _camera.NewFrame += CameraNewFrame;
                    }
                }

                Monitor.Exit(this);
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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            IntPtr hwnd = Handle;
            if (ResizeParent && Parent != null && Parent.IsHandleCreated)
            {
                hwnd = Parent.Handle;
            }
            MousePos mousePos = GetMousePos(e.Location);
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
                        NativeCalls.SendMessage(hwnd, NativeCalls.WmSyscommand, NativeCalls.ScDragsizeSe, IntPtr.Zero);
                    }
                    break;
               case MousePos.NoWhere:
                    {
                        if (e.Location.X>0 && e.Location.Y>Height-22)
                        {
                            MessageBox.Show("Add a new MJPEG source to iSpy and use the address below to connect:\n\nhttp://" + MainForm.AddressIPv4 + ":" + iSpyServer.Default.LANPort +"/?camid=" + Camobject.id);
                        }
                    }
                   
                    break;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
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
                    break;
            }
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
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Cursor = Cursors.Default;
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

                if (_frameCount > 0)
                {
                    Framerate = (_frameCount*1.0)/((_milliCount*1.0)/1000);
                    _frameCount = 0;
                    _milliCount = 0;
                }

                if (_frameCount > 1000)
                    _frameCount = 0;


                if (_secondCount > 1) //every second
                {
                    if (Camobject.schedule.active)
                    {
                        DateTime dtnow = DateTime.Now;
                        foreach (objectsCameraScheduleEntry entry in Camobject.schedule.entries.Where(p => p.active))
                        {
                            if (entry.daysofweek.IndexOf(((int) dtnow.DayOfWeek).ToString()) != -1)
                            {
                                if (Camobject.settings.active)
                                {
                                    string[] stop = entry.stop.Split(':');
                                    if (stop[0] != "-")
                                    {
                                        if (Convert.ToInt32(stop[0]) == dtnow.Hour)
                                        {
                                            if (Convert.ToInt32(stop[1]) == dtnow.Minute && dtnow.Second < 10)
                                            {
                                                Disable();
                                                goto skip;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    string[] start = entry.start.Split(':');
                                    if (start[0] != "-")
                                    {
                                        if (Convert.ToInt32(start[0]) == dtnow.Hour)
                                        {
                                            if (Convert.ToInt32(start[1]) == dtnow.Minute && dtnow.Second < 10)
                                            {
                                                Enable();
                                                Camobject.detector.recordondetect = entry.recordondetect;
                                                Camobject.alerts.active = entry.alerts;
                                                goto skip;
                                            }
                                        }
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
            var drawFont = new Font(FontFamily.GenericSansSerif, 9);
            var grabBrush = new SolidBrush(Color.DarkGray);
            if (Camobject.newrecordingcount > 0)
                grabBrush.Color = Color.Yellow;
            var borderPen = new Pen(grabBrush);
            var drawBrush = new SolidBrush(Color.White);
            Bitmap bm = null;
            var recBrush = new SolidBrush(Color.Red);
            string url = "http://" + MainForm.AddressIPv4 + ":" + iSpyServer.Default.LANPort + "/?camid=" + Camobject.id;
            try
            {
                Rectangle rc = ClientRectangle;
                gCam.DrawRectangle(borderPen, 0, 0, rc.Width - 1, rc.Height - 1);
                var grabPoints = new[]
                                     {
                                         new Point(rc.Width - 15, rc.Height), new Point(rc.Width, rc.Height - 15),
                                         new Point(rc.Width, rc.Height)
                                     };
                int textpos = rc.Height - 20;


                gCam.FillPolygon(grabBrush, grabPoints);

                bool message = false;

                if (Camobject.settings.active)
                {
                    if (_camera != null && !_camera.LastFrameNull)
                    {
                        bm = _camera.LastFrame;
                        gCam.DrawImage(bm, rc.X + 1, rc.Y + 1, rc.Width - 2, rc.Height - 26);


                        VideoSourceErrorState = false;
                    }
                    else
                    {
                        if (VideoSourceErrorState)
                        {
                            gCam.DrawString(
                                VideoSourceErrorMessage,
                                drawFont, drawBrush, new PointF(5, 5));
                            gCam.DrawString(
                                LocRM.GetString("Error") + ": " + Camobject.name,
                                drawFont, drawBrush, new PointF(5, textpos));
                            message = true;
                        }
                        else
                        {
                            gCam.DrawString(
                                LocRM.GetString("Connecting") + ": " + Camobject.name,
                                drawFont, drawBrush, new PointF(5, textpos));
                            message = true;
                        }
                    }
                }
                else
                {
                    gCam.DrawString(
                        Camobject.schedule.active ? LocRM.GetString("Scheduled") : LocRM.GetString("Offline"),
                        drawFont, drawBrush, new PointF(5, 5));
                    Framerate = 0;
                }

                if (!message && Framerate < 100)
                {
                    string m = "";
                    if (Framerate > 0)
                        m = string.Format("{0:F2}", Framerate) + " FPS, ";
                    gCam.DrawString(m + url,
                                     drawFont, drawBrush, new PointF(5, textpos));
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
            drawFont.Dispose();
            recBrush.Dispose();
            Monitor.Exit(this);

            base.OnPaint(pe);
        }

        private void CameraNewFrame(object sender, EventArgs e)
        {
            if (!Camera.LastFrameNull && OutSockets.Count>0)
            {
                Bitmap b = Camera.LastFrame;
                using (var imageStream = new MemoryStream())
                {
                    b.Save(imageStream, ImageFormat.Jpeg);
                    imageStream.Position = 0;
                    b.Dispose();
                    var imageArray = imageStream.GetBuffer();

                    if (!String.IsNullOrEmpty(Camobject.encodekey))
                    {
                        var marker = Encoding.ASCII.GetBytes(Camobject.encodekey);
                        var rv = new byte[imageArray.Length + marker.Length];
                        var jpegMagic = new byte[] {0xFF, 0xD8, 0xFF};
                        Buffer.BlockCopy(jpegMagic, 0, rv, 0, jpegMagic.Length);
                        Buffer.BlockCopy(marker, 0, rv, jpegMagic.Length, marker.Length);
                        Buffer.BlockCopy(imageArray, jpegMagic.Length, rv, jpegMagic.Length + marker.Length, imageArray.Length - jpegMagic.Length);
                        imageArray = rv;

                    }
                    string sResponse = "\r\n\r\n--myboundary\r\nContent-type: image/jpeg\r\nContent-length: " +
                                       imageArray.Length + "\r\n\r\n";

                    Byte[] bSendData = Encoding.ASCII.GetBytes(sResponse);

                    for (int i = 0; i < OutSockets.Count; i++)
                    {
                        Socket s = OutSockets[i];
                        if (s.Connected)
                        {
                            if (!SendToBrowser(bSendData, s) || !SendToBrowser(imageArray, s))
                            {
                                OutSockets.Remove(s);
                                i--;
                            }
                        }
                        else
                        {
                            OutSockets.Remove(s);
                            i--;
                        }
                    }
                    imageStream.Close();
                }
            }

            try
            {
                _frameCount++;
                Invalidate();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CameraWindow NewFrame Error: " + ex.Message);
            }
        }



        /// <summary>
        /// Sends data to the browser (client)
        /// </summary>
        /// <param name="bSendData">Byte Array</param>
        /// <param name="socket">Socket reference</param>
        private bool SendToBrowser(Byte[] bSendData, Socket socket)
        {
            try
            {
                if (socket.Connected)
                {
                    int sent = socket.Send(bSendData);
                    if (sent < bSendData.Length)
                    {
                        //Debug.WriteLine("Only sent " + sent + " of " + bSendData.Length);
                    }
                    if (sent == -1)
                        return false;
                    return true;
                }
            }
            catch (Exception e)
            {
                //Debug.WriteLine("Send To Browser Error: " + e.Message);
                MainForm.LogExceptionToFile(e);
            }
            return false;
        }

        private void SourceVideoSourceError(object sender, VideoSourceErrorEventArgs eventArgs)
        {
            VideoSourceErrorMessage = eventArgs.Description;
            if (!VideoSourceErrorState)
            {
                VideoSourceErrorState = true;
                MainForm.LogExceptionToFile(new Exception("VideoSourceError: " + eventArgs.Description),
                                            "Camera " + Camobject.id);
                //Monitor.Enter(this);
                _camera.LastFrameNull = true;

                //Monitor.Exit(this);
            }
            if (!ShuttingDown)
                Invalidate();
        }

        private void SourcePlayingFinished(object sender, ReasonToFinishPlaying reason)
        {
            Camobject.settings.active = false;
            Invalidate();
        }

        public void Disable()
        {
            _processing = true;
            Application.DoEvents();

            if (_camera != null)
            {
                _camera.NewFrame -= CameraNewFrame;
                _camera.VideoSource.PlayingFinished -= SourcePlayingFinished;
                _camera.VideoSource.VideoSourceError -= SourceVideoSourceError;

                if (_camera.IsRunning)
                {
                    try
                    {
                        _camera.SignalToStop();
                        if (_camera.VideoSource is VideoCaptureDevice)
                        {
                            //need to make sure removal of this videosource is successful
                            int counter = 0;
                            while (_camera.IsRunning && counter < 2)
                            {
                                Thread.Sleep(500);
                                counter++;
                            }
                            if (_camera.IsRunning)
                                _camera.Stop();
                        }
                    }
                    catch (Exception ex)
                    {
                        MainForm.LogExceptionToFile(ex, "Camera " + Camobject.id);
                    }
                    _camera.VideoSource = null;
                }
                try
                {
                    _camera.LastFrameUnmanaged.Dispose();
                    _camera.LastFrameUnmanaged = null;
                }
                catch
                {
                }
                _camera = null;
                BackColor = iSpyServer.Default.BackColor;
            }
            Camobject.settings.active = false;
            _frameCount = 0;

            foreach (
                objectsFloorplan ofp in
                    MainForm.FloorPlans.Where(
                        p => p.objects.@object.Where(q => q.type == "camera" && q.id == Camobject.id).Count() > 0).
                        ToList())
            {
                ofp.needsupdate = true;
            }
            MainForm.NeedsSync = true;
            if (!ShuttingDown)
                Invalidate();
            GC.Collect();
            _processing = false;
        }

        public void Enable()
        {
            _processing = true;

            switch (Camobject.settings.sourceindex)
            {
                case 0:
                    var jpegSource = new JPEGStream(Camobject.settings.videosourcestring);
                    if (Camobject.settings.frameinterval != 0)
                        jpegSource.FrameInterval = Camobject.settings.frameinterval;
                    if (Camobject.settings.login != "")
                    {
                        jpegSource.Login = Camobject.settings.login;
                        jpegSource.Password = Camobject.settings.password;
                    }
                    //jpegSource.SeparateConnectionGroup = true;
                    jpegSource.RequestTimeout = iSpyServer.Default.IPCameraTimeout;
                    OpenVideoSource(jpegSource, false);
                    break;
                case 1:
                    var mjpegSource = new MJPEGStream(Camobject.settings.videosourcestring)
                                          {
                                              Login = Camobject.settings.login,
                                              Password = Camobject.settings.password,
                                              RequestTimeout = iSpyServer.Default.IPCameraTimeout,
                                              HttpUserAgent = Camobject.settings.useragent
                                          };
                    //mjpegSource.SeparateConnectionGroup = true;
                    OpenVideoSource(mjpegSource, false);
                    break;
                case 2:
                    //var fileSource = new AVIFileVideoSource(Camobject.settings.videosourcestring);
                    //OpenVideoSource(fileSource, true);
                    break;
                case 3:
                    string moniker = Camobject.settings.videosourcestring;

                    var videoSource = new VideoCaptureDevice(moniker);
                    string[] wh = Camobject.resolution.Split('x');
                    videoSource.DesiredFrameSize = new Size(Convert.ToInt32(wh[0]), Convert.ToInt32(wh[1]));
                    videoSource.DesiredFrameRate = Camobject.settings.framerate;

                    var availableVideoInputs = videoSource.AvailableCrossbarVideoInputs;

                    foreach (VideoInput input in availableVideoInputs)
                    {
                        if ((input.Index == Camobject.settings.crossbarindex))
                        {
                            videoSource.CrossbarVideoInput = input;
                            break;
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
                    OpenVideoSource(desktopSource, false);
                    break;
                case 5:
                    var ks = new KinectStream(NV("UniqueKinectId"), Convert.ToBoolean(NV("KinectSkeleton")));
                    OpenVideoSource(ks, true);
                    break;
            }


            if (Camera != null)
            {
                if (!Camera.IsRunning)
                {
                    Camera.Start();
                }

                Camobject.settings.active = true;

                if (File.Exists(Camobject.settings.maskimage))
                {
                    Camera.Mask = System.Drawing.Image.FromFile(Camobject.settings.maskimage);
                }
            }
            _frameCount = 0;
            VideoSourceErrorState = false;
            VideoSourceErrorMessage = "";
            Camobject.ftp.ready = true;
            MainForm.NeedsSync = true;

            Invalidate();
            _lastRun = DateTime.Now.Ticks;
            _processing = false;
        }

        private string NV(string name)
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

        private void OpenVideoSource(IVideoSource source, bool @override)
        {
            if (!@override && Camera != null && Camera.VideoSource != null && Camera.VideoSource.Source == source.Source)
            {
                return;
            }
            if (Camera != null)
            {
                Disable();
            }

            Camera = new Camera(source);
            source.PlayingFinished += SourcePlayingFinished;
            source.VideoSourceError += SourceVideoSourceError;
            return;
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
}