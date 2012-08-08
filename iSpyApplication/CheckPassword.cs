using System;
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
            string p = MainForm.Conf.Password_Protect_Password;
            if (txtPassword.Text == p)
            {
                DialogResult = DialogResult.OK;
            }
            else
            {
                DialogResult = DialogResult.Cancel;
                MessageBox.Show(LocRm.GetString("PasswordIncorrect"), LocRm.GetString("Note"));
            }
            Close();
        }

        private void CheckPasswordLoad(object sender, EventArgs e)
        {
            
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
    }
}