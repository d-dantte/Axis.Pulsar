using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using System.Xml.Linq;

namespace Axis.Pulsar.Core.Tests.Grammar.Results
{
    [TestClass]
    public class NodeRecognitionResultTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            var node = ISymbolNode.Of("atom", "tokens");
            var failedError = FailedRecognitionError.Of("symbol", 0);
            var partialError = PartialRecognitionError.Of("symbol", 0, 1);

            Assert.IsNotNull(NodeRecognitionResult.Of(node));
            Assert.IsNotNull(NodeRecognitionResult.Of(failedError));
            Assert.IsNotNull(NodeRecognitionResult.Of(partialError));
            Assert.ThrowsException<ArgumentNullException>
                (() => NodeRecognitionResult.Of(null!));
        }

        [TestMethod]
        public void Is_Tests()
        {
            var node = ISymbolNode.Of("atom", "tokens");
            var failedError = FailedRecognitionError.Of("symbol", 0);
            var partialError = PartialRecognitionError.Of("symbol", 0, 1);

            var nodeResult = NodeRecognitionResult.Of(node);
            var freResult = NodeRecognitionResult.Of(failedError);
            var preResult = NodeRecognitionResult.Of(partialError);
            var nullResult = default(NodeRecognitionResult);

            Assert.IsTrue(nodeResult.Is(out ISymbolNode n));
            Assert.IsFalse(nodeResult.Is(out FailedRecognitionError fre));
            Assert.IsFalse(nodeResult.Is(out PartialRecognitionError pre));
            Assert.IsFalse(nodeResult.IsNull());

            Assert.IsTrue(freResult.Is(out fre));
            Assert.IsFalse(freResult.Is(out pre));

            Assert.IsTrue(preResult.Is(out pre));
            Assert.IsFalse(preResult.Is(out n));

            Assert.IsTrue(nullResult.IsNull());
        }

        [TestMethod]
        public void MapMatch_Tests()
        {
            var node = ISymbolNode.Of("atom", "tokens");
            var failedError = FailedRecognitionError.Of("symbol", 0);
            var partialError = PartialRecognitionError.Of("symbol", 0, 1);

            var nodeResult = NodeRecognitionResult.Of(node);
            var freResult = NodeRecognitionResult.Of(failedError);
            var preResult = NodeRecognitionResult.Of(partialError);
            var nullResult = default(NodeRecognitionResult);

            var result = nodeResult.MapMatch(
                s => "1",
                f => "2",
                f => "3",
                () => "4");
            Assert.AreEqual("1", result);

            result = freResult.MapMatch(
                s => "1",
                f => "2",
                f => "3",
                () => "4");
            Assert.AreEqual("2", result);

            result = preResult.MapMatch(
                s => "1",
                f => "2",
                f => "3",
                () => "4");
            Assert.AreEqual("3", result);

            result = nullResult.MapMatch(
                s => "1",
                f => "2",
                f => "3",
                () => "4");
            Assert.AreEqual("4", result);

            result = nullResult.MapMatch(
                s => "1",
                f => "2",
                f => "3");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Map_Tests()
        {
            var node = ISymbolNode.Of("atom", "tokens");
            var nodeResult = NodeRecognitionResult.Of(node);

            Assert.AreEqual(
                "abc",
                nodeResult.Map((ISymbolNode node) => "abc"));

            Assert.ThrowsException<InvalidOperationException>(
                () => nodeResult.Map((FailedRecognitionError fre) => "abc"));
        }

        [TestMethod]
        public void ConsumeMatch_Tests()
        {
            var node = ISymbolNode.Of("atom", "tokens");
            var failedError = FailedRecognitionError.Of("symbol", 0);
            var partialError = PartialRecognitionError.Of("symbol", 0, 1);

            var nodeResult = NodeRecognitionResult.Of(node);
            var freResult = NodeRecognitionResult.Of(failedError);
            var preResult = NodeRecognitionResult.Of(partialError);
            var nullResult = default(NodeRecognitionResult);
            string? result = null;

            nodeResult.ConsumeMatch(
                s => result = "1",
                f => result = "2",
                f => result = "3",
                () => result = "4");
            Assert.AreEqual("1", result);

            freResult.ConsumeMatch(
                s => result = "1",
                f => result = "2",
                f => result = "3",
                () => result = "4");
            Assert.AreEqual("2", result);

            preResult.ConsumeMatch(
                s => result = "1",
                f => result = "2",
                f => result = "3",
                () => result = "4");
            Assert.AreEqual("3", result);

            nullResult.ConsumeMatch(
                s => result = "1",
                f => result = "2",
                f => result = "3",
                () => result = "4");
            Assert.AreEqual("4", result);

            nullResult.ConsumeMatch(
                s => result = "1",
                f => result = "2",
                f => result = "3");
        }

        [TestMethod]
        public void Consume_Tests()
        {
            var node = ISymbolNode.Of("atom", "tokens");
            var nodeResult = NodeRecognitionResult.Of(node);
            string? result = null;

            nodeResult.Consume((ISymbolNode node) => result = "abc");
            Assert.AreEqual("abc", result);

            Assert.ThrowsException<InvalidOperationException>(
                () => nodeResult.Consume((FailedRecognitionError fre) => result = "abc"));
        }

        [TestMethod]
        public void WithMatch_Tests()
        {
            var node = ISymbolNode.Of("atom", "tokens");
            var failedError = FailedRecognitionError.Of("symbol", 0);
            var partialError = PartialRecognitionError.Of("symbol", 0, 1);

            var nodeResult = NodeRecognitionResult.Of(node);
            var freResult = NodeRecognitionResult.Of(failedError);
            var preResult = NodeRecognitionResult.Of(partialError);
            var nullResult = default(NodeRecognitionResult);
            string? result = null;

            nodeResult.WithMatch(
                s => result = "1",
                f => result = "2",
                f => result = "3",
                () => result = "4");
            Assert.AreEqual("1", result);

            freResult.WithMatch(
                s => result = "1",
                f => result = "2",
                f => result = "3",
                () => result = "4");
            Assert.AreEqual("2", result);

            preResult.WithMatch(
                s => result = "1",
                f => result = "2",
                f => result = "3",
                () => result = "4");
            Assert.AreEqual("3", result);

            nullResult.WithMatch(
                s => result = "1",
                f => result = "2",
                f => result = "3",
                () => result = "4");
            Assert.AreEqual("4", result);

            nullResult.WithMatch(
                s => result = "1",
                f => result = "2",
                f => result = "3");
        }

        [TestMethod]
        public void With_Tests()
        {
            var node = ISymbolNode.Of("atom", "tokens");
            var nodeResult = NodeRecognitionResult.Of(node);
            string? result = null;

            nodeResult.With((ISymbolNode node) => result = "abc");
            Assert.AreEqual("abc", result);

            Assert.ThrowsException<InvalidOperationException>(
                () => nodeResult.With((FailedRecognitionError fre) => result = "abc"));
        }

        [TestMethod]
        public void Get_Tests()
        {
            var node = ISymbolNode.Of("atom", "tokens");
            var nodeResult = NodeRecognitionResult.Of(node);

            var result = nodeResult.Get<ISymbolNode>();
            Assert.AreEqual(node, result);

            Assert.ThrowsException<InvalidOperationException>(
                () => nodeResult.Get<FailedRecognitionError>());
        }
    }
}
