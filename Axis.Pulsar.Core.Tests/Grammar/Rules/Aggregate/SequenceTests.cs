using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;
using Moq;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Rules.Aggregate;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules.Aggregate
{
    [TestClass]
    public class SequenceTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            var rule = SetupRule(
                NodeAggregationResult.Of(ISymbolNode.Of("a", "b")));
            var seq = new Sequence(rule);
            Assert.IsNotNull(seq);
            Assert.AreEqual(1, seq.Elements.Length);
            Assert.AreEqual(AggregationType.Sequence, seq.Type);

            Assert.ThrowsException<ArgumentNullException>(
                () => new Sequence((IAggregationElement[])null!));
            Assert.ThrowsException<ArgumentException>(
                () => new Sequence(Array.Empty<IAggregationElement>()));
            Assert.ThrowsException<InvalidOperationException>(
                () => Sequence.Of(null!, null!));
        }

        [TestMethod]
        public void TryRecognize_Tests()
        {
            // setup
            var passingElementMock = SetupRule(
                NodeAggregationResult.Of(ISymbolNode.Of("dummy", Tokens.Of("source"))));

            var passingOptionalElementMock = SetupRule(
                NodeAggregationResult.Of(
                    ISymbolNode.Of(AggregationType.Sequence, true, ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var unrecognizedElementMock = SetupRule(
                NodeAggregationResult.Of(
                    new AggregateRecognitionError(
                        FailedRecognitionError.Of(
                            SymbolPath.Of("bleh"),
                            10))));

            var partiallyRecognizedElementMock = SetupRule(
                NodeAggregationResult.Of(
                    new AggregateRecognitionError(
                        PartialRecognitionError.Of(
                            SymbolPath.Of("bleh"),
                            10,
                            3))));
        }

        [TestMethod]
        public void TryRecognize1_Tests()
        {
            var passingElementMock = SetupRule(
                NodeAggregationResult.Of(ISymbolNode.Of("dummy", Tokens.Of("source"))));

            var seq = Sequence.Of(
                passingElementMock,
                passingElementMock);
            var success = seq.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ISymbolNode agg));
            Assert.IsTrue(agg.Is(out ISymbolNode.Aggregate nseq));
            Assert.AreEqual(2, nseq.Nodes.Length);
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

            var seq = Sequence.Of(
                passingElementMock,
                unrecognizedElementMock);
            var success = seq.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out AggregateRecognitionError ge));
            Assert.IsInstanceOfType<FailedRecognitionError>(ge.Cause);
            Assert.AreEqual(1, ge.RecognizedNodes.Length);
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

            var seq = Sequence.Of(
                passingElementMock,
                partiallyRecognizedElementMock);
            var success = seq.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out AggregateRecognitionError ge));
            Assert.AreEqual(0, ge.RecognizedNodes.Length);
        }

        [TestMethod]
        public void TryRecognize4_Tests()
        {
            var passingElementMock = SetupRule(
                NodeAggregationResult.Of(ISymbolNode.Of("dummy", Tokens.Of("source"))));

            var passingOptionalElementMock = SetupRule(
                NodeAggregationResult.Of(
                    ISymbolNode.Of(AggregationType.Sequence, true, ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var seq = Sequence.Of(
                passingElementMock,
                passingOptionalElementMock,
                passingOptionalElementMock,
                passingElementMock);
            var success = seq.TryRecognize("dummy", "dummy", null!, out var result);
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

            var passingOptionalElementMock = SetupRule(
                NodeAggregationResult.Of(
                    ISymbolNode.Of(AggregationType.Sequence, true, ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var unrecognizedElementMock = SetupRule(
                NodeAggregationResult.Of(
                    new AggregateRecognitionError(
                        FailedRecognitionError.Of(SymbolPath.Of("bleh"), 10))));

            var seq = Sequence.Of(
                passingElementMock,
                passingOptionalElementMock,
                passingOptionalElementMock,
                passingElementMock,
                unrecognizedElementMock);
            var success = seq.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out AggregateRecognitionError ge));
            Assert.AreEqual(2, ge.RequiredNodeCount);
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
                    Console.WriteLine("invoked");
                    innerResult = result;
                    return innerResult.Is(out ISymbolNode _);
                }));

            return rule.Object;
        }
    }
}
