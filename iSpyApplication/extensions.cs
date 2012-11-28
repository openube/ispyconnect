using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using System.Drawing;

namespace iSpyApplication
{
    public static class Extensions
    {
        private static readonly Dictionary<string, Color> Colours = new Dictionary<string, Color>();
        
        public static bool IsValidEmail(this string email)
        {
            var message = new MailMessage();
            bool f = false;
            try
            {
                message.To.Add(email);//use built in validator
            }
            catch
            {
                f = true;
            }
            message.Dispose();
            return !f;
        }
        

        public static Color ToColor(this string colorRGB)
        {
            if (Colours.ContainsKey(colorRGB))
                return Colours[colorRGB];
            
            string[] cols = colorRGB.Split(',');
            var c = Color.FromArgb(Convert.ToInt16(cols[0]), Convert.ToInt16(cols[1]), Convert.ToInt16(cols[2]));
            Colours.Add(colorRGB,c);
            return c;

        }

        public static String ToRGBString(this Color color)
        {
            return color.R + "," + color.G + "," + color.B;
        }
    }
}