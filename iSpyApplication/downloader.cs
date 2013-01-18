using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Xml;

namespace iSpyApplication
{
    public partial class downloader : Form
    {
        public string Url;
        public string SaveLocation;
        public string Format;

        private bool success;
        private bool cancel;

        public downloader()
        {
            InitializeComponent();
        }

        private void downloader_Load(object sender, EventArgs e)
        {
            UISync.Init(this);
            backgroundWorker1.RunWorkerAsync();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string sUrlToReadFileFrom = Url;
            var url = new Uri(sUrlToReadFileFrom);
            var request = WebRequest.Create(url);
            var response = request.GetResponse();
            // gets the size of the file in bytes
            if (response != null)
            {
                Int64 iSize = response.ContentLength;

                // keeps track of the total bytes downloaded so we can update the progress bar
                int iRunningByteTotal = 0;

                // use the webclient object to download the file

                using (Stream streamRemote = response.GetResponseStream())
                {
                    if (streamRemote != null)
                    {
                        streamRemote.ReadTimeout = 8000;
                        // loop the stream and get the file into the byte buffer
                        var byteBuffer = new byte[iSize];
                        int iByteSize;
                        while ((iByteSize = streamRemote.Read(byteBuffer, iRunningByteTotal, byteBuffer.Length - iRunningByteTotal)) > 0 && !backgroundWorker1.CancellationPending)
                        {
                            iRunningByteTotal += iByteSize;

                            // calculate the progress out of a base "100"
                            var dIndex = (double) (iRunningByteTotal);
                            var dTotal = (double) byteBuffer.Length;
                            var dProgressPercentage = (dIndex/dTotal);
                            var iProgressPercentage = (int) (dProgressPercentage*100);

                            // update the progress bar
                            backgroundWorker1.ReportProgress(iProgressPercentage);
                            int total = iRunningByteTotal;
                            UISync.Execute(() => lblProgress.Text = "Downloaded " + total + " of " + iSize);
                        }
                        if (!backgroundWorker1.CancellationPending)
                        {
                            if (SaveLocation.EndsWith(".xml"))
                            {
                                var ms = new MemoryStream(byteBuffer);
                                var doc = new XmlDocument();
                                try
                                {
                                    doc.Load(ms);
                                    doc.Save(SaveLocation);
                                    success = true;

                                }
                                catch (Exception ex)
                                {
                                    MainForm.LogExceptionToFile(ex);
                                    DialogResult = DialogResult.Cancel;
                                    Close();
                                    return;
                                }
                                ms.Dispose();
                            }
                        }
                        else
                        {
                            MainForm.LogMessageToFile("Update cancelled");
                        }
                    }
                    else
                    {
                        MainForm.LogErrorToFile("Response stream from " + Url + " failed");
                    }
                }
                response.Close();
            }
            else
            {
                MainForm.LogErrorToFile("Response from "+Url+" failed");
            }
            
        }

        private class UISync
        {
            private static ISynchronizeInvoke _sync;

            public static void Init(ISynchronizeInvoke sync)
            {
                _sync = sync;
            }

            public static void Execute(Action action)
            {
                try { _sync.BeginInvoke(action, null); }
                catch { }
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pbDownloading.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (success)
            {
                DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show(this, "Update Failed", "See log file for more information");
            }
            Close();
        }

        private void downloader_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (backgroundWorker1.IsBusy)
            {
                if (MessageBox.Show(this, "Cancel update?","Confirm", MessageBoxButtons.YesNo)==DialogResult.Yes)
                {
                    backgroundWorker1.CancelAsync();
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
