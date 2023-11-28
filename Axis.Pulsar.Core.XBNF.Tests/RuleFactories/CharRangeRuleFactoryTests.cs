namespace Axis.Pulsar.Core.XBNF.Tests.RuleFactories
{
    [TestClass]
    public class CharRangeRuleFactoryTests
    {
        [TestMethod]
        public void ParseRanges_Tests()
        {
            var ranges = CharRangeRuleFactory.ParseRanges("^\\n, ^\\x0d, \\s");
            var excludes = ranges.Excludes.ToArray();
            var includes = ranges.Includes.ToArray();
            Assert.AreEqual(2, excludes.Count());
            Assert.AreEqual('\n', excludes[0]);
            Assert.AreEqual('\r', excludes[1]);
            Assert.AreEqual(1, includes.Length);
            Assert.AreEqual(' ', includes[0]);
        }
    }

    [TestClass]
    public class RangesEscapeTransformerTests
    {
        [TestMethod]
        public void DecodeTests()
        {
            var transformer = new CharRangeRuleFactory.RangesEscapeTransformer();

            var decoded = transformer.Decode("a-z");
            Assert.AreEqual("a-z", decoded);

            decoded = transformer.Decode("a-\\^");
            Assert.AreEqual("a-^", decoded);

            decoded = transformer.Decode("\0-\\'");
            Assert.AreEqual("\0-'", decoded);

            decoded = transformer.Decode("\0-\\ ");
            Assert.AreEqual("\0-\\x20", decoded);
        }
    }
}
