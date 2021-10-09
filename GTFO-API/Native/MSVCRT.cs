using System.Runtime.InteropServices;

namespace GTFO.API.Native
{
    internal static class MSVCRT
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void* memcpy(void* destination, void* source, long num);
    }
}
