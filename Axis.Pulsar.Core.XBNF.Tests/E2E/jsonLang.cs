using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Core.XBNF.Lang;

namespace Axis.Pulsar.Core.XBNF.Tests.E2E
{
    [TestClass]
    public class JsonLang
    {
        private static ILanguageContext _lang;

        static JsonLang()
        {
            try
            {
                // get language string
                using var langDefStream = ResourceLoader.Load("SampleGrammar.json.xbnf");
                var langText = new StreamReader(langDefStream!).ReadToEnd();

                // build importer
                var importer = XBNFImporter.Builder
                    .NewBuilder()
                    .WithDefaultAtomicRuleDefinitions()
                    .Build();

                // import
                _lang = importer.ImportLanguage(langText);
            }
            catch(Exception ex)
            {
                _lang = null!;
                ex.Throw();
            }
        }

        [TestMethod]
        public void ValidRecognitionTests()
        {
            // {
            //    // comment
            // }
            var result = _lang.Recognize("{\n    // comment\n}");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDataResult(out var node));
            Assert.AreEqual("json", node.Name);
            var obj = node.FindNodes("json-object/<{>").ToArray();
            Assert.AreEqual(1, obj.Length);

            // {}
            result = _lang.Recognize("{}");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDataResult(out node));
            Assert.AreEqual("json", node.Name);
            Assert.IsTrue("{}".Equals(node.Tokens));
            obj = node.FindNodes("json-object/<{>").ToArray();
            Assert.AreEqual(1, obj.Length);

            // {"abc": 123, "bleh": true}
            string input = "{\"abc\": 123, \"bleh\": true}";
            result = _lang.Recognize(input);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDataResult(out node));
            Assert.AreEqual("json", node.Name);
            obj = node.FindNodes("json-object/property/json-string/<\"bleh\">").ToArray();
            Assert.AreEqual(1, obj.Length);

            // {"abc": 123, "bleh": [true, {}, null, "me"]}
            input = "{\"abc\": 123, \"bleh\": [true, {}, null, \"me\"]}";
            result = _lang.Recognize(input);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsDataResult(out node));
            Assert.AreEqual("json", node.Name);
            Assert.AreEqual(1, obj.Length);

        }

        [TestMethod]
        public void List_Tests()
        {
            TokenReader input = "[true, {}, \"me\", null]";
            var success = _lang.Grammar["json-list"].TryProcessRule(
                input,
                "parent",
                _lang,
                out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());

        }

        [TestMethod]
        public void Object_Tests()
        {
            //TokenReader input = "{\"abc\": 123,\n \"bleh\": true}";
            // this gives a partial recognition error when $scientific-decimal has a threshold of 2. The correct threshold is 4, however, the the partial recognition error
            // should report that it originates from the $scientific-decimal, not the $json-object. Investigate why this is happening - someone is possibly consuming the partial errors
            TokenReader input = "{\r\n    \"this\": \"is\",\r\n    \"json\": [\r\n        \"at\",\r\n        {\r\n            \"its\": true,\r\n            \"finest\": 10.5,\r\n            \"times\": []\r\n        }\r\n    ]\r\n}";
            var success = _lang.Grammar["json-object"].TryProcessRule(
                input,
                "parent",
                _lang,
                out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(1, result.Resolve().FindNodes("property/json-value/json-number/int/regular-int/<123>").ToArray().Length);
            Assert.AreEqual(4, result.Resolve().FindNodes("property/json-value|json-string").ToArray().Length);

        }

        [TestMethod]
        public void Property_Tests()
        {
            var success = _lang.Grammar["property"].TryProcessRule(
                @"""abc"": 123,   ",
                "parent",
                _lang,
                out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(1, result.Resolve().FindNodes("json-value/json-number/int/regular-int/<123>").ToArray().Length);
            Assert.AreEqual(2, result.Resolve().FindNodes("json-value|json-string").ToArray().Length);

        }

        [TestMethod]
        public void Number_Tests()
        {
            var success = _lang.Grammar["json-number"].TryProcessRule(
                @"123",
                "parent",
                _lang,
                out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(1, result.Resolve().FindNodes("int/regular-int/<123>").ToArray().Length);


            success = _lang.Grammar["json-number"].TryProcessRule(
                @"123    ",
                "parent",
                _lang,
                out result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(1, result.Resolve().FindNodes("int/regular-int/<123>").ToArray().Length);
        }

        [TestMethod]
        public void InvaidRecognitionTests()
        {

        }
    }
}
