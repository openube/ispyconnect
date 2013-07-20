using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

        public static void DeleteAllContent(int objectTypeId, string directoryName)
        {
            if (objectTypeId == 1)
            {
                var lFi = new List<FileInfo>();
                var dirinfo = new DirectoryInfo(MainForm.Conf.MediaDirectory + "audio\\" +
                                              directoryName + "\\");

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
                var dirinfo = new DirectoryInfo(MainForm.Conf.MediaDirectory + "video\\" +
                                              directoryName + "\\");

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
                System.Array.ForEach(Directory.GetFiles(MainForm.Conf.MediaDirectory + "video\\" +
                                              directoryName + "\\thumbs\\"), delegate(string path)
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