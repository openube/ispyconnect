//
//
//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
//  REMAINS UNCHANGED. 
//  SEE  http://www.mp3dev.org/ FOR TECHNICAL AND COPYRIGHT INFORMATION REGARDING 
//  LAME PROJECT.
//
//  Email:  yetiicb@hotmail.com
//
//  Copyright (C) 2002-2003 Idael Cardoso. 
//
//
//  About Thomson and/or Fraunhofer patents:
//  Any use of this product does not convey a license under the relevant 
//  intellectual property of Thomson and/or Fraunhofer Gesellschaft nor imply 
//  any right to use this product in any finished end user or ready-to-use final 
//  product. An independent license for such use is required. 
//  For details, please visit http://www.mp3licensing.com.
//

using System;
using System.IO;

namespace iSpyApplication.MP3Stream
{
  /// <summary>
  /// Convert PCM audio data to PCM format
  /// The data received through the method write is assumed as PCM audio data. 
  /// This data is converted to MP3 format and written to the result stream. 
  /// </summary>
  public class Mp3Writer :  AudioWriter
  {
    private bool _closed;
    private readonly BE_CONFIG _mMp3Config;
    private readonly uint _mHLameStream;
    private readonly uint _mInputSamples;
    private readonly uint _mOutBufferSize;
    private readonly byte[] _mInBuffer;
    private int _mInBufferPos;
    private readonly byte[] _mOutBuffer;

    public bool IsStreamOwner
    {
        get;
        private set;
    }

      /// <summary>
      /// Create a Mp3Writer with the default MP3 format
      /// </summary>
      /// <param name="output">Stream that will hold the MP3 resulting data</param>
      /// <param name="inputDataFormat">PCM format of input data</param>
      /// <param name="isStreamOwner"> </param>
      public Mp3Writer(Stream output, WaveFormat inputDataFormat, bool isStreamOwner)
      :this(output, inputDataFormat, new BE_CONFIG(inputDataFormat), isStreamOwner)
    {
    }

      /// <summary>
      /// Create a Mp3Writer with specific MP3 format
      /// </summary>
      /// <param name="output">Stream that will hold the MP3 resulting data</param>
      /// <param name="cfg">Writer Config</param>
      /// <param name="isStreamOwner"> </param>
      public Mp3Writer(Stream output, Mp3WriterConfig cfg, bool isStreamOwner)
      :this(output, cfg.Format, cfg.Mp3Config, isStreamOwner)
    {
    }

      /// <summary>
      /// Create a Mp3Writer with specific MP3 format
      /// </summary>
      /// <param name="output">Stream that will hold the MP3 resulting data</param>
      /// <param name="inputDataFormat">PCM format of input data</param>
      /// <param name="mp3Config">Desired MP3 config</param>
      /// <param name="isStreamOwner"> </param>
      public Mp3Writer(Stream output, WaveFormat inputDataFormat, BE_CONFIG mp3Config, bool isStreamOwner)
      :base(output, inputDataFormat)
    {
      IsStreamOwner = isStreamOwner;
      try
      {
        _mMp3Config = mp3Config;
        uint lameResult = Lame_encDll.beInitStream(_mMp3Config, ref _mInputSamples, ref _mOutBufferSize, ref _mHLameStream);
        if ( lameResult != Lame_encDll.BE_ERR_SUCCESSFUL)
        {
          throw new ApplicationException(string.Format("Lame_encDll.beInitStream failed with the error code {0}", lameResult));
        }
        _mInBuffer = new byte[_mInputSamples*2]; //Input buffer is expected as short[]
        _mOutBuffer = new byte[_mOutBufferSize];
      }
      catch
      {
        CloseStream();
        throw;
      }
    }

    /// <summary>
    /// MP3 Config of final data
    /// </summary>
    public BE_CONFIG Mp3Config
    {
      get
      {
        return _mMp3Config;
      }
    }

    protected override int GetOptimalBufferSize()
    {
      return _mInBuffer.Length;
    }

