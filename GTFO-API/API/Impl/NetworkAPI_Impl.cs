using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using GTFO.API.Resources;
using UnhollowerBaseLib.Attributes;
using UnityEngine;

namespace GTFO.API.Impl
{
    internal class NetworkingEventInfo<T> where T : struct
    {
        public string Name { get; set; }
        public Action<ulong, T> OnReceive { get; set; }
    }
    internal class NetworkAPI_Impl : MonoBehaviour
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
                MethodInfo registerMethod = typeof(NetworkAPI_Impl).GetMethod("RegisterEvent").MakeGenericMethod(cachedEvent.Value.Type);
                registerMethod.Invoke(this, new object[] { cachedEvent.Key, cachedEvent.Value.OnReceive });
            }
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
            Type eventType = m_EventTypes[eventName];
            object eventInfo = m_Events[eventName];

            XORPacket(ref packetData, m_Signature, startIndex);

            int size = Marshal.SizeOf(eventType);
            IntPtr pPacket = Marshal.AllocHGlobal(size);
            Marshal.Copy(packetData, startIndex, pPacket, size);

            object managedPacket = Marshal.PtrToStructure(pPacket, eventType);

            Marshal.FreeHGlobal(pPacket);

            object receiveDelegate = eventInfo.GetType().GetProperty("OnReceive").GetValue(eventInfo);

            Type receiveDelegateType = typeof(Action<,>).MakeGenericType(typeof(ulong), eventType);
            receiveDelegateType.GetMethod("Invoke").Invoke(receiveDelegate, new object[] { senderId, managedPacket });
        }

        public void RegisterEvent<T>(string eventName, Action<ulong, T> onReceive) where T : struct
        {
            if (EventExists(eventName)) throw new ArgumentException($"An event with the name {eventName} has already been registered.");

            NetworkingEventInfo<T> eventInfo = new()
            {
                Name = eventName,
                OnReceive = onReceive
            };

            m_Events.Add(eventName, eventInfo);
            m_EventTypes.Add(eventName, typeof(T));
        }

        [HideFromIl2Cpp]
        public bool EventExists(string eventName) => m_Events.ContainsKey(eventName);

        public byte[] MakePacketBytes<T>(string eventName, T payload) where T : struct
        {
            byte[] eventNameBytes = Encoding.UTF8.GetBytes(eventName);

            int size = 2 + NetworkConstants.MagicSize + 8 + 2 + eventNameBytes.Length + Marshal.SizeOf<T>();
            IntPtr pPacketBase = Marshal.AllocHGlobal(size);

            IntPtr pPacket = pPacketBase;

            Marshal.WriteInt16(pPacket, (short)m_ReplicatorKey);
            pPacket += 2;

            Marshal.Copy(Encoding.ASCII.GetBytes(NetworkConstants.Magic), 0, pPacket, NetworkConstants.MagicSize);
            pPacket += NetworkConstants.MagicSize;

            Marshal.WriteInt64(pPacket, (long)m_Signature);
            pPacket += 8;

            Marshal.WriteInt16(pPacket, (short)eventNameBytes.Length);
            pPacket += 2;

            Marshal.Copy(eventNameBytes, 0, pPacket, eventNameBytes.Length);
            pPacket += eventNameBytes.Length;

            Marshal.StructureToPtr(payload, pPacket, true);

            byte[] packetBytes = new byte[size];
            Marshal.Copy(pPacketBase, packetBytes, 0, size);

            Marshal.FreeHGlobal(pPacketBase);

            XORPacket(ref packetBytes, m_Signature, 2 + NetworkConstants.MagicSize + 8 + 2 + eventNameBytes.Length);
            return packetBytes;
        }

        public ushort m_ReplicatorKey = 0xFFFD;
        public ulong m_Signature = NetworkConstants.VersionSignature;

        private readonly Dictionary<string, object> m_Events = new();
        private readonly Dictionary<string, Type> m_EventTypes = new();

        private static NetworkAPI_Impl s_Instance;
    }
}
