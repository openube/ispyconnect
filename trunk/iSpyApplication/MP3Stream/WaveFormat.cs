using System.Runtime.InteropServices;

namespace iSpyApplication.MP3Stream
{
    public enum WaveFormats
    {
        Pcm = 1,
        Float = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    public class WaveFormat
    {
        public short wFormatTag;
        public short nChannels;
        public int nSamplesPerSec;
        public int nAvgBytesPerSec;
        public short nBlockAlign;
        public short wBitsPerSample;
        public short cbSize;

        public WaveFormat(int rate, int bits, int channels)
        {
            wFormatTag = (short)WaveFormats.Pcm;
            nChannels = (short)channels;
            nSamplesPerSec = rate;
            wBitsPerSample = (short)bits;
            cbSize = 0;

            nBlockAlign = (short)(channels * (bits / 8));
            nAvgBytesPerSec = nSamplesPerSec * nBlockAlign;
        }
    }
}
