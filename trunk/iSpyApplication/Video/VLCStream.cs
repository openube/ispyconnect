using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using AForge.Video;
using Declarations;
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
    public class VlcStream : IVideoSource, IAudioSource
    {
        public int FormatWidth = 320, FormatHeight = 240;
        private volatile bool _isrunning;
        public string[] Arguments;
        private int _framesReceived;
        private IMediaPlayerFactory _mFactory;
        private IMedia _mMedia;
        private IVideoPlayer _mPlayer;
        public volatile bool Isstopping;


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
            Arguments = arguments;
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
            get { return _isrunning; }
        }

        private bool _starting;






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
            Isstopping = false;
            if (!IsRunning && !_starting)
            {
                _starting = true;
                // check source
                if (string.IsNullOrEmpty(_source))
                    throw new ArgumentException("Video source is not specified.");

                DisposePlayer();

                _mFactory = new MediaPlayerFactory(false);

                _mPlayer = _mFactory.CreatePlayer<IVideoPlayer>();
                _mPlayer.Events.PlayerPlaying += EventsPlayerPlaying;
                _mPlayer.Events.PlayerStopped += EventsPlayerStopped;
                _mPlayer.Events.PlayerEncounteredError += EventsPlayerEncounteredError;


                _mMedia = _mFactory.CreateMedia<IMedia>(_source, Arguments);
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

                _mMedia.Parse(true);
                _framesReceived = 0;
                _mPlayer.Play();
            }
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
        private SoundFormat SoundFormatCallback(SoundFormat sf)
        {
            if (_needsSetup)
            {
                _recordingFormat = new WaveFormat(sf.Rate, 16, sf.Channels);
                _waveProvider = new BufferedWaveProvider(RecordingFormat);
                _sampleChannel = new SampleChannel(_waveProvider);

                _meteringProvider = new MeteringSampleProvider(_sampleChannel);
                _meteringProvider.StreamVolume += MeteringProviderStreamVolume;
                _needsSetup = false;
                if (HasAudioStream != null)
                    HasAudioStream(this, EventArgs.Empty);
            }

            return sf;
        }

        private void SoundCallback(Sound soundData)
        {
            if (DataAvailable == null || _needsSetup) return;

            var samples = new byte[soundData.SamplesSize];
            Marshal.Copy(soundData.SamplesData, samples, 0, (int)soundData.SamplesSize);

            _waveProvider.AddSamples(samples, 0, samples.Length);

            if (Listening && WaveOutProvider != null)
            {
                WaveOutProvider.AddSamples(samples, 0, samples.Length);
            }

            //forces processing of volume level without piping it out
            var sampleBuffer = new float[samples.Length];
            _meteringProvider.Read(sampleBuffer, 0, samples.Length);

            if (DataAvailable != null)
                DataAvailable(this, new DataAvailableEventArgs((byte[])samples.Clone()));
        }
        #endregion

        private void FrameCallback(Bitmap frame)
        {
            _framesReceived++;
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
            if (_mPlayer != null)
            {
                Isstopping = true;
                _mPlayer.Stop();
            }
        }
        #endregion

        private void EventsPlayerEncounteredError(object sender, EventArgs e)
        {
            Debug.Print("VLC Error");
            _starting = false;

            DisposePlayer();

            if (VideoSourceError != null)
                VideoSourceError(sender, new VideoSourceErrorEventArgs("Error playing stream"));
        }

        private void EventsPlayerStopped(object sender, EventArgs e)
        {
            _starting = false;
            Debug.Print("VLC Stopped");
            _isrunning = false;
            if (PlayingFinished != null)
                PlayingFinished(sender, ReasonToFinishPlaying.StoppedByUser);
            if (AudioFinished != null)
                AudioFinished(sender, Audio.ReasonToFinishPlaying.StoppedByUser);
        }

        private void DisposePlayer()
        {
            if (_mFactory == null)
                return;
            try
            {
                _mPlayer.Dispose();
            }
            catch { }
            try
            {
                _mFactory.Dispose();
            }
            catch { }
            try
            {
                _mMedia.Dispose();
            }
            catch { }

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