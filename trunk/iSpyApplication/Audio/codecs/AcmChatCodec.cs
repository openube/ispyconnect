using System;
using NAudio.Wave;
using NAudio.Wave.Compression;
using NAudio;

namespace iSpyApplication.Audio.codecs
{
    /// <summary>
    /// useful base class for deriving any chat codecs that will use ACM for decode and encode
    /// </summary>
    abstract class AcmChatCodec : INetworkChatCodec
    {
        private WaveFormat encodeFormat;
        private AcmStream encodeStream;
        private AcmStream decodeStream;
        private int decodeSourceBytesLeftovers;
        private int encodeSourceBytesLeftovers;

        public AcmChatCodec(WaveFormat recordFormat, WaveFormat encodeFormat)
        {
            this.RecordFormat = recordFormat;
            this.encodeFormat = encodeFormat;
        }

        public WaveFormat RecordFormat { get; private set; }

        public byte[] Encode(byte[] data, int offset, int length)
        {
            if (this.encodeStream == null)
            {
                this.encodeStream = new AcmStream(this.RecordFormat, this.encodeFormat);
            }
            return Convert(encodeStream, data, offset, length, ref encodeSourceBytesLeftovers);
        }

        public byte[] Decode(byte[] data, int offset, int length)
        {
            if (this.decodeStream == null)
            {
                this.decodeStream = new AcmStream(this.encodeFormat, this.RecordFormat);
            }
            return Convert(decodeStream, data, offset, length, ref decodeSourceBytesLeftovers);
        }

        private static byte[] Convert(AcmStream conversionStream, byte[] data, int offset, int length, ref int sourceBytesLeftovers)
        {
            int bytesInSourceBuffer = length + sourceBytesLeftovers;
            System.Array.Copy(data, offset, conversionStream.SourceBuffer, sourceBytesLeftovers, length);
            int sourceBytesConverted;
            int bytesConverted = conversionStream.Convert(bytesInSourceBuffer, out sourceBytesConverted);
            sourceBytesLeftovers = bytesInSourceBuffer - sourceBytesConverted;
            if (sourceBytesLeftovers > 0)
            {
                // shift the leftovers down
                System.Array.Copy(conversionStream.SourceBuffer, sourceBytesConverted, conversionStream.SourceBuffer, 0, sourceBytesLeftovers);
            }
            byte[] encoded = new byte[bytesConverted];
            System.Array.Copy(conversionStream.DestBuffer, 0, encoded, 0, bytesConverted);
            return encoded;
        }

        public abstract string Name { get; }

        public int BitsPerSecond
        {
            get
            {
                return this.encodeFormat.AverageBytesPerSecond * 8;
            }
        }

        public void Dispose()
        {
            if (encodeStream != null)
            {
                encodeStream.Dispose();
                encodeStream = null;
            }
            if (decodeStream != null)
            {
                decodeStream.Dispose();
                decodeStream = null;
            }
        }

        public bool IsAvailable
        {
            get
            {
                // determine if this codec is installed on this PC
                bool available = true;
                try
                {
                    using (var tempEncoder = new AcmStream(this.RecordFormat, this.encodeFormat)) { }
                    using (var tempDecoder = new AcmStream(this.encodeFormat, this.RecordFormat)) { }
                }
                catch (MmException)
                {
                    available = false;
                }
                return available;
            }
        }
    }
}
