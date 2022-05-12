using System;
using System.Text;
using GTFO.API.Impl;
using GTFO.API.Resources;
using HarmonyLib;
using Il2CppInterop.Runtime;
using SNetwork;

namespace GTFO.API.Patches
{
    [HarmonyPatch(typeof(SNet_Replication))]
    internal class SNet_Replication_Patches
    {
        [HarmonyPatch(nameof(SNet_Replication.RecieveBytes))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        public static bool RecieveBytes_Prefix(Il2CppStructArray<byte> bytes, uint size, ulong messagerID)
        {
            if (size < 12) return true;

            // The implicit constructor duplicates the memory, so copying it once and using that is best
            byte[] _bytesCpy = bytes;

            ushort replicatorKey = BitConverter.ToUInt16(_bytesCpy, 0);
            if (NetworkAPI_Impl.Instance.m_ReplicatorKey == replicatorKey)
            {
                string magic = Encoding.ASCII.GetString(_bytesCpy, 2, NetworkConstants.MagicSize);
                if (magic != NetworkConstants.Magic)
                {
                    APILogger.Verbose("NetworkApi", $"Received invalid magic from {messagerID} ({magic} != {NetworkConstants.MagicSize}).");
                    return true;
                }

                ulong versionSignature = BitConverter.ToUInt64(_bytesCpy, 2 + NetworkConstants.MagicSize);
                if ((versionSignature & 0xFF00000000000000) != 0xFF00000000000000)
                {
                    APILogger.Verbose("NetworkApi", $"Received invalid version signature from {messagerID}.");
                    return true;
                }

                if (versionSignature != NetworkAPI_Impl.Instance.m_Signature)
                {
                    APILogger.Error("NetworkApi", $"Received incompatible version signature, cannot unmask packet. Is {messagerID} on the same version?. ({versionSignature} != {NetworkAPI_Impl.Instance.m_Signature})");
                    return false;
                }

                ushort eventNameLen = BitConverter.ToUInt16(_bytesCpy, 10 + NetworkConstants.MagicSize);
                string eventName = Encoding.UTF8.GetString(_bytesCpy, 12 + NetworkConstants.MagicSize, eventNameLen);

                if (!NetworkAPI.IsEventRegistered(eventName))
                {
                    APILogger.Error("NetworkApi", $"{messagerID} invoked an event {eventName} which isn't registered.");
                    return false;
                }

                NetworkAPI_Impl.Instance.HandlePacket(eventName, messagerID, _bytesCpy, 12 + NetworkConstants.MagicSize + eventNameLen);
                return false;
            }
            return true;
        }
    }
}
