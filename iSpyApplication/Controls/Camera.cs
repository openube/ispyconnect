using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Video;
using AForge.Vision.Motion;
using Image = System.Drawing.Image;
using Point = System.Drawing.Point;

namespace iSpyApplication.Controls
{
    /// <summary>
    /// Camera class
    /// </summary>
    public class Camera
    {
        public CameraWindow CW;

        public volatile bool LastFrameNull = true;
        public bool MotionDetected;
        public float MotionLevel;

        public Rectangle[] MotionZoneRectangles;
        public IVideoSource VideoSource;
        public double Framerate;
        public double RealFramerate;
        public UnmanagedImage LastFrameUnmanaged;
        private Queue<double> _framerates;
        private Queue<double> _realframerates;
        private HSLFiltering _filter;
        private volatile bool _requestedToStop;
        private readonly object _sync = new object();
        private MotionDetector _motionDetector;
        private int _processFrameCount;

        // alarm level
        private double _alarmLevel = 0.0005;
        private int _height = -1;
        private DateTime _lastframeProcessed = DateTime.MinValue;
        private DateTime _lastframeEvent = DateTime.MinValue;

        private int _width = -1;

        //digital controls
        public float ZFactor = 1;
        public Point ZPoint = Point.Empty;

        public HSLFiltering Filter
        {
            get
            {
                if (CW.Camobject.detector.colourprocessingenabled)
                {
                    if (_filter != null)
                        return _filter;
                    if (!String.IsNullOrEmpty(CW.Camobject.detector.colourprocessing))
                    {
                        string[] config = CW.Camobject.detector.colourprocessing.Split(CW.Camobject.detector.colourprocessing.IndexOf("|") != -1 ? '|' : ',');
                        _filter = new HSLFiltering
                                      {
                                          FillColor =
                                              new HSL(Convert.ToInt32(config[2]), float.Parse(config[5]),
                                                      float.Parse(config[8])),
                                          FillOutsideRange = Convert.ToInt32(config[9])==0,
                                          Hue = new IntRange(Convert.ToInt32(config[0]), Convert.ToInt32(config[1])),
                                          Saturation = new Range(float.Parse(config[3]), float.Parse(config[4])),
                                          Luminance = new Range(float.Parse(config[6]), float.Parse(config[7])),
                                          UpdateHue = Convert.ToBoolean(config[10]),
                                          UpdateSaturation = Convert.ToBoolean(config[11]),
                                          UpdateLuminance = Convert.ToBoolean(config[12])

                        };
                    }
                    
                }

                return null;
            }
        }

        internal Rectangle ViewRectangle
        {
            get
            {
                int newWidth = Convert.ToInt32(Width / ZFactor);
                int newHeight = Convert.ToInt32(Height / ZFactor);

                int left = ZPoint.X - newWidth / 2;
                int top = ZPoint.Y - newHeight / 2;
                int right = ZPoint.X + newWidth / 2;
                int bot = ZPoint.Y + newHeight / 2;

                if (left < 0)
                {
                    right += (0 - left);
                    left = 0;
                }
                if (right > Width)
                {
                    left -= (right - Width);
                    right = Width;
                }
                if (top < 0)
                {
                    bot += (0 - top);
                    top = 0;
                }
                if (bot > Height)
                {
                    top -= (bot - Height);
                    bot = Height;
                }

                ZPoint.X = left + (right - left) / 2;
                ZPoint.Y = top + (bot - top) / 2;

                return new Rectangle(left, top, right - left, bot - top);
            }
        }

