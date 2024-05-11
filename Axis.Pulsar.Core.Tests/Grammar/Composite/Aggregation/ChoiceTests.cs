using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;
using Moq;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Composite.Group;
using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Tests.Grammar.Composite.Groups
{
    [TestClass]
    public class ChoiceTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var choice = new Choice(
                Cardinality.OccursOnlyOnce(),
                SetupRule(
                    Cardinality.OccursOnlyOnce(),
                    SymbolAggregationResult.Of(
                    ISymbolNodeAggregation.Of(ISymbolNode.Of("dummy", Tokens.Of("source"))))));
            Assert.IsNotNull(choice);
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), choice.Cardinality);
            Assert.AreEqual(1, choice.Elements.Length);

            Assert.ThrowsException<ArgumentNullException>(
                () => Choice.Of(Cardinality.OccursOnlyOnce(), (IAggregationElementRule[])null!));

            Assert.ThrowsException<ArgumentException>(
                () => new Choice(Cardinality.OccursOnlyOnce()));

            Assert.ThrowsException<ArgumentException>(
                () => new Choice(Cardinality.OccursOnlyOnce(), null!, null!));
        }

        [TestMethod]
        public void TryRecognize1_Tests()
        {
            var cardinality = Cardinality.OccursOnly(1);
            var passingElementMock = SetupRule(
                cardinality,
                SymbolAggregationResult.Of(
                    ISymbolNodeAggregation.Of(ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var passingOptionalElementMock = SetupRule(
                Cardinality.OccursNeverOrAtMost(2),
                SymbolAggregationResult.Of(
                    ISymbolNodeAggregation.Of(true, ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var ch = Choice.Of(
                Cardinality.OccursOptionally(),
                passingOptionalElementMock,
                passingElementMock);
            var success = ch.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ISymbolNodeAggregation agg));
            Assert.IsTrue(agg.Is(out ISymbolNodeAggregation.Sequence nseq));
            Assert.AreEqual(2, nseq.Count);
        }

        [TestMethod]
        public void TryRecognize2_Tests()
        {
            var cardinality = Cardinality.OccursOnly(1);
            var passingElementMock = SetupRule(
                cardinality,
                SymbolAggregationResult.Of(
                    ISymbolNodeAggregation.Of(ISymbolNode.Of("dummy2", Tokens.Of("source2")))));

            var ch = Choice.Of(
                Cardinality.OccursOptionally(),
                passingElementMock,
                passingElementMock);
            var success = ch.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ISymbolNodeAggregation agg));
            Assert.IsTrue(agg.Is(out ISymbolNodeAggregation.Sequence nseq));
            Assert.AreEqual(1, nseq.Count);
        }

        [TestMethod]
        public void TryRecognize3_Tests()
        {
            var cardinality = Cardinality.OccursOnly(1);
            var passingElementMock = SetupRule(
                cardinality,
                SymbolAggregationResult.Of(
                    ISymbolNodeAggregation.Of(ISymbolNode.Of("dummy3", Tokens.Of("source3")))));

            var ch = Choice.Of(
                cardinality,
                passingElementMock,
                passingElementMock);
            var success = ch.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ISymbolNodeAggregation agg));
            Assert.IsTrue(agg.Is(out ISymbolNodeAggregation.Sequence nseq));
            Assert.AreEqual(1, nseq.Count);
        }

        [TestMethod]
        public void TryRecognize4_Tests()
        {
            var cardinality = Cardinality.OccursOnly(1);
            var passingElementMock = SetupRule(
                cardinality,
                SymbolAggregationResult.Of(
                    ISymbolNodeAggregation.Of(ISymbolNode.Of("dummy4", Tokens.Of("source4")))));

            var unrecognizedElementMock = SetupRule(
                cardinality,
                SymbolAggregationResult.Of(
                    new SymbolAggregationError(
                        elementCount: 0,
                        cause: FailedRecognitionError.Of(
                            SymbolPath.Of("bleh"),
                            10))));

            var ch = Choice.Of(
                Cardinality.OccursOnly(1),
                unrecognizedElementMock,
                passingElementMock);
            var success = ch.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ISymbolNodeAggregation agg));
            Assert.IsTrue(agg.Is(out ISymbolNodeAggregation.Sequence nseq));
            Assert.AreEqual(1, nseq.Count);
        }

        [TestMethod]
        public void TryRecognize5_Tests()
        {
            var cardinality = Cardinality.OccursOnly(1);
            var passingElementMock = SetupRule(
                cardinality,
                SymbolAggregationResult.Of(
                    ISymbolNodeAggregation.Of(ISymbolNode.Of("dummy5", Tokens.Of("source5")))));

            var partiallyRecognizedElementMock = SetupRule(
                cardinality,
                SymbolAggregationResult.Of(
                    new SymbolAggregationError(
                        elementCount: 0,
                        cause: PartialRecognitionError.Of(
                            SymbolPath.Of("bleh"), 10, 5))));

            var ch = Choice.Of(
                Cardinality.OccursOnly(1),
                partiallyRecognizedElementMock,
                passingElementMock);
            var success = ch.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out SymbolAggregationError ge));
            Assert.AreEqual(0, ge.ElementCount);
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
