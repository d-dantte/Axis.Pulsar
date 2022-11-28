using Axis.Pulsar.Grammar.Language;
    
namespace Axis.Pusar.Grammar.Tests.Language
{
    using PatternMatchType = Pulsar.Grammar.Language.MatchType;

    [TestClass]
    public class MatchTypeTest
    {
        [TestMethod]
        public void Of_WithValidArgs_ReturnsCorrectInstance()
        {
            var matchType = PatternMatchType.Of(1, true);
            Assert.IsTrue(matchType is PatternMatchType.Open);

            matchType = PatternMatchType.Of(2, 2);
            Assert.IsTrue(matchType is PatternMatchType.Closed);
        }

        [TestMethod]
        public void MatchType_WithInvalidCtorArgs_ThrowsExceptions()
        {
            Assert.ThrowsException<ArgumentException>(() => new PatternMatchType.Open(0, true));
            Assert.ThrowsException<ArgumentException>(() => new PatternMatchType.Open(-1, true));
            Assert.ThrowsException<ArgumentException>(() => new PatternMatchType.Closed(0, 1));
            Assert.ThrowsException<ArgumentException>(() => new PatternMatchType.Closed(1, 0));
            Assert.ThrowsException<ArgumentException>(() => new PatternMatchType.Closed(4, 3));
        }

        [TestMethod]
        public void PropertyTests()
        {
            var open = new PatternMatchType.Open(3, false);
            Assert.AreEqual(3, open.MaxMismatch);
            Assert.AreEqual(false, open.AllowsEmptyTokens);

            var closed = new PatternMatchType.Closed(2, 12);
            Assert.AreEqual(2, closed.MinMatch);
            Assert.AreEqual(12, closed.MaxMatch);
        }
    }
}
