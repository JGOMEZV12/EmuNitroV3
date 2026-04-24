using System;

namespace Polar.Utilities
{
    public class HabboEncoding
    {
        public static int DecodeInt32(byte[] v)
        {
            if (v.Length < 4)
            {
                return 0;
            }
            return (v[0] << 24) | (v[1] << 16) | (v[2] << 8) | v[3];
        }

        public static int DecodeInt16(byte[] v)
        {
            if (v.Length < 2)
            {
                return 0;
            }
            return (v[0] << 8) | v[1];
        }
    }
}
