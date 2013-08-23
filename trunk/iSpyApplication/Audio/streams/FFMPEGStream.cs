using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
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
        private float _gain;
        private ManualResetEvent _stopEvent;
        private AudioFileReader _afr;

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
        //public event AudioSourceErrorEventHandler AudioSourceError;

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


        private volatile bool _isrunning;
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
                return _isrunning;
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
            _isrunning = true;
            _reasonToStop = ReasonToFinishPlaying.StoppedByUser;
            _afr = null;
            bool open = false;
            string errmsg = "";
            
            try
            {
                Program.WriterMutex.WaitOne();
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
                ShutDown("Could not open audio stream" + ": " + _source);
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
                errmsg = e.Message;
            }
            ShutDown(errmsg);
        }

        private void ShutDown(string errmsg)
        {
            bool err = !String.IsNullOrEmpty(errmsg);
            if (err)
            {
                //if (AudioSourceError != null)
                //    AudioSourceError(this, new AudioSourceErrorEventArgs(errmsg));

                MainForm.LogErrorToFile(errmsg + ": " + _source);
                _reasonToStop = ReasonToFinishPlaying.DeviceLost;
            }

            if (IsFileSource && !err)
                _reasonToStop = ReasonToFinishPlaying.StoppedByUser;

            try
            {
                Debug.WriteLine("Closing " + _source);
                _afr.Close();
                Debug.WriteLine("Closed " + _source);
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            Debug.WriteLine("Disposing " + _source);
            _afr.Dispose();
            Debug.WriteLine("Disposed " + _source);

            // release events
            _stopEvent.Close();
            _stopEvent.Dispose();
            _stopEvent = null;

            if (AudioFinished != null)
                AudioFinished(this, _reasonToStop);
            Debug.WriteLine("Quit");
            _isrunning = false;
        }


        void SampleChannelPreVolumeMeter(object sender, StreamVolumeEventArgs e)
        {
            if (LevelChanged != null)
            {
                LevelChanged(this, new LevelChangedEventArgs(e.MaxSampleValues));
            }
        }

        private bool _stopping;
        /// <summary>
        /// Stop audio source.
        /// </summary>
        /// 
        /// <remarks><para>Stops audio source.</para>
        /// </remarks>
        /// 
        public void Stop()
        {
            if (IsRunning && !_stopping)
            {
                _stopping = true;
                if (_afr != null)
                {
                    try
                    {
                        _afr.Abort();
                    }
                    catch
                    {
                        //disposed?
                    }

                }
                if (_thread != null && _thread.IsAlive)
                {
                    _stopEvent.Set();

                    while (_thread != null && _thread.IsAlive)
                    {
                        try
                        {
                            _thread.Join(1000);
                        }
                        catch { }
                        Application.DoEvents();
                    }
                }
                _thread = null;
                _stopping = false;
            }
        }

        public WaveFormat RecordingFormat { get; set; }
    }
}
