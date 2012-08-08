using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace iSpyServer
{
    public static class extensions
    {

        public static string ReplaceString(this string str, string oldValue, string newValue)
        {
            StringBuilder sb = new StringBuilder();

            int previousIndex = 0;
            int index = str.IndexOf(oldValue, StringComparison.CurrentCultureIgnoreCase);
            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, StringComparison.CurrentCultureIgnoreCase);
            }
            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
        }

        public static bool IsValidEmail(this string Email)
        {
            string strRegex = @"^([a-zA-Z0-9_\-\.\+]+)@((\[[0-9]{1,3}" +
                  @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                  @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
            Regex re = new Regex(strRegex);
            bool _b = re.IsMatch(Email);
            return _b;
        }
    }
}
