
namespace iSpyApplication
{
    public class Enums
    {
        public enum AlertMode { Movement, NoMovement };

        public enum PtzCommand
        {
            Center,
            Left,
            Upleft,
            Up,
            UpRight,
            Right,
            DownRight,
            Down,
            DownLeft,
            ZoomIn,
            ZoomOut,
            Stop
        } ;

        public enum MatchMode
        {
            IsInList = 0,
            NotInList = 1
        } ;

        public enum AudioStreamMode
        {
            PCM,
            MP3
        }
    }
}
