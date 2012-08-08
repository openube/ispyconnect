using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

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

        private void DeleteSelectedMedia()
        {
            int d = 0;
            lock (flowPreview.Controls)
            {
                for (int i = 0; i < flowPreview.Controls.Count; i++)
                {
                    var pb = (PreviewBox)flowPreview.Controls[i];
                    if (pb.Selected)
                    {
                        d++;
                        RemovePreviewBox(pb);
                        i--;
                    }
                }
            }
            if (d>0)
                LoadPreviews();
        }

        private void RemovePreviewBox(PreviewBox pb)
        {
            string[] parts = pb.FileName.Split('\\');
            string fn = parts[parts.Length - 1];
            string id = fn.Substring(0, fn.IndexOf('_'));

            try
            {
                //movie
                File.Delete(pb.FileName);
                GetCameraWindow(Convert.ToInt32(id)).FileList.RemoveAll(p => p.Filename == fn);
                MasterFileList.RemoveAll(p => p.Filename == fn);

                //preview
                string dir = pb.FileName.Substring(0, pb.FileName.LastIndexOf("\\"));

                var lthumb = dir + "\\thumbs\\" + fn.Substring(0, fn.LastIndexOf(".")) + "_large.jpg";
                File.Delete(lthumb);

                lthumb = dir + "\\thumbs\\" + fn.Substring(0, fn.LastIndexOf(".")) + ".jpg";
                File.Delete(lthumb);
            }
            catch (Exception ex)
            {
                LogExceptionToFile(ex);
            }
            flowPreview.Controls.Remove(pb);
            pb.Dispose();
        }

        private void ClearPreviewBoxes()
        {
            lock (flowPreview.Controls)
            {
                foreach(PreviewBox c in flowPreview.Controls)
                {
                    c.Dispose();
                }
                flowPreview.Controls.Clear();
            }
        }

        public void LoadPreviews()
        {
            UISync.Execute(ClearPreviewBoxes);
            lock (flowPreview.Controls)
            {
                
                MasterFileList = new List<FilePreview>();
                foreach (Control c in _pnlCameras.Controls)
                {
                    try
                    {
                        if (c is CameraWindow)
                        {
                            var cw = ((CameraWindow)c);
                            List<FilesFile> ffs;
                            lock (cw.FileList)
                            {
                               ffs = cw.FileList.ToList();
                            }
                            foreach (FilesFile ff in ffs)
                            {
                                MasterFileList.Add(new FilePreview(ff.Filename, ff.DurationSeconds, cw.Camobject.name,
                                                                   ff.CreatedDateTicks, 2, cw.Camobject.id));
                            }
                        }
                    }
                    catch
                    {
                        
                    }
                }
                MasterFileList =
                    MasterFileList.OrderByDescending(p => p.CreatedDateTicks).Take(Conf.PreviewItems).ToList();
                foreach (FilePreview fp in MasterFileList)
                {
                    FilePreview fp1 = fp;
                    var filename = Conf.MediaDirectory + "video\\" + Cameras.Single(p => p.id == fp1.ObjectId).directory +
                                   "\\" + fp.Filename;
                    FilePreview fp2 = fp;
                    var thumb = Conf.MediaDirectory + "video\\" + Cameras.Single(p => p.id == fp2.ObjectId).directory +
                                "\\thumbs\\" + fp.Filename.Substring(0, fp.Filename.LastIndexOf(".")) + ".jpg";
                    AddPreviewControl(thumb, filename, fp.Duration, (new DateTime(fp.CreatedDateTicks)), false);
                }
            }
        }

        public void RemovePreviewByFileName(string fn)
        {
            lock (flowPreview.Controls)
            {
                PreviewBox pb = null;
                foreach (PreviewBox c in flowPreview.Controls)
                {
                    if (c.FileName.EndsWith(fn))
                    {
                        pb = c;
                        break;
                    }
                }

                if (pb != null)
                    UISync.Execute(() => RemovePreviewBox(pb));
            }
        }

    }
}
