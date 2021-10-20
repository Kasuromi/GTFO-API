using System;
using System.Runtime.InteropServices;
using GTFO.API.Native;
using GTFO.API.Utilities;

namespace GTFO.API.Patches.Native
{
    internal class SyringeFirstPerson_Patch : NativePatch<SyringeFirstPerson_Patch.SyringeUsedDelegate>
    {
        public override unsafe void* MethodPtr => MemoryUtils.GetIl2CppMethod<SyringeFirstPerson._ApplySyringe_d__18>("MoveNext", "System.Boolean");
        public override string JmpStartSig => "\xC6\x81\xB4\x00\x00\x00\x01\x8B\x87\xD0\x01\x00\x00\x85\xC0";
        public override string JmpStartMask => "xxxxxxxxxxxxxxx";

        public override byte[] CodeCave => new byte[] {
            0xC6, 0x81, 0xB4, 0x00, 0x00, 0x00, 0x01,                           // mov      byte ptr [rcx+0B4h], 1
            0x48, 0x89, 0xf9,                                                   // mov      rcx,                 rdi
        };

        public override uint TrampolineSize => 13;
        public override unsafe SyringeUsedDelegate To => OnSyringeApplyEffect;

        public static unsafe eSyringeType OnSyringeApplyEffect(void* pSyringe)
        {
            SyringeFirstPerson syringe = new SyringeFirstPerson(new IntPtr(pSyringe));
            if (PrefabAPI.OnSyringeUsed(syringe)) return (eSyringeType)unchecked(-1);

            return syringe.m_type;
        }

        [UnmanagedFunctionPointer(CallingConvention.FastCall)]
        public unsafe delegate eSyringeType SyringeUsedDelegate(void* pSyringe);
    }
}
