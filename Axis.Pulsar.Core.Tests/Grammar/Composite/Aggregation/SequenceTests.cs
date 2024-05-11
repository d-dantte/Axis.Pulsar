using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;
using Moq;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Composite.Group;

namespace Axis.Pulsar.Core.Tests.Grammar.Composite.Groups
{
    [TestClass]
    public class SequenceTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            var rule = SetupRule(
                Cardinality.OccursOnlyOnce(),
                SymbolAggregationResult.Of(ISymbolNodeAggregation.Of(ISymbolNode.Of("a", "b"))));
            var cardinality = Cardinality.OccursOnlyOnce();
            var seq = new Sequence(cardinality, rule);
            Assert.IsNotNull(seq);
            Assert.AreEqual(cardinality, seq.Cardinality);
            Assert.AreEqual(1, seq.Elements.Length);

            Assert.ThrowsException<ArgumentNullException>(
                () => new Sequence(cardinality, (IAggregationElementRule[])null!));
            Assert.ThrowsException<ArgumentException>(
                () => new Sequence(cardinality, Array.Empty<IAggregationElementRule>()));
            Assert.ThrowsException<InvalidOperationException>(
                () => Sequence.Of(cardinality, null!, null!));
        }

        [TestMethod]
        public void TryRecognize_Tests()
        {
            // setup
            var passingElementMock = SetupRule(
                Cardinality.OccursOnlyOnce(),
                SymbolAggregationResult.Of(ISymbolNodeAggregation.Of(ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var passingOptionalElementMock = SetupRule(
                Cardinality.OccursOnlyOnce(),
                SymbolAggregationResult.Of(
                    ISymbolNodeAggregation.Of(true, ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var unrecognizedElementMock = SetupRule(
                Cardinality.OccursOnlyOnce(),
                SymbolAggregationResult.Of(
                    new SymbolAggregationError(
                        elementCount: 0,
                        cause: FailedRecognitionError.Of(
                            SymbolPath.Of("bleh"),
                            10))));

            var partiallyRecognizedElementMock = SetupRule(
                Cardinality.OccursOnlyOnce(),
                SymbolAggregationResult.Of(
                    new SymbolAggregationError(
                        elementCount: 0,
                        cause: PartialRecognitionError.Of(
                            SymbolPath.Of("bleh"),
                            10,
                            3))));
        }

        [TestMethod]
        public void TryRecognize1_Tests()
        {
            var passingElementMock = SetupRule(
                Cardinality.OccursOnlyOnce(),
                SymbolAggregationResult.Of(ISymbolNodeAggregation.Of(ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var seq = Sequence.Of(
                Cardinality.OccursOnly(1),
                passingElementMock,
                passingElementMock);
            var success = seq.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ISymbolNodeAggregation agg));
            Assert.IsTrue(agg.Is(out ISymbolNodeAggregation.Sequence nseq));
            Assert.AreEqual(2, nseq.Count);
        }

        [TestMethod]
        public void TryRecognize2_Tests()
        {
            var passingElementMock = SetupRule(
                Cardinality.OccursOnlyOnce(),
                SymbolAggregationResult.Of(ISymbolNodeAggregation.Of(ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var unrecognizedElementMock = SetupRule(
                Cardinality.OccursOnlyOnce(),
                SymbolAggregationResult.Of(
                    new SymbolAggregationError(
                        elementCount: 0,
                        cause: FailedRecognitionError.Of(
                            SymbolPath.Of("bleh"),
                            10))));

            var seq = Sequence.Of(
                Cardinality.OccursOnly(1),
                passingElementMock,
                unrecognizedElementMock);
            var success = seq.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out SymbolAggregationError ge));
            Assert.IsInstanceOfType<FailedRecognitionError>(ge.Cause);
            Assert.AreEqual(1, ge.ElementCount);
        }

        [TestMethod]
        public void TryRecognize3_Tests()
        {
            var passingElementMock = SetupRule(
                Cardinality.OccursOnlyOnce(),
                SymbolAggregationResult.Of(ISymbolNodeAggregation.Of(ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var partiallyRecognizedElementMock = SetupRule(
                Cardinality.OccursOnlyOnce(),
                SymbolAggregationResult.Of(
                    new SymbolAggregationError(
                        elementCount: 0,
                        cause: PartialRecognitionError.Of(
                            SymbolPath.Of("bleh"),
                            10,
                            3))));

            var seq = Sequence.Of(
                Cardinality.OccursOnly(1),
                passingElementMock,
                partiallyRecognizedElementMock);
            var success = seq.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out SymbolAggregationError ge));
            Assert.AreEqual(1, ge.ElementCount);
        }

        [TestMethod]
        public void TryRecognize4_Tests()
        {
            var passingElementMock = SetupRule(
                Cardinality.OccursOnlyOnce(),
                SymbolAggregationResult.Of(ISymbolNodeAggregation.Of(ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var passingOptionalElementMock = SetupRule(
                Cardinality.OccursOnlyOnce(),
                SymbolAggregationResult.Of(
                    ISymbolNodeAggregation.Of(true,  ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var seq = Sequence.Of(
                Cardinality.OccursOnly(1),
                passingElementMock,
                passingOptionalElementMock,
                passingOptionalElementMock,
                passingElementMock);
            var success = seq.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ISymbolNodeAggregation agg));
            Assert.IsTrue(agg.Is(out ISymbolNodeAggregation.Sequence nseq));
            Assert.IsFalse(nseq.IsOptional);
            Assert.AreEqual(4, nseq.Count);
            Assert.AreEqual(2, nseq.RequiredNodeCount());
        }

        [TestMethod]
        public void TryRecognize5_Tests()
        {
            var passingElementMock = SetupRule(
                Cardinality.OccursOnlyOnce(),
                SymbolAggregationResult.Of(ISymbolNodeAggregation.Of(ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var passingOptionalElementMock = SetupRule(
                Cardinality.OccursOnlyOnce(),
                SymbolAggregationResult.Of(
                    ISymbolNodeAggregation.Of(true, ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var unrecognizedElementMock = SetupRule(
                Cardinality.OccursOnlyOnce(),
                SymbolAggregationResult.Of(
                    new SymbolAggregationError(
                        elementCount: 0,
                        cause: FailedRecognitionError.Of(
                            SymbolPath.Of("bleh"),
                            10))));

            var seq = Sequence.Of(
                Cardinality.OccursOnly(1),
                passingElementMock,
                passingOptionalElementMock,
                passingOptionalElementMock,
                passingElementMock,
                unrecognizedElementMock);
            var success = seq.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out SymbolAggregationError ge));
            Assert.AreEqual(2, ge.ElementCount);
        }

        private IAggregationElementRule SetupRule(
            Cardinality cardinality,
            SymbolAggregationResult result)
        {
            var rule = new Mock<IAggregationElementRule>();
            rule.Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<SymbolAggregationResult>.IsAny))
                .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        SymbolPath path,
                        ILanguageContext cxt,
                        out SymbolAggregationResult innerResult) =>
                {
                    Console.WriteLine("invoked");
                    innerResult = result;
                    return innerResult.Is(out ISymbolNodeAggregation _);
                }));

            rule.Setup(m => m.Cardinality).Returns(cardinality);

            return rule.Object;
        }
    }
}
