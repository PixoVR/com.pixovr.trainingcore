using System;
using System.Text;

namespace PixoVR.TrainingCore.Editor.Migration
{
    /// <summary>
    /// Unity's fileID for a class inside a precompiled DLL: the first 4 bytes (little-endian int32)
    /// of MD4("s\0\0\0" + namespace + className). Implemented in managed code (no MD4 in .NET).
    /// </summary>
    public static class FileIDUtil
    {
        /// <summary>Compute the DLL-type fileID for a namespaced class name.</summary>
        public static int ComputeFileID(string ns, string className)
        {
            var input = "s\0\0\0" + (ns ?? "") + className;
            var hash = Md4(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToInt32(hash, 0);
        }

        /// <summary>MD4 hash (RFC 1320).</summary>
        public static byte[] Md4(byte[] data)
        {
            uint a = 0x67452301, b = 0xefcdab89, c = 0x98badcfe, d = 0x10325476;

            long bitLen = data.Length * 8L;
            int padLen = (56 - (data.Length + 1) % 64 + 64) % 64 + 1;
            var msg = new byte[data.Length + padLen + 8];
            Array.Copy(data, msg, data.Length);
            msg[data.Length] = 0x80;
            for (int i = 0; i < 8; i++)
                msg[msg.Length - 8 + i] = (byte)(bitLen >> (8 * i));

            for (int off = 0; off < msg.Length; off += 64)
            {
                uint A0 = a, B0 = b, C0 = c, D0 = d;
                var x = new uint[16];
                for (int i = 0; i < 16; i++)
                    x[i] = BitConverter.ToUInt32(msg, off + i * 4);

                // round 1
                for (int i = 0; i < 16; i++)
                {
                    int k = i;
                    int s = new[] { 3, 7, 11, 19 }[i % 4];
                    switch (i % 4)
                    {
                        case 0: a = Rol(a + F(b, c, d) + x[k], s); break;
                        case 1: d = Rol(d + F(a, b, c) + x[k], s); break;
                        case 2: c = Rol(c + F(d, a, b) + x[k], s); break;
                        default: b = Rol(b + F(c, d, a) + x[k], s); break;
                    }
                }
                // round 2
                var r2 = new[] { 0, 4, 8, 12, 1, 5, 9, 13, 2, 6, 10, 14, 3, 7, 11, 15 };
                for (int i = 0; i < 16; i++)
                {
                    int k = r2[i];
                    int s = new[] { 3, 5, 9, 13 }[i % 4];
                    switch (i % 4)
                    {
                        case 0: a = Rol(a + G(b, c, d) + x[k] + 0x5a827999u, s); break;
                        case 1: d = Rol(d + G(a, b, c) + x[k] + 0x5a827999u, s); break;
                        case 2: c = Rol(c + G(d, a, b) + x[k] + 0x5a827999u, s); break;
                        default: b = Rol(b + G(c, d, a) + x[k] + 0x5a827999u, s); break;
                    }
                }
                // round 3
                var r3 = new[] { 0, 8, 4, 12, 2, 10, 6, 14, 1, 9, 5, 13, 3, 11, 7, 15 };
                for (int i = 0; i < 16; i++)
                {
                    int k = r3[i];
                    int s = new[] { 3, 9, 11, 15 }[i % 4];
                    switch (i % 4)
                    {
                        case 0: a = Rol(a + H(b, c, d) + x[k] + 0x6ed9eba1u, s); break;
                        case 1: d = Rol(d + H(a, b, c) + x[k] + 0x6ed9eba1u, s); break;
                        case 2: c = Rol(c + H(d, a, b) + x[k] + 0x6ed9eba1u, s); break;
                        default: b = Rol(b + H(c, d, a) + x[k] + 0x6ed9eba1u, s); break;
                    }
                }

                a += A0; b += B0; c += C0; d += D0;
            }

            var result = new byte[16];
            Array.Copy(BitConverter.GetBytes(a), 0, result, 0, 4);
            Array.Copy(BitConverter.GetBytes(b), 0, result, 4, 4);
            Array.Copy(BitConverter.GetBytes(c), 0, result, 8, 4);
            Array.Copy(BitConverter.GetBytes(d), 0, result, 12, 4);
            return result;
        }

        private static uint Rol(uint v, int s) => (v << s) | (v >> (32 - s));
        private static uint F(uint x, uint y, uint z) => (x & y) | (~x & z);
        private static uint G(uint x, uint y, uint z) => (x & y) | (x & z) | (y & z);
        private static uint H(uint x, uint y, uint z) => x ^ y ^ z;
    }
}
