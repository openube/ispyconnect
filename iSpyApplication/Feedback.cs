using System;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class Feedback : Form
    {
        public Feedback()
        {
            InitializeComponent();
            RenderResources();
        }
        
        private void BtnFeedbackClick(object sender, EventArgs e)
        {
            bool success = false;
            string feedback = txtFeedback.Text.Trim();
            if (feedback == "")
            {
                MessageBox.Show(LocRm.GetString("Feedback_PleaseEnter"), LocRm.GetString("Error"));
                return;
            }

            string fromEmail = txtEmail.Text.Trim();
            if (!IsValidEmail(fromEmail))
            {
                if (
                    MessageBox.Show(LocRm.GetString("Feedback_ValidateEmail"), LocRm.GetString("AreYouSure"),
                                    MessageBoxButtons.YesNo) == DialogResult.No)
                    return;
            }
            var rep = new Reporting.Reporting();
            try
            {
                rep.SendFeedback("iSpy Feedback: " + feedback + "<br/><br/>Version: " + Application.ProductVersion,
                                  fromEmail);
                success = true;
                rep.Dispose();
            }
            catch (Exception ex)
            {
                rep.Dispose();
                MainForm.LogExceptionToFile(ex);
                MessageBox.Show(LocRm.GetString("Feedback_NotSent"), LocRm.GetString("Error"));
            }
            if (success)
            {
                MessageBox.Show(LocRm.GetString("Feedback_Sent"), LocRm.GetString("Note"));
                Close();
            }
        }

        private void BtnCancelClick(object sender, EventArgs e)
        {
            Close();
        }

        private void FeedbackLoad(object sender, EventArgs e)
        {
        }

        private void RenderResources()
        {
            Text = LocRm.GetString("Feedback");
            btnCancel.Text = LocRm.GetString("Cancel");
            btnFeedback.Text = LocRm.GetString("LeaveFeedback");
            label1.Text = LocRm.GetString("leavingFeedbackHelpsUsIde");
            label2.Text = LocRm.GetString("YourEmail");
        }

        private static bool IsValidEmail(string email)
        {
            if (email.IndexOf("@", StringComparison.Ordinal) == -1 || email.IndexOf(".", StringComparison.Ordinal) == -1 || email.Length < 5)
                return false;
            return true;
        }
    }
}