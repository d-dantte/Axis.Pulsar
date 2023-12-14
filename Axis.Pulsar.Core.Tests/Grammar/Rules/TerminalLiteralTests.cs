using Axis.Pulsar.Core.Grammar.Nodes;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;
using Axis.Luna.Common.Results;

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
                ProductionPath.Of("dummy-path"),
                null!,
                out var result);

            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var node = result.Resolve();
            Assert.AreEqual("t", node.Name);
            Assert.AreEqual(Tokens.Of("stuff"), node.Tokens);

            success = literal.TryRecognize(
                "not-stuff",
                ProductionPath.Of("dummy-path"),
                null!,
                out result);

            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out FailedRecognitionError ute));
        }
    }
}
