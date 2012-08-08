// AForge FFMPEG Library
// AForge.NET framework
// http://www.aforgenet.com/framework/
//
// Copyright © AForge.NET, 2009-2011
// contacts@aforgenet.com
//

#pragma once

using namespace System;

extern int audio_codecs[];

extern int AUDIO_CODECS_COUNT;

namespace AForge { namespace Video { namespace FFMPEG
{
	/// <summary>
	/// Enumeration of some audio codecs from FFmpeg library, which are available for writing audio files.
	/// </summary>
	public enum class AudioCodec
	{
		None = -1,
		/// <summary>
		/// MPEG-3
		/// </summary>
		MP3 = 0,
		AAC,
		M4A,
	};

} } }