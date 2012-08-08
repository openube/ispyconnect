using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using g711audio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PictureBox = AForge.Controls.PictureBox;

namespace iSpyServer
{
    public sealed partial class VolumeLevel : PictureBox
    {
        #region Private

        private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);
        private int _bitsPerSample = 16;
        private int _channels = 1;
        private long _lastRun = DateTime.Now.Ticks;
        private int _milliCount;
        private bool _processing;
        private int _sampleRate = 8000;
        private double _value;
        private WaveFormat _recordingFormat;
        private WaveIn _waveIn;

        private WaveInProvider _waveProvider;
        private MeteringSampleProvider _meteringProvider;
        private SampleChannel _sampleChannel;

        public BufferedWaveProvider WaveOutProvider { get; set; }

        #endregion

        #region Public

        #region Delegates

        public delegate void NewDataAvailable(object sender, NewDataAvailableArgs eventArgs);

        public delegate void RemoteCommandEventHandler(object sender, ThreadSafeCommand e);

        #endregion

        public bool IsEdit;
        public bool NoSource;
        public Queue<short> PlayBuffer;
        public bool ResizeParent;
        public objectsMicrophone Micobject;

        #endregion

        #region SizingControls

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
            if ((ResizeParent) && (Parent != null) && (Parent.IsHandleCreated))
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
                        if (e.Location.X > 0 && e.Location.Y > Height - 22)
                        {
                            MessageBox.Show(
                                "Add a new iSpyServer microphone source to iSpy and use the address below to connect:\n\nhttp://" +
                                MainForm.AddressIPv4 + ":" + iSpyServer.Default.LANPort + "/?micid=" + Micobject.id);
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
                double ar = Convert.ToDouble(MinimumSize.Width)/Convert.ToDouble(MinimumSize.Height);
                Width = Convert.ToInt32(ar*Height);
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
            BackColor = iSpyServer.Default.BackColor;
            Micobject = om;
        }


        [DefaultValue(false)]
        public double Value
        {
            get { return _value; }
            set
            {
                _value = value;
                Invalidate();
            }
        }

