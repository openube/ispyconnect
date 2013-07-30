using System;
using System.Threading;
using AForge.Video;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using AudioFileReader = iSpy.Video.FFMPEG.AudioFileReader;

namespace iSpyApplication.Audio.streams
{
    class FFMPEGAudioStream: IAudioSource
    {
        private string _source;
        private bool _listening;
        private float _volume;
        private ManualResetEvent _stopEvent;
        private AudioFileReader _afr;
        private volatile bool _waitingOnMutex;

        private Thread _thread;

        private BufferedWaveProvider _waveProvider;
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
            set { 
                _volume = value;
                if (_sampleChannel!=null)
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

                if (value)
                {
                    WaveOutProvider = new BufferedWaveProvider(RecordingFormat) { DiscardOnBufferOverflow = true };
                }
                _listening = value;
            }
        }

        private bool IsFileSource
        {
            get { return _source != null && _source.IndexOf("://", StringComparison.Ordinal) == -1; }
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
                }
                return false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalDeviceStream"/> class.
        /// </summary>
        /// 
        public FFMPEGAudioStream() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalDeviceStream"/> class.
        /// </summary>
        /// 
        /// <param name="source">source, which provides audio data.</param>
        /// 
        public FFMPEGAudioStream(string source)
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
            if (IsRunning)
                return;
            if (string.IsNullOrEmpty(_source))
                throw new ArgumentException("Audio source is not specified.");

            _stopEvent = new ManualResetEvent(false);
            _thread = new Thread(FfmpegListener)
            {
                Name = "FFMPEG Audio Receiver (" + _source + ")"
            };
            _thread.Start();
            //_stopped = false;

                        
        }

        public string Cookies = "";
        public int Timeout = 8000;
        public int AnalyseDuration = 2000;

        //public bool NoBuffer;
        ReasonToFinishPlaying _reasonToStop = ReasonToFinishPlaying.StoppedByUser;

        private void FfmpegListener()
        {
            _reasonToStop = ReasonToFinishPlaying.StoppedByUser;
            _afr = null;
            _waitingOnMutex = true;
            Program.WriterMutex.WaitOne();
            _waitingOnMutex = false;
            bool open = false;
            
            try
            {
                _afr = new AudioFileReader();
                int i = _source.IndexOf("://", StringComparison.Ordinal);
                if (i>-1)
                {
                    _source = _source.Substring(0, i).ToLower() + _source.Substring(i);
                }
                _afr.Timeout = Timeout;
                _afr.AnalyzeDuration = AnalyseDuration;
                _afr.Open(_source);
                open = true;
            }
            catch (Exception ex)
            {
                MainForm.LogErrorToFile(ex.Message);
            }
            finally
            {
                try
                {
                    Program.WriterMutex.ReleaseMutex();
                }
                catch (ObjectDisposedException)
                {
                    //can happen on shutdown
                }
            }

            if (_afr == null || !_afr.IsOpen || !open)
            {
                if (AudioFinished!=null)
                    AudioFinished(this, ReasonToFinishPlaying.VideoSourceError);
                return;
            }


            RecordingFormat = new WaveFormat(_afr.SampleRate, 16, _afr.Channels);
            _waveProvider = new BufferedWaveProvider(RecordingFormat) { DiscardOnBufferOverflow = true };
            
            
            _sampleChannel = new SampleChannel(_waveProvider);
            _sampleChannel.PreVolumeMeter += SampleChannelPreVolumeMeter;

            int mult = _afr.BitsPerSample / 8;
            double btrg = Convert.ToDouble(_afr.SampleRate * mult * _afr.Channels);
            DateTime lastPacket = DateTime.Now;
            bool realTime = !IsFileSource;

            var buffer = new byte[_afr.SampleRate* 2 * _afr.Channels * 2]; //2 second buffer
            int buffcount = 0;
            bool err = false;
            try
            {
                DateTime req = DateTime.Now;
                while (!_stopEvent.WaitOne(10, false))
                {
                    byte[] data = _afr.ReadAudioFrame();
                    if (data==null || data.Equals(0))
                    {
                        if (!realTime)
                        {
                            _reasonToStop = ReasonToFinishPlaying.EndOfStreamReached;
                            break;
                        }
                    }
                    if (data!=null && data.Length > 0)
                    {
                        lastPacket = DateTime.Now;
                        if (DataAvailable != null)
                        {
                            //forces processing of volume level without piping it out
                            _waveProvider.AddSamples(data, 0, data.Length);

                            var sampleBuffer = new float[data.Length];
                            _sampleChannel.Read(sampleBuffer, 0, data.Length);
                            DataAvailable(this, new DataAvailableEventArgs((byte[])data.Clone()));

                            if (WaveOutProvider!=null && Listening)
                            {
                                WaveOutProvider.AddSamples(data, 0, data.Length);
                            }
                            
                        }

                        if (realTime)
                        {
                            if (_stopEvent.WaitOne(30, false))
                                break;
                        }
                        else
                        {
                            //
                            double f = (data.Length/btrg)*1000;
                            if (f > 0)
                            {
                                var span = DateTime.Now.Subtract(req);
                                var msec = Convert.ToInt32(f - (int) span.TotalMilliseconds);
                                if ((msec > 0) && (_stopEvent.WaitOne(msec, false)))
                                    break;
                                req = DateTime.Now;
                            }
                        }
                    }
                    else
                    {
                        if ((DateTime.Now - lastPacket).TotalMilliseconds > 5000)
                        {
                            throw new Exception("Audio source timeout");
                        }
                        if (_stopEvent.WaitOne(30, false))
                            break;
                    }
                    
                }
                
            }
            catch (Exception e)
            {
                if (AudioSourceError!=null)
                    AudioSourceError(this, new AudioSourceErrorEventArgs(e.Message));
                MainForm.LogExceptionToFile(e);

                _reasonToStop = ReasonToFinishPlaying.DeviceLost;
                err = true;
            }

            try
            {
                _afr.Close();
            }
            catch(Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            _afr.Dispose();

            // release events
            _stopEvent.Close();
            _stopEvent.Dispose();
            _stopEvent = null;

            if (IsFileSource && !err)
                _reasonToStop = ReasonToFinishPlaying.StoppedByUser;

            if (AudioFinished != null)
                AudioFinished(this, _reasonToStop);
        }

        void SampleChannelPreVolumeMeter(object sender, StreamVolumeEventArgs e)
        {
            if (LevelChanged != null)
            {
                LevelChanged(this, new LevelChangedEventArgs(e.MaxSampleValues));
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
            if (IsRunning)
            {
                if (_afr != null)
                {
                    _afr.Abort();

                }
                _stopEvent.Set();

                if (!_waitingOnMutex)
                {
                    //wait for video source shutdown
                    //dont change this to a plain join as if a scheduled reconnect coincides with a manual reset there can be a deadlock
                    _thread.Join(4000);

                }
                else
                {
                    //thread is waiting on the mutex
                    _thread.Abort();
                }
                _waitingOnMutex = false;
            }
        }

        public WaveFormat RecordingFormat { get; set; }
    }
}
