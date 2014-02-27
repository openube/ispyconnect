using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using AForge.Video;
using iSpy.Video.FFMPEG;
using iSpyApplication.Audio;
using iSpyApplication.Audio.streams;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReasonToFinishPlaying = AForge.Video.ReasonToFinishPlaying;

namespace iSpyApplication.Video
{
    public class FFMPEGStream : IVideoSource, IAudioSource, ISupportsAudio
    {
        private int _framesReceived;
        private ManualResetEvent _stopEvent;
        private Thread _thread;
        private string _source;
        private double _delay;
        private int _stopWatchOffset;
        private int _initialSeek = -1;
        
        #region Audio
        private float _gain;
        private bool _listening;
        private Thread _tOutput;
        private const int MAXBuffer = 60;

        private BufferedWaveProvider _waveProvider;
        public SampleChannel SampleChannel;

        public BufferedWaveProvider WaveOutProvider { get; set; }
        public VolumeWaveProvider16New VolumeProvider { get; set; }
        #endregion
        

        /// <summary>
        /// Initializes a new instance of the <see cref="FFMPEGStream"/> class.
        /// </summary>
        /// 
        public FFMPEGStream()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FFMPEGStream"/> class.
        /// </summary>
        /// 
        /// <param name="source">URL, which provides video stream.</param>
        public FFMPEGStream(string source)
        {
            _source = source;
        }

        public IAudioSource OutAudio;

        #region IVideoSource Members

        /// <summary>
        /// New frame event.
        /// </summary>
        /// 
        /// <remarks><para>Notifies clients about new available frame from video source.</para>
        /// 
        /// <para><note>Since video source may have multiple clients, each client is responsible for
        /// making a copy (cloning) of the passed video frame, because the video source disposes its
        /// own original copy after notifying of clients.</note></para>
        /// </remarks>
        /// 
        public event NewFrameEventHandler NewFrame;
        public event VideoSourceErrorEventHandler VideoSourceError;
        public event PlayingFinishedEventHandler PlayingFinished;

        public event DataAvailableEventHandler DataAvailable;
        public event LevelChangedEventHandler LevelChanged;
        public event AudioFinishedEventHandler AudioFinished;

        public event HasAudioStreamEventHandler HasAudioStream;

        /// <summary>
        /// Video source.
        /// </summary>
        /// 
        /// <remarks>URL, which provides video stream.</remarks>
        /// 
        public string Source
        {
            get { return _source; }
            set { _source = value; }
        }

        /// <summary>
        /// Received bytes count.
        /// </summary>
        /// 
        /// <remarks>Number of bytes the video source provided from the moment of the last
        /// access to the property.
        /// </remarks>
        /// 
        public long BytesReceived
        {
            get
            {
                //long bytes = 0;
                //bytesReceived = 0;
                return 0;
            }
        }

        /// <summary>
        /// Received frames count.
        /// </summary>
        /// 
        /// <remarks>Number of frames the video source provided from the moment of the last
        /// access to the property.
        /// </remarks>
        /// 
        public int FramesReceived
        {
            get
            {
                int frames = _framesReceived;
                _framesReceived = 0;
                return frames;
            }
        }
        private readonly object _threadLock = new object();

        /// <summary>
        /// State of the video source.
        /// </summary>
        /// 
        /// <remarks>Current state of video source object - running or not.</remarks>
        /// 
        public bool IsRunning
        {
             get
            {
                return ThreadAlive;
            }
        }

        private bool ThreadAlive
        {
            get
            {
                lock (_threadLock)
                {
                    return _thread != null && !_thread.Join(TimeSpan.Zero);
                }
            }
        }


        /// <summary>
        /// Start video source.
        /// </summary>
        /// 
        /// <remarks>Starts video source and return execution to caller. Video source
        /// object creates background thread and notifies about new frames with the
        /// help of <see cref="NewFrame"/> event.</remarks>
        /// 
        /// <exception cref="ArgumentException">Video source is not specified.</exception>
        /// 
        public void Start()
        {
             if (IsRunning) return;

             //Debug.WriteLine("Starting "+_source);
            _framesReceived = 0;

            // create events
            _stopEvent = new ManualResetEvent(false);

            // create and start new thread
            lock (_threadLock)
            {
                _thread = new Thread(FfmpegListener) {Name = "ffmpeg " + _source};
                _thread.Start();
            }

            Seekable = IsFileSource;
            _initialSeek = -1;
            _stopWatchOffset = 0;
        }

        private bool _paused;
        
        public bool IsPaused
        {
            get { return _paused; }
        }

        public void Play()
        {
            _paused = false;
            _sw.Start();
        }

        public void Pause()
        {
            _paused = true;
            _sw.Stop();
        }

        public double PlaybackRate = 1;

