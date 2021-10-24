using System;

namespace GTFO.API.Attributes
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    internal class Il2CppInterfaceAttribute : Attribute
    {
        public Type Type;
        public Il2CppInterfaceAttribute(Type type)
        {
            Type = type;
        }
    }
}
