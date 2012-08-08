using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace iSpyApplication.Audio.streams
{
    class LocalDeviceStream: IAudioSource
    {
        private string _source;
        private volatile bool _isrunning;
        private float _volume;
        private bool _listening;

        private WaveIn _waveIn;
        private WaveInProvider _waveProvider;
        private SampleChannel _sampleChannel;

        public BufferedWaveProvider WaveOutProvider { get; set; }


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
                    WaveOutProvider = new BufferedWaveProvider(RecordingFormat) { DiscardOnBufferOverflow = true };
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
                return _isrunning;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalDeviceStream"/> class.
        /// </summary>
        /// 
        public LocalDeviceStream() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalDeviceStream"/> class.
        /// </summary>
        /// 
        /// <param name="source">source, which provides audio data.</param>
        /// 
        public LocalDeviceStream(string source)
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
            if (!IsRunning)
            {
                // check source
                if (string.IsNullOrEmpty(_source))
                    throw new ArgumentException("Audio source is not specified.");

                int i = 0, selind = -1;
                for (int n = 0; n < WaveIn.DeviceCount; n++)
                {
                    if (WaveIn.GetCapabilities(n).ProductName == _source)
                        selind = i;
                    i++;
                }
                if (selind == -1)
                {
                    //device no longer connected
                    if (AudioSourceError!=null)
                        AudioSourceError(this, new AudioSourceErrorEventArgs("not connected"));
                    return;
                }

                _waveIn = new WaveIn {BufferMilliseconds = 200, DeviceNumber = selind, WaveFormat = RecordingFormat};
                _waveIn.DataAvailable += WaveInDataAvailable;
                _waveIn.RecordingStopped += WaveInRecordingStopped;

                _waveProvider = new WaveInProvider(_waveIn);
                _sampleChannel = new SampleChannel(_waveProvider);
                _sampleChannel.PreVolumeMeter+=SampleChannelPreVolumeMeter;
                _waveIn.StartRecording();

            }
        }

        void SampleChannelPreVolumeMeter(object sender, StreamVolumeEventArgs e)
        {
            if (LevelChanged != null)
            {
                LevelChanged(this, new LevelChangedEventArgs(e.MaxSampleValues));
            }
        }

        void WaveInDataAvailable(object sender, WaveInEventArgs e)
        {
            _isrunning = true;
            if (DataAvailable != null)
            {
                //forces processing of volume level without piping it out
                if (_sampleChannel != null)
                {
                    var sampleBuffer = new float[e.BytesRecorded];
                    _sampleChannel.Read(sampleBuffer, 0, e.BytesRecorded);
                    
                    if (Listening && WaveOutProvider!=null)
                    {
                        WaveOutProvider.AddSamples(e.Buffer, 0,e.Buffer.Length);
                    }
                    var da = new DataAvailableEventArgs((byte[])e.Buffer.Clone());
                    DataAvailable(this, da);
                }
            }
        }

        void WaveInRecordingStopped(object sender, EventArgs e)
        {
            _isrunning = false;
            if (AudioFinished != null)
                AudioFinished(this, ReasonToFinishPlaying.StoppedByUser);
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
            if (_waveIn != null)
            {
                // signal to stop
                try {_waveIn.StopRecording();}
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                }
                _waveIn = null;
            }
        }


        public WaveFormat RecordingFormat { get; set; }
    }
}
