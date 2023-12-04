using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.IO;
using Axis.Pulsar.Grammar.Language.Rules.CustomTerminals;
using Axis.Pulsar.Languages.xBNF;

namespace Axis.Pulsar.Grammar.Benchmarks.Json
{
    internal static class LangUtil
    {
        internal static Language.Grammar Grammar { get; }

        internal static string SampleJson { get; }

        static LangUtil()
        {
            using var inputStream = typeof(LangUtil)
                .Assembly
                .GetManifestResourceStream($"{typeof(LangUtil).Namespace}.json.xbnf");

            Grammar = new Importer()
                .RegisterTerminal(new CommentRule("LineComment"))
                .RegisterTerminal(new DelimitedString("DQSString", "\""))
                .As<IImporter>()
                .ImportGrammar(inputStream);

            using var sampleStream = typeof(LangUtil)
                .Assembly
                .GetManifestResourceStream($"{typeof(LangUtil).Namespace}.sample.json");
            SampleJson = new StreamReader(sampleStream!).ReadToEnd();
        }
    }
}
