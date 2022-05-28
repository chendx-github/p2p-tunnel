using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common.libs.extends
{
    public static class NumberExtends
    {
        public static byte[] ToBytes(this long num)
        {
            return BitConverter.GetBytes(num);
        }

        public static byte[] ToBytes(this ulong num)
        {
            return BitConverter.GetBytes(num);
        }

        public static byte[] ToBytes(this int num)
        {
            return BitConverter.GetBytes(num);
        }

        public static byte[] ToBytes(this short num)
        {
            return BitConverter.GetBytes(num);
        }

        public static short ToInt16(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToInt16(bytes, startindex);
        }
        public static int ToInt32(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToInt32(bytes, startindex);
        }
        public static long ToInt64(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToInt64(bytes, startindex);
        }
        public static ulong ToUInt64(this byte[] bytes, int startindex = 0)
        {
            return BitConverter.ToUInt64(bytes, startindex);
        }
    }
}
