using System;
using System.Windows.Forms;

namespace iSpyServer
{
    public partial class ErrorReporting : Form
    {
        public Exception UnhandledException;
        public ErrorReporting()
        {
            InitializeComponent();
            RenderResources();
        }
        private void btnFeedback_Click(object sender, EventArgs e)
        {
            string _HumanDescription = txtHumanDescription.Text.ToString().Trim();
            Reporting.Reporting _rep = new global::iSpyServer.Reporting.Reporting();
            try
            {
                string g = Guid.NewGuid().ToString();
                
                _rep.LogApplicationException(11, "iSpy Version: " + Application.ProductVersion + "<br/><br/>" + UnhandledException.Message + "<br/><br/>User Notes:<br/>" + _HumanDescription, UnhandledException.Source, UnhandledException.HelpLink, UnhandledException.StackTrace, g);               
                
            }
            catch (Exception)
            {
                _rep.Dispose();
                //MainForm.LogExceptionToFile(ex);
                MessageBox.Show(LocRM.GetString("SendErrorReportError"), LocRM.GetString("Error"));
                return;
            }
            _rep.Dispose();
            iSpyServer.Default.Enable_Error_Reporting = chkErrorReporting.Checked;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            iSpyServer.Default.Enable_Error_Reporting = chkErrorReporting.Checked;
            Close();
        }

        private void Feedback_Load(object sender, EventArgs e)
        {
            chkErrorReporting.Checked = iSpyServer.Default.Enable_Error_Reporting;
            txtErrorReport.Text = UnhandledException.Message.ToString() + Environment.NewLine + Environment.NewLine + UnhandledException.StackTrace.ToString();
            txtHumanDescription.Text = MainForm.EmailAddress;
        }

        private void RenderResources() {
            
            this.Text = LocRM.GetString("ErrorReporting");
            btnCancel.Text = LocRM.GetString("Cancel");
            btnFeedback.Text = LocRM.GetString("SendErrorReport");
            chkErrorReporting.Text = LocRM.GetString("EnableErrorReporting");
            label1.Text = LocRM.GetString("ispyHasEncounteredAnUnhan");
            label2.Text = LocRM.GetString("PleaseEnterStepsToReprodu");
            label3.Text = LocRM.GetString("IncludeYourEmailAddressIf");
        }

    }
}