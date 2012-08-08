using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Reflection;
using System.IO;
using System.Security.AccessControl;
using Microsoft.Win32;

namespace OffLine.Installer
{
    // Taken from:http://msdn2.microsoft.com/en-us/library/system.configuration.configurationmanager.aspx
    // Set 'RunInstaller' attribute to true.

    [RunInstaller(true)]
    public class InstallerClass : System.Configuration.Install.Installer
    {
        public InstallerClass()
            : base()
        {
            // Attach the 'Committed' event.
            this.Committed += new InstallEventHandler(MyInstaller_Committed);
            // Attach the 'Committing' event.
            this.Committing += new InstallEventHandler(MyInstaller_Committing);
        }

        // Event handler for 'Committing' event.
        private void MyInstaller_Committing(object sender, InstallEventArgs e)
        {
       
        }

        // Event handler for 'Committed' event.
        private void MyInstaller_Committed(object sender, InstallEventArgs e)
        {
            
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
