using NAudio.Wave;

namespace iSpyApplication.Audio
{

    /// <summary>
    /// Audio source interface.
    /// </summary>
    /// 
    /// <remarks>The interface describes common methods for different type of Audio sources.</remarks>
    /// 
    public interface ISupportsAudio
    {
        /// <summary>
        /// HasAudioStream event.
        /// </summary>
        /// 
        event HasAudioStreamEventHandler HasAudioStream;

        WaveFormat RecordingFormat { get; set; }
    }
}
