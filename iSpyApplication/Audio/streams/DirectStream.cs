using System;
using System.IO;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace iSpyApplication.Audio.streams
{
    class DirectStream: IAudioSource
    {
        private Stream _stream;
        private float _volume;
        private bool _listening;
        private ManualResetEvent _stopEvent;

        private Thread _thread;

        private WaveFormat _recordingFormat;
        private BufferedWaveProvider _waveProvider;
        private SampleChannel _sampleChannel;

        public int PacketSize = 882;
        public int Interval = 40;

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
        /// <remarks></remarks>
        /// 
        public virtual string Source
        {
            get { return "direct stream"; }
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
        /// Initializes a new instance of the <see cref="LocalDeviceStream"/> class.
        /// </summary>
        /// 
        public DirectStream() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalDeviceStream"/> class.
        /// </summary>
        /// 
        /// <param name="stream">source, which provides audio data.</param>
        /// 
        public DirectStream(Stream stream)
        {
            _stream = stream;
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
                if (_stream == null)
                    throw new ArgumentException("Audio source is not specified.");

                _waveProvider = new BufferedWaveProvider(RecordingFormat);
                _sampleChannel = new SampleChannel(_waveProvider);
                _sampleChannel.PreVolumeMeter += new EventHandler<StreamVolumeEventArgs>(_sampleChannel_PreVolumeMeter);

                _stopEvent = new ManualResetEvent(false);
                _thread = new Thread(DirectStreamListener)
                                          {
                                              Name = "DirectStream Audio Receiver"
                                          };
                _thread.Start();

            }
        }

        void _sampleChannel_PreVolumeMeter(object sender, StreamVolumeEventArgs e)
        {
            if (LevelChanged != null)
                LevelChanged(this, new LevelChangedEventArgs(e.MaxSampleValues));
        }


        private void DirectStreamListener()
        {
            try
            {
                var data = new byte[PacketSize];
                if (_stream != null)
                {
                    while (!_stopEvent.WaitOne(0, false))
                    {
                       
                        if (DataAvailable != null)
                        {
                            int recbytesize = _stream.Read(data, 0, PacketSize);
                            if (recbytesize > 0)
                            {
                                if (_sampleChannel != null)
                                {
                                    _waveProvider.AddSamples(data, 0, recbytesize);

                                    var sampleBuffer = new float[recbytesize];
                                    _sampleChannel.Read(sampleBuffer, 0, recbytesize);

                                    if (Listening && WaveOutProvider != null)
                                    {
                                        WaveOutProvider.AddSamples(data, 0, recbytesize);
                                    }
                                    var da = new DataAvailableEventArgs((byte[]) data.Clone());
                                    DataAvailable(this, da);
                                }
                                
                            }
                            else
                            {
                                break;
                            }
                            
                            
                            if (_stopEvent.WaitOne(Interval, false))
                                break;
                        }

                        
                    }
                }

                if (AudioFinished != null)
                    AudioFinished(this, ReasonToFinishPlaying.StoppedByUser);
            }
            catch (Exception e)
            {
                if (AudioSourceError!=null)
                    AudioSourceError(this, new AudioSourceErrorEventArgs(e.Message));
                MainForm.LogExceptionToFile(e);
            }
            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }


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
            if (this.IsRunning)
            {
                _stopEvent.Set();
                _thread.Join();

                Free();
            }
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
