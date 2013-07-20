using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace iSpyApplication.Video
{
    /// <summary>
    /// Static class containing methods and properties which are useful when 
    /// using libvlc from a .net application.
    /// </summary>
    public static class VlcHelper
    {
        private static string _vlcInstallationFolder;

        public static readonly Version  VMin = new Version(2,0,0);

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
                if (_vlcInstallationFolder == null)
                {


                    if (Program.Platform == "x64")
                    {
                        _vlcInstallationFolder = Program.AppPath + "VLC64";
                    }
                    else
                    {
                        RegistryKey vlcKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\VideoLAN\VLC", false);
                        if (vlcKey != null)
                        {
                            _vlcInstallationFolder = (string)vlcKey.GetValue("InstallDir")
                                                     + Path.DirectorySeparatorChar;
                            vlcKey.Close();
                        }
                    }
                }
                return _vlcInstallationFolder;
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
                if (Program.Platform == "x64")
                    return true;
                try
                {
                    RegistryKey vlcKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\VideoLAN\VLC\", false);
                    if (vlcKey != null)
                    {
                        var v = Version.Parse(vlcKey.GetValue("Version").ToString());
                        return (v.CompareTo(VMin) >= 0);
                    }
                }
                catch
                {
                }
                return false;
            }
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
                if (Program.Platform == "x64")
                    return new Version(2,0,6);
                try
                {
                    RegistryKey vlcKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\VideoLAN\VLC\", false);
                    if (vlcKey != null)
                    {
                        var v = Version.Parse(vlcKey.GetValue("Version").ToString());
                        return v;
                    }
                }
                catch
                {
                }
                return new Version(0,0);
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