using System;
using System.Windows.Forms;
using iSpyServer.iSpyWS;

namespace iSpyServer
{
    public partial class Feedback : Form
    {
        
        public Feedback()
        {
            InitializeComponent();
            RenderResources();
        }

        private void btnFeedback_Click(object sender, EventArgs e)
        {
            bool success = false;
            string _Feedback = txtFeedback.Text.ToString().Trim();
            if (_Feedback == "")
            {
                MessageBox.Show(LocRM.GetString("Feedback_PleaseEnter"), LocRM.GetString("Error"));
                return;
            }

            string FromEmail = txtEmail.Text.Trim();
            if (!IsValidEmail(FromEmail))
            {
                if (MessageBox.Show(LocRM.GetString("Feedback_ValidateEmail"), LocRM.GetString("AreYouSure"), MessageBoxButtons.YesNo) == DialogResult.No)
                    return;
            }
            Reporting.Reporting _rep = new global::iSpyServer.Reporting.Reporting();
            try
            {
                
                _rep.SendFeedback("iSpyServer Feedback: " + _Feedback + "<br/><br/>Version: " + Application.ProductVersion, FromEmail);
                success = true;
                _rep.Dispose();
            }
            catch (Exception ex)
            {
                _rep.Dispose();
                MainForm.LogExceptionToFile(ex);
                MessageBox.Show(LocRM.GetString("Feedback_NotSent"), LocRM.GetString("Error"));
            }
            if (success)
            {
                MessageBox.Show(LocRM.GetString("Feedback_Sent"), LocRM.GetString("Note"));
                Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Feedback_Load(object sender, EventArgs e)
        {
            
        }

        private void RenderResources() {
            this.Text = LocRM.GetString("Feedback");
            btnCancel.Text = LocRM.GetString("Cancel");
            btnFeedback.Text = LocRM.GetString("LeaveFeedback");
            label1.Text = LocRM.GetString("leavingFeedbackHelpsUsIde");
            label2.Text = LocRM.GetString("YourEmail");
        }

        bool IsValidEmail(string Email)
        {
            if (Email.IndexOf("@") == -1 || Email.IndexOf(".") == -1 || Email.Length < 5)
                return false;
            return true;
        }
    }
}