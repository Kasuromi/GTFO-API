using System;
using System.Runtime.InteropServices;
using GTFO.API.Utilities;

namespace GTFO.API.Native
{
    internal abstract class NativePatch<T> where T : Delegate
    {
        private static readonly byte[] s_CodeCaveHeader = new byte[] {
            0x48, 0x83, 0xEC, 0x28                                              // sub      rsp,  0x28
        };

        private static readonly byte[] s_CodeCaveFooter = new byte[] {
            0x48, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,         // movabs   rax,  0x00
            0xFF, 0xD0,                                                         // call     rax

            0x48, 0x83, 0xC4, 0x28,                                             // add      rsp,  0x28
            0xC3                                                                // ret
        };

        public abstract byte[] CodeCave { get; }
        public abstract unsafe void* MethodPtr { get; }
        public abstract string JmpStartSig { get; }
        public abstract string JmpStartMask { get; }
        public abstract uint TrampolineSize { get; }
        public abstract unsafe T To { get; }

        public virtual unsafe void Apply()
        {
            void* pTrampolineStart = MemoryUtils.FindSignatureInBlock(MethodPtr, 4096, JmpStartSig, JmpStartMask);
            void* pTrampolineEnd = (void*)((long)pTrampolineStart + TrampolineSize);

            void* pCodeCaveBlock = Kernel32.VirtualAlloc((void*)0x00, (ulong)(s_CodeCaveHeader.Length + CodeCave.Length + s_CodeCaveFooter.Length), 0x00001000 | 0x00002000, 0x40);
            fixed (byte* pCodeCaveHeader = s_CodeCaveHeader)
            {
                MSVCRT.memcpy(pCodeCaveBlock, pCodeCaveHeader, s_CodeCaveHeader.Length);
            }
            fixed (byte* pCodeCave = CodeCave)
            {
                MSVCRT.memcpy((void*)((long)pCodeCaveBlock + s_CodeCaveHeader.Length), pCodeCave, CodeCave.Length);
            }

            void* pFooterBlock = (void*)((long)pCodeCaveBlock + s_CodeCaveHeader.Length + CodeCave.Length);
            fixed (byte* pCodeCaveFooter = s_CodeCaveFooter)
            {
                MSVCRT.memcpy(
                    pFooterBlock,
                    pCodeCaveFooter,
                    s_CodeCaveFooter.Length
                );
            }

            *(long*)((long)pFooterBlock + 2) = Marshal.GetFunctionPointerForDelegate(To).ToInt64();

            APILogger.Verbose(nameof(NativePatch<T>), $"Method: 0x{(long)MethodPtr:X2}, Trampoline: 0x{(long)pTrampolineStart:X2}, Code Cave: 0x{(long)pCodeCaveBlock:X2}");
            MemoryUtils.CreateTrampolineBetween(pTrampolineStart, pTrampolineEnd, pCodeCaveBlock);
        }
    }
}
