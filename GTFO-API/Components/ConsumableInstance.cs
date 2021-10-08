using System;

namespace GTFO.API.Components
{
    /// <summary>
    /// The core class that is used for consumable instances
    /// </summary>
    public class ConsumableInstance : Item
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public ConsumableInstance(IntPtr hdl) : base(hdl) { }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Invoked when the <see cref="ConsumableInstance"/> is about to be despawned
        /// </summary>
        public virtual unsafe void OnPreDespawn()
        {
        }

        /// <summary>
        /// Invoked when the <see cref="ConsumableInstance"/> has been despawned
        /// </summary>
        public virtual unsafe void OnPostDespawn()
        {
        }
    }
}
