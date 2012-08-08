// AForge FFMPEG Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2009-2011
// contacts@aforgenet.com
//
// updates to add audio functionality  © iSpyConnect.com, 2012
// sean@ispyconnect.com

#using <System.Xml.Dll>
#include "StdAfx.h"
#include "VideoFileReader.h"
using namespace System::Runtime::InteropServices;


namespace libffmpeg
{
	extern "C"
	{
		// disable warnings about badly formed documentation from FFmpeg, which we don't need at all
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

// A structure to encapsulate all FFMPEG related private variable
ref struct ReaderPrivateData
{
public:
	libffmpeg::AVFormatContext*		FormatContext;

	libffmpeg::AVStream*			VideoStream;
	libffmpeg::AVCodecContext*		CodecContext;
	libffmpeg::AVFrame*				VideoFrame;
	struct libffmpeg::SwsContext*	ConvertContext;

	libffmpeg::AVCodecContext*		AudioCodecContext;
	libffmpeg::AVStream*			AudioStream;
	libffmpeg::int16_t*				Buffer;
	
	
	int BytesRemaining;

	ReaderPrivateData( )
	{
		FormatContext     = NULL;
		VideoStream       = NULL;
		CodecContext      = NULL;
		VideoFrame        = NULL;
		ConvertContext	  = NULL;

		AudioStream       = NULL;
		AudioCodecContext = NULL;

		BytesRemaining = 0;
	}
};
#pragma endregion

// Class constructor
VideoFileReader::VideoFileReader( void ) :
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
	// do something 
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

// Opens the specified video file
void VideoFileReader::Open( String^ fileName )
{
    CheckIfDisposed( );

	// close previous file if any was open
	Close( );

	data = gcnew ReaderPrivateData( );
	bool success = false;

	IntPtr ptr = Marshal::StringToHGlobalAnsi( fileName );
	char* nativeFileName = reinterpret_cast<char*>( static_cast<void*>( ptr ) );

	try
	{
		// open the specified video file
		data->FormatContext = open_file( nativeFileName );
		if ( data->FormatContext == NULL )
		{
			throw gcnew System::IO::IOException( "Cannot open the video file." );
		}

		// retrieve stream information
		if ( libffmpeg::av_find_stream_info( data->FormatContext ) < 0 )
		{
			throw gcnew VideoException( "Cannot find stream information." );
		}

		// search for the first video stream
		for ( unsigned int i = 0; i < data->FormatContext->nb_streams; i++ )
		{
			if( data->FormatContext->streams[i]->codec->codec_type == libffmpeg::AVMEDIA_TYPE_VIDEO )
			{
				// get the pointer to the codec context for the video stream
				data->CodecContext = data->FormatContext->streams[i]->codec;
				data->CodecContext->flags|=AVFMT_NOFILE|AVFMT_FLAG_IGNIDX; 
				data->CodecContext->flags&=~AVFMT_FLAG_GENPTS; 

				data->VideoStream  = data->FormatContext->streams[i];
				break;
			}
		}
		if ( data->VideoStream == NULL )
		{
			throw gcnew VideoException( "Cannot find video stream in the specified file." );
		}

		// find decoder for the video stream
		libffmpeg::AVCodec* codec = libffmpeg::avcodec_find_decoder( data->CodecContext->codec_id );
		if ( codec == NULL )
		{
			throw gcnew VideoException( "Cannot find codec to decode the video stream." );
		}

		

		// open the codec
		if ( libffmpeg::avcodec_open( data->CodecContext, codec ) < 0 )
		{
			throw gcnew VideoException( "Cannot open video codec." );
		}

		// allocate video frame
		data->VideoFrame = libffmpeg::avcodec_alloc_frame( );

		// prepare scaling context to convert RGB image to video format
		data->ConvertContext = libffmpeg::sws_getContext( data->CodecContext->width, data->CodecContext->height, data->CodecContext->pix_fmt,
				data->CodecContext->width, data->CodecContext->height, libffmpeg::PIX_FMT_BGR24,
				SWS_BICUBIC, NULL, NULL, NULL );

		if ( data->ConvertContext == NULL )
		{
			throw gcnew VideoException( "Cannot initialize frames conversion context." );
		}

		// get some properties of the video file
		m_width  = data->CodecContext->width;
		m_height = data->CodecContext->height;
		if (data->VideoStream->r_frame_rate.den==0)
			m_frameRate = 25;
		else
			m_frameRate = data->VideoStream->r_frame_rate.num / data->VideoStream->r_frame_rate.den;

		if (m_frameRate==0)
			m_frameRate = 25;

		m_codecName = gcnew String( data->CodecContext->codec->name );
		m_framesCount = data->VideoStream->nb_frames;


		//START AUDIO STUFF
		// search for the first audio stream
		for ( unsigned int i = 0; i < data->FormatContext->nb_streams; i++ )
		{
			if( data->FormatContext->streams[i]->codec->codec_type == libffmpeg::AVMEDIA_TYPE_AUDIO )
			{
				// get the pointer to the codec context for the audio stream
				data->AudioCodecContext = data->FormatContext->streams[i]->codec;
				data->AudioStream  = data->FormatContext->streams[i];
				break;
			}
		}
		if ( data->AudioStream != NULL )
		{
			libffmpeg::AVCodec* audiocodec = libffmpeg::avcodec_find_decoder( data->AudioCodecContext->codec_id );
			if ( audiocodec != NULL )
			{
				if ( libffmpeg::avcodec_open2( data->AudioCodecContext, audiocodec, NULL ) >= 0 )
				{
					if (data->AudioCodecContext->time_base.num > 1000 && data->AudioCodecContext->time_base.den == 1)
						data->AudioCodecContext->time_base.den = 1000;

					data->Buffer = (libffmpeg::int16_t*) libffmpeg::av_malloc(AVCODEC_MAX_AUDIO_FRAME_SIZE);	
					
					m_audiocodecName = gcnew String( data->AudioCodecContext->codec->name );
					m_channels = data->AudioCodecContext->channels;
					m_sampleRate = data->AudioCodecContext->sample_rate;
					m_bitsPerSample = 16;

				}			
			}
			
		}


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

// Close current video file
void VideoFileReader::Close(  )
{
	if ( data != nullptr )
	{
		if ( data->Buffer != NULL )
		{
			libffmpeg::av_free( data->Buffer );
		}

		if ( data->AudioCodecContext != NULL )
		{
			libffmpeg::avcodec_close( data->AudioCodecContext );
		}

		if ( data->VideoFrame != NULL )
		{
			libffmpeg::av_free( data->VideoFrame );
		}

		if ( data->CodecContext != NULL )
		{
			libffmpeg::avcodec_close( data->CodecContext );
		}

		if ( data->FormatContext != NULL )
		{
			libffmpeg::av_close_input_file( data->FormatContext );
		}

		if ( data->ConvertContext != NULL )
		{
			libffmpeg::sws_freeContext( data->ConvertContext );
		}
		data = nullptr;
	}
}




// Read next video frame of the current video file
Bitmap^ VideoFileReader::ReadVideoFrame(  )
{
    CheckIfDisposed( );

	if ( data == nullptr )
	{
		throw gcnew System::IO::IOException( "Cannot read video frames since video file is not open." );
	}

	int frameFinished;
	Bitmap^ bitmap = nullptr;

	int bytesDecoded;
	bool exit = false;

	libffmpeg::AVPacket packet;
	libffmpeg::av_init_packet(&packet);
   int i = 0;
   while(true)
   {
	  if (libffmpeg::av_read_frame(data->FormatContext, &packet) >= 0)	{
		  if(packet.stream_index == data->VideoStream->index)
		  {

			 //decode video frame
			 libffmpeg::avcodec_decode_video2(data->CodecContext, data->VideoFrame, &frameFinished, &packet);

			 //did we get a video frame?
			 if(frameFinished)
			 {
				bitmap = gcnew Bitmap( data->CodecContext->width, data->CodecContext->height, PixelFormat::Format24bppRgb );
	
				// lock the bitmap
				BitmapData^ bitmapData = bitmap->LockBits( System::Drawing::Rectangle( 0, 0, data->CodecContext->width, data->CodecContext->height ), ImageLockMode::ReadOnly, PixelFormat::Format24bppRgb );

				libffmpeg::uint8_t* ptr = reinterpret_cast<libffmpeg::uint8_t*>( static_cast<void*>( bitmapData->Scan0 ) );

				libffmpeg::uint8_t* srcData[4] = { ptr, NULL, NULL, NULL };
				int srcLinesize[4] = { bitmapData->Stride, 0, 0, 0 };

				// convert video frame to the RGB bitmap
				libffmpeg::sws_scale( data->ConvertContext, data->VideoFrame->data, data->VideoFrame->linesize, 0,	data->CodecContext->height, srcData, srcLinesize );

				bitmap->UnlockBits( bitmapData );

			 }

			 libffmpeg::av_free_packet(&packet);
			 if(frameFinished)
			 {
				 break;
			 }
		  }
		  else
			  libffmpeg::av_free_packet(&packet);
	  }
	  else
	  {
		  av_free_packet(&packet);  
		  break;
	  }
   }	
   
   return bitmap;
}

// Read next audio frame of the current audio file
array<unsigned char>^ VideoFileReader::ReadAudioFrame(  )
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
				int in_used = libffmpeg::avcodec_decode_audio3(data->AudioCodecContext, data->Buffer , &chunk, &packet);

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