using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.XBNF.Lang;

namespace Axis.Pulsar.Core.XBNF.Tests.E2E
{
    [TestClass]
    public class Int1Lang
    {
        private static ILanguageContext _lang;

        static Int1Lang()
        {
            // get language string
            using var langDefStream = ResourceLoader.Load("SampleGrammar.Int1.xbnf");
            var langText = new StreamReader(langDefStream!).ReadToEnd();

            // build importer
            var importer = XBNFImporter.Builder
                .NewBuilder()
                .WithDefaultAtomicRuleDefinitions()
                .Build();

            // import
            _lang = importer.ImportLanguage(langText);
        }

        [TestMethod]
        public void ValidRecognitionTests()
        {
            // regular int 45
            var result = _lang.Recognize("45");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDataResult(out var node));
            Assert.AreEqual("int", node.Name);
            Assert.IsTrue("45".Equals(node.Tokens));
            var regularInt = node.FindNodes("regular-int/@t<45>").ToArray();
            Assert.AreEqual(1, regularInt.Length);


            // regular int -0050145
            result = _lang.Recognize("-0050145");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDataResult(out node));
            Assert.AreEqual("int", node.Name);
            Assert.IsTrue("-0050145".Equals(node.Tokens));
            regularInt = node.FindNodes("regular-int/<-0050145>").ToArray();
            Assert.AreEqual(1, regularInt.Length);


            // null int null.int
            result = _lang.Recognize("null.int");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDataResult(out node));
            Assert.AreEqual("int", node.Name);
            Assert.IsTrue("null.int".Equals(node.Tokens));
            regularInt = node.FindNodes("null-int/@t<null.int>").ToArray();
            Assert.AreEqual(1, regularInt.Length);


            // binary int 0b00101_0001
            result = _lang.Recognize("0b00101_0001");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDataResult(out node));
            Assert.AreEqual("int", node.Name);
            Assert.IsTrue("0b00101_0001".Equals(node.Tokens));
            regularInt = node.FindNodes("binary-int/@t").ToArray();
            Assert.AreEqual(1, regularInt.Length);


            // hex int 0x3c2a
            result = _lang.Recognize("0x3c2a");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDataResult(out node));
            Assert.AreEqual("int", node.Name);
            Assert.IsTrue("0x3c2a".Equals(node.Tokens));
            regularInt = node.FindNodes("hex-int/<0x3c2a>").ToArray();
            Assert.AreEqual(1, regularInt.Length);

        }

        [TestMethod]
        public void InvaidRecognitionTests()
        {

        }
    }
}
