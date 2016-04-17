using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.extensions
{
    public static class ByteArrayHelper
    {
        public static string ToHex(this byte[] bytes, bool upperCase)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);

            foreach (byte b in bytes)
                result.Append(b.ToString(upperCase ? "X2" : "x2"));

            return result.ToString();
        }
    }
}
