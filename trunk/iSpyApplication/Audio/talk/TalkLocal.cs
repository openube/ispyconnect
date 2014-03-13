using System;
using iSpyApplication.Audio.streams;
using NAudio.Wave;

namespace iSpyApplication.Audio.talk
{
    internal class TalkLocal: ITalkTarget
    {
        private readonly object _obj = new object();
        private bool _bTalking;
        private readonly IAudioSource _audioSource;
        private IWavePlayer _waveOut;

        public TalkLocal(IAudioSource audioSource)
        {
            _audioSource = audioSource;
        }

        public void Start()
        {
            _audioSource.Listening = true;
            if (_audioSource.WaveOutProvider != null)
            {
                _waveOut = new DirectSoundOut(100);
                _waveOut.Init(_audioSource.WaveOutProvider);
                _waveOut.Play();

                _audioSource.DataAvailable -= AudioSourceDataAvailable;
                _audioSource.DataAvailable += AudioSourceDataAvailable;
                _bTalking = true;
            }
        }
        
        public void Stop()
        {
            if (_bTalking)
            {
                lock (_obj)
                {
                    _audioSource.DataAvailable -= AudioSourceDataAvailable;

                    if (_bTalking)
                    {
                        _bTalking = false;
                    }
                    if (TalkStopped != null)
                        TalkStopped(this, EventArgs.Empty);
                }
            }
            if (_audioSource!=null)
                _audioSource.Listening = false;
            if (_waveOut != null)
            {
                _waveOut.Stop();
                _waveOut.Dispose();
            }
        }

        public bool Connected
        {
            get { return true; }
        }

        public event TalkStoppedEventHandler TalkStopped;
        
        
        private void AudioSourceDataAvailable(object sender, DataAvailableEventArgs e)
        {
            //let it just pipe through
        }
    }
}