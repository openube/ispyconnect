using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using AForge.Video;
using Declarations;
using Declarations.Events;
using Declarations.Media;
using Declarations.Players;
using Implementation;
using iSpyApplication.Audio;
using iSpyApplication.Audio.streams;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NewFrameEventHandler = AForge.Video.NewFrameEventHandler;
using ReasonToFinishPlaying = AForge.Video.ReasonToFinishPlaying;

namespace iSpyApplication.Video
{
    public class VlcStream : IVideoSource, IAudioSource, ISupportsAudio
    {
        public int FormatWidth = 320, FormatHeight = 240;
        private volatile bool _isrunning;
        private bool _starting;
        private readonly string[] _arguments;
        private int _framesReceived;
        private IMediaPlayerFactory _mFactory;
        private IMedia _mMedia;
        private IVideoPlayer _mPlayer;
        private volatile bool _isstopping;


        #region Audio
        private float _volume;
        private bool _listening;

        private bool _needsSetup = true;

        public int BytePacket = 400;

        private WaveFormat _recordingFormat;
        private BufferedWaveProvider _waveProvider;
        private MeteringSampleProvider _meteringProvider;
        private SampleChannel _sampleChannel;

        public BufferedWaveProvider WaveOutProvider { get; set; }

        #endregion

        // URL for VLCstream
        private string _source;

