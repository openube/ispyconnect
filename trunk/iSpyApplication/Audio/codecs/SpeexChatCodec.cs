using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using NSpeex;
using System.Diagnostics;

namespace iSpyApplication.Audio.codecs
{
    class NarrowBandSpeexCodec : SpeexChatCodec
    {
        public NarrowBandSpeexCodec() : 
            base(BandMode.Narrow, 8000, "Speex Narrow Band")
        {
        
        }
    }

    class WideBandSpeexCodec : SpeexChatCodec
    {
        public WideBandSpeexCodec() : 
            base(BandMode.Wide, 16000, "Speex Wide Band (16kHz)")
        {

        }
    }

    class UltraWideBandSpeexCodec : SpeexChatCodec
    {
        public UltraWideBandSpeexCodec() :
            base(BandMode.UltraWide, 32000, "Speex Ultra Wide Band (32kHz)")
        {

        }
    }

    class SpeexChatCodec : INetworkChatCodec
    {
        private WaveFormat recordingFormat;
        private SpeexDecoder decoder;
        private SpeexEncoder encoder;
        private WaveBuffer encoderInputBuffer;
        private string description;

        public SpeexChatCodec(BandMode bandMode, int sampleRate, string description)
        {
            this.decoder = new SpeexDecoder(bandMode);
            this.encoder = new SpeexEncoder(bandMode);
            this.recordingFormat = new WaveFormat(sampleRate, 16, 1);
            this.description = description;
            this.encoderInputBuffer = new WaveBuffer(this.recordingFormat.AverageBytesPerSecond); // more than enough
        }

        public string Name
        {
            get { return description; }
        }

        public int BitsPerSecond
        {
            get { return -1; }
        }

        public WaveFormat RecordFormat
        {
            get { return recordingFormat; }
        }

        public byte[] Encode(byte[] data, int offset, int length)
        {
            FeedSamplesIntoEncoderInputBuffer(data, offset, length);
            int samplesToEncode = encoderInputBuffer.ShortBufferCount;
            if (samplesToEncode % encoder.FrameSize != 0)
            {
                samplesToEncode -= samplesToEncode % encoder.FrameSize;
            }
            byte[] outputBufferTemp = new byte[length]; // contains more than enough space
            int bytesWritten = encoder.Encode(encoderInputBuffer.ShortBuffer, 0, samplesToEncode, outputBufferTemp, 0, length);
            byte[] encoded = new byte[bytesWritten];
            System.Array.Copy(outputBufferTemp, 0, encoded, 0, bytesWritten);
            ShiftLeftoverSamplesDown(samplesToEncode);
            return encoded;
        }

        private void ShiftLeftoverSamplesDown(int samplesEncoded)
        {
            int leftoverSamples = encoderInputBuffer.ShortBufferCount - samplesEncoded;
            System.Array.Copy(encoderInputBuffer.ByteBuffer, samplesEncoded * 2, encoderInputBuffer.ByteBuffer, 0, leftoverSamples * 2);
            encoderInputBuffer.ShortBufferCount = leftoverSamples;
        }

        private void FeedSamplesIntoEncoderInputBuffer(byte[] data, int offset, int length)
        {
            System.Array.Copy(data, offset, encoderInputBuffer.ByteBuffer, encoderInputBuffer.ByteBufferCount, length);
            encoderInputBuffer.ByteBufferCount += length;
        }

        public byte[] Decode(byte[] data, int offset, int length)
        {
            byte[] outputBufferTemp = new byte[length * 320];
            WaveBuffer wb = new WaveBuffer(outputBufferTemp);
            int samplesDecoded = decoder.Decode(data, offset, length, wb.ShortBuffer, 0, false);
            int bytesDecoded = samplesDecoded * 2;
            byte[] decoded = new byte[bytesDecoded];
            System.Array.Copy(outputBufferTemp, 0, decoded, 0, bytesDecoded);
            return decoded;
        }

        public void Dispose()
        {
            // nothing to do
        }

        public bool IsAvailable { get { return true; } }
    }
}
