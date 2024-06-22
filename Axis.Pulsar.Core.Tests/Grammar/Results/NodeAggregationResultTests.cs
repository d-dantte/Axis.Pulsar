using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;

namespace Axis.Pulsar.Core.Tests.Grammar.Results
{
    [TestClass]
    public class NodeAggregationResultTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            var node = ISymbolNode.Of("atom", "tokens");
            var failedError = FailedRecognitionError.Of("symbol", 0);
            var aggregateError = AggregateRecognitionError.Of(
                failedError,
                node);

            Assert.IsNotNull(NodeAggregationResult.Of(node));
            Assert.IsNotNull(NodeAggregationResult.Of(aggregateError));
            Assert.ThrowsException < ArgumentNullException>(
                () => NodeAggregationResult.Of(null!));
        }

        [TestMethod]
        public void Is_Tests()
        {
            var node = ISymbolNode.Of("atom", "tokens");
            var failedError = FailedRecognitionError.Of("symbol", 0);
            var aggregateError = AggregateRecognitionError.Of(
                failedError,
                node);

            var result = NodeAggregationResult.Of(node);
            Assert.IsTrue(result.Is(out ISymbolNode n));
            Assert.IsFalse(result.Is(out AggregateRecognitionError e));
            Assert.IsFalse(result.IsNull());

            result = NodeAggregationResult.Of(aggregateError);
            Assert.IsFalse(result.Is(out n));
            Assert.IsTrue(result.Is(out e));

            result = default;
            Assert.IsTrue(result.IsNull());
        }

        [TestMethod]
        public void MapMatch_Tests()
        {
            var node = ISymbolNode.Of("atom", "tokens");
            var failedError = FailedRecognitionError.Of("symbol", 0);
            var aggregateError = AggregateRecognitionError.Of(
                failedError,
                node);

            var nodeResult = NodeAggregationResult.Of(node);
            var errorResult = NodeAggregationResult.Of(aggregateError);
            var nullResult = default(NodeAggregationResult);

            var result = nodeResult.MapMatch(
                s => "1",
                f => "2",
                () => "3");
            Assert.AreEqual("1", result);

            result = errorResult.MapMatch(
                s => "1",
                f => "2",
                () => "3");
            Assert.AreEqual("2", result);

            result = nullResult.MapMatch(
                s => "1",
                f => "2",
                () => "3");
            Assert.AreEqual("3", result);

            result = nullResult.MapMatch(
                s => "1",
                f => "2");
            Assert.IsNull(result);

            Assert.AreEqual(node, nodeResult.Get<ISymbolNode>());
            Assert.ThrowsException<InvalidOperationException>(
                () => nodeResult.Get<AggregateRecognitionError>());

            Assert.AreEqual("abc", nodeResult.Map((ISymbolNode _) => "abc"));
            Assert.ThrowsException<InvalidOperationException>(
                () => nodeResult.Map((AggregateRecognitionError _) => "123"));
        }

        [TestMethod]
        public void Consume_Tests()
        {
            var node = ISymbolNode.Of("atom", "tokens");
            var failedError = FailedRecognitionError.Of("symbol", 0);
            var aggregateError = AggregateRecognitionError.Of(
                failedError,
                node);

            var nodeResult = NodeAggregationResult.Of(node);
            var errorResult = NodeAggregationResult.Of(aggregateError);
            var nullResult = default(NodeAggregationResult);
            string? result = null;

            nodeResult.ConsumeMatch(
                s => result = "1",
                f => result = "2",
                () => result = "3");
            Assert.AreEqual("1", result);

            errorResult.ConsumeMatch(
                s => result = "1",
                f => result = "2",
                () => result = "3");
            Assert.AreEqual("2", result);

            nullResult.ConsumeMatch(
               s => result = "1",
               f => result = "2",
               () => result = "3");
            Assert.AreEqual("3", result);

            nullResult.ConsumeMatch(
               s => result = "1",
               f => result = "2");

            nodeResult.Consume((ISymbolNode _) => result = "abc");
            Assert.AreEqual("abc", result);
            Assert.ThrowsException<InvalidOperationException>(
                () => nodeResult.Consume((AggregateRecognitionError _) => result = "123"));
        }

        [TestMethod]
        public void With_Tests()
        {
            var node = ISymbolNode.Of("atom", "tokens");
            var failedError = FailedRecognitionError.Of("symbol", 0);
            var aggregateError = AggregateRecognitionError.Of(
                failedError,
                node);

            var nREsult = NodeAggregationResult.Of(node);
            var aResult = NodeAggregationResult.Of(aggregateError);
            var nResult = default(NodeAggregationResult);
            string? result = null;

            _ = nREsult.WithMatch(
                s => result = "1",
                f => result = "2",
                () => result = "3");
            Assert.AreEqual("1", result);

            _ = aResult.WithMatch(
                s => result = "1",
                f => result = "2",
                () => result = "3");
            Assert.AreEqual("2", result);

            _ = nResult.WithMatch(
               s => result = "1",
               f => result = "2",
               () => result = "3");
            Assert.AreEqual("3", result);

            _ = nResult.WithMatch(
               s => result = "1",
               f => result = "2");
        }
    }
}
