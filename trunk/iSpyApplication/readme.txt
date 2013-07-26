To build iSpy you will need to:

Remove the reference to the FFMPEG and InstallHelper projects (they contain sensitive code signing information) and add a reference to iSpy.Video.FFMPEG.dll in the DLLS directory

Remove all the pre and post build events for the projects (they are used to authenticode sign iSpy - the certificate is not part of the project).