        public WaveFormat RecordingFormat
        {
            get { return _recordingFormat; }
            set
            {
                _recordingFormat = value;
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
                _milliCount += ts.Milliseconds;
                _lastRun = DateTime.Now.Ticks;

                double secondCount = (_milliCount/1000.0);

                while (_milliCount > 1000)
                    _milliCount -= 1000;

                if (secondCount > 1) //approx every second
                {
                    DateTime dtnow = DateTime.Now;
                    foreach (objectsMicrophoneScheduleEntry entry in Micobject.schedule.entries.Where(p => p.active))
                    {
                        if (entry.daysofweek.IndexOf(((int) dtnow.DayOfWeek).ToString()) != -1)
                        {
                            if (Micobject.settings.active)
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
                                            goto skip;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            skip:
            _processing = false;
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            // lock
            Monitor.Enter(this);

            Graphics gMic = pe.Graphics;
            Rectangle rc = ClientRectangle;

            var grabPoints = new[]
                                 {
                                     new Point(rc.Width - 15, rc.Height), new Point(rc.Width, rc.Height - 15),
                                     new Point(rc.Width, rc.Height)
                                 };
            var drawFont = new Font(FontFamily.GenericSansSerif, 9);
            var grabBrush = new SolidBrush(Color.DarkGray);
            if (Micobject.newrecordingcount > 0)
                grabBrush.Color = Color.Yellow;
            var borderPen = new Pen(grabBrush);
            var drawBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
            gMic.FillPolygon(grabBrush, grabPoints);
            gMic.DrawRectangle(borderPen, 0, 0, rc.Width - 1, rc.Height - 1);
            if (Micobject.settings.active)
            {
                int drawW = Convert.ToInt32(Convert.ToDouble((rc.Width - 1.0))*(Value/100.0));
                if (drawW < 1)
                    drawW = 1;

                Brush b = new SolidBrush(iSpyServer.Default.VolumeLevelColor);

                gMic.FillRectangle(b, rc.X + 2, rc.Y + 2, drawW - 4, rc.Height - 20);

                gMic.DrawString("http://" + MainForm.AddressIPv4 + ":" + iSpyServer.Default.LANPort + "/?micid=" + Micobject.id, drawFont, drawBrush,
                                 new PointF(5, rc.Height - 18));

                b.Dispose();
            }
            else
            {
                if (NoSource)
                {
                    gMic.DrawString(LocRM.GetString("NoSource") + ": " + Micobject.name,
                                     drawFont, drawBrush, new PointF(5, 5));
                }
                else
                {
                    if (Micobject.schedule.active)
                    {
                        gMic.DrawString(LocRM.GetString("Scheduled") + ": " + Micobject.name,
                                         drawFont, drawBrush, new PointF(5, 5));
                    }
                    else
                    {
                        gMic.DrawString(LocRM.GetString("Inactive") + ": " + Micobject.name,
                                         drawFont, drawBrush, new PointF(5, 5));
                    }
                }
            }
            borderPen.Dispose();
            drawFont.Dispose();
            grabBrush.Dispose();
            drawBrush.Dispose();
            Monitor.Exit(this);

            base.OnPaint(pe);
        }
        public void ApplySchedule()
        {
            if (!Micobject.schedule.active || Micobject.schedule == null || Micobject.schedule.entries == null ||
                Micobject.schedule.entries.Count() == 0)
                return;
            //find most recent schedule entry
            DateTime dNow = DateTime.Now;
            TimeSpan shortest = TimeSpan.MaxValue;
            objectsMicrophoneScheduleEntry mostrecent = null;
            bool isstart = true;

            foreach (objectsMicrophoneScheduleEntry entry in Micobject.schedule.entries)
            {
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
                    Micobject.alerts.active = mostrecent.alerts;
                    if (!Micobject.settings.active)
                        Enable();
                }
                else
                {
                    if (Micobject.settings.active)
                        Disable();
                }
            }
        }

        public void UpdateLevel(double newLevel)
        {
            if (newLevel != 0)
            {
                //work out percentage
                if (newLevel < 0)
                    newLevel = 0;
                Value = (newLevel)*100;
            }
        }

        void _meteringProvider_StreamVolume(object sender, StreamVolumeEventArgs e)
        {
            UpdateLevel(e.MaxSampleValues[0]);

        }
        public List<Socket> OutSockets = new List<Socket>();

        void WaveInDataAvailable(object sender, WaveInEventArgs e)
        {
            
            //forces processing of volume level without piping it out
            var sampleBuffer = new float[e.BytesRecorded];
            if (_meteringProvider != null)
            {
                _meteringProvider.Read(sampleBuffer, 0, e.BytesRecorded);

                if (OutSockets.Count > 0)
                {
                    var enc = new byte[e.Buffer.Length/2];
                    ALawEncoder.ALawEncode(e.Buffer,enc);

                    for (int i = 0; i < OutSockets.Count; i++)
                    {
                        Socket s = OutSockets[i];
                        if (s.Connected)
                        {
                            if (!SendToBrowser(enc, s))
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
                }
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


        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _processing = true;
                _stopEvent.Close();
            }
            base.Dispose(disposing);
        }

        public void Disable()
        {
            _processing = true;

            if (_waveIn != null)
            {
                try
                {
                    _waveIn.StopRecording();
                }
                catch
                {
                }
            }

            NoSource = false;

            foreach (
                objectsFloorplan ofp in
                    MainForm.FloorPlans.Where(
                        p => p.objects.@object.Where(q => q.type == "microphone" && q.id == Micobject.id).Count() > 0).
                        ToList())
            {
                ofp.needsupdate = true;
            }
            Micobject.settings.active = false;


            MainForm.NeedsSync = true;
            Invalidate();
            GC.Collect();
            _processing = false;
        }

        public void Enable()
        {
            _processing = true;
            _sampleRate = Micobject.settings.samples;
            _bitsPerSample = Micobject.settings.bits;
            _channels = Micobject.settings.channels;

            RecordingFormat = new WaveFormat(_sampleRate, _bitsPerSample, _channels);

            //local device
            int i = 0, selind = -1;
            for (int n = 0; n < WaveIn.DeviceCount; n++)
            {
                if (WaveIn.GetCapabilities(n).ProductName == Micobject.settings.sourcename)
                    selind = i;
                i++;
            }
            if (selind == -1)
            {
                //device no longer connected
                Micobject.settings.active = false;
                NoSource = true;
                _processing = false;
                return;
            }


            _waveIn = new WaveIn { BufferMilliseconds = 40, DeviceNumber = selind, WaveFormat = RecordingFormat };
            _waveIn.DataAvailable += WaveInDataAvailable;
            _waveIn.RecordingStopped += WaveInRecordingStopped;

            _waveProvider = new WaveInProvider(_waveIn);
            _sampleChannel = new SampleChannel(_waveProvider);

            _meteringProvider = new MeteringSampleProvider(_sampleChannel);
            _meteringProvider.StreamVolume += _meteringProvider_StreamVolume;

            try
            {
                _waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
                MessageBox.Show(LocRM.GetString("AudioMonitoringError") + ": " + ex.Message, LocRM.GetString("Error"));
                _processing = false;
                return;
            }

            NoSource = false;
            Micobject.settings.active = true;

            MainForm.NeedsSync = true;
            Invalidate();
            _processing = false;
        }

        private void WaveInRecordingStopped(object sender, EventArgs e)
        {
            Micobject.settings.active = false;
            if (_waveIn != null)
            {
                _waveIn.Dispose();
                _waveIn = null;
            }
        }

       


        private void VolumeLevelFinal_Resize(object sender, EventArgs e)
        {
        }

        private void VolumeLevel_MouseDown(object sender, MouseEventArgs e)
        {
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


}