using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.XBNF.Lang;

namespace Axis.Pulsar.Core.XBNF.Tests.E2E
{
    [TestClass]
    public class SampleLang
    {
        private static ILanguageContext? Context;
        private static object @lock = new object();

        private static ILanguageContext GetContext()
        {
            if (Context is not null)
                return Context;

            lock(@lock)
            {
                if (Context is not null)
                    return Context;

                using var langDefStream = ResourceLoader.Load("SampleGrammar.SampleLang.xbnf");
                var langText = new StreamReader(langDefStream!).ReadToEnd();

                // build importer
                var importer = XBNFImporter
                    .NewBuilder()
                    .WithDefaultAtomicRuleDefinitions()
                    .Build();

                // import
                Context = importer.ImportLanguage(langText);
                return Context;
            }
        }

        [TestMethod]
        public void SampleLangTest()
        {
            ILanguageContext? _lang = null;
            try
            {
                // get language string
                using var langDefStream = ResourceLoader.Load("SampleGrammar.SampleLang.xbnf");
                var langText = new StreamReader(langDefStream!).ReadToEnd();

                // build importer
                var importer = XBNFImporter
                    .NewBuilder()
                    .WithDefaultAtomicRuleDefinitions()
                    .Build();

                // import
                _lang = importer.ImportLanguage(langText);
            }
            catch (Exception ex)
            {
                ex.Throw();
            }
        }

        [TestMethod]
        public void SampleRecognition_Tests()
        {
            var context = GetContext()!;
            var recognizer = context.Grammar.GetProduction("boolean-value-exp");
            var success = false;
            var result = default(NodeRecognitionResult);
            var node = default(ISymbolNode);

            success = recognizer.TryRecognize(
                $"true",
                "root",
                context,
                out result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out node));
            Assert.AreEqual($"true", node.Tokens.ToString());
        }

        [TestMethod]
        public void SampleLang2Tests()
        {
            ILanguageContext? _lang = null;
            try
            {
                // get language string
                using var langDefStream = ResourceLoader.Load("SampleGrammar.SampleLang2.xbnf");
                var langText = new StreamReader(langDefStream!).ReadToEnd();

                // build importer
                var importer = XBNFImporter
                    .NewBuilder()
                    .WithDefaultAtomicRuleDefinitions()
                    .Build();

                // import
                _lang = importer.ImportLanguage(langText);
                Assert.IsNotNull(_lang);
            }
            catch (Exception ex)
            {
                ex.Throw();
            }
        }
    }
}
