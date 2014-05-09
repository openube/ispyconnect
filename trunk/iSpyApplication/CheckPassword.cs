using System;
using System.Linq;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class CheckPassword : Form
    {
        public CheckPassword()
        {
            InitializeComponent();
            RenderResources();
        }

        private void Button1Click(object sender, EventArgs e)
        {
            DoCheckPassword();
        }

        private void DoCheckPassword()
        {
            foreach (var g in MainForm.Conf.Permissions)
            {
                if (txtPassword.Text == EncDec.DecryptData(g.password, MainForm.Conf.EncryptCode))
                {
                    if (MainForm.Group != g.name)
                        MainForm.NeedsResourceUpdate = true;
                    MainForm.Group = g.name;
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }
            }
            
            DialogResult = DialogResult.Cancel;
            MessageBox.Show(LocRm.GetString("PasswordIncorrect"), LocRm.GetString("Note"));
            
            Close();
        }

        private void CheckPasswordLoad(object sender, EventArgs e)
        {
            txtPassword.Focus();
        }

        private void RenderResources() {
            
            Text = LocRm.GetString("ApplicationHasBeenLocked");
            button1.Text = LocRm.GetString("Unlock");
            label1.Text = LocRm.GetString("UnlockPassword");
        }


        private void TxtPasswordKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DoCheckPassword();
            }
        }

        private void CheckPassword_Shown(object sender, EventArgs e)
        {
            this.Activate();
        }
    }
}