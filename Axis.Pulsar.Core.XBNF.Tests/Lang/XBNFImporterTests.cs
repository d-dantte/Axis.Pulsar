using Axis.Pulsar.Core.XBNF.Lang;

namespace Axis.Pulsar.Core.XBNF.Tests.Lang
{
    [TestClass]
    public class XBNFImporterTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            // build importer
            var importer = XBNFImporter
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .Build();

            Assert.IsNotNull(importer);
        }

        [TestMethod]
        public void ImportLanguage_Tests()
        {
            // get language string
            using var langDefStream = ResourceLoader.Load("SampleGrammar.Int1.xbnf");
            var langText = new StreamReader(langDefStream!).ReadToEnd();

            // build importer
            var importer = XBNFImporter
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .Build();

            // import
            var lang = importer.ImportLanguage(langText);
            Assert.IsNotNull(lang);
            Assert.AreEqual(0, lang.ProductionValidators.Count);
            Assert.AreEqual(5, lang.Grammar.ProductionCount);
        }
    }
}
