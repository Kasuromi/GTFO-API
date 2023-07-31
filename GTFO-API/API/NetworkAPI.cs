using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GTFO.API.Attributes;
using GTFO.API.Extensions;
using GTFO.API.Impl;
using GTFO.API.Resources;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using SNetwork;

namespace GTFO.API
{
    [API("Network")]
    public static class NetworkAPI
    {
        internal class CachedEvent
        {
            public string EventName { get; set; }
            public Type PayloadType { get; set; }
            public object OnReceive { get; set; }
            public bool IsFreeSize { get; set; }
        }

        /// <summary>
        /// Status info for the <see cref="NetworkAPI"/>
        /// </summary>
        public static ApiStatusInfo Status => APIStatus.Network;

        /// <summary>
        /// Checks if an event is registered in the <see cref="NetworkAPI"/>
        /// </summary>
        /// <param name="eventName">The name of the event to check.</param>
        /// <returns>If the specified event name is in use</returns>
        public static bool IsEventRegistered(string eventName) => NetworkAPI_Impl.Instance.EventExists(eventName);

        #region FixedSize Event
        /// <summary>
        /// Registers a network event with a name and receive action.
        /// </summary>
        /// <typeparam name="T">The struct that will be used for the event info</typeparam>
        /// <param name="eventName">The name of the event to register</param>
        /// <param name="onReceive">The method that will be invoked when the event is received from another player</param>
        /// <exception cref="ArgumentException">The event is already registered</exception>
        public static void RegisterEvent<T>(string eventName, Action<ulong, T> onReceive) where T : struct
        {
            if (!APIStatus.Network.Ready)
            {
                if (s_EventCache.ContainsKey(eventName)) throw new ArgumentException($"An event with the name {eventName} has already been registered.", nameof(eventName));
                s_EventCache.TryAdd(eventName, new CachedEvent
                {
                    EventName = eventName,
                    PayloadType = typeof(T),
                    OnReceive = onReceive,
                    IsFreeSize = false
                });
                return;
            }
            NetworkAPI_Impl.Instance.RegisterEvent(eventName, onReceive);
        }

        /// <summary>
        /// Sends an event to every player connected to the lobby
        /// </summary>
        /// <typeparam name="T">The struct that holds the event information</typeparam>
        /// <param name="eventName">The name of the event to invoke</param>
        /// <param name="payload">The data to send</param>
        /// <param name="channelType">The <see cref="SNet_ChannelType"/> to send the event to</param>
        public static void InvokeEvent<T>(string eventName, T payload, SNet_ChannelType channelType = SNet_ChannelType.GameOrderCritical) where T : struct
        {
            SNet.GetSendSettings(ref channelType, out SNet_SendGroup style, out SNet_SendQuality quality, out int channel);
            SNet.Core.SendBytes(MakeBytes(eventName, payload), style, quality, channel);
        }

        /// <summary>
        /// Sends an event to a specific player in the lobby
        /// </summary>
        /// <typeparam name="T">The struct that holds the event information</typeparam>
        /// <param name="eventName">The name of the event to invoke</param>
        /// <param name="payload">The data to send</param>
        /// <param name="target">The player to send the event to</param>
        /// <param name="channelType">The <see cref="SNet_ChannelType"/> to send the event to</param>
        public static void InvokeEvent<T>(string eventName, T payload, SNet_Player target, SNet_ChannelType channelType = SNet_ChannelType.GameOrderCritical) where T : struct
        {
            SNet.GetSendSettings(ref channelType, out _, out SNet_SendQuality quality, out int channel);
            SNet.Core.SendBytes(MakeBytes(eventName, payload), quality, channel, target);
        }

        /// <summary>
        /// Sends an event to a specific set of players in the lobby
        /// </summary>
        /// <typeparam name="T">The struct that holds the event information</typeparam>
        /// <param name="eventName">The name of the event to invoke</param>
        /// <param name="payload">The data to send</param>
        /// <param name="targets">The players to send the event to</param>
        /// <param name="channelType">The <see cref="SNet_ChannelType"/> to send the event to</param>
        public static void InvokeEvent<T>(string eventName, T payload, List<SNet_Player> targets, SNet_ChannelType channelType = SNet_ChannelType.GameOrderCritical) where T : struct
        {
            SNet.GetSendSettings(ref channelType, out _, out SNet_SendQuality quality, out int channel);
            SNet.Core.SendBytes(MakeBytes(eventName, payload), quality, channel, targets.ToIl2Cpp());
        }
        #endregion FixedSize Event

