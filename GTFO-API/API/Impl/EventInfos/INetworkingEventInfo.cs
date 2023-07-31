using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTFO.API.Impl
{
    internal interface INetworkingEventInfo
    {
        string Name { get; }
        Type EventType { get; }
        int EventTypeSize { get; }
        byte[] Header { get; }
        public int HeaderSize => Header.Length;
        void InvokeOnReceive(ulong sender, object payload);

        public bool IsFreeSized()
        {
            return EventTypeSize == FreeByteSize;
        }

        public const int FreeByteSize = -1;
    }
}
