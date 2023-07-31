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
                EventNameBytes = Encoding.UTF8.GetBytes(eventName)
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

            byte[] eventNameBytes = info.EventNameBytes;
            int nameBytesLength = eventNameBytes.Length;
            int payloadLength = payload.Length;
            int size = 2 + NetworkConstants.MagicSize + 8 + 2 + nameBytesLength + payloadLength;
            IntPtr pPacketBase = Marshal.AllocHGlobal(size);

            IntPtr pPacket = pPacketBase;

            //Write Packet Bytes
            Marshal.WriteInt16(pPacket, (short)m_ReplicatorKey);
            pPacket += 2;

            Marshal.Copy(s_MagicBytes, 0, pPacket, NetworkConstants.MagicSize);
            pPacket += NetworkConstants.MagicSize;

            Marshal.WriteInt64(pPacket, (long)m_Signature);
            pPacket += 8;

            Marshal.WriteInt16(pPacket, (short)nameBytesLength);
            pPacket += 2;

            Marshal.Copy(eventNameBytes, 0, pPacket, nameBytesLength);
            pPacket += nameBytesLength;

            Marshal.Copy(payload, 0, pPacket, payloadLength);
            //

            byte[] packetBytes = new byte[size];
            Marshal.Copy(pPacketBase, packetBytes, 0, size);
            Marshal.FreeHGlobal(pPacketBase);

            XORPacket(ref packetBytes, m_Signature, 2 + NetworkConstants.MagicSize + 8 + 2 + eventNameBytes.Length);
            return packetBytes;
        }
    }
}
