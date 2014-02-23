using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Declarations;
using Declarations.Events;
using Declarations.Media;
using Declarations.Players;
using Implementation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using iSpyApplication.Video;
using NewFrameEventHandler = AForge.Video.NewFrameEventHandler;
using ReasonToFinishPlaying = AForge.Video.ReasonToFinishPlaying;

namespace iSpyApplication.Audio.streams
{
    public class VLCStream : IAudioSource
    {
        public int FormatWidth = 320, FormatHeight = 240;
        private volatile bool _isrunning;
        private bool _starting;
        private readonly string[] _arguments;
        private int _framesReceived;
        private IMediaPlayerFactory _mFactory;
        private IMedia _mMedia;
        private IVideoPlayer _mPlayer;
        //private volatile bool _isstopping;
        private volatile bool _stoprequested;
        private readonly object _lock = new object();


        #region Audio
        private float _gain;
        private bool _listening;

        private bool _needsSetup = true;

        public int BytePacket = 400;

        private WaveFormat _recordingFormat;
        private BufferedWaveProvider _waveProvider;
        private SampleChannel _sampleChannel;

        public BufferedWaveProvider WaveOutProvider { get; set; }

        #endregion

        // URL for VLCstream
        private string _source;

        /// <summary>
        /// Initializes a new instance of the <see cref="VLCStream"/> class.
        /// </summary>
        /// 
        public VLCStream()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VLCStream"/> class.
        /// </summary>
        /// 
        /// <param name="source">URL, which provides VLCstream.</param>
        /// <param name="arguments"></param>
        public VLCStream(string source, string[] arguments)
        {
            _source = source;
            _arguments = arguments;
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
        //public event VideoSourceErrorEventHandler VideoSourceError;

        /// <summary>
        /// Video playing finished event.
        /// </summary>
        /// 
        /// <remarks><para>This event is used to notify clients that the video playing has finished.</para>
        /// </remarks>
        /// 
        //public event PlayingFinishedEventHandler PlayingFinished;

        /// <summary>
        /// Video source.
        /// </summary>
        /// 
        /// <remarks>URL, which provides VLCstream.</remarks>
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
                const long bytes = 0;
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
            get { return _isrunning; }
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
            lock (_lock)
            {
                if (!VlcHelper.VlcInstalled)
                    return;
                if (_stoprequested)
                    return;
                if (!IsRunning && !_starting)
                {
                    _starting = true;
                    // check source
                    if (string.IsNullOrEmpty(_source))
                        throw new ArgumentException("Audio source is not specified.");
                    lock (_lock)
                    {
                        _mFactory = new MediaPlayerFactory();

                        _mPlayer = _mFactory.CreatePlayer<IVideoPlayer>();
                        _mPlayer.Events.PlayerPlaying += EventsPlayerPlaying;
                        _mPlayer.Events.TimeChanged += EventsTimeChanged;

                        bool file = false;
                        try
                        {
                            if (File.Exists(_source))
                            {
                                file = true;
                            }
                        }
                        catch
                        {

                        }
                        if (file)
                            _mMedia = _mFactory.CreateMedia<IMediaFromFile>(_source, _arguments);
                        else
                            _mMedia = _mFactory.CreateMedia<IMedia>(_source, _arguments);


                        _mMedia = _mFactory.CreateMedia<IMedia>(_source, _arguments);
                        _mMedia.Events.DurationChanged += EventsDurationChanged;
                        _mMedia.Events.StateChanged += EventsStateChanged;
                        _mPlayer.Open(_mMedia);


                        _needsSetup = true;
                        var fc = new Func<SoundFormat, SoundFormat>(SoundFormatCallback);
                        _mPlayer.CustomAudioRenderer.SetFormatCallback(fc);
                        var ac = new AudioCallbacks {SoundCallback = SoundCallback};
                        _mPlayer.CustomAudioRenderer.SetCallbacks(ac);
                        _mPlayer.CustomAudioRenderer.SetExceptionHandler(Handler);

                        _mMedia.Parse(true);
                        _framesReceived = 0;
                        Duration = Time = 0;
                        _timestamp = _lastframetimestamp = DateTime.MinValue;
                        _mPlayer.Play();

                        //check if file source (isseekable in _mPlayer is not reliable)
                        Seekable = false;
                        try
                        {
                            var p = Path.GetFullPath(_mMedia.Input);
                            Seekable = !String.IsNullOrEmpty(p);
                        }
                        catch (Exception)
                        {
                            Seekable = false;
                        }
                    }
                }
            }
        }

        void Handler(Exception ex)
        {
            MainForm.LogExceptionToFile(ex);

        }

        void EventsStateChanged(object sender, MediaStateChange e)
        {
            lock (_lock)
            {
                switch (e.NewState)
                {
                    case MediaState.Ended:
                    case MediaState.Stopped:
                    case MediaState.Error:
                        if (_isrunning || _starting)
                        {
                            DisposePlayer();
                            Duration = Time = 0;

                            _starting = false;
                            _isrunning = false;
                            Thread.Sleep(500); //lets buffered frames stop before raising finished event
                            //if file source then dont reconnect
                            if (!Seekable && !_stoprequested)
                            {
                                if (AudioFinished != null)
                                    AudioFinished(sender, ReasonToFinishPlaying.DeviceLost);

                            }
                            else
                            {
                                if (AudioFinished != null)
                                    AudioFinished(sender, ReasonToFinishPlaying.StoppedByUser);
                            }
                        }
                        _stoprequested = false;
                        break;
                }
            }
        }

        private DateTime _timestamp = DateTime.MinValue;
        private DateTime _lastframetimestamp = DateTime.MinValue;

        public int TimeOut = 8000;

