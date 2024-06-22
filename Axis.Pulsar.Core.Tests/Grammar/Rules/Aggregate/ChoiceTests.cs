using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;
using Moq;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Rules.Aggregate;
using Axis.Pulsar.Core.Grammar.Rules.Atomic;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules.Aggregate
{
    [TestClass]
    public class ChoiceTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var choice = new Choice(
                SetupRule(
                    NodeAggregationResult.Of(
                    ISymbolNode.Of("dummy", Tokens.Of("source")))));
            Assert.IsNotNull(choice);
            Assert.AreEqual(1, choice.Elements.Length);
            Assert.AreEqual(AggregationType.Choice, choice.Type);

            Assert.ThrowsException<ArgumentNullException>(
                () => Choice.Of((IAggregationElement[])null!));

            Assert.ThrowsException<ArgumentException>(
                () => new Choice());

            Assert.ThrowsException<InvalidOperationException>(
                () => new Choice(null!, null!));

            Assert.ThrowsException<InvalidOperationException>(
                () => new Choice(
                    new Repetition(
                        Cardinality.OccursOptionally(),
                        new AtomicRuleRef(new TerminalLiteral("id", "tokens")))));
        }

        [TestMethod]
        public void TryRecognize1_Tests()
        {
            var passingElementMock = SetupRule(
                NodeAggregationResult.Of(
                    ISymbolNode.Of("dummy", Tokens.Of("source"))));

            var passingOptionalElementMock = SetupRule(
                NodeAggregationResult.Of(
                    ISymbolNode.Of(
                        AggregationType.Unit, true,
                        ISymbolNode.Of("dummy", Tokens.Of("source")))));

            var ch = Choice.Of(
                passingOptionalElementMock,
                passingElementMock);
            var success = ch.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ISymbolNode agg));
            Assert.IsTrue(agg.Is(out ISymbolNode.Aggregate nseq));
            Assert.AreEqual(1, nseq.Nodes.Length);
        }

        [TestMethod]
        public void TryRecognize2_Tests()
        {
            var passingElementMock = SetupRule(
                NodeAggregationResult.Of(
                    ISymbolNode.Of("dummy2", Tokens.Of("source2"))));

            var ch = Choice.Of(
                passingElementMock,
                passingElementMock);
            var success = ch.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ISymbolNode agg));
        }

        [TestMethod]
        public void TryRecognize3_Tests()
        {
            var passingElementMock = SetupRule(
                NodeAggregationResult.Of(
                    ISymbolNode.Of("dummy4", Tokens.Of("source4"))));

            var unrecognizedElementMock = SetupRule(
                NodeAggregationResult.Of(
                    new AggregateRecognitionError(
                        FailedRecognitionError.Of(
                            SymbolPath.Of("bleh"),
                            10))));

            var ch = Choice.Of(
                unrecognizedElementMock,
                unrecognizedElementMock);
            var success = ch.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out AggregateRecognitionError are));
        }

        [TestMethod]
        public void TryRecognize4_Tests()
        {
            var passingElementMock = SetupRule(
                NodeAggregationResult.Of(
                    ISymbolNode.Of("dummy4", Tokens.Of("source4"))));

            var unrecognizedElementMock = SetupRule(
                NodeAggregationResult.Of(
                    new AggregateRecognitionError(
                        FailedRecognitionError.Of(
                            SymbolPath.Of("bleh"),
                            10))));

            var ch = Choice.Of(
                unrecognizedElementMock,
                passingElementMock);
            var success = ch.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out ISymbolNode agg));
            Assert.IsTrue(agg.Is(out ISymbolNode.Atom _));
        }

        [TestMethod]
        public void TryRecognize5_Tests()
        {
            var passingElementMock = SetupRule(
                NodeAggregationResult.Of(
                    ISymbolNode.Of("dummy5", Tokens.Of("source5"))));

            var partiallyRecognizedElementMock = SetupRule(
                NodeAggregationResult.Of(
                    new AggregateRecognitionError(
                        PartialRecognitionError.Of(
                            SymbolPath.Of("bleh"), 10, 5))));

            var ch = Choice.Of(
                partiallyRecognizedElementMock,
                passingElementMock);
            var success = ch.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out AggregateRecognitionError ge));
            Assert.AreEqual(0, ge.RecognizedNodes.Length);
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
