namespace iSpyApplication.Controls
{
    public class Delegates
    {
        public delegate void NotificationEventHandler(object sender, NotificationType e);

        public delegate void DisableDelegate();

        public delegate void EnableDelegate();

        public delegate void AddAudioDelegate();

        public delegate void FileListUpdatedEventHandler(object sender);

        public delegate void RemoteCommandEventHandler(object sender, ThreadSafeCommand e);

        public delegate void NewDataAvailable(object sender, NewDataAvailableArgs eventArgs);
    }

    
}
