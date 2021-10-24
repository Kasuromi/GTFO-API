using System;
using GTFO.API.Wrappers;

namespace GTFO.API.Components
{
    /// <summary>
    /// The core class that is used for consumable instances
    /// </summary>
    public class ConsumableInstance : ItemWrapped
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public ConsumableInstance(IntPtr hdl) : base(hdl) { }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
