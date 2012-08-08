using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Collections;
using iSpyServer;
using System.IO;
using iSpyServer.Reporting;
using System.Resources;

static class Program
{
    public static Mutex mutex;
    public static string AppPath = "";
    public static string AppDataPath = "";
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);


            bool firstInstance;
            mutex = new Mutex(false, "iSpyServer", out firstInstance);
            AppPath = (Application.StartupPath.ToLower());
            AppPath = AppPath.Replace(@"\bin\debug", @"\").Replace(@"\bin\release", @"\");
            AppPath = AppPath.Replace(@"\bin\x86\debug", @"\").Replace(@"\bin\x86\release", @"\");

            AppPath = Program.AppPath.Replace(@"\\", @"\");
            if (!AppPath.EndsWith(@"\"))
                AppPath += @"\";

            AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\iSpyServer\";
            if (!Directory.Exists(AppDataPath))
            {
                Directory.CreateDirectory(AppDataPath);
            }

            bool _silentstartup = false;

            string _command = "";
            if (args.Length > 0)
            {
                if (args[0].ToLower().Trim() == "-silent" || args[0].ToLower().Trim('\\') == "s")
                {
                    _silentstartup = true;
                }
                else
                {
                    foreach (string _s in args)
                    {
                        _command += _s + " ";
                    }
                }
            }

            if (!firstInstance)
            {
                if (_command != "")
                {
                    File.WriteAllText(AppPath + "external_command.txt", _command);
                    //ensures pickup by filesystemwatcher
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    MessageBox.Show(LocRM.GetString("iSpyRunning"), LocRM.GetString("Note"),MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Application.Exit();
                return;
            }

            var w = Process.GetProcessesByName("ispymonitor");
            if (w.Length == 0)
            {
                try
                {
                    var si = new ProcessStartInfo(AppPath + "/ispymonitor.exe", "ispyserver");
                    Process.Start(si);
                }
                catch
                { }
            }

            File.WriteAllText(AppPath + "external_command.txt", "");
            if (iSpyServer.iSpyServer.Default.Enable_Password_Protect)
                _silentstartup = true;

            if (_command.StartsWith("open"))
                _command = _command.Substring(5);
            MainForm _mf = new MainForm(_silentstartup, _command);
            Application.Run(_mf);
            
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(ex.Message, "Program Error:");
            while (ex.InnerException != null)
            {
                MessageBox.Show(ex.InnerException.Message, LocRM.GetString("Error"));
                ex = ex.InnerException;

            }
        }
    }

    private static int _ReportedExceptionCount = 0;
    private static ErrorReporting _ER = null;
    static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
        try
        {
            if (e.Exception.Message == "NoDriver calling waveInPrepareHeader")
            {
                //USB audio unplugged (typically the cause) - no other way to catch this exception in the volume level control due to limitation in NAudio
            }
            else
            {
                if (iSpyServer.iSpyServer.Default.Enable_Error_Reporting && _ReportedExceptionCount == 0 && e.Exception != null && e.Exception.Message != null && e.Exception.Message.ToString().Trim() != "")
                {
                    if (_ER == null)
                    {
                        _ER = new ErrorReporting();
                        _ER.UnhandledException = e.Exception;
                        _ER.ShowDialog();
                        _ER.Dispose();
                        _ER = null;
                        _ReportedExceptionCount++;
                    }

                }
            }
            MainForm.LogExceptionToFile(e.Exception);
        }
        catch (Exception ex2)
        {
            MainForm.LogExceptionToFile(ex2);
        }
    }

}