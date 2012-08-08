#pragma once

using namespace System;
using namespace System::Drawing;
using namespace System::Drawing::Imaging;
using namespace AForge::Video;

namespace AForge { namespace Video { namespace FFMPEG
{
	ref struct ReaderPrivateData2;

	/// <summary>
	/// Class for reading audio files utilizing FFmpeg library.
	/// </summary>
    /// 
    /// <remarks><para>The class allows to read audio files using <a href="http://www.ffmpeg.org/">FFmpeg</a> library.</para>
    /// 
	/// <para><note>Make sure you have <b>FFmpeg</b> binaries (DLLs) in the output folder of your application in order
	/// to use this class successfully. <b>FFmpeg</b> binaries can be found in Externals folder provided with AForge.NET
	/// framework's distribution.</note></para>
	///
    /// <para>Sample usage:</para>
    /// <code>
    /// // create instance of audio reader
    /// AudioFileReader reader = new AudioFileReader( );
    /// // open audio file
    /// reader.Open( "test.avi" );
    /// // check some of its attributes
    /// Console.WriteLine( "width:  " + reader.Width );
    /// Console.WriteLine( "height: " + reader.Height );
    /// Console.WriteLine( "fps:    " + reader.FrameRate );
    /// Console.WriteLine( "codec:  " + reader.CodecName );
    /// // read 100 audio frames out of it
    /// for ( int i = 0; i &lt; 100; i++ )
    /// {
    ///     Bitmap audioFrame = reader.ReadAudioFrame( );
    ///     // process the frame somehow
    ///     // ...
    /// 
    ///     // dispose the frame when it is no longer required
    ///     audioFrame.Dispose( );
    /// }
    /// reader.Close( );
	/// </code>
    /// </remarks>
	///
	public ref class AudioFileReader : IDisposable
	{
	public:

		/// <summary>
		/// Sample rate of stream
		/// </summary>
		///
        /// <exception cref="System::IO::IOException">Thrown if no audio file was open.</exception>
		///
		property int SampleRate
		{
			int get( )
			{
				CheckIfAudioFileIsOpen( );
				return m_sampleRate;
			}
		}

		/// <summary>
		/// Sample rate of stream
		/// </summary>
		///
        /// <exception cref="System::IO::IOException">Thrown if no audio file was open.</exception>
		///
		property int Channels
		{
			int get( )
			{
				CheckIfAudioFileIsOpen( );
				return m_channels;
			}
		}

		/// <summary>
		/// Sample rate of stream
		/// </summary>
		///
        /// <exception cref="System::IO::IOException">Thrown if no audio file was open.</exception>
		///
		property int BitsPerSample
		{
			int get( )
			{
				CheckIfAudioFileIsOpen( );
				return m_bitsPerSample;
			}
		}

		/// <summary>
		/// Name of codec used for encoding the opened audio file.
		/// </summary>
		///
        /// <exception cref="System::IO::IOException">Thrown if no audio file was open.</exception>
		///
		property String^ CodecName
		{
			String^ get( )
			{
				CheckIfAudioFileIsOpen( );
				return m_codecName;
			}
		}

		/// <summary>
		/// The property specifies if a audio file is opened or not by this instance of the class.
		/// </summary>
		property bool IsOpen
		{
			bool get ( )
			{
				return ( data != nullptr );
			}
		}

    protected:

        /// <summary>
        /// Object's finalizer.
        /// </summary>
        /// 
        !AudioFileReader( )
        {
            Close( );
        }

	public:

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioFileReader"/> class.
        /// </summary>
        /// 
		AudioFileReader( void );

        /// <summary>
        /// Disposes the object and frees its resources.
        /// </summary>
        /// 
        ~AudioFileReader( )
        {
            this->!AudioFileReader( );
            disposed = true;
        }

		/// <summary>
        /// Open audio file with the specified name.
        /// </summary>
		///
		/// <param name="fileName">Video file name to open.</param>
		///
        /// <exception cref="System::IO::IOException">Cannot open audio file with the specified name.</exception>
        /// <exception cref="VideoException">A error occurred while opening the audio file. See exception message.</exception>
		///
		void Open( String^ fileName );

        /// <summary>
        /// Read next audio frame of the currently opened audio file.
        /// </summary>
		/// 
		/// <returns>Returns next audio frame of the opened file or <see langword="null"/> if end of
		/// file was reached. The returned audio frame has 24 bpp color format.</returns>
        /// 
        /// <exception cref="System::IO::IOException">Thrown if no audio file was open.</exception>
        /// <exception cref="VideoException">A error occurred while reading next audio frame. See exception message.</exception>
        /// 
		array<unsigned char>^ ReadAudioFrame( );

        /// <summary>
        /// Close currently opened audio file if any.
        /// </summary>
        /// 
		void Close( );

	private:

		String^ m_codecName;
		int m_sampleRate, m_channels,m_bitsPerSample;


	private:
		// Checks if audio file was opened
		void CheckIfAudioFileIsOpen( )
		{
			if ( data == nullptr )
			{
				throw gcnew System::IO::IOException( "Video file is not open, so can not access its properties." );
			}
		}

        // Check if the object was already disposed
        void CheckIfDisposed( )
        {
            if ( disposed )
            {
                throw gcnew System::ObjectDisposedException( "The object was already disposed." );
            }
        }

	private:
		// private data of the class
		ReaderPrivateData2^ data;
        bool disposed;
	};

} } }
