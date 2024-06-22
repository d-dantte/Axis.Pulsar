using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Rules.Aggregate;
using Moq;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules.Aggregate
{
    [TestClass]
    public class SetTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            var rule = SetupRule(
                NodeAggregationResult.Of(ISymbolNode.Of("a", "b")));
            var set = new Set(rule);
            Assert.IsNotNull(set);
            Assert.AreEqual(1, set.Elements.Length);
            Assert.AreEqual(AggregationType.Set, set.Type);

            Assert.ThrowsException<ArgumentNullException>(
                () => new Set((IAggregationElement[])null!));
            Assert.ThrowsException<ArgumentException>(
                () => new Set(Array.Empty<IAggregationElement>()));
            Assert.ThrowsException<InvalidOperationException>(
                () => Set.Of(null!, null!));
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => new Set(0, rule));
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => new Set(2, rule));
        }

        [TestMethod]
        public void TryRecognize_Tests()
        {
            // setup
            var passingElementMock = SetupRule(
                NodeAggregationResult.Of(ISymbolNode.Of("dummy", Tokens.Of("source"))));

            var passingOptionalElementMock = SetupRule(
                NodeAggregationResult.Of(
                    ISymbolNode.Of(AggregationType.Set, true, ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var unrecognizedElementMock = SetupRule(
                NodeAggregationResult.Of(
                    new AggregateRecognitionError(
                        FailedRecognitionError.Of(SymbolPath.Of("bleh"), 10))));

            var partiallyRecognizedElementMock = SetupRule(
                NodeAggregationResult.Of(
                    new AggregateRecognitionError(
                        PartialRecognitionError.Of(SymbolPath.Of("bleh"), 10, 3))));
        }

        [TestMethod]
        public void TryRecognize1_Tests()
        {
            var passingElementMock = SetupRule(
                NodeAggregationResult.Of(ISymbolNode.Of("dummy", Tokens.Of("source"))));

            var set = Set.Of(
                passingElementMock,
                passingElementMock);
            var success = set.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ISymbolNode node));
            Assert.IsTrue(node.Is(out ISymbolNode.Aggregate agg));
            Assert.AreEqual(2, agg.Nodes.Length);
        }

        [TestMethod]
        public void TryRecognize2_Tests()
        {
            var passingElementMock = SetupRule(
                NodeAggregationResult.Of(ISymbolNode.Of("dummy", Tokens.Of("source"))));

            var unrecognizedElementMock = SetupRule(
                NodeAggregationResult.Of(
                    new AggregateRecognitionError(
                        FailedRecognitionError.Of(SymbolPath.Of("bleh"), 10))));

            var set = Set.Of(
                1,
                passingElementMock,
                unrecognizedElementMock);
            var success = set.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ISymbolNode node));
            Assert.IsTrue(node.Is(out ISymbolNode.Aggregate agg));
            Assert.AreEqual(1, agg.Nodes.Length);
        }

        [TestMethod]
        public void TryRecognize3_Tests()
        {
            var passingElementMock = SetupRule(
                NodeAggregationResult.Of(ISymbolNode.Of("dummy", Tokens.Of("source"))));

            var partiallyRecognizedElementMock = SetupRule(
                NodeAggregationResult.Of(
                    new AggregateRecognitionError(
                        PartialRecognitionError.Of(SymbolPath.Of("bleh"), 10, 3))));

            var set = Set.Of(
                passingElementMock,
                partiallyRecognizedElementMock);
            var success = set.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out AggregateRecognitionError ge));
            Assert.IsInstanceOfType<PartialRecognitionError>(ge.Cause);
        }

        [TestMethod]
        public void TryRecognize4_Tests()
        {
            var passingElementMock = SetupRule(
                NodeAggregationResult.Of(ISymbolNode.Of("dummy", Tokens.Of("source"))));

            var passingOptionalElementMock = SetupRule(
                NodeAggregationResult.Of(
                    ISymbolNode.Of(AggregationType.Set, true, ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var set = Set.Of(
                passingElementMock,
                passingOptionalElementMock,
                passingOptionalElementMock,
                passingElementMock);
            var success = set.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ISymbolNode agg));
            Assert.IsTrue(agg.Is(out ISymbolNode.Aggregate nseq));
            Assert.IsFalse(nseq.IsOptional);
            Assert.AreEqual(4, nseq.Nodes.Length);
            Assert.AreEqual(2, nseq.RequiredNodeCount());
        }

        [TestMethod]
        public void TryRecognize5_Tests()
        {
            var passingElementMock = SetupRule(
                NodeAggregationResult.Of(ISymbolNode.Of("dummy", Tokens.Of("source"))));

            var unrecognizedElementMock = SetupRule(
                NodeAggregationResult.Of(
                    new AggregateRecognitionError(
                        FailedRecognitionError.Of(SymbolPath.Of("bleh"), 10))));

            var set = Set.Of(
                3,
                passingElementMock,
                passingElementMock,
                unrecognizedElementMock,
                unrecognizedElementMock);
            var success = set.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out AggregateRecognitionError are));
        }

        private IAggregationElement SetupRule(
            NodeAggregationResult result)
        {
            var rule = new Mock<IAggregationElement>();
            rule.Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<NodeAggregationResult>.IsAny))
                .Returns(new AggregateRecognition((
                    TokenReader reader,
                    SymbolPath path,
                    ILanguageContext cxt,
                    out NodeAggregationResult innerResult) =>
                {
                    innerResult = result;
                    return innerResult.Is(out ISymbolNode _);
                }));

            return rule.Object;
        }
    }
}
