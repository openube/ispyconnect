using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Reflection;

namespace OffLine.Installer
{

    [RunInstaller(true)]
    public class InstallerClass : System.Configuration.Install.Installer
    {
        public InstallerClass()
        {
            // Attach the 'Committed' event.
            Committed += MyInstallerCommitted;
            // Attach the 'Committing' event.
            Committing += MyInstallerCommitting;
        }



        // Event handler for 'Committing' event.
        private void MyInstallerCommitting(object sender, InstallEventArgs e)
        {
            //delete existing dlls
            //Console.WriteLine("");
            //Console.WriteLine("Committing Event occured.");
            //Console.WriteLine("");

            //add registry entries for handling URLs
            //RegistryKey rkApp = Registry.ClassesRoot.CreateSubKey("ispy");
            //rkApp.SetValue("URL Protocol", "");
            //rkApp.SetValue("","URL:ispy Protocol");

            //RegistryKey rkAppIcon = rkApp.CreateSubKey("DefaultIcon");
            //rkAppIcon.SetValue("", "\""+Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\iSpy.exe,1\"");

            //RegistryKey rkShell = rkApp.CreateSubKey("shell");
            //rkShell = rkShell.CreateSubKey("open");
            //rkShell = rkShell.CreateSubKey("command");
            //rkShell.SetValue("", "\""+Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\iSpy.exe\" \"%1\"");

            //rkShell.Close();
            //rkAppIcon.Close();
            //rkApp.Close();            
        }

        // Event handler for 'Committed' event.
        private void MyInstallerCommitted(object sender, InstallEventArgs e)
        {
            //string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //Process.Start(appPath + @"\iSpy.exe","-firstrun");

            //copy across xml files
            string appDataPath = "";
            try
            {
                string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\";
                appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\iSpy\";

                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }
                if (!Directory.Exists(appDataPath + @"XML"))
                {
                    Directory.CreateDirectory(appDataPath + @"XML");
                }

                var didest = new DirectoryInfo(appDataPath + @"XML\");
                var disource = new DirectoryInfo(appPath + @"XML\");

                TryCopy(disource + @"PTZ2.xml", didest + @"PTZ2.xml", false); //may have been customised
                TryCopy(disource + @"Translations.xml", didest + @"Translations.xml", true);
                TryCopy(disource + @"Sources.xml", didest + @"Sources.xml", true);

                if (!File.Exists(didest + @"objects.xml"))
                {
                    TryCopy(disource + @"objects.xml", didest + @"objects.xml", true);
                }

                if (!File.Exists(didest + @"config.xml"))
                {
                    TryCopy(disource + @"config.xml", didest + @"config.xml", true);
                }

                if (!Directory.Exists(appDataPath + @"WebServerRoot"))
                {
                    Directory.CreateDirectory(appDataPath + @"WebServerRoot");
                }
                didest = new DirectoryInfo(appDataPath + @"WebServerRoot");
                disource = new DirectoryInfo(appPath + @"WebServerRoot");
                CopyAll(disource, didest);
            }
            catch
            {
                //let it install anyway, it'll rebuild on start
            }

            if (appDataPath != "")
            {
                string path = Context.Parameters["SourceDir"];

                path = path.Trim().Trim('\\') + @"\";
                try
                {
                    if (File.Exists(path + "custom.txt"))
                    {
                        TryCopy(path + @"custom.txt", appDataPath + @"custom.txt", true);
                        TryCopy(path + @"logo.jpg", appDataPath + @"logo.jpg", true);
                        TryCopy(path + @"logo.png", appDataPath + @"logo.png", true);
                    }

                }
                catch
                {

                }
            }
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
                try { fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true); }
                catch
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

        private static void TryCopy(string source, string target, bool overwrite)
        {
            try
            {
                File.Copy(source,target, overwrite);
            }
            catch
            {
                
            }
        }

        protected override void OnBeforeInstall(IDictionary savedState)
        {
            //MessageBox.Show("Deleting...");
            //string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //foreach (var f in Directory.GetFiles(appPath, "*.dll"))
            //{
            //    try
            //    {
            //        File.Delete(f);
            //    }
            //    catch { }
            //} 
            
            base.OnBeforeInstall(savedState);
        }

        // Override the 'Install' method.
        public override void Install(IDictionary savedState)
        {
           
            base.Install(savedState);
        }

        // Override the 'Commit' method.
        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);
        }

        // Override the 'Rollback' method.
        public override void Rollback(IDictionary savedState)
        {
            //try
            //{
            //    Registry.ClassesRoot.DeleteSubKey("ispy");
            //}
            //catch { }
            base.Rollback(savedState);
        }

        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);
        }
    }
}