using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using iSpyApplication.Controls;
using iSpyApplication.Properties;

namespace iSpyApplication
{
    partial class MainForm
    {
        internal void SelectMediaRange(PreviewBox controlFrom, PreviewBox controlTo)
        {
            lock (flowPreview.Controls)
            {
                if (controlFrom != null && controlTo != null)
                {
                    if (flowPreview.Controls.Contains(controlFrom) && flowPreview.Controls.Contains(controlTo))
                    {
                        bool start = false;
                        foreach (PreviewBox p in flowPreview.Controls)
                        {
                            if (p == controlFrom)
                            {
                                start = true;
                            }
                            if (start)
                                p.Selected = true;
                            if (p == controlTo)
                                break;
                        }
                        start = false;
                        foreach (PreviewBox p in flowPreview.Controls)
                        {
                            if (p == controlTo)
                            {
                                start = true;
                            }
                            if (start)
                                p.Selected = true;
                            if (p == controlFrom)
                                break;
                        }
                    }
                }
                flowPreview.Invalidate(true);
            }
        }

        public void DeleteSelectedMedia()
        {
            lock (flowPreview.Controls)
            {
                for (int i = 0; i < flowPreview.Controls.Count; i++)
                {
                    var pb = (PreviewBox)flowPreview.Controls[i];
                    if (pb.Selected)
                    {
                        RemovePreviewBox(pb);
                        i--;
                    }
                }
            }

        }

        private void RemovePreviewBox(PreviewBox pb)
        {
            string[] parts = pb.FileName.Split('\\');
            string fn = parts[parts.Length - 1];
            string id = fn.Substring(0, fn.IndexOf('_'));

            try
            {
                //movie
                FileOperations.Delete(pb.FileName);
                var cw = GetCameraWindow(Convert.ToInt32(id));
                if (cw!=null)
                {
                    cw.RemoveFile(fn);
                }
                

                //preview
                string dir = pb.FileName.Substring(0, pb.FileName.LastIndexOf("\\", StringComparison.Ordinal));

                var lthumb = dir + @"\thumbs\" + fn.Substring(0, fn.LastIndexOf(".", StringComparison.Ordinal)) + "_large.jpg";
                FileOperations.Delete(lthumb);

                lthumb = dir + @"\thumbs\" + fn.Substring(0, fn.LastIndexOf(".", StringComparison.Ordinal)) + ".jpg";
                FileOperations.Delete(lthumb);
            }
            catch (Exception ex)
            {
                LogExceptionToFile(ex);
            }
            flowPreview.Controls.Remove(pb);
            pb.MouseDown -= PbMouseDown;
            pb.MouseEnter -= PbMouseEnter;
            pb.Dispose();

            NeedsMediaRefresh = DateTime.Now;
           
        }

        public void LoadPreviews()
        {
            if (!flowPreview.Loading)
            {
                NeedsMediaRefresh = DateTime.MinValue;
                UISync.Execute(RenderPreviewBoxes);
                
            }
        }

        private void RenderPreviewBoxes()  {

            lock (flowPreview.Controls)
            {
                if (MediaPanelPage * Conf.PreviewItems > MasterFileList.Count-1)
                {
                    MediaPanelPage = 0;
                }
                int pageCount = (MasterFileList.Count - 1)/Conf.PreviewItems + 1;

                llblPage.Text = String.Format("{0} / {1}", (MediaPanelPage + 1), pageCount);

                var currentList = new List<PreviewBox>();
                var displayList = MasterFileList.OrderByDescending(p => p.CreatedDateTicks).Skip(MediaPanelPage*Conf.PreviewItems).Take(Conf.PreviewItems).ToList();
                for(int i=0;i<flowPreview.Controls.Count;i++)
                {
                    var pb = (PreviewBox) flowPreview.Controls[i];
                    if (displayList.Count(p => p.CreatedDateTicks == pb.CreatedDate.Ticks)==0)
                    {
                        flowPreview.Controls.Remove(pb);
                        pb.MouseDown -= PbMouseDown;
                        pb.MouseEnter -= PbMouseEnter;
                        pb.Dispose();
                        i--;
                    }
                    else
                    {
                        currentList.Add(pb);
                    }
                }
                int ci = 0;
                foreach (FilePreview fp in displayList)
                {
                    var pb = currentList.FirstOrDefault(p => p.CreatedDate.Ticks == fp.CreatedDateTicks);
                    if (pb==null)
                    {
                        FilePreview fp1 = fp;
                        switch (fp1.ObjectTypeId)
                        {
                            case 1:
                                var v = Microphones.SingleOrDefault(p => p.id == fp1.ObjectId);
                                if (v != null)
                                {
                                    var filename = Conf.MediaDirectory + "audio\\" + v.directory + "\\" + fp.Filename;
                                    pb = AddPreviewControl(Resources.audio, filename, fp.Duration, (new DateTime(fp.CreatedDateTicks)),v.name);
                                }
                                break;
                            case 2:
                                var c = Cameras.SingleOrDefault(p => p.id == fp1.ObjectId);
                                if (c != null)
                                {
                                    var filename = Conf.MediaDirectory + "video\\" + c.directory + "\\" + fp.Filename;
                                    var thumb = Conf.MediaDirectory + "video\\" + c.directory + "\\thumbs\\" +
                                                fp.Filename.Substring(0,
                                                                      fp.Filename.LastIndexOf(".", StringComparison.Ordinal)) +
                                                ".jpg";
                                    pb = AddPreviewControl(thumb, filename, fp.Duration, (new DateTime(fp.CreatedDateTicks)),
                                                           c.name);
                                }
                                break;
                        }
                        
                    }
                    if (pb != null)
                    {
                        flowPreview.Controls.SetChildIndex(pb, ci);
                        ci++;
                    }
                }
                
                   
            }
        }

        public void RemovePreviewByFileName(string fn)
        {
            lock (flowPreview.Controls)
            {
                PreviewBox pb = flowPreview.Controls.Cast<PreviewBox>().FirstOrDefault(c => c.FileName.EndsWith(fn));
                if (pb != null)
                {
                    UISync.Execute(() => RemovePreviewBox(pb));
                }
            }
        }

        private void llblBack_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MediaPanelPage--;
            if (MediaPanelPage < 0)
                MediaPanelPage = 0;
            else
            {
                foreach (PreviewBox pb in flowPreview.Controls)
                {
                    pb.MouseDown -= PbMouseDown;
                    pb.MouseEnter -= PbMouseEnter;
                    pb.Dispose();
                }
                flowPreview.Controls.Clear();
                LoadPreviews();
            }

        }

        private void llblNext_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MediaPanelPage++;
            if (MediaPanelPage * Conf.PreviewItems >= MasterFileList.Count)
                MediaPanelPage--;
            else
            {
                foreach (PreviewBox pb in flowPreview.Controls)
                {
                    pb.MouseDown -= PbMouseDown;
                    pb.MouseEnter -= PbMouseEnter;
                    pb.Dispose();
                }
                flowPreview.Controls.Clear();
                LoadPreviews();
            }

        }

        private void llblPage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var p = new Pager();
            int i = MediaPanelPage;
            p.ShowDialog(this);
            if (i != MediaPanelPage)
            {
                foreach (PreviewBox pb in flowPreview.Controls)
                {
                    pb.MouseDown -= PbMouseDown;
                    pb.MouseEnter -= PbMouseEnter;
                    pb.Dispose();
                }
                flowPreview.Controls.Clear();
                LoadPreviews();
            }
        }

        private void flowPreview_MouseLeave(object sender, EventArgs e)
        {
            tsslMediaInfo.Text = "";
        }

    }
}
