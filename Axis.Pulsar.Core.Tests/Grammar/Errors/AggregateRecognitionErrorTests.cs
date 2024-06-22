using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Tests.Grammar.Errors
{
    [TestClass]
    public class AggregateRecognitionErrorTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            var nodeError = FailedRecognitionError.Of("symbol", 0);
            var node = ISymbolNode.Of("name", "tokens");
            var error = AggregateRecognitionError.Of(nodeError, node);
            var error2 = AggregateRecognitionError.Of(nodeError, new List<ISymbolNode> { node });

            Assert.IsNotNull(error2);
            Assert.IsNotNull(error);
            Assert.AreEqual(nodeError, error.Cause);
            Assert.AreEqual(1, error.RecognizedNodes.Length);
            Assert.AreEqual(1, error.RequiredNodeCount);
            Assert.AreEqual(0, default(AggregateRecognitionError).RequiredNodeCount);

            Assert.ThrowsException<ArgumentNullException>(
                () => new AggregateRecognitionError(null!, node));

            Assert.ThrowsException<ArgumentNullException>(
                () => new AggregateRecognitionError(nodeError, default(IEnumerable<ISymbolNode>)!));

            Assert.ThrowsException<InvalidOperationException>(
                () => new AggregateRecognitionError(nodeError, new ISymbolNode[] { null! }));
        }
    }
}
