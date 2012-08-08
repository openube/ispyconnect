#pragma once
using namespace System;
#include "AudioCodec.h"
namespace AForge { namespace Video { namespace FFMPEG
{
	ref struct AudioWriterPrivateData;
    public ref class AudioFileWriter : IDisposable
	{
	public:
		property AudioCodec ACodec
		{
			AudioCodec get( )
			{
				CheckIfAudioFileIsOpen( );
				return m_audiocodec;
			}
		}
		property bool IsOpen
		{
			bool get ( )
			{
				return ( data != nullptr );
			}
		}

    protected:
        !AudioFileWriter( )
        {
            Close( );
        }

	public:
		AudioFileWriter( void );
        ~AudioFileWriter( )
        {
            this->!AudioFileWriter( );
            disposed = true;
        }
		void Open( String^ fileName);
		void Open( String^ fileName, AudioCodec audioCodec, int bitrate, int samplerate, int channels );
		void WriteAudio(BYTE* soundBuffer, int soundBufferSize);
		void Close( );
		void Flush( );

	private:
		AudioCodec m_audiocodec;
	private:
		void CheckIfAudioFileIsOpen( )
		{
			if ( data == nullptr )
			{
				throw gcnew System::IO::IOException( "Audio file is not open, so can not access its properties." );
			}
		}
        void CheckIfDisposed( )
        {
            if ( disposed )
            {
                throw gcnew System::ObjectDisposedException( "The object was already disposed." );
            }
        }
	private:
		AudioWriterPrivateData^ data;
        bool disposed;
	};

} } }
