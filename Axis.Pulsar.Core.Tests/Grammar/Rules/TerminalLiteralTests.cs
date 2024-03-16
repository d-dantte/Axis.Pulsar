using Axis.Pulsar.Core.Grammar.Atomic;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules
{
    [TestClass]
    public class TerminalLiteralTests
    {
        [TestMethod]
        public void TryRecognize_Tests()
        {
            var literal = new TerminalLiteral("t", "stuff");
            var success = literal.TryRecognize(
                "stuff",
                SymbolPath.Of("dummy-path"),
                null!,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ICSTNode node));
            Assert.AreEqual("t", node.Symbol);
            Assert.AreEqual(Tokens.Of("stuff"), node.Tokens);

            success = literal.TryRecognize(
                "not-stuff",
                SymbolPath.Of("dummy-path"),
                null!,
                out result);

            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out FailedRecognitionError ute));
        }
    }
}
