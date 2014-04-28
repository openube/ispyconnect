using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Kinect;
using Microsoft.Win32;

namespace iSpyApplication.Video
{
    /// <summary>
    /// Static class containing methods and properties which are useful when 
    /// using libvlc from a .net application.
    /// </summary>
    public static class VlcHelper
    {
        private static string _vlcInstallationFolder="";

        private static bool _failed;
        public static readonly Version  VMin = new Version(2,0,0);
        private static Version _versionVLC = new Version(0,0,0);

        #region AddVlcToPath method

        /// <summary>
        /// Looks in the registry to see where VLC is installed, and temporarily
        /// adds that folder to the PATH environment variable, so that the
        /// runtime is able to locate libvlc.dll and other libraries that it
        /// depends on, without needing to copy them all to the folder where 
        /// your application is.
        /// </summary>
        public static void AddVlcToPath()
        {
            // Get the current value of the PATH environment variable
            string currentPath = Environment.GetEnvironmentVariable("PATH");

            // Concatenate the VLC installation and plugins folders onto the 
            // current path
            if (currentPath != null)
                if (currentPath.IndexOf(VlcInstallationFolder, StringComparison.Ordinal) == -1)
                {
                    string newPath = VlcInstallationFolder + ";"
                                     + VlcPluginsFolder + ";"
                                     + currentPath;

                    // Update the PATH environment variable
                    Environment.SetEnvironmentVariable("PATH", newPath);
                }
        }

        #endregion

        #region VlcInstallationFolder property

        /// <summary>
        /// Gets the location of the folder where VLC is installed, from the 
        /// registry.
        /// </summary>
        public static string VlcInstallationFolder
        {
            get
            {
                if (VlcInstalled)
                    return _vlcInstallationFolder;
                return "";
            }
        }

        #endregion

        #region VlcInstalled property

        ///// <summary>
        ///// Check if VLC is installed
        ///// </summary>
        public static bool VlcInstalled
        {
            get
            {

                if (_vlcInstallationFolder != "")
                    return true;
                if (_failed)
                    return false;

                try
                {
                    RegistryKey vlcKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\VideoLAN\VLC\", false);
                    if (vlcKey != null)
                    {
                        var v = Version.Parse(vlcKey.GetValue("Version").ToString());
                        _vlcInstallationFolder = (string) vlcKey.GetValue("InstallDir")
                                                    + Path.DirectorySeparatorChar;
                        if (v.CompareTo(VMin) >= 0)
                        {
                            MainForm.LogMessageToFile("Using VLC from " + _vlcInstallationFolder+" (v"+v+")");
                            _versionVLC = Version.Parse(vlcKey.GetValue("Version").ToString());
                            return true;
                        }
                    }
                }
                catch(Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                }
                if (Program.Platform == "x64")
                {
                    bool b = CheckFolder64(@"C:\Program Files\VideoLAN\VLC\");
                    if (!b)
                    {
                        b = CheckFolder64(Program.AppPath + @"VLC64\");
                    }
                    if (b)
                        return true;
                }
                _failed = true;
                return false;
            }
        }

        private static bool CheckFolder64(string folder)
        {
            try
            {
                if (File.Exists(folder+"libvlc.dll"))
                {
                    if (Helper.UnmanagedDllIs64Bit(folder + "libvlc.dll"))
                    {
                        // Get the file version for the notepad.
                        FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(folder + "libvlc.dll");

                        var v = Version.Parse(myFileVersionInfo.FileVersion);
                        if (v.CompareTo(VMin) >= 0)
                        {

                            _vlcInstallationFolder = folder;
                            _versionVLC = v;
                            MainForm.LogMessageToFile("Using VLC (x64) from " + _vlcInstallationFolder + " (v" + _versionVLC + ")");
                            return true;
                        }
                        MainForm.LogMessageToFile("VLC in " + folder + " is 64 bit but below minimum version. Please update with the latest version of VLC.");
                    }
                    else
                        MainForm.LogMessageToFile("VLC in " + folder + " is not 64 bit. You need to replace it with the 64 bit version of VLC.");
                }
            }
            catch
            {

            }
            return false;
        }

        #endregion

        #region VlcVersion property

        /// <summary>
        /// Check if VLC is installed
        /// </summary>
        public static Version VlcVersion
        {
            get
            {
                return _versionVLC;
            }
        }

        #endregion

        #region VlcPluginsFolder property

        /// <summary>
        /// Gets the location of the VLC plugins folder.
        /// </summary>
        public static string VlcPluginsFolder
        {
            get
            {
                return VlcInstallationFolder
                       + "plugins"
                       + Path.DirectorySeparatorChar;
            }
        }

        #endregion
    }
}