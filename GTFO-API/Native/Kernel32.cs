using System.Runtime.InteropServices;

namespace GTFO.API.Native
{
    internal static unsafe class Kernel32
    {
        [DllImport("kernel32.dll")]
        public static extern unsafe void* VirtualAlloc(void* lpAddress, ulong dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        public static extern unsafe bool VirtualProtect(void* lpAddress, ulong dwSize, uint flNewProtect, uint* lpflOldProtect);
    }
}
