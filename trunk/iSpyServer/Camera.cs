using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using AForge.Imaging;
using AForge.Video;
using Image = System.Drawing.Image;

namespace iSpyServer
{
    /// <summary>
    /// Camera class
    /// </summary>
    public class Camera
    {
        public bool LastFrameNull = true;

        public Rectangle[] MotionZoneRectangles;
        public CameraWindow CW;
        private Image _mask;

        // alarm level
        private int _height = -1;
        public UnmanagedImage LastFrameUnmanaged;
        public IVideoSource VideoSource;
        private int _width = -1;

        public Camera() : this(null)
        {
        }

        public Camera(IVideoSource source)
        {
            VideoSource = source; // new AsyncVideoSource(source);
            VideoSource.NewFrame += VideoNewFrame;
        }

        public Bitmap LastFrame
        {
            get
            {
                Bitmap bm = null;
                lock (this)
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
                                bm.Dispose();
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

        public Image Mask
        {
            get
            {
                if (_mask != null)
                {
                    return _mask;
                }
                return null;
            }
            set { _mask = value; }
        }

        public event EventHandler NewFrame;

        // Constructor

        // Start video source
        public void Start()
        {
            if (VideoSource != null)
            {
                VideoSource.Start();
            }
        }

        // Siganl video source to stop
        public void SignalToStop()
        {
            if (VideoSource != null)
            {
                VideoSource.SignalToStop();
            }
        }

        // Wait video source for stop
        public void WaitForStop()
        {
            // lock
            Monitor.Enter(this);

            if (VideoSource != null)
            {
                VideoSource.WaitForStop();
            }
            // unlock
            Monitor.Exit(this);
        }

        // Abort camera
        public void Stop()
        {
            // lock
            Monitor.Enter(this);

            if (VideoSource != null)
            {
                VideoSource.Stop();
            }
            // unlock
            Monitor.Exit(this);
        }

        // On new frame

        private void VideoNewFrame(object sender, NewFrameEventArgs e)
        {
            var tsBrush = new SolidBrush(iSpyServer.Default.TimestampColor);
            var f = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Regular, GraphicsUnit.Pixel);
            var sbTs = new SolidBrush(Color.FromArgb(128, 0, 0, 0));
            Bitmap bmOrig = null, bmp = null;
            Graphics g = null, gCam = null;
            try
            {
                if (e.Frame != null)
                {
                    _width = e.Frame.Width;
                    _height = e.Frame.Height;

                    lock (this)
                    {
                        if (LastFrameUnmanaged != null)
                            LastFrameUnmanaged.Dispose();


                        if (CW.Camobject.settings.resize && (CW.Camobject.settings.desktopresizewidth != e.Frame.Width || CW.Camobject.settings.desktopresizeheight != e.Frame.Height))
                        {
                            var result = new Bitmap(CW.Camobject.settings.desktopresizewidth, CW.Camobject.settings.desktopresizeheight, PixelFormat.Format24bppRgb);
                            using (Graphics g2 = Graphics.FromImage(result))
                                g2.DrawImage(e.Frame, 0, 0, result.Width, result.Height);
                            e.Frame.Dispose();
                            bmOrig = result;
                        }
                        else
                        {
                            bmOrig = e.Frame;
                        }

                        if (CW.Camobject.flipx)
                        {
                            bmOrig.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        }
                        if (CW.Camobject.flipy)
                        {
                            bmOrig.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        }

                        if (Mask != null)
                        {
                            g = Graphics.FromImage(bmOrig);
                            g.DrawImage(Mask, 0, 0, _width, _height);
                        }


                        LastFrameUnmanaged = UnmanagedImage.FromManagedImage(bmOrig);

                        if (CW.Camobject.settings.timestamplocation != 0 &&
                            CW.Camobject.settings.timestampformatter != "")
                        {
                            bmp = LastFrameUnmanaged.ToManagedImage();
                            gCam = Graphics.FromImage(bmp);

                            string timestamp =
                                String.Format(
                                    CW.Camobject.settings.timestampformatter.Replace("{FPS}",
                                                                                      string.Format("{0:F2}",
                                                                                                    CW.Framerate)),
                                    DateTime.Now);
                            Size rs = gCam.MeasureString(timestamp, f).ToSize();
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
                            gCam.DrawString(timestamp, f, tsBrush, p);

                            LastFrameUnmanaged.Dispose();
                            LastFrameUnmanaged = UnmanagedImage.FromManagedImage(bmp);
                        }
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
                        lock (this)
                        {
                            LastFrameUnmanaged.Dispose();
                            LastFrameUnmanaged = null;
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
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
            f.Dispose();
            sbTs.Dispose();

            if (NewFrame != null && LastFrameUnmanaged != null)
            {
                LastFrameNull = false;
                NewFrame(this, new EventArgs());
            }
        }
    }
}