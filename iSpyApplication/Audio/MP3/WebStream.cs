//MP3 Decoder - converts mp3 streams to pcm audio- something i wrote recently, might be useful in the future

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using iSpyApplication.Audio;

namespace iSpyPlugin.Audio.streams
{
    public class WebStream
    {
        private string _source;
        private float _volume;
        private bool _listening;
        private ManualResetEvent _stopEvent = null;

        private Thread _thread;
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
                    if (!_thread.Join(0))
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
        /// <param name="source">source, which provides audio data.</param>
        /// 
        public WebStream(string source)
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

                _stopEvent = new ManualResetEvent(false);
                _thread = new Thread(StreamMP3)
                {
                    Name = "iSpyServer Audio Receiver (" + _source + ")"
                };
                _thread.Start();

            }
        }

        private BufferedWaveProvider _bufferedWaveProvider;
        private DirectSoundOut _waveOut;

        private void StreamMP3()
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(_source);
            HttpWebResponse resp = null;
            try
            {
                resp = (HttpWebResponse)webRequest.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status != WebExceptionStatus.RequestCanceled)
                {
                    
                }
                return;
            }
            byte[] buffer = new byte[16384 * 4]; // needs to be big enough to hold a decompressed frame

            IMp3FrameDecompressor decompressor = null;

            try
            {
                using (var responseStream = resp.GetResponseStream())
                {
                    var readFullyStream = new ReadFullyStream(responseStream);
                    while (!_stopEvent.WaitOne(0, false))
                    {
                        if (_bufferedWaveProvider != null && _bufferedWaveProvider.BufferLength - _bufferedWaveProvider.BufferedBytes < _bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4)
                        {
                            Debug.WriteLine("Buffer getting full, taking a break");
                            Thread.Sleep(100);
                        }
                        else
                        {
                            Mp3Frame frame = null;
                            try
                            {
                                frame = Mp3Frame.LoadFromStream(readFullyStream);
                            }
                            catch (EndOfStreamException)
                            {
                                // reached the end of the MP3 file / stream
                                break;
                            }
                            catch (WebException)
                            {
                                // probably we have aborted download from the GUI thread
                                break;
                            }
                            if (decompressor == null)
                            {
                                // don't think these details matter too much - just help ACM select the right codec
                                // however, the buffered provider doesn't know what sample rate it is working at
                                // until we have a frame
                                WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2, frame.FrameLength, frame.BitRate);
                                decompressor = new AcmMp3FrameDecompressor(waveFormat);
                                this._bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat)
                                                                {BufferDuration = TimeSpan.FromSeconds(5)};

                                _waveOut = new DirectSoundOut(1000);
                                _waveOut.Init(_bufferedWaveProvider);
                                _waveOut.Play();
                                _waveOut.PlaybackStopped += wavePlayer_PlaybackStopped;
                            }
                            int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                            //Debug.WriteLine(String.Format("Decompressed a frame {0}", decompressed));
                            _bufferedWaveProvider.AddSamples(buffer, 0, decompressed);
                        }
                        if (_stopEvent.WaitOne(0, false))
                            break;
                    }
                    Debug.WriteLine("Exiting");
                    // was doing this in a finally block, but for some reason
                    // we are hanging on response stream .Dispose so never get there
                    decompressor.Dispose();
                }
            }
            finally
            {
                if (decompressor != null)
                {
                    decompressor.Dispose();
                }
                if (_waveOut!=null)
                {
                    _waveOut.Stop();
                }
            }
        }

        void wavePlayer_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            // dispose of wave output
            if (_waveOut != null)
            {
                _waveOut.Dispose();
                _waveOut = null;
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

    }
}
