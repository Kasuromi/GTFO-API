using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppInterop.Runtime.Attributes;
using GTFO.API.Resources;
using UnityEngine;
using System.Runtime.InteropServices;

namespace GTFO.API.Impl
{
    internal sealed partial class NetworkAPI_Impl : MonoBehaviour
    {
        [HideFromIl2Cpp]
        public void RegisterFreeSizedEvent(string eventName, Action<ulong, byte[]> onReceive)
        {
            if (EventExists(eventName))
            {
                throw new ArgumentException($"EventName '{eventName}' is already registered!", nameof(eventName));
            }

            FreeSizedNetworkingEventInfo eventInfo = new()
            {
                Name = eventName,
                OnReceive = onReceive,
                Header = MakeHeaderBytes(eventName)
            };

            m_Events.Add(eventName, eventInfo);
        }

        [HideFromIl2Cpp]
        public byte[] MakeFreeSizedPacketBytes(string eventName, byte[] payload)
        {
            if (!m_Events.TryGetValue(eventName, out INetworkingEventInfo info))
            {
                throw new ArgumentException($"An event with the name {eventName} was not registered.", nameof(eventName));
            }

            if (!info.IsFreeSized())
            {
                throw new InvalidOperationException($"Event '{eventName}' is NOT registered as FreeSized Event!");
            }

            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload), $"Payload was null!");
            }

            if (payload.Length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(payload), $"Payload was Empty!");
            }

            int payloadSize = payload.Length;
            int headerSize = info.HeaderSize;
            int packetSize = headerSize + payloadSize;

            IntPtr pPacketBase = Marshal.AllocHGlobal(packetSize);
            IntPtr pPacket = pPacketBase;

            Marshal.Copy(info.Header, 0, pPacket, headerSize);
            pPacket += info.HeaderSize;

            Marshal.Copy(payload, 0, pPacket, payloadSize);

            byte[] packetBytes = new byte[packetSize];
            Marshal.Copy(pPacketBase, packetBytes, 0, packetSize);
            Marshal.FreeHGlobal(pPacketBase);

            XORPacket(ref packetBytes, m_Signature, offset: headerSize);
            return packetBytes;
        }
    }
}
