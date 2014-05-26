using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace iSpyApplication
{
    public class NewDataAvailableArgs : EventArgs
    {
        private readonly byte[] _decodedData;

        public NewDataAvailableArgs(byte[] decodedData)
        {
            _decodedData = decodedData;
        }

        public byte[] DecodedData
        {
            get { return _decodedData; }
        }
    }

    public class VolumeChangedEventArgs : EventArgs
    {
        public int NewLevel;

        public VolumeChangedEventArgs(int newLevel)
        {
            NewLevel = newLevel;
        }
    }
    public class NotificationType : EventArgs
    {
        public int Objectid;
        public int Objecttypeid;
        public string Text;
        public string Type;
        public string PreviewImage;
        public string OverrideMessage;

        public NotificationType(string type, string text, string previewimage, string overrideMessage = "")
        {
            Type = type;
            Text = text;
            PreviewImage = previewimage;
            OverrideMessage = overrideMessage;
        }
    }

    public class DualFrameEventArgs : EventArgs
    {
        public DualFrameEventArgs(Bitmap displayFrame, Bitmap recordFrame)
        {
            DisplayFrame = displayFrame;
            RecordFrame = recordFrame;
        }

        public Bitmap DisplayFrame { get; private set; }
        public Bitmap RecordFrame { get; private set; }
    }
}
