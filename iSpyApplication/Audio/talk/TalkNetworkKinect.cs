using System;
using System.Net.Sockets;
using iSpyApplication.Audio.streams;
using NAudio.Wave;


namespace iSpyApplication.Audio.talk
{
    internal class TalkNetworkKinect: ITalkTarget
    {
        private readonly object _obj = new object();
        private readonly int _port = 80;
        private readonly string _server;
        private NetworkStream _avstream;
        private bool _bTalking;
        private readonly WaveFormat _waveFormat = new WaveFormat(8000, 16, 1);
        private readonly IAudioSource _audioSource;
        const string Hdr = "TALK";

        public TalkNetworkKinect(string server, int port, IAudioSource audioSource)
        {
            _server = server;
            _port = port;
            _audioSource = audioSource;
        }

        public void Start()
        {
            try
            {
                var tcp = new TcpClient(_server, _port);
                _avstream = tcp.GetStream();
                _avstream.Write(System.Text.Encoding.ASCII.GetBytes(Hdr), 0, Hdr.Length);

                StartTalk();
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
                if (TalkStopped != null)
                    TalkStopped(this, EventArgs.Empty);
            }
        }
        
        public void Stop()
        {
            StopTalk();
        }

        public event TalkStoppedEventHandler TalkStopped;
        
        private void StartTalk()
        {
            if (_bTalking)
            {
                StopTalk();
            }
            _audioSource.DataAvailable += AudioSourceDataAvailable;
            _bTalking = true;
        }

        private void StopTalk()
        {
            if (_bTalking)
            {
                lock (_obj)
                {
                    _audioSource.DataAvailable -= AudioSourceDataAvailable;

                    if (_avstream != null)
                    {
                        _avstream.Close();
                        _avstream.Dispose();
                        _avstream = null;
                    }

                    if (_bTalking)
                    {
                        _bTalking = false;
                    }
                    if (TalkStopped != null)
                        TalkStopped(this, EventArgs.Empty);
                }
            }
        }
        
        private void AudioSourceDataAvailable(object sender, DataAvailableEventArgs e)
        {
            try
            {
                lock (_obj)
                {
                    if (_bTalking && _avstream != null)
                    {
                        byte[] bSrc = e.RawData;
                        int totBytes = bSrc.Length;

                        if (!_audioSource.RecordingFormat.Equals(_waveFormat))
                        {
                            var ws = new TalkHelperStream(bSrc, totBytes, _audioSource.RecordingFormat);
                            var helpStm = new WaveFormatConversionStream(_waveFormat, ws);
                            totBytes = helpStm.Read(bSrc, 0, 25000);
                            ws.Close();
                            ws.Dispose();
                            helpStm.Close();
                            helpStm.Dispose();
                        }
                        var enc = new byte[totBytes / 2];
                        ALawEncoder.ALawEncode(bSrc, totBytes, enc);

                        try {
                            _avstream.Write(enc, 0, enc.Length);
                        }
                        catch (SocketException)
                        {
                            StopTalk();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
                StopTalk();
            }
        }
    }
}