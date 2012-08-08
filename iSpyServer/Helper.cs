using System;

namespace iSpyServer
{
    public static class Helper
    {
        public static double CalculateSensitivity(double percent)
        {
            const double minimum = 0.00000001;
            const double maximum = 0.1;
            return minimum + ((maximum - minimum)/100)*Convert.ToDouble(100 - percent);
        }

        public static string ZeroPad(int i)
        {
            if (i < 10)
                return "0" + i;
            return i.ToString();
        }
    }
}