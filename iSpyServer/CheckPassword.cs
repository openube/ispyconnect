using System;
using System.Windows.Forms;

namespace iSpyServer
{
    public partial class CheckPassword : Form
    {
        public CheckPassword()
        {
            InitializeComponent();
            RenderResources();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DoCheckPassword();
        }

        private void DoCheckPassword()
        {
            string _p = iSpyServer.Default.Password_Protect_Password;
            if (txtPassword.Text == _p)
            {
                DialogResult = System.Windows.Forms.DialogResult.OK;
            }
            else
            {
                DialogResult = System.Windows.Forms.DialogResult.Cancel;
                MessageBox.Show(LocRM.GetString("PasswordIncorrect"), LocRM.GetString("Note"));
            }
            Close();
        }

        private void CheckPassword_Load(object sender, EventArgs e)
        {
            
        }

        private void RenderResources() {
            
            this.Text = LocRM.GetString("ApplicationHasBeenLocked");
            button1.Text = LocRM.GetString("Unlock");
            label1.Text = LocRM.GetString("UnlockPassword");
        }


        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DoCheckPassword();
            }
        }
    }
}