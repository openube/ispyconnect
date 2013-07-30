To build iSpy you will need to:

Remove the reference to the FFMPEG and InstallHelper projects (they contain sensitive code signing information) and add a reference to iSpy.Video.FFMPEG.dll in the DLLS directory

Remove all the pre and post build events for the projects (they are used to authenticode sign iSpy - the certificate is not part of the project).

To debug:
copy all the av*.dll, postproc-52.dll, swscale-2.dll, swresample-0.dll and lame_enc.dll files from the ispy application directory to the bin/x86/debug directory

If you get reference errors, install the latest version of ispy from ispyconnect and copy the updated dlls from the program files folder to the DLLS folder