using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using AForge.Video;
using Microsoft.Kinect;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace iSpyServer
{
    public class KinectStream : IVideoSource
    {
        private readonly Pen _inferredBonePen = new Pen(Brushes.Gray, 1);
        private readonly Pen _trackedBonePen = new Pen(Brushes.Green, 6);
        private readonly Brush _trackedJointBrush = new SolidBrush(Color.FromArgb(255, 68, 192, 68));
        private readonly Brush _inferredJointBrush = Brushes.Yellow;
        private Skeleton[] _skeletons = new Skeleton[0];
        private const int JointThickness = 3;
        private KinectSensor _sensor;
        private readonly bool _skeleton;
        private readonly bool _bound;
        private DateTime _lastFrameTimeStamp = DateTime.Now;

        private const double MaxInterval = 1000d/15;
        
        private long _bytesReceived;
        private int _framesReceived;
        private string _uniqueKinectId;
        ////Depth Stuff
        //private short[] depthPixels;
        //private byte[] colorPixels;
        //private readonly bool _depth;

        
        public KinectStream()
        {
            
        }

        public KinectStream(string uniqueKinectId, bool skeleton)
        {
            _uniqueKinectId = uniqueKinectId;
            _skeleton = skeleton;
            //_depth = depth;

        }

        #region IVideoSource Members

        public event NewFrameEventHandler NewFrame;


        public event VideoSourceErrorEventHandler VideoSourceError;


        public event PlayingFinishedEventHandler PlayingFinished;


        public long BytesReceived
        {
            get
            {
                long bytes = _bytesReceived;
                _bytesReceived = 0;
                return bytes;
            }
        }


        public virtual string Source
        {
            get { return _uniqueKinectId; }
            set { _uniqueKinectId = value; }
        }


        public int FramesReceived
        {
            get
            {
                int frames = _framesReceived;
                _framesReceived = 0;
                return frames;
            }
        }

        private bool _isrunning;
        public bool IsRunning
        {
            get { return _isrunning; }
        }

        public bool MousePointer;

        public void Start()
        {
            if (_sensor != null)
                Stop();

            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected && _uniqueKinectId == potentialSensor.UniqueKinectId)
                {
                    _sensor = potentialSensor;
                    break;
                }
            }
            if (_sensor==null)
            {
                MainForm.LogMessageToFile("Sensor not found: "+_uniqueKinectId);
                _isrunning = false;
                return;
            }

            
            if (_skeleton)
            {
                _sensor.SkeletonStream.Enable();
                _sensor.SkeletonFrameReady += SensorSkeletonFrameReady;
            }

            //if (_depth)
            //{
            //    _sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            //    _sensor.DepthFrameReady += SensorDepthFrameReady;
            //    // Allocate space to put the depth pixels we'll receive
            //    this.depthPixels = new short[_sensor.DepthStream.FramePixelDataLength];

            //    // Allocate space to put the color pixels we'll create
            //    this.colorPixels = new byte[_sensor.DepthStream.FramePixelDataLength * sizeof(int)];

            //    // This is the bitmap we'll display on-screen
            //    _colorBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth, this.sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

            //}
            //else
            //{
                _sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                _sensor.ColorFrameReady += SensorColorFrameReady;
            //}
            
            // Turn on the skeleton stream to receive skeleton frames
            

            // Start the sensor
            try
            {
                _sensor.Start();
                _isrunning = true;

            }
            catch (IOException)
            {
                _sensor = null;
                _isrunning = false;
            }
        }

        //void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        //{
        //    using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
        //    {
        //        if (depthFrame != null)
        //        {
        //            // Copy the pixel data from the image to a temporary array
        //            depthFrame.CopyPixelDataTo(this.depthPixels);

        //            // Convert the depth to RGB
        //            int colorPixelIndex = 0;
        //            for (int i = 0; i < this.depthPixels.Length; ++i)
        //            {
        //                // discard the portion of the depth that contains only the player index
        //                short depth = (short)(this.depthPixels[i] >> DepthImageFrame.PlayerIndexBitmaskWidth);

        //                // to convert to a byte we're looking at only the lower 8 bits
        //                // by discarding the most significant rather than least significant data
        //                // we're preserving detail, although the intensity will "wrap"
        //                // add 1 so that too far/unknown is mapped to black
        //                byte intensity = (byte)((depth + 1) & byte.MaxValue);

        //                // Write out blue byte
        //                colorPixels[colorPixelIndex++] = intensity;

        //                // Write out green byte
        //                colorPixels[colorPixelIndex++] = intensity;

        //                // Write out red byte                        
        //                colorPixels[colorPixelIndex++] = intensity;

        //                // We're outputting BGR, the last byte in the 32 bits is unused so skip it
        //                // If we were outputting BGRA, we would write alpha here.
        //                ++colorPixelIndex;
        //            }

        //            // Write the pixel data into our bitmap
        //            TypeConverter tc = TypeDescriptor.GetConverter(typeof(Bitmap));
        //            Bitmap bmap = (Bitmap)tc.ConvertFrom(colorPixels);
        //            NewFrame(this, new NewFrameEventArgs(bmap));
        //            // release the image
        //            bmap.Dispose(); 
        //        }
        //    }
        //}

        void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            lock (_skeletons)
            {
                using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
                {
                    if (skeletonFrame != null)
                    {
                        _skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                        skeletonFrame.CopySkeletonDataTo(_skeletons);
                    }
                }
            }
        }

        void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            if ((DateTime.Now - _lastFrameTimeStamp).TotalMilliseconds >= MaxInterval)
            {
                _lastFrameTimeStamp = DateTime.Now;

                using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
                {
                    Bitmap bmap = ImageToBitmap(imageFrame);
                    if (bmap != null)
                    {
                        lock (_skeletons)
                        {
                            foreach (Skeleton skel in _skeletons)
                            {
                                DrawBonesAndJoints(skel, Graphics.FromImage(bmap));
                            }
                        }
                        // notify client
                        NewFrame(this, new NewFrameEventArgs(bmap));
                        // release the image
                        bmap.Dispose();
                    }
                }
            }

        }

        void DrawBonesAndJoints(Skeleton skeleton, Graphics g)
        {
            // Render Torso
            DrawBone(skeleton, g, JointType.Head, JointType.ShoulderCenter);
            DrawBone(skeleton, g, JointType.ShoulderCenter, JointType.ShoulderLeft);
            DrawBone(skeleton, g, JointType.ShoulderCenter, JointType.ShoulderRight);
            DrawBone(skeleton, g, JointType.ShoulderCenter, JointType.Spine);
            DrawBone(skeleton, g, JointType.Spine, JointType.HipCenter);
            DrawBone(skeleton, g, JointType.HipCenter, JointType.HipLeft);
            DrawBone(skeleton, g, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            DrawBone(skeleton, g, JointType.ShoulderLeft, JointType.ElbowLeft);
            DrawBone(skeleton, g, JointType.ElbowLeft, JointType.WristLeft);
            DrawBone(skeleton, g, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            DrawBone(skeleton, g, JointType.ShoulderRight, JointType.ElbowRight);
            DrawBone(skeleton, g, JointType.ElbowRight, JointType.WristRight);
            DrawBone(skeleton, g, JointType.WristRight, JointType.HandRight);

            // Left Leg
            DrawBone(skeleton, g, JointType.HipLeft, JointType.KneeLeft);
            DrawBone(skeleton, g, JointType.KneeLeft, JointType.AnkleLeft);
            DrawBone(skeleton, g, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            DrawBone(skeleton, g, JointType.HipRight, JointType.KneeRight);
            DrawBone(skeleton, g, JointType.KneeRight, JointType.AnkleRight);
            DrawBone(skeleton, g, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = _trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = _inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    var p = SkeletonPointToScreen(joint.Position);
                    g.FillEllipse(drawBrush, p.X, p.Y, JointThickness, JointThickness);
                }
            }
        }

        private void DrawBone(Skeleton skeleton, Graphics g, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = _inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = _trackedBonePen;
            }

            g.DrawLine(drawPen, SkeletonPointToScreen(joint0.Position), SkeletonPointToScreen(joint1.Position));
        }

        public void SignalToStop()
        {
            Stop();
        }


        public void WaitForStop()
        {
            Stop();
        }


        public void Stop()
        {
            try
            {
                _sensor.Stop();
            }
            catch (IOException)
            {
            }
            _sensor = null;
            _isrunning = false;

        }

        #endregion

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = _sensor.MapSkeletonPointToDepth(skelpoint,DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        static Bitmap ImageToBitmap(
                     ColorImageFrame image)
        {
            try
            {
                if (image != null)
                {
                    var pixeldata =
                        new byte[image.PixelDataLength];
                    
                    image.CopyPixelDataTo(pixeldata);

                    var bmap = new Bitmap(
                        image.Width,
                        image.Height,
                        PixelFormat.Format32bppRgb);
                    BitmapData bmapdata = bmap.LockBits(
                        new Rectangle(0, 0,
                                      image.Width, image.Height),
                        ImageLockMode.WriteOnly,
                        bmap.PixelFormat);
                    var ptr = bmapdata.Scan0;
                    Marshal.Copy(pixeldata, 0, ptr,
                                 image.PixelDataLength);
                    bmap.UnlockBits(bmapdata);


                    return bmap;
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            return null;
        }

        //Bitmap ImageToBitmap(
        //    DepthImageFrame Image)
        //{
        //    if (Image != null)
        //    {
        //        short[] pixeldata =
        //            new short[Image.PixelDataLength];
        //        Image.CopyPixelDataTo(pixeldata);

        //        Bitmap bmap = new Bitmap(
        //            Image.Width,
        //            Image.Height,
        //            PixelFormat.Format16bppGrayScale);

        //        BitmapData bmapdata = bmap.LockBits(
        //            new Rectangle(0, 0,
        //                          Image.Width, Image.Height),
        //            ImageLockMode.WriteOnly,
        //            bmap.PixelFormat);
        //        IntPtr ptr = bmapdata.Scan0;
        //        Marshal.Copy(pixeldata, 0, ptr,
        //                     Image.PixelDataLength);
        //        bmap.UnlockBits(bmapdata);
        //        return bmap;
        //    }
        //    return null;
        //}
    }
}