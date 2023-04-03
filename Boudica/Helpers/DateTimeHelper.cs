using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Helpers
{
    public static class DateTimeHelper
    {
        public static bool IsDateDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c != '/' && (c < '0' || c > '9'))
                    return false;
            }

            return true;
        }
        public static bool IsTimeDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c != ':' && (c < '0' || c > '9'))
                    return false;
            }

            return true;
        }
    }
}
