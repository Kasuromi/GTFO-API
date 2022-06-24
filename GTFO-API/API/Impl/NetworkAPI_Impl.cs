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
    internal interface INetworkingEventInfo
    {
        string Name { get; }
        Type EventType { get; }
        int EventTypeSize { get; }
        byte[] EventNameBytes { get; }
        void InvokeOnReceive(ulong sender, object payload);
    }

    internal class NetworkingEventInfo<T> : INetworkingEventInfo where T : struct
    {
        public string Name { get; set; }
        public Action<ulong, T> OnReceive { get; set; }
        public Type EventType { get; set; }
        public int EventTypeSize { get; set; }
        public byte[] EventNameBytes { get; set; }

        public void InvokeOnReceive(ulong sender, object payload)
        {
            if (payload is T castedPayload)
            {
                OnReceive?.Invoke(sender, castedPayload);
            }
        }
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
            Type eventType = eventInfo.EventType;

            XORPacket(ref packetData, m_Signature, startIndex);

            int size = Marshal.SizeOf(eventType);
            IntPtr pPacket = Marshal.AllocHGlobal(size);
            Marshal.Copy(packetData, startIndex, pPacket, size);

            object managedPacket = Marshal.PtrToStructure(pPacket, eventType);

            Marshal.FreeHGlobal(pPacket);

            eventInfo.InvokeOnReceive(senderId, managedPacket);
        }

        [HideFromIl2Cpp]
        public void RegisterEvent<T>(string eventName, Action<ulong, T> onReceive) where T : struct
        {
            if (EventExists(eventName))
            {
                throw new ArgumentException($"An event with the name {eventName} has already been registered.", nameof(eventName));
            }

            if (typeof(T).IsGenericType)
            {
                throw new ArgumentException($"Generic type is not allowed to register", nameof(T));
            }

            NetworkingEventInfo<T> eventInfo = new()
            {
                Name = eventName,
                OnReceive = onReceive,
                EventType = typeof(T),
                EventTypeSize = Marshal.SizeOf<T>(),
                EventNameBytes = Encoding.UTF8.GetBytes(eventName)
            };

            m_Events.Add(eventName, eventInfo);
        }

        [HideFromIl2Cpp]
        public bool EventExists(string eventName) => m_Events.ContainsKey(eventName);

        [HideFromIl2Cpp]
        public byte[] MakePacketBytes<T>(string eventName, T payload) where T : struct
        {
            if (!m_Events.TryGetValue(eventName, out INetworkingEventInfo info))
            {
                throw new ArgumentException($"An event with the name {eventName} was not registered.", nameof(eventName));
            }

            if (info.EventType != typeof(T))
            {
                throw new ArgumentException($"Payload type was incorrect! expecting: {typeof(T).FullName}", nameof(eventName));
            }

            byte[] eventNameBytes = info.EventNameBytes;
            int size = 2 + NetworkConstants.MagicSize + 8 + 2 + eventNameBytes.Length + info.EventTypeSize;
            IntPtr pPacketBase = Marshal.AllocHGlobal(size);

            IntPtr pPacket = pPacketBase;

            Marshal.WriteInt16(pPacket, (short)m_ReplicatorKey);
            pPacket += 2;

            Marshal.Copy(m_MagicBytes, 0, pPacket, NetworkConstants.MagicSize);
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

        private readonly Dictionary<string, INetworkingEventInfo> m_Events = new();

        private static readonly byte[] m_MagicBytes = Encoding.ASCII.GetBytes(NetworkConstants.Magic);

        private static NetworkAPI_Impl s_Instance;
    }
}
