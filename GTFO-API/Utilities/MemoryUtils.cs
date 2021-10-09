using System;
using GTFO.API.Native;
using UnhollowerBaseLib;

namespace GTFO.API.Utilities
{
    internal static class MemoryUtils
    {
        private static byte[] _trampolineShellcode = new byte[] {
            0x48, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,         // movabs   rax,  0x00
            0xFF, 0xD0                                                          // call     rax
        };

        public static unsafe void* GetIl2CppMethod<T>(string methodName, string returnTypeName, params string[] argTypes) where T : Il2CppObjectBase
        {
            return *(void**)IL2CPP.GetIl2CppMethod(Il2CppClassPointerStore<T>.NativeClassPtr, false, methodName, returnTypeName, argTypes).ToPointer();
        }

        public static unsafe void* FindSignatureInBlock(void* block, ulong blockSize, string pattern, string mask, ulong sigOffset = 0)
            => FindSignatureInBlock(block, blockSize, pattern.ToCharArray(), mask.ToCharArray(), sigOffset);

        public static unsafe void* FindSignatureInBlock(void* block, ulong blockSize, char[] pattern, char[] mask, ulong sigOffset = 0)
        {
            for (ulong address = 0; address < blockSize; address++)
            {
                bool found = true;
                for (uint offset = 0; offset < mask.Length; offset++)
                {
                    if (((*(byte*)(address + (ulong)block + offset)) != (byte)pattern[offset]) && mask[offset] != '?')
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                    return (void*)(address + (ulong)block + sigOffset);
            }
            return (void*)0;
        }

        public static unsafe byte[] MakeTrampoline(void* destination)
        {
            byte[] trampolineShellcode = new byte[_trampolineShellcode.Length];
            Array.Copy(_trampolineShellcode, 0, trampolineShellcode, 0, _trampolineShellcode.Length);
            fixed (byte* pTrampolineShellcode = trampolineShellcode)
            {
                *(ulong*)(pTrampolineShellcode + 2) = (ulong)destination;
            }
            return trampolineShellcode;
        }

        public static unsafe void CreateTrampolineBetween(void* start, void* end, void* destination)
        {
            ulong trampolineBlockSize = (ulong)end - (ulong)start;
            if (trampolineBlockSize < (ulong)_trampolineShellcode.Length)
            {
                throw new Exception($"Trampoline block size is not enough to create.");
            }

            uint oldProtect;
            if (!Kernel32.VirtualProtect(start, trampolineBlockSize, 0x40, &oldProtect))
            {
                throw new Exception($"Failed to change protection of trampoline block.");
            }

            APILogger.Verbose(nameof(MemoryUtils), $"NOPing trampoline block");
            for (ulong i = 0; i < trampolineBlockSize; i++)
                *(byte*)((ulong)start + i) = 0x90;

            APILogger.Verbose(nameof(MemoryUtils), $"Creating trampoline shellcode");
            byte[] trampoline = MakeTrampoline(destination);

            APILogger.Verbose(nameof(MemoryUtils), $"Writing trampoline shellcode");
            for (ulong i = 0; i < (ulong)trampoline.Length; i++)
            {
                *(byte*)((ulong)start + i) = trampoline[i];
            }

            if (!Kernel32.VirtualProtect(start, trampolineBlockSize, oldProtect, &oldProtect))
            {
                throw new Exception($"Failed to revert trampoline block protection.");
            }
        }
    }
}
