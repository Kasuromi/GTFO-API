using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BepInEx;
using Globals;
using GTFO.API.Attributes;
using GTFO.API.Resources;

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
        /// Invoked when all external soundbanks have been loaded
        /// </summary>
        public static event Action OnSoundBanksLoaded;

        internal static void Setup()
        {
            EventAPI.OnManagersSetup += OnLoadSoundBanks;
        }

        private static void OnLoadSoundBanks()
        {
            string path = Path.Combine(Paths.BepInExRootPath, "assets", "SoundBank");
            Directory.CreateDirectory(path)
                .EnumerateFiles()
                .Where(file => file.Extension.Contains(".bnk"))
                .ToList()
                .ForEach(file => LoadBank(file));
            //Loaded those bad boys
            OnSoundBanksLoaded?.Invoke();
        }

        private static void LoadBank(FileInfo file)
        {
            FileStream stream = file.OpenRead();
            int length = (int)stream.Length;
            byte[] buffer = new byte[length];
            if (stream.Read(buffer, 0, length).Equals(0)) return;
            try
            {
                var size = (uint)buffer.Length;
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                var ptr = handle.AddrOfPinnedObject();

                if ((ptr.ToInt64() & 15L) != 0L)
                {
                    byte[] array = new byte[(long)buffer.Length + 16L];
                    IntPtr newPtr = GCHandle.Alloc(array, GCHandleType.Pinned).AddrOfPinnedObject();
                    int offset = 0;
                    if ((newPtr.ToInt64() & 15L) != 0L)
                    {
                        long realignedPointerLoc = (newPtr.ToInt64() + 15L) & -16L;
                        offset = (int)(realignedPointerLoc - newPtr.ToInt64());
                        newPtr = new IntPtr(realignedPointerLoc);
                    }
                    Array.Copy(buffer, 0, array, offset, buffer.Length);
                    ptr = newPtr;
                    handle.Free();
                }

                if (AkSoundEngine.LoadBank(ptr, size, out uint bankId).Equals(AKRESULT.AK_Success))
                {
                    APILogger.Info(nameof(SoundBankAPI), $"loaded : {file.Name}, {bankId}");
                }
            }
            catch (Exception exception)
            {
                APILogger.Debug(nameof(SoundBankAPI), exception);
                APILogger.Warn(nameof(SoundBankAPI), $"exception thrown when allocating bnk : {file.Name}");
            }
            stream.Dispose();
        }
    }
}
