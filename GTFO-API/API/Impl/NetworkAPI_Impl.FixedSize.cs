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
                EventNameBytes = Encoding.UTF8.GetBytes(eventName)
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

            byte[] eventNameBytes = info.EventNameBytes;
            int size = 2 + NetworkConstants.MagicSize + 8 + 2 + eventNameBytes.Length + info.EventTypeSize;
            IntPtr pPacketBase = Marshal.AllocHGlobal(size);

            IntPtr pPacket = pPacketBase;

            Marshal.WriteInt16(pPacket, (short)m_ReplicatorKey);
            pPacket += 2;

            Marshal.Copy(s_MagicBytes, 0, pPacket, NetworkConstants.MagicSize);
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
    }
}
