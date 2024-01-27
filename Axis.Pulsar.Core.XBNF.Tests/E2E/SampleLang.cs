using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.XBNF.Lang;

namespace Axis.Pulsar.Core.XBNF.Tests.E2E
{
    [TestClass]
    public class SampleLang
    {
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
                var importer = XBNFImporter.Builder
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
            using var langDefStream = ResourceLoader.Load("SampleGrammar.SampleLang.xbnf");
            var langText = new StreamReader(langDefStream!).ReadToEnd();

            // build importer
            var importer = XBNFImporter.Builder
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .Build();

            // import
            var lang = importer.ImportLanguage(langText);


            var recognizer = lang.Grammar.GetProduction("attribute-set-access-exp");
            var success = recognizer.TryRecognize(
                "@subject[Something]",
                "root",
                lang,
                out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ICSTNode node));
            Assert.AreEqual("@subject[Something]", node.Tokens.ToString());

            success = recognizer.TryRecognize(
                "@subject['Something else']",
                "root",
                lang,
                out result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out node));
            Assert.AreEqual("@subject['Something else']", node.Tokens.ToString());
        }
    }
}
