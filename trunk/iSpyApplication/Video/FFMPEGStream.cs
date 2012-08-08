using System;
using System.Drawing;
using System.Threading;
using AForge.Video;
using AForge.Video.FFMPEG;
using iSpyApplication.Audio;
using iSpyApplication.Audio.streams;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReasonToFinishPlaying = AForge.Video.ReasonToFinishPlaying;

namespace iSpyApplication.Video
{
    public class FFMPEGStream : IVideoSource, IAudioSource
    {
        // recieved byte count
        private int _framesReceived;
        private ManualResetEvent _stopEvent;
        private Thread _thread;
        private string _source;


        #region Audio
        private float _volume;
        private bool _listening;

        public int BytePacket = 400;

        private WaveFormat _recordingFormat;
        private BufferedWaveProvider _waveProvider;
        private MeteringSampleProvider _meteringProvider;
        private SampleChannel _sampleChannel;

        public BufferedWaveProvider WaveOutProvider { get; set; }

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

        /// <summary>
        /// Video source error event.
        /// </summary>
        /// 
        /// <remarks>This event is used to notify clients about any type of errors occurred in
        /// video source object, for example internal exceptions.</remarks>
        /// 
        public event VideoSourceErrorEventHandler VideoSourceError;

        /// <summary>
        /// Video playing finished event.
        /// </summary>
        /// 
        /// <remarks><para>This event is used to notify clients that the video playing has finished.</para>
        /// </remarks>
        /// 
        public event PlayingFinishedEventHandler PlayingFinished;

        

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
                long bytes = 0;
                //bytesReceived = 0;
                return bytes;
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
                if (_thread != null)
                {
                    // check thread status
                    if (_thread.Join(0) == false)
                        return true;

                    // the thread is not running, free resources
                    Free();
                }
                return false;
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
            _framesReceived = 0;
            _mFramesReceived = 0;

            // create events
            _stopEvent = new ManualResetEvent(false);

            // create and start new thread
            _thread = new Thread(FfmpegListener) { Name = "ffmpeg " + _source };
            _thread.Start();
        }

        private int _mFramesReceived;

        private void FfmpegListener()
        {
            ReasonToFinishPlaying reasonToStop = ReasonToFinishPlaying.StoppedByUser;

            VideoFileReader vfr = null;
            Program.WriterMutex.WaitOne();
            try
            {
                vfr = new VideoFileReader();
                vfr.Open(_source);
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            Program.WriterMutex.ReleaseMutex();
            if (vfr == null || !vfr.IsOpen)
            {
                if (PlayingFinished!=null)
                    PlayingFinished(this, ReasonToFinishPlaying.VideoSourceError);
                return;
            }
            bool hasaudio = false;
            if (vfr.Channels > 0)
            {
                hasaudio = true;
                RecordingFormat = new WaveFormat(vfr.SampleRate, 16, vfr.Channels);

                WaveOutProvider = new BufferedWaveProvider(RecordingFormat) {DiscardOnBufferOverflow = true};
                _waveProvider = new BufferedWaveProvider(RecordingFormat) {DiscardOnBufferOverflow = true};


                _sampleChannel = new SampleChannel(_waveProvider);
                _meteringProvider = new MeteringSampleProvider(_sampleChannel);
                _meteringProvider.StreamVolume += MeteringProviderStreamVolume;

                if (HasAudioStream != null)
                    HasAudioStream(this, EventArgs.Empty);
            }

            int interval = 1000 / ((vfr.FrameRate == 0) ? 25 : vfr.FrameRate);

            byte[] data;
            Bitmap frame;
            
            try
            {
                while (!_stopEvent.WaitOne(0, false))
                {
                    DateTime start = DateTime.Now;
                    frame = vfr.ReadVideoFrame();
                    if ( frame == null )
			        {
				        reasonToStop = ReasonToFinishPlaying.EndOfStreamReached;
                        break;
			        }
                    
                    if (NewFrame!=null)
                        NewFrame(this, new NewFrameEventArgs(frame));
                    frame.Dispose();

                    if (hasaudio)
                    {
                        data = vfr.ReadAudioFrame();
                        if (DataAvailable != null)
                        {
                            _waveProvider.AddSamples(data, 0, data.Length);

                            if (Listening)
                            {
                                WaveOutProvider.AddSamples(data, 0, data.Length);
                            }

                            _mFramesReceived++;

                            //forces processing of volume level without piping it out
                            var sampleBuffer = new float[data.Length];

                            _meteringProvider.Read(sampleBuffer, 0, data.Length);
                            DataAvailable(this, new DataAvailableEventArgs((byte[]) data.Clone()));
                        }
                    }
                    
                    if (interval > 0)
                    {
                        // get frame extract duration
                        TimeSpan span = DateTime.Now.Subtract(start);

                        // miliseconds to sleep
                        int msec = interval - (int)span.TotalMilliseconds;

                        if ((msec > 0) && (_stopEvent.WaitOne(msec, false)))
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                if (VideoSourceError != null)
                    VideoSourceError(this, new VideoSourceErrorEventArgs(e.Message));
                MainForm.LogExceptionToFile(e);
                reasonToStop = ReasonToFinishPlaying.DeviceLost;
            }
            if (PlayingFinished != null)
                PlayingFinished(this, reasonToStop);
        }


        void MeteringProviderStreamVolume(object sender, StreamVolumeEventArgs e)
        {
            if (LevelChanged != null)
                LevelChanged(this, new LevelChangedEventArgs(e.MaxSampleValues));

        }

        /// <summary>
        /// Free resource.
        /// </summary>
        /// 
        private void Free()
        {
            _thread = null;

            // release events
            _stopEvent.Close();
            _stopEvent = null;
        }

        #region Audio Stuff
        public event DataAvailableEventHandler DataAvailable;
        public event LevelChangedEventHandler LevelChanged;
        public event AudioSourceErrorEventHandler AudioSourceError;
        public event AudioFinishedEventHandler AudioFinished;
        public event HasAudioStreamEventHandler HasAudioStream;

        public float Volume
        {
            get { return _volume; }
            set
            {
                _volume = value;
                if (_sampleChannel != null)
                {
                    _sampleChannel.Volume = value;
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
                if (value)
                {
                    WaveOutProvider = new BufferedWaveProvider(RecordingFormat) { DiscardOnBufferOverflow = true };
                }

                _listening = value;
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

        /// <summary>
        /// Stop video source.
        /// </summary>
        /// 
        public void Stop()
        {
            if (IsRunning)
            {
                _stopEvent.Set();
            }
        }
        #endregion

    }
}