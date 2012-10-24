// AForge FFMPEG Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2009-2011
// contacts@aforgenet.com
//
// addition to the aforge ffmpeg library to add H264 and audio encoding support © iSpyConnect.com, 2012
// sean@ispyconnect.com

#include "StdAfx.h"
#include "VideoFileWriter.h"
#include <vcclr.h>

#define MAX_AUDIO_PACKET_SIZE (128 * 1024)
using System::Runtime::InteropServices::Marshal;

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

static void write_video_frame( WriterPrivateData^ data );
static void open_video( WriterPrivateData^ data );
static void open_audio( WriterPrivateData^ data );
static void add_video_stream( WriterPrivateData^ data, int width, int height, bool crf, int bitRate,
							  enum libffmpeg::CodecID codec_id, enum libffmpeg::PixelFormat pixelFormat, int framerate );
static void add_audio_stream( WriterPrivateData^ data, enum libffmpeg::CodecID codec_id);

// A structure to encapsulate all FFMPEG related private variable
ref struct WriterPrivateData
{
public:
	
	libffmpeg::AVFormatContext*		FormatContext;
	libffmpeg::AVStream*			VideoStream;
	libffmpeg::AVStream*			AudioStream;
	libffmpeg::AVFrame*				VideoFrame;
	struct libffmpeg::SwsContext*	ConvertContext;
	struct libffmpeg::SwsContext*	ConvertContextGrayscale;
	

	libffmpeg::uint8_t*	VideoOutputBuffer;
	int VideoOutputBufferSize;

	libffmpeg::uint8_t*	AudioEncodeBuffer;
	char* AudioBuffer;

	int AudioEncodeBufferSize;
	int	AudioInputSampleSize;
	int AudioBufferSizeCurrent;
	int AudioBufferSize;
	int FrameNumber;
	bool IsConstantFramerate;

	int SampleRate;
	int BitRate;
	int Channels;

	WriterPrivateData( )
	{
		FormatContext     = NULL;
		VideoStream       = NULL;
		AudioStream		  = NULL;
		VideoFrame        = NULL;
		ConvertContext	  = NULL;
		ConvertContextGrayscale = NULL;
		VideoOutputBuffer = NULL;

		AudioEncodeBuffer = NULL;
		AudioEncodeBufferSize = 0;
		AudioInputSampleSize = NULL;

		AudioBufferSize = 1024 * 1024 * 4;
		AudioBuffer = new char[AudioBufferSize];
		AudioBufferSizeCurrent = 0;
		FrameNumber = 0;
		IsConstantFramerate = false;
	}
};
#pragma endregion

// Class constructor
VideoFileWriter::VideoFileWriter( void ) :
    data( nullptr ), disposed( false )
{
	libffmpeg::AVCodec *codec = libffmpeg::avcodec_find_encoder(libffmpeg::CODEC_ID_H264);
	if ( !codec )
	{
		libffmpeg::av_register_all( );
	}
}



void VideoFileWriter::Open( String^ fileName, int width, int height)
{
	Open( fileName, width, height, true, VideoCodec::Default );
}

void VideoFileWriter::Open( String^ fileName, int width, int height, bool crf, VideoCodec codec )
{
	Open( fileName, width, height, crf, codec,  1600000, 0 );
}

void VideoFileWriter::Open( String^ fileName, int width, int height, bool crf, VideoCodec codec, int bitRate, int framerate )
{
	Open( fileName, width, height, crf, codec,  bitRate, AudioCodec::None, framerate,0,0,0 );
}

char* ManagedStringToUnmanagedUTF8Char(String^ str)
{
    pin_ptr<const wchar_t> wch = PtrToStringChars(str);
    int nBytes = ::WideCharToMultiByte(CP_UTF8, NULL, wch, -1, NULL, 0, NULL, NULL);
    char* lpszBuffer = new char[nBytes];
    ZeroMemory(lpszBuffer, (nBytes) * sizeof(char)); 
    nBytes = ::WideCharToMultiByte(CP_UTF8, NULL, wch, -1, lpszBuffer, nBytes, NULL, NULL);
    return lpszBuffer;
}

