using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Atomic;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Tests.Grammar.Atomic
{
    [TestClass]
    public class TerminalLiteralTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var literal = new TerminalLiteral("id", "abcd", true);
            Assert.IsNotNull(literal);
            Assert.AreEqual("id", literal.Id);
            Assert.IsTrue(literal.IsCaseSensitive);

            literal = TerminalLiteral.Of("id", "abcd", true);
            Assert.IsNotNull(literal);
            Assert.AreEqual("id", literal.Id);
            Assert.IsTrue(literal.IsCaseSensitive);

            literal = new TerminalLiteral("id", "abcd");
            Assert.IsNotNull(literal);
            Assert.AreEqual("id", literal.Id);
            Assert.IsTrue(literal.IsCaseSensitive);

            Assert.ThrowsException<ArgumentNullException>(
                () => new TerminalLiteral("id", null!, false));

            Assert.ThrowsException<ArgumentException>(
                () => new TerminalLiteral("...", "abcd", false));
        }

        [TestMethod]
        public void TryRecognize_Tests()
        {
            var literal = new TerminalLiteral("id", "abcd", true);

            Assert.ThrowsException<ArgumentNullException>(
                () => literal.TryRecognize(null!, "abcd", null!, out _));

            var recognized = literal.TryRecognize("abcd", "abcd", null!, out var result);
            Assert.IsTrue(recognized);
            Assert.IsTrue(result.Is(out ISymbolNode node));
            Assert.AreEqual<Tokens>("abcd", node.Tokens);

            recognized = literal.TryRecognize("abce", "abcd", null!, out result);
            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out FailedRecognitionError fre));
            Assert.AreEqual(0, fre.TokenSegment.Offset);

            recognized = literal.TryRecognize("abc", "abcd", null!, out result);
            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out fre));
            Assert.AreEqual(0, fre.TokenSegment.Offset);
        }
    }
}
