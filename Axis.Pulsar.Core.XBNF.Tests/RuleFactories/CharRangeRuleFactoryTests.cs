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
            var unescaped = CharRangeRuleFactory.Unescape("a-z");
            Assert.AreEqual("a-z", unescaped);

            unescaped = CharRangeRuleFactory.Unescape("a-\\^");
            Assert.AreEqual("a-^", unescaped);

            unescaped = CharRangeRuleFactory.Unescape("\0-\\'");
            Assert.AreEqual("\0-'", unescaped);

            unescaped = CharRangeRuleFactory.Unescape("\0-\\ ");
            Assert.AreEqual("\0-\\x20", unescaped);
        }
    }
}