// Creates a video file with the specified name and properties
void VideoFileWriter::Open( String^ fileName, int width, int height, bool crf, VideoCodec codec, int bitRate, AudioCodec audioCodec, int framerate, int bitrate, int samplerate, int channels)
{
    CheckIfDisposed( );

	// close previous file if any open
	Close( );

	data = gcnew WriterPrivateData( );
	bool success = false;

	// check width and height
	if ( ( ( width & 1 ) != 0 ) || ( ( height & 1 ) != 0 ) )
	{
		Console::WriteLine("Video file resolution must be a multiple of two." );
		throw gcnew ArgumentException( "Video file resolution must be a multiple of two." );
	}
		// check video codec
	if ( ( (int) codec < -1 ) || ( (int) codec >= CODECS_COUNT ) )
	{
		Console::WriteLine("Invalid video codec is specified." );
		throw gcnew ArgumentException( "Invalid video codec is specified." );
	}

	m_width  = width;
	m_height = height;
	m_codec  = codec;
	m_audiocodec = audioCodec;
	m_bitRate = bitRate;
	
	// convert specified managed String to unmanaged string
	//array<Byte>^ encodedBytes = System::Text::Encoding::UTF8->GetBytes(fileName);

	//int size = Marshal::SizeOf(encodedBytes[0]) * encodedBytes->Length;
	//IntPtr pnt = Marshal::AllocHGlobal(size);
	//Marshal::Copy(encodedBytes, 0, pnt, encodedBytes->Length);

	char *nativeFileName= ManagedStringToUnmanagedUTF8Char(fileName);

	try
	{
		// gues about destination file format from its file name
		libffmpeg::AVOutputFormat* outputFormat = libffmpeg::av_guess_format( NULL, nativeFileName, NULL );

		if ( !outputFormat )
		{
			// gues about destination file format from its short name
			outputFormat = libffmpeg::av_guess_format( "mp4", NULL, NULL );

			if ( !outputFormat )
			{
				Console::WriteLine("Cannot find suitable output format." );
				throw gcnew VideoException( "Cannot find suitable output format." );
			}
		}

		// prepare format context
		data->FormatContext = libffmpeg::avformat_alloc_context( );

		if ( !data->FormatContext )
		{
			Console::WriteLine("Cannot allocate format context." );
			throw gcnew VideoException( "Cannot allocate format context." );
		}
		data->FormatContext->oformat = outputFormat;

		// add video stream using the specified video codec

		add_video_stream( data, width, height, crf, bitRate,
			( codec == VideoCodec::Default ) ? outputFormat->video_codec : (libffmpeg::CodecID) video_codecs[(int) codec],
			( codec == VideoCodec::Default ) ? libffmpeg::PIX_FMT_YUV420P : (libffmpeg::PixelFormat) pixel_formats[(int) codec], framerate );

		if (audioCodec!=AudioCodec::None)	{
			data->SampleRate=samplerate;
			data->BitRate = bitrate;
			data->Channels = channels;
			add_audio_stream(data,  (libffmpeg::CodecID) audio_codecs[(int)audioCodec]);

		}

		// set the output parameters (must be done even if no parameters)
		if ( libffmpeg::av_set_parameters( data->FormatContext, NULL ) < 0 )
		{
			Console::WriteLine("Failed configuring format context." );
			throw gcnew VideoException( "Failed configuring format context." );
		}

		open_video( data );

		if (audioCodec!=AudioCodec::None)
			open_audio( data );

		// open output file
		if ( !( outputFormat->flags & AVFMT_NOFILE ) )
		{
			if ( libffmpeg::avio_open( &data->FormatContext->pb, nativeFileName, AVIO_FLAG_WRITE ) < 0 )
			{
				Console::WriteLine("Cannot open the video file." );
				throw gcnew System::IO::IOException( "Cannot open the video file." );
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
void VideoFileWriter::Close( )
{
	if ( data != nullptr )
	{
		if ( data->FormatContext )
		{
			if ( data->VideoOutputBuffer )
			{
				Flush();
				libffmpeg::av_free( data->VideoOutputBuffer );
			}
			if ( data->FormatContext->pb != NULL )
			{
				libffmpeg::av_write_trailer( data->FormatContext );
			}

			if ( data->AudioEncodeBuffer )
			{
				libffmpeg::avcodec_flush_buffers(data->AudioStream->codec);
				libffmpeg::av_free( data->AudioEncodeBuffer );
			}		

			if ( data->AudioStream)	{
				libffmpeg::avcodec_close( data->AudioStream->codec );
			}

			if ( data->VideoStream )
			{
				libffmpeg::avcodec_close( data->VideoStream->codec );
			}
						
			if (data->AudioBuffer)
			{
				delete[] data->AudioBuffer;
				data->AudioBuffer = NULL;
			}
			
			if ( data->VideoFrame )
			{
				libffmpeg::av_free( data->VideoFrame->data[0] );
				libffmpeg::av_free( data->VideoFrame );
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

		if ( data->ConvertContext != NULL )
		{
			libffmpeg::sws_freeContext( data->ConvertContext );
		}

		if ( data->ConvertContextGrayscale != NULL )
		{
			libffmpeg::sws_freeContext( data->ConvertContextGrayscale );
		}

		data = nullptr;
	}

	m_width  = 0;
	m_height = 0;
}


// Writes new video frame to the opened video file
void VideoFileWriter::WriteVideoFrame( Bitmap^ frame )
{
	WriteVideoFrame( frame, TimeSpan::MinValue);
}


// Writes new video frame to the opened video file
void VideoFileWriter::WriteAudio(BYTE* soundBuffer, int soundBufferSize)
{
	CheckIfDisposed( );

	if ( data == nullptr )
	{
		throw gcnew System::IO::IOException( "A video file was not opened yet." );
	}

	 // Add sound
	AddAudioSamples(data, soundBuffer, soundBufferSize);
}

void VideoFileWriter::WriteVideoFrame( Bitmap^ frame, TimeSpan timestamp)
{
    CheckIfDisposed( );

	if ( data == nullptr )
	{
		throw gcnew System::IO::IOException( "A video file was not opened yet." );
	}

	if ( ( frame->PixelFormat != PixelFormat::Format24bppRgb ) &&
	     ( frame->PixelFormat != PixelFormat::Format32bppArgb ) &&
		 ( frame->PixelFormat != PixelFormat::Format32bppPArgb ) &&
	 	 ( frame->PixelFormat != PixelFormat::Format32bppRgb ) &&
		 ( frame->PixelFormat != PixelFormat::Format8bppIndexed ) )
	{
		throw gcnew ArgumentException( "The provided bitmap must be 24 or 32 bpp color image or 8 bpp grayscale image." );
	}

	if ( ( frame->Width != m_width ) || ( frame->Height != m_height ) )
	{
		throw gcnew ArgumentException( "Bitmap size must be of the same as video size, which was specified on opening video file." );
	}

	// lock the bitmap
	BitmapData^ bitmapData = frame->LockBits( System::Drawing::Rectangle( 0, 0, m_width, m_height ),
		ImageLockMode::ReadOnly,
		( frame->PixelFormat == PixelFormat::Format8bppIndexed ) ? PixelFormat::Format8bppIndexed : PixelFormat::Format24bppRgb );

	libffmpeg::uint8_t* ptr = reinterpret_cast<libffmpeg::uint8_t*>( static_cast<void*>( bitmapData->Scan0 ) );

	libffmpeg::uint8_t* srcData[4] = { ptr, NULL, NULL, NULL };
	int srcLinesize[4] = { bitmapData->Stride, 0, 0, 0 };

	// convert source image to the format of the video file
	if ( frame->PixelFormat == PixelFormat::Format8bppIndexed )
	{
		libffmpeg::sws_scale( data->ConvertContextGrayscale, srcData, srcLinesize, 0, m_height, data->VideoFrame->data, data->VideoFrame->linesize );
	}
	else
	{
		libffmpeg::sws_scale( data->ConvertContext, srcData, srcLinesize, 0, m_height, data->VideoFrame->data, data->VideoFrame->linesize );
	}

	frame->UnlockBits( bitmapData );

	if (!data->IsConstantFramerate)
	{
		data->VideoFrame->pts = static_cast<libffmpeg::int64_t>( timestamp.TotalMilliseconds );
	}
	else
		data->VideoFrame->pts = data->FrameNumber;
	// write the converted frame to the video file
	write_video_frame( data );
	data->FrameNumber++;
	
}

void VideoFileWriter::Flush(void)
{
	if ( data != nullptr )
	{
		int out_size = 0;
		int ret = 0;
	
		libffmpeg::AVCodecContext* c = data->VideoStream->codec;
		/* get the delayed frames */
		while (1) {
			libffmpeg::AVPacket packet;
			libffmpeg::av_init_packet(&packet);
		
			out_size = libffmpeg::avcodec_encode_video(c, data->VideoOutputBuffer, data->VideoOutputBufferSize, NULL);
			if (out_size < 0) {
				//fprintf(stderr, "Error encoding delayed frame %d\n", out_size);
				break;
			}
			if (out_size == 0) {
				break;
			}
			if (c->coded_frame->pts != AV_NOPTS_VALUE) {
				packet.pts = av_rescale_q(c->coded_frame->pts,
							  c->time_base,
							  data->VideoStream->time_base);
				//fprintf(stderr, "Video Frame PTS: %d\n", (int)packet.pts);
			} else {
				//fprintf(stderr, "Video Frame PTS: not set\n");
			}
			if (c->coded_frame->key_frame) {
				packet.flags |= AV_PKT_FLAG_KEY;
			}
			packet.stream_index = data->VideoStream->index;
			packet.data = data->VideoOutputBuffer;
			packet.size = out_size;

			ret = libffmpeg::av_interleaved_write_frame( data->FormatContext, &packet );
			libffmpeg::av_free_packet(&packet);
			if (ret != 0) {
				//fprintf(stderr, "Error writing delayed frame %d\n", ret);
				break;
			}
		}
		libffmpeg::avcodec_flush_buffers(data->VideoStream->codec);
	}
}


void VideoFileWriter::AddAudioSamples( WriterPrivateData^ data,  BYTE* soundBuffer, int soundBufferSize)
{
 if (!data->AudioStream)
	 return;
  libffmpeg::AVCodecContext* codecContext = data->AudioStream->codec;   

  memcpy(data->AudioBuffer + data->AudioBufferSizeCurrent,  soundBuffer, soundBufferSize);
  data->AudioBufferSizeCurrent += soundBufferSize;

  BYTE* pSoundBuffer = (BYTE *)data->AudioBuffer;
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

	//sync audio with video
	//if (data->VideoStream->codec->coded_frame->pts != AV_NOPTS_VALUE) {
	//			pkt.pts = av_rescale_q(data->VideoStream->codec->coded_frame->pts,  data->VideoStream->codec->time_base,  data->VideoStream->time_base);
 //   }
	//Console::WriteLine("AUDIO PTS: "+pkt.pts);
    

	pkt.flags |= AV_PKT_FLAG_KEY;
    pkt.stream_index = data->AudioStream->index;
    pkt.data = data->AudioEncodeBuffer;
	
    // Write the compressed frame in the media file.
    if (libffmpeg::av_interleaved_write_frame(data->FormatContext, &pkt) != 0) 
    {
      break;
    }
	libffmpeg::av_free_packet(&pkt);
    nCurrentSize -= packSizeInSize;  
    pSoundBuffer += packSizeInSize;      
  }

  // save excess
  memcpy(data->AudioBuffer, data->AudioBuffer + data->AudioBufferSizeCurrent - nCurrentSize, nCurrentSize);
  data->AudioBufferSizeCurrent = nCurrentSize; 

}

#pragma region Private methods
// Writes video frame to opened video file
void write_video_frame( WriterPrivateData^ data )
{
	libffmpeg::AVCodecContext* codecContext = data->VideoStream->codec;
	int out_size, ret = 0;
	libffmpeg::AVPacket packet;
	if ( data->FormatContext->oformat->flags & AVFMT_RAWPICTURE )
	{
		Console::WriteLine( "raw picture must be written" );
	}
	else
	{
		// encode the image

		out_size = libffmpeg::avcodec_encode_video( codecContext, data->VideoOutputBuffer, data->VideoOutputBufferSize, data->VideoFrame );

		// if zero size, it means the image was buffered
		if ( out_size > 0 )
		{
			
			libffmpeg::av_init_packet( &packet );

			if ( codecContext->coded_frame->pts != AV_NOPTS_VALUE )
			{
				packet.pts = libffmpeg::av_rescale_q( codecContext->coded_frame->pts, codecContext->time_base, data->VideoStream->time_base );
			}

			if ( codecContext->coded_frame->key_frame )
			{
				packet.flags |= AV_PKT_FLAG_KEY;
			}
			packet.dts = AV_NOPTS_VALUE;

			packet.stream_index = data->VideoStream->index;
			packet.data = data->VideoOutputBuffer;
			packet.size = out_size;

			// write the compressed frame to the media file
			ret = libffmpeg::av_interleaved_write_frame( data->FormatContext, &packet );

			libffmpeg::av_free_packet(&packet);
			//ret = libffmpeg::av_write_frame( data->FormatContext, &packet );
		}
		else
		{
			// image was buffered
		}
	}

	if ( ret != 0 )
	{
		throw gcnew VideoException( "Error while writing video frame ("+ret+")" );
	}
}


// Allocate picture of the specified format and size
static libffmpeg::AVFrame* alloc_picture( enum libffmpeg::PixelFormat pix_fmt, int width, int height  )
{
	libffmpeg::AVFrame* picture;
	void* picture_buf;
	int size;

	picture = libffmpeg::avcodec_alloc_frame( );
	//picture->pts = libffmpeg::video_pts;

	if ( !picture )
	{
		return NULL;
	}

	size = libffmpeg::avpicture_get_size( pix_fmt, width, height );
	picture_buf = libffmpeg::av_malloc( size );
	if ( !picture_buf )
	{
		libffmpeg::av_free( picture );
		return NULL;
	}

	libffmpeg::avpicture_fill( (libffmpeg::AVPicture *) picture, (libffmpeg::uint8_t *) picture_buf, pix_fmt, width, height );

	return picture;
}


// Create new video stream and configure it
void add_video_stream( WriterPrivateData^ data,  int width, int height, bool crf, int bitRate,
					  enum libffmpeg::CodecID codecId, enum libffmpeg::PixelFormat pixelFormat, int framerate )
{
	libffmpeg::AVCodec *codec = libffmpeg::avcodec_find_encoder(codecId);
	libffmpeg::AVCodecContext* codecContex;
	// create new stream
	data->VideoStream = libffmpeg::avformat_new_stream( data->FormatContext, codec );
	if ( !data->VideoStream )
	{
		throw gcnew VideoException( "Failed creating new video stream." );
	}
	

	codecContex = data->VideoStream->codec;
	codecContex->codec_id   = codecId;
	codecContex->codec_type = libffmpeg::AVMEDIA_TYPE_VIDEO;

	// put sample parameters
	codecContex->bit_rate = bitRate;
	codecContex->width    = width;
	codecContex->height   = height;

	// time base: this is the fundamental unit of time (in seconds) in terms
	// of which frame timestamps are represented. for fixed-fps content,
	// timebase should be 1/framerate and timestamp increments should be
	// identically 1.

	codecContex->time_base.num = 1;
	codecContex->time_base.den = 1000;

	if (framerate>0)	{ //avi - fixed fps
			codecContex->time_base.den = framerate;
			//codecContex->crfen = framerate;
			data->IsConstantFramerate = true;
	}
	

	//codecContex->gop_size = 12; // emit one intra frame every twelve frames at most
	codecContex->pix_fmt  = pixelFormat;

	if ( codecContex->codec_id == libffmpeg::CODEC_ID_MPEG1VIDEO )
	{
		// Needed to avoid using macroblocks in which some coeffs overflow.
		// This does not happen with normal video, it just happens here as
		// the motion of the chroma plane does not match the luma plane.
		codecContex->mb_decision = 2;
	}

	if ( codecContex->codec_id == libffmpeg::CODEC_ID_H264 )
	{
		data->VideoStream->need_parsing = libffmpeg::AVSTREAM_PARSE_FULL_ONCE;

		//codecContex->coder_type = 0;  // coder = 1
		//codecContex->flags|=CODEC_FLAG_LOOP_FILTER;   // flags=+loop
		//codecContex->me_cmp= 256;// me_cmp|= 1 // cmp=+chroma, where CHROMA = 1
		//codecContex->partitions|=X264_PART_I8X8+X264_PART_I4X4+X264_PART_P8X8+X264_PART_B8X8; // partitions=+parti8x8+parti4x4+partp8x8+partb8x8
		//codecContex->me_method = libffmpeg::ME_HEX;    // me_method=hex
		//codecContex->me_subpel_quality = 7;   // subq=7
		//codecContex->me_range = 16;   // me_range=16
		//codecContex->gop_size = 250;  // g=250
		//codecContex->keyint_min = 25; // keyint_min=25
		//codecContex->scenechange_threshold = 40;  // sc_threshold=40
		//codecContex->i_quant_factor = 0.71; // i_qfactor=0.71
		//codecContex->b_frame_strategy = 1;  // b_strategy=1
		//codecContex->qcompress = 0.6; // qcomp=0.6
		//codecContex->qmin = 10;   // qmin=0
		//codecContex->qmax = 51;   // qmax=51
		//codecContex->max_qdiff = 4;   // qdiff=4
		//codecContex->max_b_frames = 0;    // bf=3
		//codecContex->refs = 5;    // refs=3
		////codecContex->directpred = 1;  // directpred=1
		//codecContex->trellis = 0; // trellis=1 (requires coder_type = 1)
		//codecContex->flags2|=CODEC_FLAG2_MIXED_REFS+CODEC_FLAG2_WPRED+CODEC_FLAG2_8X8DCT;
		////codecContex->flags2|=CODEC_FLAG2_BPYRAMID+CODEC_FLAG2_MIXED_REFS+CODEC_FLAG2_WPRED+CODEC_FLAG2_8X8DCT+CODEC_FLAG2_FASTPSKIP;  // flags2=+bpyramid+mixed_refs+wpred+dct8x8+fastpskip
		////codecContex->weighted_p_pred = 2; // wpredp=2
		//codecContex->partitions|=X264_PART_I8X8+X264_PART_I4X4+X264_PART_P8X8+X264_PART_B8X8;

		codecContex->coder_type = 0;
		codecContex->flags|=CODEC_FLAG_LOOP_FILTER;
		codecContex->profile=FF_PROFILE_H264_BASELINE; //Baselinev
		codecContex->scenechange_threshold = 40; 
        codecContex->gop_size=8;
        codecContex->max_b_frames=0;
        codecContex->max_qdiff=4;
		codecContex->me_method=7;
        codecContex->me_range=16;
		codecContex->me_cmp|= 1;
		codecContex->me_subpel_quality = 6; 
        codecContex->qmin=10;
        codecContex->qmax=51;
        codecContex->qcompress=0.6f;
        codecContex->keyint_min=5;
        codecContex->trellis=0;
        codecContex->level=13; //Level 1.3
		codecContex->refs = 1;
		codecContex->weighted_p_pred = 0;
		if (crf)
			codecContex->crf = 20.0f; //quality is set by bitrate
		codecContex->flags2|=CODEC_FLAG2_BPYRAMID-CODEC_FLAG2_WPRED-CODEC_FLAG2_8X8DCT;
				
	}

	// some formats want stream headers to be separate
	if( data->FormatContext->oformat->flags & AVFMT_GLOBALHEADER )
	{
		codecContex->flags |= CODEC_FLAG_GLOBAL_HEADER;
	}


	
}

// Open video codec and prepare out buffer and picture
void open_video( WriterPrivateData^ data )
{
	libffmpeg::AVCodecContext* codecContext = data->VideoStream->codec;
	libffmpeg::AVCodec* codec = libffmpeg::avcodec_find_encoder( codecContext->codec_id );

	if ( !codec )
	{
		Console::WriteLine("Cannot find video codec." );
		throw gcnew VideoException( "Cannot find video codec." );
	}

	// open the codec 
	int i = libffmpeg::avcodec_open( codecContext, codec );
	if ( i < 0 )
	{
		Console::WriteLine("Cannot open video codec. ("+i+")" );
		throw gcnew VideoException( "Cannot open video codec." );
	}

	data->VideoOutputBuffer = NULL;
	if ( !( data->FormatContext->oformat->flags & AVFMT_RAWPICTURE ) )
	{
         // allocate output buffer 
         data->VideoOutputBufferSize = 6 * codecContext->width * codecContext->height; // more than enough even for raw video
		 data->VideoOutputBuffer = (libffmpeg::uint8_t*) libffmpeg::av_malloc( data->VideoOutputBufferSize );
	}

	// allocate the encoded raw picture
	data->VideoFrame = alloc_picture( codecContext->pix_fmt, codecContext->width, codecContext->height );

	if ( !data->VideoFrame )
	{
		Console::WriteLine("Cannot allocate video picture." );
		throw gcnew VideoException( "Cannot allocate video picture." );
	}

	// prepare scaling context to convert RGB image to video format
	data->ConvertContext = libffmpeg::sws_getContext( codecContext->width, codecContext->height, libffmpeg::PIX_FMT_BGR24,
			codecContext->width, codecContext->height, codecContext->pix_fmt,
			SWS_BICUBIC, NULL, NULL, NULL );
	// prepare scaling context to convert grayscale image to video format
	data->ConvertContextGrayscale = libffmpeg::sws_getContext( codecContext->width, codecContext->height, libffmpeg::PIX_FMT_GRAY8,
			codecContext->width, codecContext->height, codecContext->pix_fmt,
			SWS_BICUBIC, NULL, NULL, NULL );

	if ( ( data->ConvertContext == NULL ) || ( data->ConvertContextGrayscale == NULL ) )
	{
		Console::WriteLine("Cannot initialize frames conversion context." );
		throw gcnew VideoException( "Cannot initialize frames conversion context." );
	}
}

void add_audio_stream( WriterPrivateData^ data,  enum libffmpeg::CodecID codec_id )
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
	  codecContex->time_base.den = codecContex->sample_rate;
	  

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


void open_audio( WriterPrivateData^ data )
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

