using System;
using System.IO;
using System.Runtime.InteropServices;

namespace iSpyApplication
{
    public static class FileOperations
    {
        private const int FoDelete = 3;
        private const int FofAllowundo = 0x40;
        private const int FofNoconfirmation = 0x0010;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
        public struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.U4)]
            public int wFunc;
            public string pFrom;
            public string pTo;
            public short fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern int SHFileOperation(ref SHFILEOPSTRUCT fileOp);

        private static void DeleteFileOperation(string filePath)
        {
            var fileop = new SHFILEOPSTRUCT
                             {
                                 wFunc = FoDelete,
                                 pFrom = filePath + '\0' + '\0',
                                 fFlags = FofAllowundo | FofNoconfirmation
                             };

            SHFileOperation(ref fileop);
        }

        public static bool Delete(string filePath)
        {
            try
            {
                if (MainForm.Conf.DeleteToRecycleBin)
                {
                    DeleteFileOperation(filePath);
                }
                else
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
                return false;
            }
            return true;
        }
    }
}
