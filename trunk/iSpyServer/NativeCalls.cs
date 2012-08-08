using System;
using System.Runtime.InteropServices;

namespace iSpyServer
{
    public static class NativeCalls
    {
        public const int WmSyscommand = 0x0112;
        public static IntPtr ScDragsizeS = (IntPtr)0xF006;
        public static IntPtr ScDragsizeE = (IntPtr)0xF002;
        public static IntPtr ScDragsizeSe = (IntPtr)0xF008;

        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ReleaseCapture(IntPtr hwnd);
    }
}