    public override void Close()
    {
      if (!_closed)
      {
        try
        {
          uint encodedSize = 0;
          if ( _mInBufferPos > 0)
          {
            if ( Lame_encDll.EncodeChunk(_mHLameStream, _mInBuffer, 0, (uint)_mInBufferPos, _mOutBuffer, ref encodedSize) == Lame_encDll.BE_ERR_SUCCESSFUL )
            {
              if ( encodedSize > 0)
              {
                base.Write(_mOutBuffer, 0, (int)encodedSize);
              }
            }
          }
          encodedSize = 0;
          if (Lame_encDll.beDeinitStream(_mHLameStream, _mOutBuffer, ref encodedSize) == Lame_encDll.BE_ERR_SUCCESSFUL )
          {
            if ( encodedSize > 0)
            {
              base.Write(_mOutBuffer, 0, (int)encodedSize);
            }
          }
        }
        finally
        {
          Lame_encDll.beCloseStream(_mHLameStream);
        }
      }
      _closed = true;
      CloseStream();
    }

    private void CloseStream()
    {
        if (IsStreamOwner)
        {
            base.Close();
        }
        else
        {
            Flush();
        }
    }
  
  
    /// <summary>
    /// Send to the compressor an array of bytes.
    /// </summary>
    /// <param name="buffer">Input buffer</param>
    /// <param name="index">Start position</param>
    /// <param name="count">Bytes to process. The optimal size, to avoid buffer copy, is a multiple</param>
    public override void Write(byte[] buffer, int index, int count)
    {
        uint encodedSize = 0;
        while (count > 0)
      {
          uint lameResult;
          if ( _mInBufferPos > 0 ) 
        {
          int toCopy = Math.Min(count, _mInBuffer.Length - _mInBufferPos);
          Buffer.BlockCopy(buffer, index, _mInBuffer, _mInBufferPos, toCopy);
          _mInBufferPos += toCopy;
          index += toCopy;
          count -= toCopy;
          if (_mInBufferPos >= _mInBuffer.Length)
          {
            _mInBufferPos = 0;
            if ( (lameResult=Lame_encDll.EncodeChunk(_mHLameStream, _mInBuffer, _mOutBuffer, ref encodedSize)) == Lame_encDll.BE_ERR_SUCCESSFUL )
            {
              if ( encodedSize > 0)
              {
                base.Write(_mOutBuffer, 0, (int)encodedSize);
              }
            }
            else
            {
              throw new ApplicationException(string.Format("Lame_encDll.EncodeChunk failed with the error code {0}", lameResult));
            }
          }
        }
        else
        {
          if (count >= _mInBuffer.Length)
          {
            if ( (lameResult=Lame_encDll.EncodeChunk(_mHLameStream, buffer, index, (uint)_mInBuffer.Length, _mOutBuffer, ref encodedSize)) == Lame_encDll.BE_ERR_SUCCESSFUL )
            {
              if ( encodedSize > 0)
              {
                base.Write(_mOutBuffer, 0, (int)encodedSize);
              }
            }
            else
            {
              throw new ApplicationException(string.Format("Lame_encDll.EncodeChunk failed with the error code {0}", lameResult)); 
            }
            count -= _mInBuffer.Length;
            index += _mInBuffer.Length;
          }
          else
          {
            Buffer.BlockCopy(buffer, index, _mInBuffer, 0, count);
            _mInBufferPos = count;
            index += count;
            count = 0;
          }
        }
      }
    }
  
    /// <summary>
    /// Send to the compressor an array of bytes.
    /// </summary>
    /// <param name="buffer">The optimal size, to avoid buffer copy, is a multiple</param>
    public override void Write(byte[] buffer)
    {
      Write(buffer, 0, buffer.Length);
    }
  
    protected override AudioWriterConfig GetWriterConfig()
    {
      return new Mp3WriterConfig(m_InputDataFormat, Mp3Config);
    }
  }
}