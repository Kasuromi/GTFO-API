using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using GTFO.API.Attributes;
using GTFO.API.Resources;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Runtime;
using UnhollowerBaseLib.Runtime.VersionSpecific.Class;
using UnhollowerRuntimeLib;

namespace GTFO.API
{
    [API("Il2Cpp")]
    public static class Il2CppAPI
    {
        /// <summary>
        /// Status info for the <see cref="Il2CppAPI"/>
        /// </summary>
        public static ApiStatusInfo Status => APIStatus.Il2Cpp;

        static Il2CppAPI()
        {
            Status.Created = true;
            Status.Ready = true;
        }

        /// <summary>
        /// Injects a class and its interfaces with <see cref="ClassInjector"/>
        /// </summary>
        /// <typeparam name="T">The class to inject into Il2Cpp</typeparam>
        public static unsafe void InjectWithInterface<T>() where T : Il2CppObjectBase
        {
            List<INativeClassStruct> interfaces = new();
            IEnumerable<Il2CppInterfaceAttribute> attributes = GetCustomAttributesInType<T, Il2CppInterfaceAttribute>();
            foreach (var attribute in attributes)
            {
                Il2CppClass* pClass = (Il2CppClass*)(IntPtr)typeof(Il2CppClassPointerStore<>)
                    .MakeGenericType(attribute.Type)
                    .GetField("NativeClassPtr")
                    .GetValue(null);

                il2cpp_runtime_class_init(pClass);

                interfaces.Add(UnityVersionHandler.Wrap(pClass));
            }

            ClassInjector.RegisterTypeInIl2Cpp<T>(interfaces.ToArray());
        }

        private static IEnumerable<TAttribute> GetCustomAttributesInType<T, TAttribute>() where TAttribute : Attribute
        {
            var attributeType = typeof(TAttribute);
            return typeof(T).GetCustomAttributes(attributeType, true)
                .Union(typeof(T).GetInterfaces()
                    .SelectMany(interfaceType => interfaceType.GetCustomAttributes(attributeType, true)))
                .Distinct().Cast<TAttribute>();
        }

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern unsafe void il2cpp_runtime_class_init(Il2CppClass* pClass);
    }
}