        private object _plugin;
        public object Plugin
        {
            get
            {
                if (_plugin == null)
                {
                    foreach (string p in MainForm.Plugins)
                    {
                        if (p.EndsWith("\\" + CW.Camobject.alerts.mode + ".dll", StringComparison.CurrentCultureIgnoreCase))
                        {
                            Assembly ass = Assembly.LoadFrom(p);
                            Plugin = ass.CreateInstance("Plugins.Main", true);
                            if (_plugin != null)
                            {
                                try
                                {
                                    try
                                    {
                                        _plugin.GetType().GetProperty("VideoSource").SetValue(_plugin, CW.Camobject.settings.videosourcestring, null);
                                    }
                                    catch { }

                                    _plugin.GetType().GetProperty("Configuration").SetValue(_plugin,CW.Camobject.alerts.pluginconfig,null);
                                    try
                                    {
                                        //used for plugins that store their configuration elsewhere
                                        _plugin.GetType().GetMethod("LoadConfiguration").Invoke(_plugin, null);
                                    }
                                    catch { }
                                    
                                    try
                                    {
                                        //used for network kinect setting syncing
                                        string dl = "";
                                        foreach(var oc in MainForm.Cameras)
                                        {
                                            string s = oc.settings.namevaluesettings;
                                            if (!String.IsNullOrEmpty(s))
                                            {
                                                if (s.ToLower().IndexOf("kinect")!=-1)
                                                {
                                                    dl += oc.name.Replace("*","").Replace("|","") + "|" + oc.id + "|" + oc.settings.videosourcestring + "*";
                                                }
                                            }
                                        }
                                        if (dl!="")
                                            _plugin.GetType().GetProperty("DeviceList").SetValue(_plugin, dl, null);
                                    }
                                    catch { }
                                    
                                    
                                    try
                                    {
                                        _plugin.GetType().GetProperty("CameraName").SetValue(_plugin, CW.Camobject.name, null);
                                    }
                                    catch { }
                                }
                                catch (Exception)
                                {
                                    //config corrupted
                                    MainForm.LogErrorToFile("Error configuring plugin - trying with a blank configuration");
                                    CW.Camobject.alerts.pluginconfig = "";
                                    _plugin.GetType().GetProperty("Configuration").SetValue(_plugin,
                                                                                            CW.Camobject.alerts.
                                                                                                pluginconfig,
                                                                                            null);
                                }
                            }
                            break;
                        }
                    }
                }
                return _plugin;
            }
            set
            {
                if (_plugin != null)
                {
                    try {_plugin.GetType().GetMethod("Dispose").Invoke(_plugin, null);} catch {}
                }
                _plugin = value;
            }
        }

        public void FilterChanged()
        {
            lock (_sync)
            {
                _filter = null;
            }

        }
        
        public Camera() : this(null, null)
        {
        }

        public Camera(IVideoSource source)
        {
            VideoSource = source;
            _motionDetector = null;
            VideoSource.NewFrame += VideoNewFrame;
        }

        public Camera(IVideoSource source, MotionDetector detector)
        {
            //VideoSource = new AsyncVideoSource(source, false);
            VideoSource = source;
            _motionDetector = detector;
            VideoSource.NewFrame += VideoNewFrame;
        }

        public Bitmap LastFrame
        {
            get
            {
                Bitmap bm = null;
                lock (_sync)
                {
                    if (LastFrameUnmanaged != null)
                    {
                        try
                        {
                            bm = LastFrameUnmanaged.ToManagedImage();
                            LastFrameNull = false;
                        }
                        catch
                        {
                            if (bm != null)
                            {
                                bm.Dispose();
                                bm = null;
                            }
                            LastFrameNull = true;
                        }
                    }
                }
                return bm;
            }
        }

        // Running propert
        public bool IsRunning
        {
            get { return (VideoSource == null) ? false : VideoSource.IsRunning; }
        }

        //


        // Width property
        public int Width
        {
            get { return _width; }
        }

        // Height property
        public int Height
        {
            get { return _height; }
        }

        // AlarmLevel property
        public double AlarmLevel
        {
            get { return _alarmLevel; }
            set { _alarmLevel = value; }
        }

        // FramesReceived property
        public int FramesReceived
        {
            get { return (VideoSource == null) ? 0 : VideoSource.FramesReceived; }
        }

        // BytesReceived property
        public long BytesReceived
        {
            get { return (VideoSource == null) ? 0 : VideoSource.BytesReceived; }
        }

        // motionDetector property
        public MotionDetector MotionDetector
        {
            get { return _motionDetector; }
            set
            {
                _motionDetector = value;
                if (value != null) _motionDetector.MotionZones = MotionZoneRectangles;
            }
        }

        public Image Mask { get; set; }

        public bool SetMotionZones(objectsCameraDetectorZone[] zones)
        {
            if (zones == null || zones.Length == 0)
            {
                ClearMotionZones();
                return true;
            }
            //rectangles come in as percentages to allow resizing and resolution changes

            if (_width > -1)
            {
                double wmulti = Convert.ToDouble(_width)/Convert.ToDouble(100);
                double hmulti = Convert.ToDouble(_height)/Convert.ToDouble(100);
                MotionZoneRectangles = zones.Select(r => new Rectangle(Convert.ToInt32(r.left*wmulti), Convert.ToInt32(r.top*hmulti), Convert.ToInt32(r.width*wmulti), Convert.ToInt32(r.height*hmulti))).ToArray();
                if (_motionDetector != null)
                    _motionDetector.MotionZones = MotionZoneRectangles;
                return true;
            }
            return false;
        }

