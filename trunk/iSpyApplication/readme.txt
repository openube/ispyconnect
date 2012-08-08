To build iSpy you will need to:

Ensure the Video.FFMPEG project builds as part of the solution and is referenced by the iSpyApplication project

Remove all the post build events for the projects (they are used to authenticode sign iSpy - the certificate is not part of the project).
