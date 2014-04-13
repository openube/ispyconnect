using System;
using System.Windows.Forms;

namespace iSpyApplication.Controls
{
    public partial class FTPConfig : Form
    {
        public configurationServer FTP;
        public FTPConfig()
        {
            InitializeComponent();
            RenderResources();
        }

        private void RenderResources()
        {
            Text = LocRm.GetString("FTPEditor");
            LocRm.SetString(label64,"Name");
            LocRm.SetString(label62, "Server");
            LocRm.SetString(label66, "Port");
            LocRm.SetString(label63, "Username");
            LocRm.SetString(label65, "Password");
            LocRm.SetString(chkUsePassive, "PassiveMode");
            LocRm.SetString(chkFTPRename, "UploadTempFileRename");
            LocRm.SetString(btnSaveFTP, "OK");
        }
        private void FTPEditor_Load(object sender, EventArgs e)
        {
            txtServerName.Text = FTP.name;
            txtFTPServer.Text = FTP.server;
            txtFTPUsername.Text = FTP.username;
            txtFTPPassword.Text = FTP.password;
            txtFTPPort.Value = FTP.port;
            chkFTPRename.Checked = FTP.rename;
            chkUsePassive.Checked = FTP.usepassive;
            if (String.IsNullOrEmpty(FTP.ident))
                FTP.ident = Guid.NewGuid().ToString();
        }

        private void btnSaveFTP_Click(object sender, EventArgs e)
        {
            FTP.name = txtServerName.Text;
            FTP.server = txtFTPServer.Text;
            FTP.username = txtFTPUsername.Text;
            FTP.password = txtFTPPassword.Text;
            FTP.port = (int)txtFTPPort.Value;
            FTP.rename = chkFTPRename.Checked;
            FTP.usepassive = chkUsePassive.Checked;
            DialogResult = DialogResult.OK;
            Close();

        }
    }
}
