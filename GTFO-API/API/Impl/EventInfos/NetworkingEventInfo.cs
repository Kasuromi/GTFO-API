using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFO.API.Impl
{
    internal sealed class NetworkingEventInfo<T> : INetworkingEventInfo where T : struct
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
}
