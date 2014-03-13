using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace iSpyApplication
{
    public static class Helper
    {
        public static double CalculateTrigger(double percent)
        {
            const double minimum = 0.00000001;
            const double maximum = 0.1;
            return minimum + ((maximum - minimum)/100)*Convert.ToDouble(percent);
        }

        public static DateTime Now
        {
            get { return DateTime.UtcNow; }
        }

        public static bool HasFeature(Enums.Features feature)
        {
            return ((1 & MainForm.Conf.FeatureSet) != 0) || (((int)feature & MainForm.Conf.FeatureSet) != 0);
        }
        public static string ZeroPad(int i)
        {
            if (i < 10)
                return "0" + i;
            return i.ToString(CultureInfo.InvariantCulture);
        }

        public static void SetTitle(Form f)
        {
            string ttl = string.Format("iSpy v{0}", Application.ProductVersion);
            if (Program.Platform != "x86")
                ttl = string.Format("iSpy 64 v{0}", Application.ProductVersion);

            if (MainForm.Conf.WSUsername != "")
            {
                ttl += string.Format(" ({0})", MainForm.Conf.WSUsername);
            }

            if (!String.IsNullOrEmpty(MainForm.Conf.Reseller))
            {
                ttl += string.Format(" Powered by {0}", MainForm.Conf.Reseller.Split('|')[0]);
            }
            else
            {
                if (!String.IsNullOrEmpty(MainForm.Conf.Vendor))
                {
                    ttl += string.Format(" with {0}", MainForm.Conf.Vendor);
                }
            }
            f.Text = ttl;
        }

        public static string GetMotionDataPoints(StringBuilder  motionData)
        {
            var elements = motionData.ToString().Trim(',').Split(',');
            if (elements.Length <= 1200)
                return String.Join(",", elements);
            
            var interval = (elements.Length / 1200d);
            var newdata = new StringBuilder(motionData.Length);
            var iIndex = 0;
            double dMax = 0;
            var tMult = 1;
            double target = 0;

            for(var i=0;i<elements.Length;i++)
            {
                try
                {
                    var dTemp = Convert.ToDouble(elements[i]);
                    if (dTemp > dMax)
                    {
                        dMax = dTemp;
                        iIndex = i;
                    }
                    if (i > target)
                    {
                        newdata.Append(elements[iIndex] + ",");
                        tMult++;
                        target = tMult*interval;
                        dMax = 0;

                    }
                }
                catch (Exception)
                {
                    //extremely long recordings can break
                    break;
                }
            }
            string r = newdata.ToString().Trim(',');
            newdata.Clear();
            newdata = null;
            return r;

        }

        internal static bool ArchiveFile(string filename)
        {

            if (!String.IsNullOrEmpty(MainForm.Conf.Archive) && Directory.Exists(MainForm.Conf.Archive))
            {
                string fn = filename.Substring(filename.LastIndexOf("\\", StringComparison.Ordinal) + 1);
                if (File.Exists(filename))
                {
                    try
                    {
                        if (!File.Exists(MainForm.Conf.Archive + fn))
                            File.Copy(filename, MainForm.Conf.Archive + fn);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MainForm.LogExceptionToFile(ex);
                    }
                }
            }
            return false;

        }

        internal static bool ArchiveAndDelete(string filename)
        {

            if (!String.IsNullOrEmpty(MainForm.Conf.Archive) && Directory.Exists(MainForm.Conf.Archive))
            {
                string fn = filename.Substring(filename.LastIndexOf("\\", StringComparison.Ordinal) + 1);
                if (File.Exists(filename))
                {
                    try
                    {
                        if (!File.Exists(MainForm.Conf.Archive + fn))
                            File.Copy(filename, MainForm.Conf.Archive + fn);
                        File.Delete(filename);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MainForm.LogExceptionToFile(ex);
                    }
                }
            }
            return false;

        }

        internal static string GetMediaDirectory(int ot, int oid)
        {
            int i = 0;
            switch (ot)
            {
                case 1:
                    {
                        var o = MainForm.Microphones.FirstOrDefault(p => p.id == oid);
                        if (o != null)
                            i = o.settings.directoryIndex;
                    }
                    break;
                case 2:
                    {
                        var o = MainForm.Cameras.FirstOrDefault(p => p.id == oid);
                        if (o != null)
                            i = o.settings.directoryIndex;
                    }
                    break;
            }
            var o2 = MainForm.Conf.MediaDirectories.FirstOrDefault(p => p.ID == i);
            if (o2 != null)
                return o2.Entry;
            return MainForm.Conf.MediaDirectories[0].Entry;
        }

        public static string GetDirectory(int objectTypeId, int objectId)
        {
            if (objectTypeId == 1)
            {
                var m = MainForm.Microphones.SingleOrDefault(p => p.id == objectId);
                if (m != null)
                    return m.directory;
                throw new Exception("could not find directory for mic " + objectId);
            }
            var c = MainForm.Cameras.SingleOrDefault(p => p.id == objectId);
            if (c != null)
                return c.directory;
            throw new Exception("could not find directory for cam " + objectId);
        }

        public static void DeleteAllContent(int objectTypeId, int objectid)
        {
            var dir = GetMediaDirectory(objectTypeId, objectid);
            var dirName = GetDirectory(objectTypeId, objectid);
            if (objectTypeId == 1)
            {
                var lFi = new List<FileInfo>();
                var dirinfo = new DirectoryInfo(dir + "audio\\" +
                                              dirName + "\\");

                lFi.AddRange(dirinfo.GetFiles());
                lFi = lFi.FindAll(f => f.Extension.ToLower() == ".mp3");

                foreach (FileInfo fi in lFi)
                {
                    try
                    {
                        FileOperations.Delete(fi.FullName);
                    }
                    catch(Exception ex)
                    {
                        MainForm.LogExceptionToFile(ex);
                    }
                }

            }
            if (objectTypeId == 2)
            {
                var lFi = new List<FileInfo>();
                var dirinfo = new DirectoryInfo(dir + "video\\" +
                                              dirName + "\\");

                lFi.AddRange(dirinfo.GetFiles());
                lFi = lFi.FindAll(f => f.Extension.ToLower() == ".mp4" || f.Extension.ToLower() == ".avi");

                foreach (FileInfo fi in lFi)
                {
                    try
                    {
                        FileOperations.Delete(fi.FullName);
                    }
                    catch(Exception ex)
                    {
                        MainForm.LogExceptionToFile(ex);
                    }
                }
                System.Array.ForEach(Directory.GetFiles(dir + "video\\" +
                                              dirName + "\\thumbs\\"), delegate(string path)
                                              {
                                                  try
                                                  {
                                                      FileOperations.Delete(path);
                                                  }
                                                  catch
                                                  {
                                                  }
                                              });

            }

        }
        // returns the number of milliseconds since Jan 1, 1970 (useful for converting C# dates to JS dates)
        public static double UnixTicks(this DateTime dt)
        {
            var d1 = new DateTime(1970, 1, 1);
            var d2 = dt.ToUniversalTime();
            var ts = new TimeSpan(d2.Ticks - d1.Ticks);
            return ts.TotalMilliseconds;
        }

        public static double UnixTicks(this long ticks)
        {
            var d1 = new DateTime(1970, 1, 1);
            var d2 = new DateTime(ticks);
            var ts = new TimeSpan(d2.Ticks - d1.Ticks);
            return ts.TotalMilliseconds;
        }
    }
}