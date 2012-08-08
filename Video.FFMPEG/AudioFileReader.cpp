// addition to the aforge ffmpeg library to add audio decoding support © iSpyConnect.com, 2012
// sean@ispyconnect.com

#include "StdAfx.h"
#include "AudioFileReader.h"

namespace libffmpeg
{
	extern "C"
	{
		// disable warnings about badly formed documentation from FFmpeg, which we don't need at all
		#pragma warning(disable:4635) 
		// disable warning about conversion int64 to int32
		#pragma warning(disable:4244) 

		#include "libavformat\avformat.h"
		#include "libavcodec\avcodec.h"
	}
}

namespace AForge { namespace Video { namespace FFMPEG
{
#pragma region Some private FFmpeg related stuff hidden out of header file

// A structure to encapsulate all FFMPEG related private variable
ref struct ReaderPrivateData2
{
public:
	libffmpeg::AVFormatContext*		FormatContext;
	libffmpeg::AVStream*			AudioStream;
	libffmpeg::AVCodecContext*		CodecContext;
	libffmpeg::int16_t*			Buffer;

	ReaderPrivateData2( )
	{
		FormatContext     = NULL;
		AudioStream       = NULL;
		CodecContext      = NULL;
	}
};
#pragma endregion

// Class constructor
AudioFileReader::AudioFileReader( void ) :
    data( nullptr ), disposed( false )
{	
	libffmpeg::av_register_all( );
	libffmpeg::avformat_network_init();
}

#pragma managed(push, off)

static int interrupt_cb(void *ctx) 
{ 
	libffmpeg::AVFormatContext* formatContext = reinterpret_cast<libffmpeg::AVFormatContext*>(ctx);

	//timeout after 5 seconds of no activity
	if (formatContext->timestamp>0 && (GetTickCount() - formatContext->timestamp >5000))
		return 1;
	
    return 0;
} 

static libffmpeg::AVFormatContext* open_file( char* fileName )
{
	libffmpeg::AVFormatContext* formatContext = libffmpeg::avformat_alloc_context( );
	formatContext->interrupt_callback.callback = interrupt_cb;
	formatContext->interrupt_callback.opaque = formatContext;

	if ( libffmpeg::avformat_open_input( &formatContext, fileName, NULL, NULL ) !=0 )
	{
		return NULL;
	}
	return formatContext;
}
#pragma managed(pop)

// Opens the specified audio file
void AudioFileReader::Open( String^ fileName )
{
    CheckIfDisposed( );

	// close previous file if any was open
	Close( );

	data = gcnew ReaderPrivateData2( );
	bool success = false;

	// convert specified managed String to unmanaged string
	IntPtr ptr = System::Runtime::InteropServices::Marshal::StringToHGlobalAnsi( fileName );
	char* nativeFileName = reinterpret_cast<char*>( static_cast<void*>( ptr ) );

	try
	{
		// open the specified audio file
		data->FormatContext = open_file( nativeFileName );
		if ( data->FormatContext == NULL )
		{
			throw gcnew System::IO::IOException( "Cannot open the audio file." );
		}

		// retrieve stream information
		if ( libffmpeg::av_find_stream_info( data->FormatContext ) < 0 )
		{
			throw gcnew VideoException( "Cannot find stream information." );
		}

		// search for the first audio stream
		for ( unsigned int i = 0; i < data->FormatContext->nb_streams; i++ )
		{
			if( data->FormatContext->streams[i]->codec->codec_type == libffmpeg::AVMEDIA_TYPE_AUDIO )
			{
				// get the pointer to the codec context for the audio stream
				data->CodecContext = data->FormatContext->streams[i]->codec;
				data->AudioStream  = data->FormatContext->streams[i];
				break;
			}
		}
		if ( data->AudioStream == NULL )
		{
			throw gcnew VideoException( "Cannot find audio stream in the specified file." );
		}

		// find decoder for the audio stream
		libffmpeg::AVCodec* codec = libffmpeg::avcodec_find_decoder( data->CodecContext->codec_id );
		if ( codec == NULL )
		{
			throw gcnew VideoException( "Cannot find codec to decode the audio stream." );
		}

		// open the codec
		if ( libffmpeg::avcodec_open2( data->CodecContext, codec, NULL ) < 0 )
		{
			throw gcnew VideoException( "Cannot open audio codec." );
		}

		if (data->CodecContext->time_base.num > 1000 && data->CodecContext->time_base.den == 1)
			data->CodecContext->time_base.den = 1000;

		// allocate audio frame


		data->Buffer = (libffmpeg::int16_t*) libffmpeg::av_malloc(AVCODEC_MAX_AUDIO_FRAME_SIZE);	

		m_codecName = gcnew String( data->CodecContext->codec->name );

		m_channels = data->CodecContext->channels;
		m_sampleRate = data->CodecContext->sample_rate;
		m_bitsPerSample = 16;


		success = true;
	}
	finally
	{
		System::Runtime::InteropServices::Marshal::FreeHGlobal( ptr );

		if ( !success )
		{
			Close( );
		}
	}
}

// Close current audio file
void AudioFileReader::Close(  )
{
	if ( data != nullptr )
	{
		if ( data->Buffer != NULL )
		{
			libffmpeg::av_free( data->Buffer );
		}

		if ( data->CodecContext != NULL )
		{
			libffmpeg::avcodec_close( data->CodecContext );
		}

		if ( data->FormatContext != NULL )
		{
			libffmpeg::av_close_input_file( data->FormatContext );
		}

		data = nullptr;
	}
}

// Read next audio frame of the current audio file
array<unsigned char>^ AudioFileReader::ReadAudioFrame(  )
{
    CheckIfDisposed( );

	if ( data == nullptr )
	{
		throw gcnew System::IO::IOException( "Cannot read audio since audio file is not open." );
	}

	//int frame_size_ptr = AVCODEC_MAX_AUDIO_FRAME_SIZE;
	bool exit = false;

	int s = 0, len = 0;
	int out_size = 0;
	int dec_size = 0;
	System::IntPtr iptr = System::IntPtr( data->Buffer );

	array<unsigned char>^ managedBuf = gcnew array<unsigned char>(AVCODEC_MAX_AUDIO_FRAME_SIZE*2);

	libffmpeg::AVPacket packet;
	libffmpeg::av_init_packet(&packet);
	
	while(true)	{
		data->FormatContext->timestamp = GetTickCount();
		if (libffmpeg::av_read_frame( data->FormatContext, &packet )<0)	{
			libffmpeg::av_free_packet(&packet);
			break;
		}

		if(packet.stream_index==data->AudioStream->index){

			int sz = packet.size;
			while(sz >0){
				int chunk = AVCODEC_MAX_AUDIO_FRAME_SIZE+FF_INPUT_BUFFER_PADDING_SIZE;
				int in_used = libffmpeg::avcodec_decode_audio3(data->CodecContext, data->Buffer , &chunk, &packet);

				// Decode audio frame
				if (in_used < 0) {
					throw gcnew System::IO::IOException( "Error decoding audio." );
				}
				
				System::Runtime::InteropServices::Marshal::Copy( iptr, managedBuf, s, s + chunk );

				s+=chunk;
				sz -= in_used;
				packet.data += in_used;
			}     
			packet.data -= packet.size;
			libffmpeg::av_free_packet(&packet);
			break;
		}
		else
		{
			libffmpeg::av_free_packet(&packet);
		}
		
	}
	Array::Resize(managedBuf, s);	
	return managedBuf;
}


} } }