        public void ClearMotionZones()
        {
            MotionZoneRectangles = null;
            if (_motionDetector != null)
                _motionDetector.MotionZones = null;
        }

        public event EventHandler NewFrame;
        public event EventHandler Alarm;

        // Constructor

        // Start video source
        public void Start()
        {
            if (VideoSource != null)
            {
                lock (_sync)
                {
                    _requestedToStop = false;
                    _framerates = new Queue<double>();
                    _realframerates = new Queue<double>();
                    _lastframeProcessed = DateTime.MinValue;
                    _lastframeEvent = DateTime.MinValue;
                    VideoSource.Start();
                }
            }
        }

        // Signal video source to stop
        public void SignalToStop()
        {
            lock (_sync)
            {
                if (VideoSource != null)
                {
                    _requestedToStop = true;
                    VideoSource.SignalToStop();
                }
            }
        }


        // Abort camera
        public void Stop()
        {
            // lock
            lock (_sync)
            {
                if (VideoSource != null)
                {
                    _requestedToStop = true;
                    VideoSource.Stop();
                }
                // unlock
            }

        }

        private double Mininterval
        {
            get
            {
                return 1000d/MaxFramerate;
            }
        }

        private double MaxFramerate
        {
            get
            {
                var ret = CW.Camobject.settings.maxframerate;
                if (CW.Recording)
                    ret = CW.Camobject.settings.maxframeraterecord;


                if (MainForm.ThrottleFramerate < ret)
                    ret = MainForm.ThrottleFramerate;
                return ret;
            }
        }

        private DateTime _motionlastdetected = DateTime.MinValue;


        //private double _dRealFrameCounter = 1;
        //private double _dPresentationFrameCounter = 1;

