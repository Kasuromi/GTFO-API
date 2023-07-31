using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Il2CppInterop.Runtime.Attributes;
using GTFO.API.Resources;
using UnityEngine;

namespace GTFO.API.Impl
{
    internal sealed partial class NetworkAPI_Impl : MonoBehaviour
    {
        [HideFromIl2Cpp]
        public void RegisterEvent<T>(string eventName, Action<ulong, T> onReceive) where T : struct
        {
            if (EventExists(eventName))
            {
                throw new ArgumentException($"Type {typeof(T)} is a generic and cannot be registered.", nameof(eventName));
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
                Header = MakeHeaderBytes(eventName)
            };

            m_Events.Add(eventName, eventInfo);
        }

        [HideFromIl2Cpp]
        public byte[] MakePacketBytes<T>(string eventName, T payload) where T : struct
        {
            if (!m_Events.TryGetValue(eventName, out INetworkingEventInfo info))
            {
                throw new ArgumentException($"An event with the name {eventName} was not registered.", nameof(eventName));
            }

            if (info.IsFreeSized())
            {
                throw new InvalidOperationException($"Event '{eventName}' is NOT registered as FixedSize Event!");
            }

            if (info.EventType != typeof(T))
            {
                throw new ArgumentException($"Payload type was incorrect! expecting: {typeof(T).FullName}", nameof(eventName));
            }

            int payloadSize = info.EventTypeSize;
            int headerSize = info.HeaderSize;
            int packetSize = headerSize + payloadSize;

            IntPtr pPacketBase = Marshal.AllocHGlobal(packetSize);
            IntPtr pPacket = pPacketBase;

            Marshal.Copy(info.Header, 0, pPacket, headerSize);
            pPacket += headerSize;

            Marshal.StructureToPtr(payload, pPacket, true);

            byte[] packetBytes = new byte[packetSize];
            Marshal.Copy(pPacketBase, packetBytes, 0, packetSize);
            Marshal.FreeHGlobal(pPacketBase);

            XORPacket(ref packetBytes, m_Signature, headerSize);
            return packetBytes;
        }
    }
}
