using System;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class ErrorReporting : Form
    {
        public Exception UnhandledException;

        public ErrorReporting()
        {
            InitializeComponent();
            RenderResources();
        }

        private void BtnFeedbackClick(object sender, EventArgs e)
        {
            string humanDescription = txtHumanDescription.Text.Trim();
            var rep = new Reporting.Reporting();
            try
            {
                string g = Guid.NewGuid().ToString();

                rep.LogApplicationException(11,
                                             "iSpy Version: " + Application.ProductVersion + "<br/><br/>" +
                                             UnhandledException.Message + "<br/><br/>User Notes:<br/>" +
                                             humanDescription, UnhandledException.Source, UnhandledException.HelpLink,
                                             UnhandledException.StackTrace, g);
            }
            catch (Exception)
            {
                rep.Dispose();
                //MainForm.LogExceptionToFile(ex);
                MessageBox.Show(LocRm.GetString("SendErrorReportError"), LocRm.GetString("Error"));
                return;
            }
            rep.Dispose();
            MainForm.Conf.Enable_Error_Reporting = chkErrorReporting.Checked;
            Close();
        }

        private void BtnCancelClick(object sender, EventArgs e)
        {
            MainForm.Conf.Enable_Error_Reporting = chkErrorReporting.Checked;
            Close();
        }

        private void FeedbackLoad(object sender, EventArgs e)
        {
            chkErrorReporting.Checked = MainForm.Conf.Enable_Error_Reporting;
            txtErrorReport.Text = UnhandledException.Message + Environment.NewLine + Environment.NewLine +
                                  UnhandledException.StackTrace;
            txtHumanDescription.Text = MainForm.EmailAddress;
        }

        private void RenderResources()
        {
            Text = LocRm.GetString("ErrorReporting");
            btnCancel.Text = LocRm.GetString("Cancel");
            btnFeedback.Text = LocRm.GetString("SendErrorReport");
            chkErrorReporting.Text = LocRm.GetString("EnableErrorReporting");
            label1.Text = LocRm.GetString("ispyHasEncounteredAnUnhan");
            label2.Text = LocRm.GetString("PleaseEnterStepsToReprodu");
            label3.Text = LocRm.GetString("IncludeYourEmailAddressIf");
        }
    }
}