        private VideoFileReader _vfr;
        private bool IsFileSource
        {
            get { return _source!=null && _source.IndexOf("://", StringComparison.Ordinal) == -1; }
        }

        private class DelayedFrame : IDisposable
        {
            public Bitmap B;
            public readonly double ShowTime;
            public DelayedFrame(Bitmap b, double showTime, double delay)
            {
                B = b;
                ShowTime = showTime + delay;
            }

            public void Dispose()
            {
                if (B!=null)
                {
                    B.Dispose();
                    B = null;
                }
            }
        }
        private class DelayedAudio
        {
            public readonly byte[] A;
            public readonly double ShowTime;
            public DelayedAudio(byte[] a, double showTime, double delay)
            {
                A = a;
                ShowTime = showTime + delay;
            }
        }

        private bool _noBuffer;

        private List<DelayedFrame> _videoframes;
        private List<DelayedAudio> _audioframes;
        private readonly Stopwatch _sw = new Stopwatch();

        private bool _realtime;

        private void FrameEmitter()
        {
            _sw.Reset();
            bool first = true;
            while (!_stopEvent.WaitOne(5))
            {
                first = EmitFrame(first);
            }
           
        }

        private bool EmitFrame(bool first)
        {
            try
            {
                if (_videoframes.Count > 0)
                {
                    DelayedFrame q = _videoframes[0];

                    if (q != null)
                    {
                        if (first)
                        {
                            first = false;
                            _sw.Start();
                        }
                        if (_sw.ElapsedMilliseconds + _stopWatchOffset > q.ShowTime)
                        {
                            if (q.B != null)
                            {
                                if (NewFrame != null)
                                    NewFrame(this, new NewFrameEventArgs(q.B));
                                q.B.Dispose();
                            }
                            _videoframes.RemoveAt(0);
                        }
                    }
                    else
                    {
                        _videoframes.RemoveAt(0);
                    }

                }
                if (_audioframes.Count > 0)
                {
                    DelayedAudio q = _audioframes[0];

                    if (q != null)
                    {
                        if (first)
                        {
                            first = false;
                            _sw.Start();
                        }
                        var dispTime = _sw.ElapsedMilliseconds + _stopWatchOffset;
                        if (dispTime > q.ShowTime)
                        {
                            if (q.A != null)
                            {
                                ProcessAudio(q.A);
                            }
                            _audioframes.RemoveAt(0);
                        }
                    }
                    else
                    {
                        _audioframes.RemoveAt(0);
                    }
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            return first;
        }


        public string Cookies = "";
        public string UserAgent = "";
        public string Headers = "";
        public int RTSPMode = 0;

        public int AnalyzeDuration = 2000;
        public int Timeout = 8000;

        private volatile bool _bufferFull;
        ReasonToFinishPlaying _reasonToStop = ReasonToFinishPlaying.StoppedByUser;

        private void FfmpegListener()
        {
            _reasonToStop = ReasonToFinishPlaying.StoppedByUser;
            _vfr = null;
            bool open = false;
            string errmsg = "";
            try
            {
                Program.WriterMutex.WaitOne();
                _vfr = new VideoFileReader();

                //ensure http/https is lower case for string compare in ffmpeg library
                int i = _source.IndexOf("://", StringComparison.Ordinal);
                if (i > -1)
                {
                    _source = _source.Substring(0, i).ToLower() + _source.Substring(i);
                }
                _vfr.Timeout = Timeout;
                _vfr.AnalyzeDuration = AnalyzeDuration;
                _vfr.Cookies = Cookies;
                _vfr.UserAgent = UserAgent;
                _vfr.Headers = Headers;
                _vfr.Flags = -1;
                _vfr.NoBuffer = _noBuffer;
                _vfr.RTSPMode = RTSPMode;
                _vfr.Open(_source);
                open = true;
            }
            catch (Exception ex)
            {
                MainForm.LogErrorToFile(ex.Message+": "+_source);
            }
            finally
            {
                Program.WriterMutex.ReleaseMutex();                
            }

            if (_vfr == null || !_vfr.IsOpen || !open)
            {
                ShutDown("Could not open stream" + ": " + _source);
                return;
            }
            if (_stopEvent.WaitOne(0))
            {
                ShutDown("");
                return;
            }

            bool hasaudio = false;
            _realtime = !IsFileSource;

            if (!_realtime)
                _noBuffer = false;

            if (_vfr.Channels > 0)
            {
                hasaudio = true;
                RecordingFormat = new WaveFormat(_vfr.SampleRate, 16, _vfr.Channels);
                _waveProvider = new BufferedWaveProvider(RecordingFormat) {DiscardOnBufferOverflow = true, BufferDuration = TimeSpan.FromMilliseconds(2500)};

                SampleChannel = new SampleChannel(_waveProvider);
                SampleChannel.PreVolumeMeter += SampleChannelPreVolumeMeter;

                if (HasAudioStream != null)
                {
                    HasAudioStream(this, EventArgs.Empty);        
                }
            }
            HasAudioStream = null;

            Duration = _vfr.Duration;

            if (!_noBuffer)
            {
                _tOutput = new Thread(FrameEmitter) {Name="ffmpeg frame emitter"};
                _tOutput.Start();
            }
            else
            {
                _tOutput = null;
            }

            _videoframes = new List<DelayedFrame>();
            _audioframes = new List<DelayedAudio>();

            double maxdrift = 0, firstmaxdrift = 0;
            const int analyseInterval = 10;
            DateTime dtAnalyse = DateTime.MinValue;
            _lastFrame = Helper.Now;

            if (_initialSeek>-1)
                _vfr.Seek(_initialSeek);
            try
            {
                while (!_stopEvent.WaitOne(5) && !MainForm.Reallyclose)
                {

                    _bufferFull = !_realtime && (_videoframes.Count > MAXBuffer || _audioframes.Count > MAXBuffer);
                    if (!_paused && !_bufferFull)
                    {
                        if (DecodeFrame(analyseInterval, hasaudio, ref firstmaxdrift, ref maxdrift, ref dtAnalyse)) break;
                        if (_noBuffer && !_stopEvent.WaitOne(0))
                        {
                            if (_videoframes.Count > 0)
                            {
                                DelayedFrame q = _videoframes[0];
                                if (q.B != null)
                                {
                                    if (NewFrame != null)
                                        NewFrame(this, new NewFrameEventArgs(q.B));
                                    q.B.Dispose();
                                }
                                _videoframes.RemoveAt(0);
                            }
                            if (_audioframes.Count > 0)
                            {
                                DelayedAudio q = _audioframes[0];

                                if (q.A != null)
                                {
                                    ProcessAudio(q.A);
                                }
                                _audioframes.RemoveAt(0);
                            }
                        }
                    }
                }
                StopOutput();
            }
            catch (Exception e)
            {
                MainForm.LogExceptionToFile(e);
                errmsg = e.Message;
            }

            if (SampleChannel != null)
            {
                SampleChannel.PreVolumeMeter -= SampleChannelPreVolumeMeter;
                SampleChannel = null;
            }

            if (_waveProvider != null)
            {
                if (_waveProvider.BufferedBytes > 0)
                    _waveProvider.ClearBuffer();
            }

            ShutDown(errmsg);
        }

        private void ShutDown(string errmsg)
        {
            bool err=!String.IsNullOrEmpty(errmsg);
            if (err)
            {
                _reasonToStop = ReasonToFinishPlaying.DeviceLost;
            }

            StopOutput();
            if (IsFileSource && !err)
                _reasonToStop = ReasonToFinishPlaying.StoppedByUser;

            if (_vfr!=null && _vfr.IsOpen)  {
                try
                {
                    _vfr.Dispose(); //calls close
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                }
            }

            // release events
            if (_stopEvent != null)
            {
                _stopEvent.Close();
                _stopEvent.Dispose();
                _stopEvent = null;
            }

            if (PlayingFinished != null)
                PlayingFinished(this, _reasonToStop);
            if (AudioFinished != null)
                AudioFinished(this, _reasonToStop);
            
        }

        private DateTime _lastFrame = DateTime.MinValue;

        private bool DecodeFrame(int analyseInterval, bool hasaudio, ref double firstmaxdrift, ref double maxdrift,
                                 ref DateTime dtAnalyse)
        {
            object frame = _vfr.ReadFrame();
            if (_stopEvent.WaitOne(0))
                return false;
            switch (_vfr.LastFrameType)
            {
                case 0:
                    //null packet
                    if (!_realtime) 
                    {
                        //end of file
                        //wait for all frames to be emitted
                        while (!_stopEvent.WaitOne(2))
                        {
                            if (_videoframes.Count == 0 && _audioframes.Count == 0)
                                break;
                        }
                        return true;
                    }
                    if ((Helper.Now - _lastFrame).TotalMilliseconds > Timeout)
                        throw new TimeoutException("Timeout reading from video stream");
                    break;
                case 1:
                    _lastFrame = Helper.Now;
                    if (hasaudio)
                    {
                        var data = frame as byte[];
                        if (data != null)
                        {
                            if (data.Length > 0)
                            {
                                double t = _vfr.AudioTime;
                                _audioframes.Add(new DelayedAudio(data, t, _delay));
                            }
                        }
                    }
                    break;
                case 2:
                    _lastFrame = Helper.Now;
                    
                    if (frame != null)
                    {
                        var bmp = frame as Bitmap;
                        if (dtAnalyse == DateTime.MinValue)
                        {
                            dtAnalyse = Helper.Now.AddSeconds(analyseInterval);
                        }

                        double t = _vfr.VideoTime;


                        if (_realtime)
                        {
                            double drift = _vfr.VideoTime - _sw.ElapsedMilliseconds;

                            if (dtAnalyse > Helper.Now)
                            {
                                if (Math.Abs(drift) > Math.Abs(maxdrift))
                                {
                                    maxdrift = drift;
                                }
                            }
                            else
                            {
                                if (firstmaxdrift > 0)
                                    _delay = 0 - (maxdrift - firstmaxdrift);
                                else
                                    firstmaxdrift = maxdrift;
                                maxdrift = 0;
                                dtAnalyse = Helper.Now.AddSeconds(analyseInterval);
                            }

                            //Console.WriteLine("delay: " + _delay + " firstmaxdrift: "+firstmaxdrift+" drift: " + drift + " maxdrift: " + maxdrift + " buffer: " + _videoframes.Count);

                        }


                        _videoframes.Add(new DelayedFrame(bmp, t, _delay));
                    }
                    break;
            }
            return false;
        }

        void StopOutput()
        {
            if (_tOutput != null)
            {
                _stopEvent.Set();
                _tOutput.Join();
                _tOutput = null;
            }
            ClearBuffer();
        }

        void ProcessAudio(byte[] data)
        {
            try
            {
                if (DataAvailable != null)
                {
                    _waveProvider.AddSamples(data, 0, data.Length);

                    var sampleBuffer = new float[data.Length];
                    SampleChannel.Read(sampleBuffer, 0, data.Length);

                    DataAvailable(this, new DataAvailableEventArgs((byte[]) data.Clone()));

                    if (WaveOutProvider != null && Listening)
                    {
                        WaveOutProvider.AddSamples(data, 0, data.Length);
                    }
                }
            }
            catch (NullReferenceException)
            {
                //DataAvailable can be removed at any time
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
        }

        void SampleChannelPreVolumeMeter(object sender, StreamVolumeEventArgs e)
        {
            if (LevelChanged != null)
            {
                LevelChanged(this, new LevelChangedEventArgs(e.MaxSampleValues));
            }
        }

        public bool Seekable;

        public long Time
        {
            get
            {
                if (_vfr.IsOpen)
                    return _vfr.VideoTime;
                return 0;
            }
        }
        public long Duration;


        private void ClearBuffer()
        {
            if (_videoframes != null)
            {
                _videoframes.DisposeAll();
                _videoframes.Clear();
            }
            if (_audioframes != null)
            {
                _audioframes.DisposeAll();
                _audioframes.Clear();
            }
        }
        public void Seek(float percentage)
        {
            int t = Convert.ToInt32((Duration/1000d)*percentage);
            if (Seekable)
            {
                _sw.Stop();
                ClearBuffer();
                _initialSeek = t;
                _vfr.Seek(t);
                _stopWatchOffset = t*1000;
                _sw.Reset();
                _sw.Start();
            }
        }

        #region Audio Stuff
        



        public float Gain
        {
            get { return _gain; }
            set
            {
                _gain = value;
                if (SampleChannel != null)
                {
                    SampleChannel.Volume = value;
                }
            }
        }

        public bool Listening
        {
            get
            {
                if (IsRunning && _listening)
                    return true;
                return false;

            }
            set
            {
                if (RecordingFormat == null)
                {
                    _listening = false;
                    return;
                }

                if (WaveOutProvider != null)
                {
                    if (WaveOutProvider.BufferedBytes>0) WaveOutProvider.ClearBuffer();
                    WaveOutProvider = null;
                }


                if (value)
                {
                    WaveOutProvider = new BufferedWaveProvider(RecordingFormat) { DiscardOnBufferOverflow = true };
                }

                _listening = value;
            }
        }

        public WaveFormat RecordingFormat { get; set; }

        #endregion

        /// <summary>
        /// Calls Stop
        /// </summary>
        public void SignalToStop()
        {
            Stop();
        }

        /// <summary>
        /// Calls Stop
        /// </summary>
        public void WaitForStop()
        {
            Stop();
        }

        private bool _stopping;
        /// <summary>
        /// Stop video source.
        /// </summary>
        /// 
        public void Stop()
        {
            if (IsRunning && !_stopping)
            {
                _stopping = true;
                if (_stopEvent != null)
                {
                    //if stopEvent is null the thread is exiting and stop has been called from a related event //bastard!
                    _stopEvent.Set();

                    while (IsRunning)
                    {
                        try
                        {
                            _thread.Join(50);
                        }
                        catch
                        {
                        }
                        Application.DoEvents();
                    }
                }


                Listening = false;


                _thread = null;
                _stopping = false;
            }
        }
        #endregion

    }
}