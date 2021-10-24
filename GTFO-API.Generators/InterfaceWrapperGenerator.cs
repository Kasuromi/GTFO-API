using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;

namespace GTFO.API
{
    [Generator]
    public class InterfaceWrapperGenerator : ISourceGenerator
    {
        static InterfaceWrapperGenerator()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                AssemblyName name = new(args.Name);
                Assembly loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().FullName == name.FullName);
                if (loadedAssembly != null)
                {
                    return loadedAssembly;
                }

                Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"GTFO.API.{name.Name}.dll");
                if (resourceStream == null)
                {
                    return null;
                }

                MemoryStream memoryStream = new MemoryStream();
                resourceStream.CopyTo(memoryStream);

                return Assembly.Load(memoryStream.ToArray());
            };
        }

        private static string[] s_KnownNamespaces = {
            "GTFO.API.Attributes",
            "Il2CppSystem.Collections.Generic",
            "UnityEngine"
        };

        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.ProjectDir", out string projectDir))
                throw new Exception("Couldn't find a build property for the project directory.");

            string interfacesPath = Path.Combine(projectDir, "..", "Resources", "Interfaces.json");
            if (!File.Exists(interfacesPath))
                throw new Exception("Couldn't find Resources/Interfaces.json");
            Dictionary<string, string[]> interfaces = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(
                File.ReadAllText(interfacesPath)
            );
            string src = "";
            foreach(var _namespace in s_KnownNamespaces)
            {
                src += $"using {_namespace};\n";
            }
            src += "namespace GTFO.API.Wrappers {\n\t#pragma warning disable CS1591\n";
            foreach(var _interface in interfaces)
            {
                string interfaceName = _interface.Key.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Last();
                src += $"\t[Il2CppInterface(typeof({_interface.Key}))]\n";
                src += $"\tpublic interface {interfaceName}Wrapper {{\n";
                foreach(var interfaceMember in _interface.Value)
                {
                    src += $"\t\t{interfaceMember}\n";
                }
                src += $"\t}}\n";
            }
            src += $"\t#pragma warning restore CS1591\n}}";
            context.AddSource("WrappedInterfaces.cs", SourceText.From(src, Encoding.UTF8));
        }
    }
}
