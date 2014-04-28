using System;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using iSpyApplication.Properties;

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
            LocRm.SetString(btnTest, "Test");
        }
        private void FTPEditor_Load(object sender, EventArgs e)
        {
            txtServerName.Text = FTP.name;
            txtFTPServer.Text = FTP.server;
            if (String.IsNullOrEmpty(txtFTPServer.Text))
                txtFTPServer.Text = "ftp://";
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
            if (txtFTPServer.Text.IndexOf("/", StringComparison.Ordinal) == -1)
            {
                txtFTPServer.Text = "ftp://" + txtFTPServer.Text;
            }

            FTP.username = txtFTPUsername.Text;
            FTP.password = txtFTPPassword.Text;
            FTP.port = (int)txtFTPPort.Value;
            FTP.rename = chkFTPRename.Checked;
            FTP.usepassive = chkUsePassive.Checked;
            DialogResult = DialogResult.OK;
            Close();

        }

        string _testloc = "test.jpg";

        private void btnTest_Click(object sender, EventArgs e)
        {
            var p = new Prompt(LocRm.GetString("UploadTo"), _testloc);
            p.ShowDialog(this);
            if (p.Val != "")
            {
                _testloc = p.Val;
                using (var imageStream = new MemoryStream())
                {
                    try
                    {
                        Resources.cam_offline.Save(imageStream, ImageFormat.Jpeg);

                        string error;
                        txtFTPServer.Text = txtFTPServer.Text.Trim('/');
                        if (txtFTPServer.Text.IndexOf("/", StringComparison.Ordinal) == -1)
                        {
                            txtFTPServer.Text = "ftp://" + txtFTPServer.Text;
                        }
                        string fn = String.Format(CultureInfo.InvariantCulture, _testloc, Helper.Now);
                        if ((new AsynchronousFtpUpLoader()).FTP(txtFTPServer.Text + ":" + txtFTPPort.Text,
                                                                chkUsePassive.Checked,
                                                                txtFTPUsername.Text, txtFTPPassword.Text, fn, 0,
                                                                imageStream.ToArray(), out error, chkFTPRename.Checked))
                        {
                            MessageBox.Show(LocRm.GetString("ImageUploaded"), LocRm.GetString("Success"));
                        }
                        else
                            MessageBox.Show(string.Format("{0}: {1}", LocRm.GetString("UploadFailed"), error), LocRm.GetString("Failed"));
                    }
                    catch (Exception ex)
                    {
                        MainForm.LogExceptionToFile(ex);
                        MessageBox.Show(ex.Message);
                    }
                    imageStream.Close();
                }
            }
        }
    }
}
