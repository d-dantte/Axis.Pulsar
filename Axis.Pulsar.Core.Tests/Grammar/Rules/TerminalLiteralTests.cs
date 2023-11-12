using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;
using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules
{
    [TestClass]
    public class TerminalLiteralTests
    {
        [TestMethod]
        public void TryRecognize_Tests()
        {
            var literal = new TerminalLiteral("stuff");
            var success = literal.TryRecognize(
                "stuff",
                ProductionPath.Of("dummy-path"),
                null!,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var node = result.Resolve();
            Assert.AreEqual("dummy-path", node.Name);
            Assert.AreEqual(Tokens.Of("stuff"), node.Tokens);

            success = literal.TryRecognize(
                "not-stuff",
                ProductionPath.Of("dummy-path"),
                null!,
                out result);

            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out UnrecognizedTokens ute));
        }
    }
}
