using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BepInEx;
using GTFO.API.Attributes;
using GTFO.API.Resources;
using HarmonyLib;

namespace GTFO.API
{
    [API("SoundBank")]
    public static class SoundBankAPI
    {
        /// <summary>
        /// Status info for the <see cref="SoundBankAPI"/>
        /// </summary>
        public static ApiStatusInfo Status => APIStatus.SoundBank;

        /// <summary>
        /// Invoked when all external soundbanks have been loaded (Not invoked if no soundbanks)
        /// </summary>
        public static event Action OnSoundBanksLoaded;

        internal static void Setup()
        {
            EventAPI.OnManagersSetup += OnLoadSoundBanks;
        }

        private static void OnLoadSoundBanks()
        {
            string path = Path.Combine(Paths.BepInExRootPath, "Assets", "SoundBank");
            FileInfo[] soundbanksToLoad = Directory.CreateDirectory(path)
                .EnumerateFiles()
                .Where(file => file.Extension.Contains(".bnk"))
                .ToArray();

            soundbanksToLoad.Do(LoadBank);

            if (soundbanksToLoad.Any())
                OnSoundBanksLoaded?.Invoke();
        }

        private static unsafe void LoadBank(FileInfo file)
        {
            using FileStream stream = file.OpenRead();
            uint length = (uint)stream.Length;
            byte[] buffer = new byte[length];
            if (stream.Read(buffer, 0, (int)length) == 0) return;

            void* nativeBank = NativeMemory.AlignedAlloc(length, 0x10);
            Unsafe.CopyBlock(ref Unsafe.AsRef<byte>(nativeBank), ref buffer[0], length);

            AKRESULT loadResult = AkSoundEngine.LoadBank((nint)nativeBank, length, out uint bankId);
            if (loadResult == AKRESULT.AK_Success)
            {
                APILogger.Info(nameof(SoundBankAPI), $"Loaded sound bank '{file.Name}' (bankId: {bankId:X2})");
            }
            else
            {
                APILogger.Error(nameof(SoundBankAPI), $"Error while loading sound bank '{file.Name}' ({loadResult})");
            }
        }
    }
}
