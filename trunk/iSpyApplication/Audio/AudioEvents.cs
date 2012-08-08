using iSpyApplication.Audio.streams;

namespace iSpyApplication.Audio
{
    using System;

    /// <summary>
    /// Delegate for new frame event handler.
    /// </summary>
    /// 
    /// <param name="sender">Sender object.</param>
    /// <param name="eventArgs">Event arguments.</param>
    /// 
    public delegate void DataAvailableEventHandler(object sender, DataAvailableEventArgs eventArgs);


    public delegate void LevelChangedEventHandler(object sender, LevelChangedEventArgs eventArgs);
    
    /// <summary>
    /// Delegate for Audio source error event handler.
    /// </summary>
    /// 
    /// <param name="sender">Sender object.</param>
    /// <param name="eventArgs">Event arguments.</param>
    /// 
    public delegate void AudioSourceErrorEventHandler(object sender, AudioSourceErrorEventArgs eventArgs);

    /// <summary>
    /// Delegate for playing finished event handler.
    /// </summary>
    /// 
    /// <param name="sender">Sender object.</param>
    /// <param name="reason">Reason of finishing Audio playing.</param>
    /// 
    public delegate void AudioFinishedEventHandler(object sender, ReasonToFinishPlaying reason);

    /// <summary>
    /// Delegate for notifying about audio stream
    /// </summary>
    /// 
    /// <param name="sender">Sender object.</param>
    /// <param name="eventArgs"></param>
    public delegate void HasAudioStreamEventHandler(object sender, EventArgs eventArgs);

    /// <summary>
    /// Reason of finishing Audio playing.
    /// </summary>
    /// 
    /// <remarks><para>When Audio source class fire the <see cref="IAudioSource.AudioFinished"/> event, they
    /// need to specify reason of finishing Audio playing. For example, it may be end of stream reached.</para></remarks>
    /// 
    public enum ReasonToFinishPlaying
    {
        /// <summary>
        /// Audio playing has finished because it end was reached.
        /// </summary>
        EndOfStreamReached,
        /// <summary>
        /// Audio playing has finished because it was stopped by user.
        /// </summary>
        StoppedByUser,
        /// <summary>
        /// Audio playing has finished because the device was lost (unplugged).
        /// </summary>
        DeviceLost,
        /// <summary>
        /// Audio playing has finished because of some error happened the Audio source (camera, stream, file, etc.).
        /// A error reporting event usually is fired to provide error information.
        /// </summary>
        AudioSourceError
    }

    /// <summary>
    /// Arguments for new frame event from Audio source.
    /// </summary>
    /// 
    public class DataAvailableEventArgs : EventArgs
    {
        private byte[] raw;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAvailableEventArgs"/> class.
        /// </summary>
        public DataAvailableEventArgs(byte[] raw)
        {
            this.raw = raw;
        }

        /// <summary>
        /// New frame from Audio source.
        /// </summary>
        /// 
        public byte[] RawData
        {
            get { return raw; }
        }
    }

    public class LevelChangedEventArgs : EventArgs
    {
        private float[] maxsamples;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAvailableEventArgs"/> class.
        /// </summary>
        public LevelChangedEventArgs(float[] maxsamples)
        {
            this.maxsamples = maxsamples;
        }

        /// <summary>
        /// New frame from Audio source.
        /// </summary>
        /// 
        public float[] MaxSamples
        {
            get { return maxsamples; }
        }
    }

    /// <summary>
    /// Arguments for Audio source error event from Audio source.
    /// </summary>
    /// 
    public class AudioSourceErrorEventArgs : EventArgs
    {
        private string description;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioSourceErrorEventArgs"/> class.
        /// </summary>
        /// 
        /// <param name="description">Error description.</param>
        /// 
        public AudioSourceErrorEventArgs(string description)
        {
            this.description = description;
        }

        /// <summary>
        /// Audio source error description.
        /// </summary>
        /// 
        public string Description
        {
            get { return description; }
        }
    }
}