        /// <summary>
        /// Initializes a new instance of the <see cref="MJPEGStream"/> class.
        /// </summary>
        /// 
        public VlcStream()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MJPEGStream"/> class.
        /// </summary>
        /// 
        /// <param name="source">URL, which provides VLCstream.</param>
        /// <param name="arguments"></param>
        public VlcStream(string source, string[] arguments)
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
            if (!VlcHelper.VlcInstalled)
                return;
            _isstopping = false;
            if (!IsRunning && !_starting)
            {
                _starting = true;
                // check source
                if (string.IsNullOrEmpty(_source))
                    throw new ArgumentException("Video source is not specified.");

                _mFactory = new MediaPlayerFactory();

                _mPlayer = _mFactory.CreatePlayer<IVideoPlayer>();
                _mPlayer.Events.PlayerPlaying += EventsPlayerPlaying;
                _mPlayer.Events.PlayerStopped += EventsPlayerStopped;
                _mPlayer.Events.PlayerEncounteredError += EventsPlayerEncounteredError;
                _mPlayer.Events.TimeChanged += EventsTimeChanged;


                _mMedia = _mFactory.CreateMedia<IMedia>(_source, _arguments);
                _mMedia.Events.DurationChanged += EventsDurationChanged;
                _mMedia.Events.StateChanged += EventsStateChanged;
                _mPlayer.Open(_mMedia);

                GC.KeepAlive(_mFactory);
                GC.KeepAlive(_mPlayer);
                GC.KeepAlive(_mMedia);

                _needsSetup = true;
                var fc = new Func<SoundFormat, SoundFormat>(SoundFormatCallback);
                _mPlayer.CustomAudioRenderer.SetFormatCallback(fc);
                var ac = new AudioCallbacks { SoundCallback = SoundCallback };
                _mPlayer.CustomAudioRenderer.SetCallbacks(ac);

                _mPlayer.CustomRenderer.SetFormat(new BitmapFormat(FormatWidth, FormatHeight, ChromaType.RV24));
                _mPlayer.CustomRenderer.SetCallback(FrameCallback);

                _mPlayer.CustomRenderer.SetExceptionHandler(Handler);
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

        void Handler(Exception ex)
        {
            MainForm.LogExceptionToFile(ex);

        }

        void EventsStateChanged(object sender, MediaStateChange e)
        {

            switch (e.NewState)
            {
                case MediaState.Ended:
                case MediaState.Stopped:
                case MediaState.Error:
                    
                    _starting = false;
                    _isrunning = false;
                    //if file source then dont reconnect
                    if (!Seekable)
                    {
                        if (PlayingFinished != null)
                            PlayingFinished(sender, ReasonToFinishPlaying.VideoSourceError);
                        if (AudioFinished != null)
                            AudioFinished(sender, ReasonToFinishPlaying.VideoSourceError);
                        
                    }
                    else
                    {
                        if (PlayingFinished != null)
                            PlayingFinished(sender, ReasonToFinishPlaying.StoppedByUser);
                        if (AudioFinished != null)
                            AudioFinished(sender, ReasonToFinishPlaying.StoppedByUser);
                    }
                    
                    break;
            }
        }

        private DateTime _timestamp = DateTime.MinValue;
        private DateTime _lastframetimestamp = DateTime.MinValue;
        
        public int TimeOut = 8000;

        public void CheckVideoTimestamp()
        {
            //some feeds keep returning frames even when the connection is lost
            //this detects that by comparing timestamps from the eventstimechanged event
            //and signals an error if more than 8 seconds ago
            bool q = _timestamp > DateTime.MinValue && (DateTime.Now - _timestamp).TotalMilliseconds > TimeOut;
            q = q || (_lastframetimestamp > DateTime.MinValue && (DateTime.Now - _lastframetimestamp).TotalMilliseconds > TimeOut);

            if (q)
            {               
                _starting = false;
                if (_mPlayer != null && !_isstopping)
                {
                    _isstopping = true;
                    _mPlayer.Events.PlayerStopped -= EventsPlayerStopped;
                    _mPlayer.Events.PlayerStopped += EventsPlayerStoppedForced;
                    _mPlayer.Stop();
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
            _timestamp = DateTime.Now;
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

        void MeteringProviderStreamVolume(object sender, StreamVolumeEventArgs e)
        {
            if (LevelChanged != null)
                LevelChanged(this, new LevelChangedEventArgs(e.MaxSampleValues));

        }
        public WaveFormat RecordingFormat
        {
            get { return _recordingFormat; }
            set
            {
                _recordingFormat = value;
            }
        }

        private int RealChannels = 0;
        private SoundFormat SoundFormatCallback(SoundFormat sf)
        {
            if (_needsSetup)
            {
                int chan = RealChannels = sf.Channels;
                if (chan > 1)
                    chan = 2;//downmix
                _recordingFormat = new WaveFormat(sf.Rate, 16, chan);
                _waveProvider = new BufferedWaveProvider(RecordingFormat);
                _sampleChannel = new SampleChannel(_waveProvider);

                _meteringProvider = new MeteringSampleProvider(_sampleChannel);
                _meteringProvider.StreamVolume += MeteringProviderStreamVolume;
                _needsSetup = false;
                if (HasAudioStream != null)
                {
                    HasAudioStream(this, EventArgs.Empty);
                    HasAudioStream = null;
                }
            }

            return sf;
        }

        private void SoundCallback(Sound soundData)
        {
            if (DataAvailable == null || _needsSetup) return;

            var data = new byte[soundData.SamplesSize];
            Marshal.Copy(soundData.SamplesData, data, 0, (int)soundData.SamplesSize);

            if (RealChannels>2)
            {
                //resample audio to 2 channels
                data = ToStereo(data, RealChannels);
            }

            _waveProvider.AddSamples(data, 0, data.Length);

            if (Listening && WaveOutProvider != null)
            {
                WaveOutProvider.AddSamples(data, 0, data.Length);
            }

            //forces processing of volume level without piping it out
            var sampleBuffer = new float[data.Length];
            _meteringProvider.Read(sampleBuffer, 0, data.Length);

            if (DataAvailable != null)
                DataAvailable(this, new DataAvailableEventArgs((byte[])data.Clone()));
        }

        private byte[] ToStereo(byte[] input, int fromChannels)
        {
            double ratio = fromChannels / 2d;
            int newLen = Convert.ToInt32(input.Length/ratio);
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

        private void FrameCallback(Bitmap frame)
        {
            _framesReceived++;
            _lastframetimestamp = DateTime.Now;
            if (NewFrame != null)
            {
                NewFrame(this, new NewFrameEventArgs(frame));
            }
            frame.Dispose();
        }


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
            if (_mPlayer != null && !_isstopping)
            {
                if (_mPlayer.IsPlaying)
                {
                    _isstopping = true;
                    _mPlayer.Stop();
                }
                else
                {
                    _isstopping = false;
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

        private void EventsPlayerEncounteredError(object sender, EventArgs e)
        {
            _starting = false;

            if (VideoSourceError != null)
                VideoSourceError(sender, new VideoSourceErrorEventArgs("Error playing stream"));
            if (AudioSourceError != null)
                AudioSourceError(sender, new AudioSourceErrorEventArgs("Error playing stream"));
        }

        private void EventsPlayerStopped(object sender, EventArgs e)
        {
            DisposePlayer();
            Duration = Time = 0;
            _starting = false;
            
            _isrunning = false;
            if (PlayingFinished != null)
                PlayingFinished(sender, ReasonToFinishPlaying.StoppedByUser);
            if (AudioFinished != null)
                AudioFinished(sender, ReasonToFinishPlaying.StoppedByUser);
        }

        private void EventsPlayerStoppedForced(object sender, EventArgs e)
        {
            DisposePlayer();
            Duration = Time = 0;
            _starting = false;

            _isrunning = false;

            if (PlayingFinished != null)
                PlayingFinished(sender, ReasonToFinishPlaying.VideoSourceError);
            if (AudioFinished != null)
                AudioFinished(sender, ReasonToFinishPlaying.VideoSourceError);

        }

        private void DisposePlayer()
        {
            if (_mFactory == null)
                return;
            try
            {
                if (_mPlayer.Events != null)
                {
                    _mPlayer.Events.PlayerStopped -= EventsPlayerStopped;
                    _mPlayer.Events.PlayerPlaying -= EventsPlayerPlaying;
                    _mPlayer.Events.PlayerEncounteredError -= EventsPlayerEncounteredError;
                    _mPlayer.Events.TimeChanged -= EventsTimeChanged;
                }

                _mPlayer.Dispose();
            }
            catch { }
            try
            {
                _mFactory.Dispose();
            }
            catch { }

            if (_mMedia != null)
            {
                try
                {
                    if (_mMedia.Events != null)
                    {
                        _mMedia.Events.DurationChanged -= EventsDurationChanged;
                        _mMedia.Events.StateChanged -= EventsStateChanged;
                    }
                    _mMedia.Dispose();
                }
                catch
                {
                }
            }

            _mPlayer = null;
            _mFactory = null;
            _mMedia = null;
        }

        private void EventsPlayerPlaying(object sender, EventArgs e)
        {
            _isrunning = true;
            _starting = false;
        }
    }
}