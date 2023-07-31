using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using GTFO.API.Resources;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace GTFO.API.Impl
{
    internal sealed partial class NetworkAPI_Impl : MonoBehaviour
    {
        public NetworkAPI_Impl(IntPtr intPtr) : base(intPtr) { }

        public static NetworkAPI_Impl Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    NetworkAPI_Impl existing = FindObjectOfType<NetworkAPI_Impl>();
                    if (existing != null) s_Instance = existing;
                }
                return s_Instance;
            }
        }

        private void Awake()
        {
            s_Instance = this;
            APIStatus.Network.Ready = true;
            foreach (var cachedEvent in NetworkAPI.s_EventCache)
            {
                if (cachedEvent.Value.IsFreeSize)
                {
                    MethodInfo registerMethod = typeof(NetworkAPI_Impl).GetMethod("RegisterFreeSizedEvent");
                    registerMethod.Invoke(this, new object[] { cachedEvent.Key, cachedEvent.Value.OnReceive });
                }
                else
                {
                    MethodInfo registerMethod = typeof(NetworkAPI_Impl).GetMethod("RegisterEvent").MakeGenericMethod(cachedEvent.Value.PayloadType);
                    registerMethod.Invoke(this, new object[] { cachedEvent.Key, cachedEvent.Value.OnReceive });
                }
            }
            NetworkAPI.s_EventCache.Clear();
        }

        internal static unsafe void XORPacket(ref byte[] packetBytes, ulong versionSig, int offset)
        {
            fixed (byte* pPacket = packetBytes)
            {
                XORPacketImpl(pPacket + offset, (uint)(packetBytes.Length - offset), (byte*)&versionSig);
            }
        }

        internal static unsafe void XORPacketImpl(byte* pPacket, uint packetLength, byte* pVersion)
        {
            for (int i = 0; i < packetLength; i++)
            {
                *(pPacket + i) = (byte)(*(pPacket + i) ^ *(pVersion + i % 8));
            }
        }

        [HideFromIl2Cpp]
        public void HandlePacket(string eventName, ulong senderId, byte[] packetData, int startIndex = 0)
        {
            INetworkingEventInfo eventInfo = m_Events[eventName];

            //Free Size Packet
            if (eventInfo.IsFreeSized()) 
            {
                XORPacket(ref packetData, m_Signature, startIndex);
                eventInfo.InvokeOnReceive(senderId, packetData);
            }
            else
            {
                Type eventType = eventInfo.EventType;

                XORPacket(ref packetData, m_Signature, startIndex);

                int size = Marshal.SizeOf(eventType);
                IntPtr pPacket = Marshal.AllocHGlobal(size);
                Marshal.Copy(packetData, startIndex, pPacket, size);

                object managedPacket = Marshal.PtrToStructure(pPacket, eventType);

                Marshal.FreeHGlobal(pPacket);

                eventInfo.InvokeOnReceive(senderId, managedPacket);
            }
        }

        [HideFromIl2Cpp]
        public bool EventExists(string eventName) => m_Events.ContainsKey(eventName);

        public ulong m_Signature = NetworkConstants.VersionSignature;
        public ushort m_ReplicatorKey = 0xFFFD;

        private readonly Dictionary<string, INetworkingEventInfo> m_Events = new();

        private static readonly byte[] s_MagicBytes = Encoding.ASCII.GetBytes(NetworkConstants.Magic);

        private static NetworkAPI_Impl s_Instance;
    }
}
