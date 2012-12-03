using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using iSpyApplication;
using iSpyApplication.Video;
using Microsoft.Win32;

internal static class Program
{
    //public static Mutex Mutex;
    private static string _apppath = "", _appdatapath = "";
    public static string AppPath
    {
        get
        {
            if (_apppath != "")
                return _apppath;
            _apppath = (Application.StartupPath.ToLower());
            _apppath = _apppath.Replace(@"\bin\debug", @"\").Replace(@"\bin\release", @"\");
            _apppath = _apppath.Replace(@"\bin\x86\debug", @"\").Replace(@"\bin\x86\release", @"\");

            _apppath = _apppath.Replace(@"\\", @"\");

            if (!_apppath.EndsWith(@"\"))
                _apppath += @"\";
            Directory.SetCurrentDirectory(_apppath);
            return _apppath;
        }   
    }
    public static string AppDataPath
    {
        get
        {
            if (_appdatapath != "")
                return _appdatapath;
            _appdatapath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\iSpy\";
            return _appdatapath;
        }
    }

    public static string ExecutableDirectory = "";
   
    public static Mutex WriterMutex;
    private static int _reportedExceptionCount;
    private static ErrorReporting _er;

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        //uninstall?
        string[] arguments = Environment.GetCommandLineArgs();

        foreach (string argument in arguments)
        {
            if (argument.Split('=')[0].ToLower() == "/u")
            {
                string guid = argument.Split('=')[1];
                string path = Environment.GetFolderPath(Environment.SpecialFolder.System);
                var si = new ProcessStartInfo(path + "/msiexec.exe", "/x " + guid);
                Process.Start(si);
                Application.Exit();
                return;
            }
        }

        try
        {
            Application.EnableVisualStyles();            
            Application.SetCompatibleTextRenderingDefault(false);


            bool firstInstance = true;
            //Mutex = new Mutex(false, "iSpy", out firstInstance);

            var me = Process.GetCurrentProcess();
            var arrProcesses = Process.GetProcessesByName(me.ProcessName);

            if (arrProcesses.Length > 1)
            {
                File.WriteAllText(AppDataPath + "external_command.txt", "showform");
                //ensures pickup by filesystemwatcher
                Thread.Sleep(1000);
                firstInstance = false;               
            }
            
            string executableName = Application.ExecutablePath;
            var executableFileInfo = new FileInfo(executableName);
            ExecutableDirectory = executableFileInfo.DirectoryName;

            bool ei = (!Directory.Exists(AppDataPath) || !Directory.Exists(AppDataPath + @"XML\") ||
                       !File.Exists(AppDataPath + @"XML\config.xml"));
            if (ei)
                EnsureInstall(true);

            bool silentstartup = false;

            string command = "";
            if (args.Length > 0)
            {
                //if (args[0].ToLower().Trim() == "-firstrun" && !ei)
                //    EnsureInstall(false);
                if (args[0].ToLower().Trim() == "-reset" && !ei)
                {
                    if (firstInstance)
                    {
                        if (
                            MessageBox.Show("Reset iSpy? This will overwrite all your settings.", "Confirm",
                                            MessageBoxButtons.OKCancel) == DialogResult.OK)
                            EnsureInstall(true);
                    }
                    else
                    {
                        MessageBox.Show("Please exit iSpy before resetting it.");
                    }
                }
                if (args[0].ToLower().Trim() == "-silent" || args[0].ToLower().Trim('\\') == "s")
                {
                    if (firstInstance)
                    {
                        silentstartup = true;
                    }
                }
                else
                {
                    for (int index = 0; index < args.Length; index++)
                    {
                        string s = args[index];
                        command += s + " ";
                    }
                }
            }

            if (!firstInstance)
            {
                if (!String.IsNullOrEmpty(command))
                {
                    File.WriteAllText(AppDataPath + "external_command.txt", command);
                    //ensures pickup by filesystemwatcher
                    Thread.Sleep(1000);
                }
                
                Application.Exit();
                return;
            }

            if (VlcHelper.VlcInstalled)
                VlcHelper.AddVlcToPath();

            File.WriteAllText(AppDataPath + "external_command.txt", "");

            //VLC integration

            
            
            WriterMutex = new Mutex();
            Application.ThreadException += ApplicationThreadException;
                
            var mf = new MainForm(silentstartup, command);
            Application.Run(mf);
            WriterMutex.Close();
            WriterMutex.Dispose();
            
        }
        catch (Exception ex)
        {
            try
            {
                MainForm.LogExceptionToFile(ex);
            } catch
            {
                
            }
            while (ex.InnerException != null)
            {
                try
                {
                    MainForm.LogExceptionToFile(ex);
                }
                catch
                {

                }
            }
        }
    }

    private static void TryCopy(string source, string target, bool overwrite)
    {
        try
        {
            File.Copy(source, target, overwrite);
        }
        catch
        {

        }
    }

    public static void EnsureInstall(bool reset)
    {

        if (!Directory.Exists(AppDataPath))
        {
            Directory.CreateDirectory(AppDataPath);
        }
        if (!Directory.Exists(AppDataPath + @"XML"))
        {
            Directory.CreateDirectory(AppDataPath + @"XML");
        }

        var didest = new DirectoryInfo(AppDataPath + @"XML\");
        var disource = new DirectoryInfo(AppPath + @"XML\");

        TryCopy(disource + @"PTZ2.xml", didest + @"PTZ2.xml", true);
        TryCopy(disource + @"Translations.xml", didest + @"Translations.xml", true);
        TryCopy(disource + @"Sources.xml", didest + @"Sources.xml", true);

        if (reset || !File.Exists(didest + @"objects.xml"))
        {
            TryCopy(disource + @"objects.xml", didest + @"objects.xml", reset);
        }

        if (reset || !File.Exists(didest + @"config.xml"))
        {
            TryCopy(disource + @"config.xml", didest + @"config.xml", reset);
        }

        if (!Directory.Exists(AppDataPath + @"WebServerRoot"))
        {
            Directory.CreateDirectory(AppDataPath + @"WebServerRoot");
        }
        didest = new DirectoryInfo(AppDataPath + @"WebServerRoot");
        disource = new DirectoryInfo(AppPath + @"WebServerRoot");
        CopyAll(disource, didest);

        if (!Directory.Exists(AppDataPath + @"WebServerRoot\Media\Audio"))
            Directory.CreateDirectory(AppDataPath + @"WebServerRoot\Media\Audio");
        if (!Directory.Exists(AppDataPath + @"WebServerRoot\Media\Video"))
            Directory.CreateDirectory(AppDataPath + @"WebServerRoot\Media\Video");

        Directory.SetCurrentDirectory(AppPath);

        //reset layout position
        Registry.CurrentUser.DeleteSubKey(@"Software\ispy\startup",false);

    }

    private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        // Check if the target directory exists, if not, create it.
        if (Directory.Exists(target.FullName) == false)
        {
            Directory.CreateDirectory(target.FullName);
        }

        // Copy each file into it’s new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
            //Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
            try {fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);} catch
            {
            }
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir =
                target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }

    private static void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
    {
        try
        {
            if (e.Exception.Message.IndexOf("NoDriver")!=-1)
            {
                //USB audio plugged/ unplugged (typically the cause) - no other way to catch this exception in the volume level control due to limitation in NAudio
            }
            else
            {
                if (MainForm.Conf.Enable_Error_Reporting && _reportedExceptionCount == 0 &&
                    e.Exception != null && e.Exception.Message.Trim() != "")
                {
                    if (_er == null)
                    {
                        _er = new ErrorReporting {UnhandledException = e.Exception};
                        _er.ShowDialog();
                        _er.Dispose();
                        _er = null;
                        _reportedExceptionCount++;
                    }
                }
            }
            MainForm.LogExceptionToFile(e.Exception);
        }
        catch (Exception ex2)
        {
            try
            {
                MainForm.LogExceptionToFile(ex2);
            }
            catch
            {
                
            }
        }
    }
}