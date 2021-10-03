using System;

namespace GTFO.API.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class APIAttribute : Attribute
    {
        public string Name;
        public APIAttribute(string name)
        {
            Name = name;
        }
    }
}
