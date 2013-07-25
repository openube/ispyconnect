using System;
using System.Collections;
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
                message.To.Add(email); //use built in validator
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
            Colours.Add(colorRGB, c);
            return c;

        }

        public static String ToRGBString(this Color color)
        {
            return color.R + "," + color.G + "," + color.B;
        }

        public static bool Has<T>(this Enum type, T value)
        {
            try
            {
                return (((int)(object)type & (int)(object)value) == (int)(object)value);
            }
            catch
            {
                return false;
            }
        }

        public static bool Is<T>(this Enum type, T value)
        {
            try
            {
                return (int)(object)type == (int)(object)value;
            }
            catch
            {
                return false;
            }
        }


        public static T Add<T>(this Enum type, T value)
        {
            try
            {
                return (T)(object)(((int)(object)type | (int)(object)value));
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    string.Format(
                        "Could not append value from enumerated type '{0}'.",
                        typeof(T).Name
                        ), ex);
            }
        }


        public static T Remove<T>(this Enum type, T value)
        {
            try
            {
                return (T)(object)(((int)(object)type & ~(int)(object)value));
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    string.Format(
                        "Could not remove value from enumerated type '{0}'.",
                        typeof(T).Name
                        ), ex);
            }
        }

        public static void DisposeAll(this IEnumerable set)
        {
            foreach (Object obj in set)
            {
                var disp = obj as IDisposable;
                if (disp != null) { disp.Dispose(); }
            }
        }
    }

}