using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Declarations;
using Declarations.Events;
using Declarations.Media;
using Declarations.Players;
using Implementation;
using iSpyApplication.Video;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace iSpyApplication.Audio.streams
{
    class VLCStream: IAudioSource
    {
        private string _source;
        private float _volume;
        private bool _listening;

        private IMediaPlayerFactory _mFactory;
        private IMedia _mMedia;
        private IVideoPlayer _mPlayer;
        private bool _needsSetup = true;

        private WaveFormat _recordingFormat;
        private BufferedWaveProvider _waveProvider;
        private SampleChannel _sampleChannel;

        public BufferedWaveProvider WaveOutProvider { get; set; }

        public String Arguments;

        /// <summary>
        /// New frame event.
        /// </summary>
        /// 
        /// <remarks><para>Notifies clients about new available frame from audio source.</para>
        /// 
        /// <para><note>Since audio source may have multiple clients, each client is responsible for
        /// making a copy (cloning) of the passed audio frame, because the audio source disposes its
        /// own original copy after notifying of clients.</note></para>
        /// </remarks>
        /// 
        public event DataAvailableEventHandler DataAvailable;

        /// <summary>
        /// New frame event.
        /// </summary>
        /// 
        /// <remarks><para>Notifies clients about new available frame from audio source.</para>
        /// 
        /// <para><note>Since audio source may have multiple clients, each client is responsible for
        /// making a copy (cloning) of the passed audio frame, because the audio source disposes its
        /// own original copy after notifying of clients.</note></para>
        /// </remarks>
        /// 
        public event LevelChangedEventHandler LevelChanged;

        /// <summary>
        /// audio source error event.
        /// </summary>
        /// 
        /// <remarks>This event is used to notify clients about any type of errors occurred in
        /// audio source object, for example internal exceptions.</remarks>
        /// 
        public event AudioSourceErrorEventHandler AudioSourceError;

        /// <summary>
        /// audio playing finished event.
        /// </summary>
        /// 
        /// <remarks><para>This event is used to notify clients that the audio playing has finished.</para>
        /// </remarks>
        /// 
        public event AudioFinishedEventHandler AudioFinished;

        /// <summary>
        /// audio source.
        /// </summary>
        /// 
        /// <remarks>URL, which provides JPEG files.</remarks>
        /// 
        public virtual string Source
        {
            get { return _source; }
            set { _source = value; }
        }

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
                    WaveOutProvider = new BufferedWaveProvider(RecordingFormat) {DiscardOnBufferOverflow = true};
                }
                
                _listening = value;
            }
        }


        /// <summary>
        /// State of the audio source.
        /// </summary>
        /// 
        /// <remarks>Current state of audio source object - running or not.</remarks>
        /// 
        public bool IsRunning
        {
            get
            {
                if (_mPlayer != null)
                {
                    // check thread status
                    return _mPlayer.IsPlaying;
                }
                return false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalDeviceStream"/> class.
        /// </summary>
        /// 
        public VLCStream() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalDeviceStream"/> class.
        /// </summary>
        /// 
        /// <param name="source">source, which provides audio data.</param>
        /// 
        public VLCStream(string source)
        {
            _source = source;
        }

        /// <summary>
        /// Start audio source.
        /// </summary>
        /// 
        /// <remarks>Starts audio source and return execution to caller. audio source
        /// object creates background thread and notifies about new frames with the
        /// help of <see cref="DataAvailable"/> event.</remarks>
        /// 
        /// <exception cref="ArgumentException">audio source is not specified.</exception>
        /// 
        public void Start()
        {
            if (!VlcHelper.VlcInstalled)
                return;
            if (!IsRunning)
            {
                // check source
                if (string.IsNullOrEmpty(_source))
                    throw new ArgumentException("Audio source is not specified.");

                DisposePlayer();

                _mFactory = new MediaPlayerFactory(false);

                _mPlayer = _mFactory.CreatePlayer<IVideoPlayer>();
                _mPlayer.Events.PlayerPlaying += EventsPlayerPlaying;
                _mPlayer.Events.PlayerStopped += EventsPlayerStopped;
                _mPlayer.Events.PlayerEncounteredError += EventsPlayerEncounteredError;

                string[] args = Arguments.Trim(',').Split(Environment.NewLine.ToCharArray(),
                                                                                StringSplitOptions.RemoveEmptyEntries);
                List<String> inargs = args.ToList();
                inargs.Add(":sout=#transcode{vcodec=none}:Display");

                _mMedia = _mFactory.CreateMedia<IMedia>(_source, inargs.ToArray());

                _mPlayer.Open(_mMedia);

                GC.KeepAlive(_mFactory);
                GC.KeepAlive(_mPlayer);
                GC.KeepAlive(_mMedia);

                _needsSetup = true;
                var fc = new Func<SoundFormat, SoundFormat>(SoundFormatCallback);
                _mPlayer.CustomAudioRenderer.SetFormatCallback(fc);
                var ac = new AudioCallbacks {SoundCallback = SoundCallback};
                _mPlayer.CustomAudioRenderer.SetCallbacks(ac);
                _mMedia.Events.ParsedChanged += EventsParsedChanged;
                _mMedia.Parse(true);

                
                _mPlayer.Play();               

            }
        }

        private void EventsParsedChanged(object sender, MediaParseChange e)
        {
            Console.WriteLine(e.Parsed);
        }

        private SoundFormat SoundFormatCallback(SoundFormat sf)
        {
            if (_needsSetup)
            {
                _recordingFormat = new WaveFormat(sf.Rate, 16, sf.Channels);
                _waveProvider = new BufferedWaveProvider(RecordingFormat);
                _sampleChannel = new SampleChannel(_waveProvider);
                _sampleChannel.PreVolumeMeter += SampleChannelPreVolumeMeter;
                
                _needsSetup = false;
            }

            return sf;
        }

        void SampleChannelPreVolumeMeter(object sender, StreamVolumeEventArgs e)
        {
            if (LevelChanged != null)
                LevelChanged(this, new LevelChangedEventArgs(e.MaxSampleValues));
        }

        private void SoundCallback(Sound soundData)
        {
            if (DataAvailable == null || _needsSetup) return;

            if (_sampleChannel != null)
            {
                var samples = new byte[soundData.SamplesSize];
                Marshal.Copy(soundData.SamplesData, samples, 0, (int)soundData.SamplesSize);

                _waveProvider.AddSamples(samples, 0, samples.Length);

                var sampleBuffer = new float[samples.Length];
                _sampleChannel.Read(sampleBuffer, 0, samples.Length);

                if (Listening && WaveOutProvider != null)
                {
                    WaveOutProvider.AddSamples(samples, 0, samples.Length);
                }
                var da = new DataAvailableEventArgs((byte[])samples.Clone());
                if (DataAvailable != null)
                    DataAvailable(this, da);
            }
        }


        private void EventsPlayerPlaying(object sender, EventArgs e)
        {

        }

        private void EventsPlayerStopped(object sender, EventArgs e)
        {
            if (AudioFinished != null)
                AudioFinished(sender, ReasonToFinishPlaying.StoppedByUser);
        }

        private void EventsPlayerEncounteredError(object sender, EventArgs e)
        {
            DisposePlayer();
            if (AudioSourceError!=null)
                AudioSourceError(sender, new AudioSourceErrorEventArgs("Error playing stream"));
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

        /// <summary>
        /// Stop audio source.
        /// </summary>
        /// 
        /// <remarks><para>Stops audio source.</para>
        /// </remarks>
        /// 
        public void Stop()
        {
            if (_mPlayer != null)
            {
                _mPlayer.Stop();
                _mMedia.Dispose();
                _mFactory.Dispose();
                _mPlayer = null;
                _mMedia = null;
                _mFactory = null;
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
    }
}
