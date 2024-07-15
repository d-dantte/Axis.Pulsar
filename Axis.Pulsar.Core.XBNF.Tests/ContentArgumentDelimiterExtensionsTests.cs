using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF.Tests
{
    [TestClass]
    public class ContentArgumentDelimiterExtensionsTests
    {
        [TestMethod]
        public void DelimiterCharacter_Tests()
        {
            var result = ContentArgumentDelimiter.Quote.DelimiterCharacter();
            Assert.AreEqual('\'', result);

            result = ContentArgumentDelimiter.DoubleQuote.DelimiterCharacter();
            Assert.AreEqual('"', result);

            result = ContentArgumentDelimiter.Grave.DelimiterCharacter();
            Assert.AreEqual('`', result);

            result = ContentArgumentDelimiter.Sol.DelimiterCharacter();
            Assert.AreEqual('/', result);

            result = ContentArgumentDelimiter.BackSol.DelimiterCharacter();
            Assert.AreEqual('\\', result);

            result = ContentArgumentDelimiter.VerticalBar.DelimiterCharacter();
            Assert.AreEqual('|', result);

            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => ContentArgumentDelimiter.None.DelimiterCharacter());
        }

        [TestMethod]
        public void DelimiterType_Tests()
        {
            var result = '\''.DelimiterType();
            Assert.AreEqual(ContentArgumentDelimiter.Quote, result);

            result = '"'.DelimiterType();
            Assert.AreEqual(ContentArgumentDelimiter.DoubleQuote, result);

            result = '`'.DelimiterType();
            Assert.AreEqual(ContentArgumentDelimiter.Grave, result);

            result = '/'.DelimiterType();
            Assert.AreEqual(ContentArgumentDelimiter.Sol, result);

            result = '\\'.DelimiterType();
            Assert.AreEqual(ContentArgumentDelimiter.BackSol, result);

            result = '|'.DelimiterType();
            Assert.AreEqual(ContentArgumentDelimiter.VerticalBar, result);

            result = 'x'.DelimiterType();
            Assert.AreEqual(ContentArgumentDelimiter.None, result);
        }
    }
}