        public void CheckTimestamp()
        {
            //some feeds keep returning frames even when the connection is lost
            //this detects that by comparing timestamps from the eventstimechanged event
            //and signals an error if more than 8 seconds ago
            bool q = _timestamp > DateTime.MinValue && (Helper.Now - _timestamp).TotalMilliseconds > TimeOut;
            q = q || (_lastframetimestamp > DateTime.MinValue && (Helper.Now - _lastframetimestamp).TotalMilliseconds > TimeOut);

            if (q)
            {
                lock (_lock)
                {
                    if (_mPlayer != null && !_stoprequested)
                    {
                        if (_mPlayer.IsPlaying)
                        {
                            _starting = false;
                            _mPlayer.Stop();
                        }
                    }
                }
            }
        }

        public bool Seekable;

        public long Time, Duration;

        void EventsDurationChanged(object sender, MediaDurationChange e)
        {
            Duration = e.NewDuration;
        }


        void EventsTimeChanged(object sender, MediaPlayerTimeChanged e)
        {
            Time = e.NewTime;
            _timestamp = Helper.Now;
        }

        #region Audio Stuff
        public event DataAvailableEventHandler DataAvailable;
        public event LevelChangedEventHandler LevelChanged;
        public event AudioFinishedEventHandler AudioFinished;
        public event HasAudioStreamEventHandler HasAudioStream;

        public float Gain
        {
            get { return _gain; }
            set
            {
                _gain = value;
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

        public WaveFormat RecordingFormat
        {
            get { return _recordingFormat; }
            set
            {
                _recordingFormat = value;
            }
        }

        private int _realChannels;
        private SoundFormat SoundFormatCallback(SoundFormat sf)
        {
            if (_needsSetup)
            {
                int chan = _realChannels = sf.Channels;
                if (chan > 1)
                    chan = 2;//downmix
                _recordingFormat = new WaveFormat(sf.Rate, 16, chan);
                _waveProvider = new BufferedWaveProvider(RecordingFormat);
                _sampleChannel = new SampleChannel(_waveProvider);

                _sampleChannel.PreVolumeMeter += SampleChannelPreVolumeMeter;
                _needsSetup = false;
                if (HasAudioStream != null)
                {
                    HasAudioStream(this, EventArgs.Empty);
                    HasAudioStream = null;
                }
            }

            return sf;
        }

        void SampleChannelPreVolumeMeter(object sender, StreamVolumeEventArgs e)
        {
            if (LevelChanged != null && e != null && e.MaxSampleValues != null)
                LevelChanged(this, new LevelChangedEventArgs(e.MaxSampleValues));
        }

        private void SoundCallback(Sound soundData)
        {
            if (DataAvailable == null || _needsSetup) return;

            var data = new byte[soundData.SamplesSize];
            Marshal.Copy(soundData.SamplesData, data, 0, (int)soundData.SamplesSize);

            if (_realChannels > 2)
            {
                //resample audio to 2 channels
                data = ToStereo(data, _realChannels);
            }

            _waveProvider.AddSamples(data, 0, data.Length);

            if (Listening && WaveOutProvider != null)
            {
                WaveOutProvider.AddSamples(data, 0, data.Length);
            }

            //forces processing of volume level without piping it out
            var sampleBuffer = new float[data.Length];
            _sampleChannel.Read(sampleBuffer, 0, data.Length);

            if (DataAvailable != null)
                DataAvailable(this, new DataAvailableEventArgs((byte[])data.Clone()));
        }

        private byte[] ToStereo(byte[] input, int fromChannels)
        {
            double ratio = fromChannels / 2d;
            int newLen = Convert.ToInt32(input.Length / ratio);
            var output = new byte[newLen];
            int outputIndex = 0;
            for (int n = 0; n < input.Length; n += (fromChannels * 2))
            {
                // copy in the first 16 bit sample
                output[outputIndex++] = input[n];
                output[outputIndex++] = input[n + 1];
                output[outputIndex++] = input[n + 2];
                output[outputIndex++] = input[n + 3];
            }
            return output;
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
            lock (_lock)
            {
                if (_mPlayer != null && !_stoprequested)
                {
                    if (_mPlayer.IsPlaying)
                    {
                        _starting = false;
                        _stoprequested = true;
                        _mPlayer.Stop();
                    }
                }
            }
        }

        #endregion

        public void Seek(float percentage)
        {
            if (_mPlayer.IsSeekable)
            {
                _mPlayer.Position = percentage;
            }
        }


        private void DisposePlayer()
        {
            if (_sampleChannel != null)
            {
                _sampleChannel.PreVolumeMeter -= SampleChannelPreVolumeMeter;
                _sampleChannel = null;
            }

            lock (_lock)
            {
                if (_mMedia != null)
                {
                    _mMedia.Events.DurationChanged -= EventsDurationChanged;
                    _mMedia.Events.StateChanged -= EventsStateChanged;
                    _mMedia.Dispose();
                    _mMedia = null;
                }

                if (_mPlayer != null)
                {
                    _mPlayer.Events.PlayerPlaying -= EventsPlayerPlaying;
                    _mPlayer.Events.TimeChanged -= EventsTimeChanged;
                    _mPlayer.Dispose();
                    _mPlayer = null;
                    //^^^ causing major issues :(
                }

                if (_mFactory != null)
                {
                    _mFactory.Dispose();
                    _mFactory = null;

                }          
                

                if (_waveProvider != null && _waveProvider.BufferedBytes > 0)
                {
                    try
                    {
                        _waveProvider.ClearBuffer();
                    }
                    catch (Exception ex)
                    {
                        string m = ex.Message;
                    }
                }
                _waveProvider = null;

                Listening = false;
            }
        }

        private void EventsPlayerPlaying(object sender, EventArgs e)
        {
            _isrunning = true;
            _starting = false;
        }
    }
}