        #region FreeSized Event
        /// <summary>
        /// Registers a free-sized network event with name and receive action.
        /// </summary>
        /// <param name="eventName">The name of the event to register</param>
        /// <param name="onReceiveBytes">The method that will be invoked when the event is received from another player</param>
        /// <exception cref="ArgumentException">The event is already registered</exception>
        public static void RegisterFreeSizedEvent(string eventName, Action<ulong, byte[]> onReceiveBytes)
        {
            if (!APIStatus.Network.Ready)
            {
                if (s_EventCache.ContainsKey(eventName)) throw new ArgumentException($"An event with the name {eventName} has already been registered.", nameof(eventName));
                s_EventCache.TryAdd(eventName, new CachedEvent
                {
                    EventName = eventName,
                    PayloadType = null,
                    OnReceive = onReceiveBytes,
                    IsFreeSize = true
                });
                return;
            }
            NetworkAPI_Impl.Instance.RegisterFreeSizedEvent(eventName, onReceiveBytes);
        }

        /// <summary>
        /// Sends an free-sized event to every player connected to the lobby
        /// </summary>
        /// <param name="eventName">The name of the event to invoke</param>
        /// <param name="payload">The data to send</param>
        /// <param name="channelType">The <see cref="SNet_ChannelType"/> to send the event to</param>
        public static void InvokeFreeSizedEvent(string eventName, byte[] payload, SNet_ChannelType channelType = SNet_ChannelType.GameOrderCritical)
        {
            SNet.GetSendSettings(ref channelType, out SNet_SendGroup style, out SNet_SendQuality quality, out int channel);
            SNet.Core.SendBytes(MakeBytes(eventName, payload), style, quality, channel);
        }

        /// <summary>
        /// Sends an free-sized event to a specific player in the lobby
        /// </summary>
        /// <param name="eventName">The name of the event to invoke</param>
        /// <param name="payload">The data to send</param>
        /// <param name="target">The player to send the event to</param>
        /// <param name="channelType">The <see cref="SNet_ChannelType"/> to send the event to</param>
        public static void InvokeFreeSizedEvent(string eventName, byte[] payload, SNet_Player target, SNet_ChannelType channelType = SNet_ChannelType.GameOrderCritical)
        {
            SNet.GetSendSettings(ref channelType, out _, out SNet_SendQuality quality, out int channel);
            SNet.Core.SendBytes(MakeBytes(eventName, payload), quality, channel, target);
        }

        /// <summary>
        /// Sends an free-sized event to a specific set of players in the lobby
        /// </summary>
        /// <param name="eventName">The name of the event to invoke</param>
        /// <param name="payload">The data to send</param>
        /// <param name="targets">The players to send the event to</param>
        /// <param name="channelType">The <see cref="SNet_ChannelType"/> to send the event to</param>
        public static void InvokeFreeSizedEvent(string eventName, byte[] payload, IEnumerable<SNet_Player> targets, SNet_ChannelType channelType = SNet_ChannelType.GameOrderCritical)
        {
            SNet.GetSendSettings(ref channelType, out _, out SNet_SendQuality quality, out int channel);
            SNet.Core.SendBytes(MakeBytes(eventName, payload), quality, channel, targets.ToList().ToIl2Cpp());
        }
        #endregion FreeSized Event

        private static Il2CppStructArray<byte> MakeBytes<T>(string eventName, T payload) where T : struct
        {
            return NetworkAPI_Impl.Instance.MakePacketBytes(eventName, payload);
        }

        private static Il2CppStructArray<byte> MakeBytes(string eventName, byte[] payload)
        {
            return NetworkAPI_Impl.Instance.MakeFreeSizedPacketBytes(eventName, payload);
        }

        internal static ConcurrentDictionary<string, CachedEvent> s_EventCache = new();
    }
}
