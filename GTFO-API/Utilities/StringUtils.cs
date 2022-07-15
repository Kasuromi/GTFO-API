using System;
using System.Runtime.InteropServices;

namespace GTFO.API.Utilities
{
    public static unsafe class StringUtils
    {
        private static readonly uint[] _lookup32 = new Func<uint[]>(() =>
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("X2");
                if (BitConverter.IsLittleEndian)
                    result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
                else
                    result[i] = ((uint)s[1]) + ((uint)s[0] << 16);
            }
            return result;
        })();
        private static readonly uint* _lookup32Ptr = (uint*)GCHandle.Alloc(_lookup32, GCHandleType.Pinned).AddrOfPinnedObject();

        public static string FromByteArrayAsHex(byte[] bytes)
        {
            var result = new char[bytes.Length * 2];
            fixed (byte* bytesPtr = bytes)
            fixed (char* resultPtr = result)
            {
                uint* resultPtr2 = (uint*)resultPtr;
                for (int i = 0; i < bytes.Length; i++)
                {
                    resultPtr2[i] = _lookup32Ptr[bytesPtr[i]];
                }
            }
            return new string(result);
        }
    }
}
