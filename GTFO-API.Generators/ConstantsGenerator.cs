using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace GTFO.API.Generators
{
    [Generator]
    public class ConstantsGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            string networkMagic = "GAPI_KSQK";
            string src = $@"
namespace GTFO.API.Resources {{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class NetworkConstants {{
        public const string Magic = ""{networkMagic}"";
        public const byte MagicSize = {networkMagic.Length};
        public const ulong VersionSignature = {(ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() | 0xFF00000000000000};
    }}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}}
";
            context.AddSource("Constants.cs", SourceText.From(src, Encoding.UTF8));
        }
    }
}
