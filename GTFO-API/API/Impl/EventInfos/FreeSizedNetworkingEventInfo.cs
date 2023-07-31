using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFO.API.Impl
{
    internal sealed class FreeSizedNetworkingEventInfo : INetworkingEventInfo
    {
        public string Name { get; set; }
        public Action<ulong, byte[]> OnReceive { get; set; }
        public Type EventType => null;
        public int EventTypeSize => INetworkingEventInfo.FreeByteSize;
        public byte[] EventNameBytes { get; set; }

        public void InvokeOnReceive(ulong sender, object payload)
        {
            if (payload is byte[] castedPayload)
            {
                OnReceive?.Invoke(sender, castedPayload);
            }
        }
    }
}
