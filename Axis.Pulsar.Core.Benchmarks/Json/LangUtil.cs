using Axis.Luna.Extensions;
using Axis.Pulsar.Core.XBNF.Lang;

namespace Axis.Pulsar.Core.Benchmarks.Json
{
    internal static class LangUtil
    {
        internal static ILanguageContext LanguageContext { get; }

        internal static string SampleJson { get; }

        static LangUtil()
        {
            using var inputReader = typeof(LangUtil)
                .Assembly
                .GetManifestResourceStream($"{typeof(LangUtil).Namespace}.json.xbnf")
                .ApplyTo(stream => new StreamReader(stream!));

            LanguageContext = XBNFImporter.Builder
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .Build()
                .ImportLanguage(inputReader.ReadToEnd());

            using var sampleStream = typeof(LangUtil)
                .Assembly
                .GetManifestResourceStream($"{typeof(LangUtil).Namespace}.sample.json");
            SampleJson = new StreamReader(sampleStream!).ReadToEnd();
        }
    }
}
