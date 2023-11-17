namespace Axis.Pulsar.Core.XBNF.Tests.RuleFactories
{
    public class CharRangeRuleFactoryTests
    {

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
