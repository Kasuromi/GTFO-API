using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BepInEx.IL2CPP.Hook;
using GTFO.API.Attributes;
using GTFO.API.Resources;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;
using Il2CppInterop.Runtime.Runtime.VersionSpecific.Class;
using Il2CppInterop.Runtime.Runtime.VersionSpecific.MethodInfo;

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

            ClassInjector.RegisterTypeInIl2Cpp<T>(new RegisterTypeOptions()
            {
                LogSuccess = true,
                Interfaces = interfaces.ToArray()
            });
        }

        /// <summary>
        /// Obtains the function pointer for an Il2Cpp internal method
        /// </summary>
        /// <typeparam name="T">The type to look for the method in</typeparam>
        /// <param name="methodName">Method name in <typeparamref name="T"/></param>
        /// <param name="isGeneric">If the requested method is generic</param>
        /// <param name="returnTypeName">Full return type name e.g (System.Void)</param>
        /// <param name="argTypes">List of full type names for the arguments e.g (System.Single)</param>
        /// <returns>Function pointer of the Il2Cpp method or 0x00 if not found</returns>
        public static unsafe void* GetIl2CppMethod<T>(string methodName, string returnTypeName, bool isGeneric, params string[] argTypes) where T : Il2CppObjectBase
        {
            void** ppMethod = (void**)IL2CPP.GetIl2CppMethod(Il2CppClassPointerStore<T>.NativeClassPtr, isGeneric, methodName, returnTypeName, argTypes).ToPointer();
            if ((long)ppMethod == 0) return ppMethod;

            return *ppMethod;
        }

        /// <summary>
        /// Obtains a delegate for an Il2Cpp internal method
        /// </summary>
        /// <typeparam name="T">The type to look for the method in</typeparam>
        /// <typeparam name="TDelegate">The delegate to return</typeparam>
        /// <param name="methodName">Method name in <typeparamref name="T"/></param>
        /// <param name="isGeneric">If the requested method is generic</param>
        /// <param name="returnTypeName">Full return type name e.g (System.Void)</param>
        /// <param name="argTypes">List of full type names for the arguments e.g (System.Single)</param>
        /// <returns>A delegate to invoke the Il2Cpp method or null if invalid</returns>
        public static unsafe TDelegate GetIl2CppMethod<T, TDelegate>(string methodName, string returnTypeName, bool isGeneric, params string[] argTypes)
            where T : Il2CppObjectBase
            where TDelegate : Delegate
        {
            void* pMethod = GetIl2CppMethod<T>(methodName, returnTypeName, isGeneric, argTypes);
            if ((long)pMethod == 0) return null;

            return Marshal.GetDelegateForFunctionPointer<TDelegate>((IntPtr)pMethod);
        }

        /// <summary>
        /// Creates (and applies) a detour to a generic il2cpp method
        /// </summary>
        /// <typeparam name="TClass">Il2Cpp class that contains the generic method</typeparam>
        /// <typeparam name="TDelegate">Delegate of the method</typeparam>
        /// <param name="methodName">Name of the il2cpp method</param>
        /// <param name="returnType">Return type of the il2cpp method</param>
        /// <param name="paramTypes">List of full type names for the arguments</param>
        /// <param name="genericArguments">List of generic arguments for the il2cpp class</param>
        /// <param name="to">The method to detour to</param>
        /// <param name="original">Delegate to the original function</param>
        /// <returns>The applied detour</returns>
        /// <exception cref="ArgumentException">The il2cpp class is not registered in il2cpp</exception>
        public static unsafe INativeDetour CreateGenericDetour<TClass, TDelegate>(string methodName, string returnType, string[] paramTypes, Type[] genericArguments, TDelegate to, out TDelegate original)
            where TClass : Il2CppSystem.Object
            where TDelegate : Delegate
        {
            IntPtr classPtr = Il2CppClassPointerStore<TClass>.NativeClassPtr;
            if (classPtr == IntPtr.Zero) throw new ArgumentException($"{typeof(TClass).Name} does not exist in il2cpp domain");
            IntPtr methodPtr = IL2CPP.GetIl2CppMethod(classPtr, true, methodName, returnType, paramTypes);

            Il2CppSystem.Reflection.MethodInfo methodInfo = new(IL2CPP.il2cpp_method_get_object(methodPtr, classPtr));
            Il2CppSystem.Reflection.MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(genericArguments.Select(Il2CppType.From).ToArray());

            INativeMethodInfoStruct il2cppMethodInfo = UnityVersionHandler.Wrap((Il2CppMethodInfo*)IL2CPP.il2cpp_method_get_from_reflection(genericMethodInfo.Pointer));

            return INativeDetour.CreateAndApply(il2cppMethodInfo.MethodPointer, to, out original);
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