        private void VideoNewFrame(object sender, NewFrameEventArgs e)
        {
            if (_requestedToStop || NewFrame==null)
                return;
    
            
            if (_lastframeEvent > DateTime.MinValue)
            {
                //discard this frame to limit framerate? - this is a hack, not the best method but adaptive framerate limiting is intensive
                if ((DateTime.Now - _lastframeProcessed).TotalMilliseconds < Mininterval)
                {
                    //Console.WriteLine("skipped: " + (DateTime.Now - _lastframeProcessed).TotalMilliseconds);
                    return;
                }

                TimeSpan tsFr = DateTime.Now - _lastframeProcessed;
                _framerates.Enqueue(1000d / tsFr.TotalMilliseconds);
                if (_framerates.Count >= 10)
                    _framerates.Dequeue();
                Framerate = _framerates.Average();


                //framerate of live stream
                //var tsFr = DateTime.Now - _lastframeEvent;
                //_realframerates.Enqueue(1000d / tsFr.TotalMilliseconds);
                //while (_realframerates.Count >30)
                //    _realframerates.Dequeue();
                //RealFramerate = _realframerates.Average();

                //_lastframeEvent = DateTime.Now;
                ////15 fps
                //_dRealFrameCounter++;
                //if (_dRealFrameCounter > RealFramerate)
                //    _dRealFrameCounter = 1;


                //if (_dRealFrameCounter / RealFramerate < _dPresentationFrameCounter / MaxFramerate)
                //{
                //    return;
                //}

                //_dPresentationFrameCounter++;
                //if (_dPresentationFrameCounter >= 1000d / Mininterval)
                //    _dPresentationFrameCounter = 1d;

                //tsFr = DateTime.Now - _lastframeProcessed;
                //_framerates.Enqueue(1000d / tsFr.TotalMilliseconds);
                //while (_framerates.Count > 30)
                //    _framerates.Dequeue();
                //Framerate = _framerates.Average();
            }
            else
            {
                _lastframeEvent = DateTime.Now;
            }
            lock (_sync)
            {
                _lastframeProcessed = DateTime.Now;

                var tsBrush = new SolidBrush(MainForm.Conf.TimestampColor.ToColor());
                var sbTs = new SolidBrush(Color.FromArgb(128, 0, 0, 0));
                Bitmap bmOrig = null, bmp = null;
                Graphics g = null, gCam = null;
                bool err = false;
                try
                {
                    // motionLevel = 0;
                    if (e.Frame != null)
                    {

                        if (LastFrameUnmanaged != null)
                            LastFrameUnmanaged.Dispose();

                        //resize?
                        if (CW.Camobject.settings.resize && (CW.Camobject.settings.desktopresizewidth != e.Frame.Width || CW.Camobject.settings.desktopresizeheight != e.Frame.Height))
                        {
                            var result = new Bitmap(CW.Camobject.settings.desktopresizewidth,CW.Camobject.settings.desktopresizeheight, PixelFormat.Format24bppRgb);

                            using (Graphics g2 = Graphics.FromImage(result))
                                g2.DrawImage(e.Frame, 0, 0, result.Width, result.Height);
                            e.Frame.Dispose();
                            bmOrig = result;
                        }
                        else
                        {
                            bmOrig = e.Frame;
                        }

                        if (CW.Camobject.rotate90)
                            bmOrig.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        if (CW.Camobject.flipx)
                        {
                            bmOrig.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        }
                        if (CW.Camobject.flipy)
                        {
                            bmOrig.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        }


                        _width = bmOrig.Width;
                        _height = bmOrig.Height;

                        if (CW.NeedMotionZones)
                            CW.NeedMotionZones = !SetMotionZones(CW.Camobject.detector.motionzones);

                        if (Mask != null)
                        {
                            g = Graphics.FromImage(bmOrig);
                            g.DrawImage(Mask, 0, 0, _width, _height);
                        }

                        if (Plugin != null)
                        {
                            bool runplugin = true;
                            if (CW.Camobject.alerts.processmode == "motion")
                            {
                                //run plugin if motion detected in last 5 seconds
                                runplugin = _motionlastdetected > DateTime.Now.AddSeconds(-5);
                            }
                            if (runplugin)
                            {
                                bmOrig =
                                    (Bitmap)
                                    Plugin.GetType().GetMethod("ProcessFrame").Invoke(Plugin, new object[] {bmOrig});
                                var pluginAlert = (String) Plugin.GetType().GetField("Alert").GetValue(Plugin);
                                if (pluginAlert != "")
                                    Alarm(pluginAlert, EventArgs.Empty);
                            }
                        }

                        LastFrameUnmanaged = UnmanagedImage.FromManagedImage(bmOrig);

                        if (_motionDetector != null)
                        {
                            if (Alarm != null)
                            {
                                _processFrameCount++;
                                if (_processFrameCount >= CW.Camobject.detector.processeveryframe || CW.Calibrating)
                                {
                                    _processFrameCount = 0;
                                    MotionLevel = _motionDetector.ProcessFrame(LastFrameUnmanaged, Filter);
                                    if (MotionLevel >= _alarmLevel)
                                    {
                                        MotionDetected = true;
                                        _motionlastdetected = DateTime.Now;
                                        Alarm(this, new EventArgs());
                                    }
                                    else
                                        MotionDetected = false;
                                }
                            }
                            else
                                MotionDetected = false;
                        }
                        else
                        {
                            MotionDetected = false;
                        }

                        if (ZFactor > 1)
                        {
                            var f1 = new ResizeNearestNeighbor(LastFrameUnmanaged.Width, LastFrameUnmanaged.Height);
                            var f2 = new Crop(ViewRectangle);
                            LastFrameUnmanaged = f2.Apply(LastFrameUnmanaged);
                            LastFrameUnmanaged = f1.Apply(LastFrameUnmanaged);
                        }



                        if (CW.Camobject.settings.timestamplocation != 0 &&
                            CW.Camobject.settings.timestampformatter != "")
                        {
                            bmp = LastFrameUnmanaged.ToManagedImage();
                            gCam = Graphics.FromImage(bmp);


                            var ts = CW.Camobject.settings.timestampformatter.Replace("{FPS}",
                                                                                      string.Format("{0:F2}", Framerate));
                            ts = ts.Replace("{CAMERA}", CW.Camobject.name);
                            ts = ts.Replace("{REC}", CW.Recording ? "REC" : "");

                            var timestamp = "Invalid Timestamp";
                            try
                            {
                                timestamp = String.Format(ts,
                                              DateTime.Now.AddHours(
                                                  Convert.ToDouble(CW.Camobject.settings.timestampoffset))).Trim();
                            }
                            catch
                            {
                                
                            }

                            var rs = gCam.MeasureString(timestamp, Drawfont).ToSize();
                            var p = new Point(0, 0);
                            switch (CW.Camobject.settings.timestamplocation)
                            {
                                case 2:
                                    p.X = _width/2 - (rs.Width/2);
                                    break;
                                case 3:
                                    p.X = _width - rs.Width;
                                    break;
                                case 4:
                                    p.Y = _height - rs.Height;
                                    break;
                                case 5:
                                    p.Y = _height - rs.Height;
                                    p.X = _width/2 - (rs.Width/2);
                                    break;
                                case 6:
                                    p.Y = _height - rs.Height;
                                    p.X = _width - rs.Width;
                                    break;
                            }
                            var rect = new Rectangle(p, rs);

                            gCam.FillRectangle(sbTs, rect);
                            gCam.DrawString(timestamp, Drawfont, tsBrush, p);

                            LastFrameUnmanaged.Dispose();
                            LastFrameUnmanaged = UnmanagedImage.FromManagedImage(bmp);
                        }
                    }
                }
                catch (UnsupportedImageFormatException ex)
                {
                    CW.VideoSourceErrorState = true;
                    CW.VideoSourceErrorMessage = ex.Message;
                    if (LastFrameUnmanaged != null)
                    {
                        try
                        {
                            lock (_sync)
                            {
                                LastFrameUnmanaged.Dispose();
                                LastFrameUnmanaged = null;
                            }
                        }
                        catch
                        {
                        }
                    }
                    err = true;
                }
                catch (Exception ex)
                {
                    if (LastFrameUnmanaged != null)
                    {
                        try
                        {
                            lock (_sync)
                            {
                                LastFrameUnmanaged.Dispose();
                                LastFrameUnmanaged = null;
                            }
                        }
                        catch
                        {
                        }
                    }
                    MainForm.LogExceptionToFile(ex);
                    err = true;
                }

                if (gCam != null)
                    gCam.Dispose();
                if (bmp != null)
                    bmp.Dispose();
                if (g != null)
                    g.Dispose();
                if (bmOrig != null)
                    bmOrig.Dispose();
                tsBrush.Dispose();
                sbTs.Dispose();

                if (err)
                    return;

                if (MotionDetector != null && !CW.Calibrating &&
                    MotionDetector.MotionProcessingAlgorithm is BlobCountingObjectsProcessing)
                {
                    try
                    {
                        var blobcounter =
                            (BlobCountingObjectsProcessing) MotionDetector.MotionProcessingAlgorithm;

                        //tracking
                        var pCenter = new Point(Width/2, Height/2);
                        if (!CW.PTZNavigate && CW.Camobject.settings.ptzautotrack && blobcounter.ObjectsCount > 0 &&
                            blobcounter.ObjectsCount < 4 && !CW.Ptzneedsstop)
                        {
                            List<Rectangle> recs =
                                blobcounter.ObjectRectangles.OrderByDescending(p => p.Width*p.Height).ToList();
                            Rectangle rec = recs.First();
                            //get center point
                            var prec = new Point(rec.X + rec.Width/2, rec.Y + rec.Height/2);

                            double dratiomin = 0.6;
                            prec.X = prec.X - pCenter.X;
                            prec.Y = prec.Y - pCenter.Y;

                            if (CW.Camobject.settings.ptzautotrackmode == 1) //vert only
                            {
                                prec.X = 0;
                                dratiomin = 0.3;
                            }

                            if (CW.Camobject.settings.ptzautotrackmode == 2) //horiz only
                            {
                                prec.Y = 0;
                                dratiomin = 0.3;
                            }

                            double angle = Math.Atan2(-prec.Y, -prec.X);
                            if (CW.Camobject.settings.ptzautotrackreverse)
                            {
                                angle = angle - Math.PI;
                                if (angle < 0 - Math.PI)
                                    angle += 2*Math.PI;
                            }
                            double dist = Math.Sqrt(Math.Pow(prec.X, 2.0d) + Math.Pow(prec.Y, 2.0d));

                            double maxdist = Math.Sqrt(Math.Pow(Width/2, 2.0d) + Math.Pow(Height/2, 2.0d));
                            double dratio = dist/maxdist;

                            if (dratio > dratiomin)
                            {
                                CW.PTZ.SendPTZDirection(angle, 1);
                                CW.LastAutoTrackSent = DateTime.Now;
                                CW.Ptzneedsstop = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MainForm.LogExceptionToFile(ex);
                    }
                }

                if (NewFrame != null && LastFrameUnmanaged != null)
                {
                    LastFrameNull = false;
                    NewFrame(this, new EventArgs());
                }
            }
            
        }

        private Font _drawfont;
        public Font Drawfont
        {
            get
            {
                if (_drawfont!=null)
                    return _drawfont;
                _drawfont = new Font(MainForm.Drawfont.FontFamily,CW.Camobject.settings.timestampfontsize);
                return _drawfont;
            }
            set { _drawfont = value; }
        }
    }
}