// AForge FFMPEG Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2009-2011
// contacts@aforgenet.com
//

#include "StdAfx.h"
#include "AudioCodec.h"

namespace libffmpeg
{
	extern "C"
	{
		#pragma warning(disable:4635) 
		#pragma warning(disable:4244) 
		#include "libavcodec\avcodec.h"
	}
}

int audio_codecs[] =
{
	libffmpeg::CODEC_ID_MP3,
	libffmpeg::CODEC_ID_AAC,
	libffmpeg::CODEC_ID_MP4ALS
};


int AUDIO_CODECS_COUNT ( sizeof( audio_codecs ) / sizeof( libffmpeg::CodecID ) );