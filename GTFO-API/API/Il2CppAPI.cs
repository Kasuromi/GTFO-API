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

                IL2CPP.il2cpp_runtime_class_init((IntPtr)pClass);

                interfaces.Add(UnityVersionHandler.Wrap(pClass));
            }

            ClassInjector.RegisterTypeInIl2Cpp(typeof(T), true, interfaces.ToArray());
        }

        /// <summary>
        /// Obtains the function pointer for an Il2Cpp internal method
        /// </summary>
        /// <typeparam name="T">The type to look for the method in</typeparam>
        /// <param name="methodName">Method name in <typeparamref name="T"/></param>
        /// <param name="returnTypeName">Full return type name e.g (System.Void)</param>
        /// <param name="argTypes">List of full type names for the arguments e.g (System.Single)</param>
        /// <returns>Function pointer of the Il2Cpp method or 0x00 if not found</returns>
        public static unsafe void* GetIl2CppMethod<T>(string methodName, string returnTypeName, params string[] argTypes) where T : Il2CppObjectBase
        {
            void** ppMethod = (void**)IL2CPP.GetIl2CppMethod(Il2CppClassPointerStore<T>.NativeClassPtr, false, methodName, returnTypeName, argTypes).ToPointer();
            if ((long)ppMethod == 0) return ppMethod;

            return *ppMethod;
        }

        /// <summary>
        /// Obtains a delegate for an Il2Cpp internal method
        /// </summary>
        /// <typeparam name="T">The type to look for the method in</typeparam>
        /// <typeparam name="TDelegate">The delegate to returns</typeparam>
        /// <param name="methodName">Method name in <typeparamref name="T"/></param>
        /// <param name="returnTypeName">Full return type name e.g (System.Void)</param>
        /// <param name="argTypes">List of full type names for the arguments e.g (System.Single)</param>
        /// <returns>A delegate to invoke the Il2Cpp method or null if invalid</returns>
        public static unsafe TDelegate GetIl2CppMethod<T, TDelegate>(string methodName, string returnTypeName, params string[] argTypes)
            where T : Il2CppObjectBase
            where TDelegate : Delegate
        {
            void* pMethod = GetIl2CppMethod<T>(methodName, returnTypeName, argTypes);
            if ((long)pMethod == 0) return null;

            return Marshal.GetDelegateForFunctionPointer<TDelegate>((IntPtr)pMethod);
        }

        private static IEnumerable<TAttribute> GetCustomAttributesInType<T, TAttribute>() where TAttribute : Attribute
        {
            var attributeType = typeof(TAttribute);
            return typeof(T).GetCustomAttributes(attributeType, true)
                .Union(typeof(T).GetInterfaces()
                    .SelectMany(interfaceType => interfaceType.GetCustomAttributes(attributeType, true)))
                .Distinct().Cast<TAttribute>();
        }
    }
}
