// addition to the aforge ffmpeg library to add H264 and audio encoding support © iSpyConnect.com, 2012
// sean@ispyconnect.com

#include "StdAfx.h"
#include "AudioFileWriter.h"
#include <vcclr.h>

#define MAX_AUDIO_PACKET_SIZE (128 * 1024)

namespace libffmpeg
{
	extern "C"
	{
		// disable warnings about badly formed documentation from FFmpeg, which don't need at all
		#pragma warning(disable:4635) 
		// disable warning about conversion int64 to int32
		#pragma warning(disable:4244) 

		#include "libavformat\avformat.h"
		#include "libavformat\avio.h"
		#include "libavcodec\avcodec.h"
		#include "libswscale\swscale.h"
	}
}

namespace AForge { namespace Video { namespace FFMPEG
{
#pragma region Some private FFmpeg related stuff hidden out of header file

static void open_audio( AudioWriterPrivateData^ data );
static void add_audio_stream( AudioWriterPrivateData^ data, enum libffmpeg::CodecID codec_id);
static void add_audio_sample( AudioWriterPrivateData^ data, BYTE* soundBuffer, int soundBufferSize);

// A structure to encapsulate all FFMPEG related private variable
ref struct AudioWriterPrivateData
{
public:
	libffmpeg::AVFormatContext*		FormatContext;
	libffmpeg::AVStream*			AudioStream;

	libffmpeg::uint8_t*	AudioEncodeBuffer;
	char* AudioBuffer;

	int AudioEncodeBufferSize;
	int	AudioInputSampleSize;
	int AudioBufferSizeCurrent;
	int AudioBufferSize;

	int BitRate;
	int SampleRate;
	int Channels;

	AudioWriterPrivateData( )
	{
		FormatContext     = NULL;
		AudioStream		  = NULL;

		AudioEncodeBuffer = NULL;
		AudioEncodeBufferSize = 0;
		AudioInputSampleSize = NULL;

		AudioBufferSize = 1024 * 1024 * 4;
		AudioBuffer = new char[AudioBufferSize];
		AudioBufferSizeCurrent = 0;
	}
};
#pragma endregion

// Class constructor
AudioFileWriter::AudioFileWriter( void ) :
    data( nullptr ), disposed( false )
{
	//libffmpeg::avcodec_init();
	libffmpeg::av_register_all( );
	//libffmpeg::avcodec_register_all();

}

void AudioFileWriter::Open( String^ fileName)
{
	Open( fileName, AudioCodec::MP3,64000,22050,1 );
}

char* ManagedStringToUnmanagedUTF8Char2(String^ str)
{
    pin_ptr<const wchar_t> wch = PtrToStringChars(str);
    int nBytes = ::WideCharToMultiByte(CP_UTF8, NULL, wch, -1, NULL, 0, NULL, NULL);
    char* lpszBuffer = new char[nBytes];
    ZeroMemory(lpszBuffer, (nBytes) * sizeof(char)); 
    nBytes = ::WideCharToMultiByte(CP_UTF8, NULL, wch, -1, lpszBuffer, nBytes, NULL, NULL);
    return lpszBuffer;
}

// Creates a video file with the specified name and properties
void AudioFileWriter::Open( String^ fileName, AudioCodec audioCodec, int BitRate, int SampleRate, int Channels)
{
    CheckIfDisposed( );

	// close previous file if any open
	Close( );
	

	data = gcnew AudioWriterPrivateData( );
	data->BitRate = BitRate;
	data->SampleRate = SampleRate;
	data->Channels = Channels;

	bool success = false;

	m_audiocodec  = audioCodec;
	
	// convert specified managed String to unmanaged string
	char *nativeFileName= ManagedStringToUnmanagedUTF8Char2(fileName);

	try
	{
		// gues about destination file format from its file name
		libffmpeg::AVOutputFormat* outputFormat = libffmpeg::av_guess_format( NULL, nativeFileName, NULL );

		if ( !outputFormat )
		{
			// gues about destination file format from its short name
			outputFormat = libffmpeg::av_guess_format( "mp3", NULL, NULL );

			if ( !outputFormat )
			{
				throw gcnew VideoException( "Cannot find suitable output format." );
			}
		}

		// prepare format context
		data->FormatContext = libffmpeg::avformat_alloc_context( );

		if ( !data->FormatContext )
		{
			throw gcnew VideoException( "Cannot allocate format context." );
		}
		data->FormatContext->oformat = outputFormat;

		add_audio_stream(data,  (libffmpeg::CodecID) audio_codecs[(int)audioCodec]);

		// set the output parameters (must be done even if no parameters)
		if ( libffmpeg::av_set_parameters( data->FormatContext, NULL ) < 0 )
		{
			throw gcnew VideoException( "Failed configuring format context." );
		}

		open_audio( data );

		// open output file
		if ( !( outputFormat->flags & AVFMT_NOFILE ) )
		{
			if ( libffmpeg::avio_open( &data->FormatContext->pb, nativeFileName, AVIO_FLAG_WRITE ) < 0 )
			{
				throw gcnew System::IO::IOException( "Cannot open the audio file." );
			}
		}

		libffmpeg::av_write_header( data->FormatContext );

		success = true;
	}
	finally
	{
		delete nativeFileName;

		if ( !success )
		{
			Close( );
		}
	}
}


// Close current video file
void AudioFileWriter::Close( )
{
	if ( data != nullptr )
	{
		if ( data->FormatContext )
		{
			if ( data->AudioEncodeBuffer )
			{
				//Flush();
				libffmpeg::av_free( data->AudioEncodeBuffer );
			}

			if ( data->FormatContext->pb != NULL )
			{
				libffmpeg::av_write_trailer( data->FormatContext );
			}

			
			if (data->AudioStream)	{
				libffmpeg::avcodec_close( data->AudioStream->codec );
			}
			
			if (data->AudioBuffer)
			{
				delete[] data->AudioBuffer;
				data->AudioBuffer = NULL;
			}
			

			for ( unsigned int i = 0; i < data->FormatContext->nb_streams; i++ )
			{
				libffmpeg::av_freep( &data->FormatContext->streams[i]->codec );
				libffmpeg::av_freep( &data->FormatContext->streams[i] );
			}

			if ( data->FormatContext->pb != NULL )
			{
				libffmpeg::avio_close( data->FormatContext->pb );
			}
			
			libffmpeg::av_free( data->FormatContext );
		}

		data = nullptr;
	}
}



// Writes new audio samples to the opened audio file
void AudioFileWriter::WriteAudio(BYTE* soundBuffer, int soundBufferSize)
{
	CheckIfDisposed( );

	if ( data == nullptr )
	{
		throw gcnew System::IO::IOException( "An audio file was not opened yet." );
	}

	 // Add sound
	add_audio_sample(data, soundBuffer, soundBufferSize);
}

void AudioFileWriter::Flush(void)
{
	if ( data != nullptr )
	{
	}
}


#pragma region Private methods
// Writes new audio samples to the opened audio file
void add_audio_sample( AudioWriterPrivateData^ data, BYTE* soundBuffer, int soundBufferSize)
{
  libffmpeg::AVCodecContext* codecContext = data->AudioStream->codec;   

  memcpy(data->AudioBuffer + data->AudioBufferSizeCurrent,  soundBuffer, soundBufferSize);
  data->AudioBufferSizeCurrent += soundBufferSize;

  BYTE* pSoundBuffer = (BYTE*)data->AudioBuffer;
  DWORD nCurrentSize    = data->AudioBufferSizeCurrent;

  // Size of packet on bytes.
  // FORMAT s16
  DWORD packSizeInSize = (2 * data->AudioInputSampleSize) * data->Channels;

  while(nCurrentSize >= packSizeInSize)
  {
    libffmpeg::AVPacket pkt;
    libffmpeg::av_init_packet(&pkt);

    pkt.size = libffmpeg::avcodec_encode_audio(codecContext, data->AudioEncodeBuffer, 
    data->AudioEncodeBufferSize, (const short *)pSoundBuffer);

    if (codecContext->coded_frame && codecContext->coded_frame->pts != AV_NOPTS_VALUE)
    {
            pkt.pts = libffmpeg::av_rescale_q(codecContext->coded_frame->pts, codecContext->time_base, data->AudioStream->time_base);
    }

    pkt.flags |= AV_PKT_FLAG_KEY;
    pkt.stream_index = data->AudioStream->index;
    pkt.data = data->AudioEncodeBuffer;
	
    // Write the compressed frame in the media file.
    if (libffmpeg::av_interleaved_write_frame(data->FormatContext, &pkt) != 0) 
    {
      break;
    }

    nCurrentSize -= packSizeInSize;  
    pSoundBuffer += packSizeInSize;      
  }

  // save excess
  memcpy(data->AudioBuffer, data->AudioBuffer + data->AudioBufferSizeCurrent - nCurrentSize, nCurrentSize);
  data->AudioBufferSizeCurrent = nCurrentSize; 

}

void add_audio_stream( AudioWriterPrivateData^ data,  enum libffmpeg::CodecID codec_id )
{
	  libffmpeg::AVCodecContext *codecContex;

	  data->AudioStream = libffmpeg::av_new_stream(data->FormatContext, 1);

	  if ( !data->AudioStream )
	  {
			throw gcnew VideoException( "Failed creating new audio stream." );
	  }

	  // Codec.
	  codecContex = data->AudioStream->codec;
	  codecContex->codec_id = codec_id;
	  codecContex->codec_type = libffmpeg::AVMEDIA_TYPE_AUDIO;
	  // Set format
	  codecContex->bit_rate    = data->BitRate;
	  codecContex->sample_rate = data->SampleRate;
	  codecContex->channels    = data->Channels;

	  codecContex->sample_fmt  = libffmpeg::SAMPLE_FMT_S16;

	  codecContex->time_base.num = 1;
	  codecContex->time_base.den = 1000;

	  data->AudioEncodeBufferSize = 4 * MAX_AUDIO_PACKET_SIZE;
	  if (data->AudioEncodeBuffer == NULL)
	  {      
		data->AudioEncodeBuffer = (libffmpeg::uint8_t*) libffmpeg::av_malloc(data->AudioEncodeBufferSize);
	  }

	  // Some formats want stream headers to be separate.
	  if( data->FormatContext->oformat->flags & AVFMT_GLOBALHEADER)
	  {
		codecContex->flags |= CODEC_FLAG_GLOBAL_HEADER;
	  }
}


void open_audio( AudioWriterPrivateData^ data )
{
	libffmpeg::AVCodecContext* codecContext = data->AudioStream->codec;
	libffmpeg::AVCodec* codec = avcodec_find_encoder( codecContext->codec_id );

	if ( !codec )
	{
		throw gcnew VideoException( "Cannot find audio codec." );
	}


   // Open it.
  if (libffmpeg::avcodec_open(codecContext, codec) < 0) 
  {
    //printf("Cannot open audio codec\n");
    return;
  }

  if (codecContext->frame_size <= 1) 
  {
    // Ugly hack for PCM codecs (will be removed ASAP with new PCM
    // support to compute the input frame size in samples. 
    data->AudioInputSampleSize = data->AudioEncodeBufferSize / codecContext->channels;
    switch (codecContext->codec_id) 
    {
      case libffmpeg::CODEC_ID_PCM_S16LE:
      case libffmpeg::CODEC_ID_PCM_S16BE:
      case libffmpeg::CODEC_ID_PCM_U16LE:
      case libffmpeg::CODEC_ID_PCM_U16BE:
        data->AudioInputSampleSize >>= 1;
        break;
      default:
        break;
    }
    codecContext->frame_size = data->AudioInputSampleSize;
  } 
  else 
  {
   data-> AudioInputSampleSize = codecContext->frame_size;
  }
}
#pragma endregion
		
} } }

