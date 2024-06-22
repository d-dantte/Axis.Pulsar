using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Rules.Aggregate;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using Moq;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules.Aggregate
{
    [TestClass]
    public class RepetitionTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            var element = Mock.Of<IAggregationElement>();
            var repetition = Repetition.Of(
                Cardinality.OccursOnlyOnce(),
                element);

            Assert.IsNotNull(repetition);
            Assert.AreEqual(AggregationType.Repetition, repetition.Type);
            Assert.AreEqual(element, repetition.Element);

            Assert.ThrowsException<ArgumentNullException>(
                () => new Repetition(Cardinality.OccursOnlyOnce(), null!));
        }

        [TestMethod]
        public void TryRecognize_WithNullArgs()
        {
            // setup
            var mockElement = Mock.Of<IAggregationElement>();
            var cardinality = Cardinality.OccursOnlyOnce();
            var repetition = new Repetition(cardinality, mockElement);

            Assert.ThrowsException<ArgumentNullException>(
                () => repetition.TryRecognize(null!, SymbolPath.Of("stuff"), null!, out _));
        }

        [TestMethod]
        public void TryRecognize_WithValidArgs()
        {
            // setup
            var passingElementMock = SetupElement(new AggregateRecognition((
                TokenReader reader,
                SymbolPath path,
                ILanguageContext cxt,
                out NodeAggregationResult result) =>
                {
                    result = NodeAggregationResult.Of(ISymbolNode.Of("dummy", Tokens.Of("source")));
                    return true;
                }));

            var failedRecognitionElementMock = SetupElement(new AggregateRecognition((
                TokenReader reader,
                SymbolPath path,
                ILanguageContext cxt,
                out NodeAggregationResult result) =>
                {
                    result = NodeAggregationResult.Of(
                        new AggregateRecognitionError(
                            FailedRecognitionError.Of(SymbolPath.Of("bleh"), 10)));
                    return false;
                }));

            var passCount = 0;
            var conditionedFailureElementMock = SetupElement(new AggregateRecognition((
                TokenReader reader,
                SymbolPath path,
                ILanguageContext cxt,
                out NodeAggregationResult result) =>
                {
                    while (passCount-- > 0)
                    {
                        result = NodeAggregationResult.Of(ISymbolNode.Of("dumy", Tokens.Of("source")));
                        return true;
                    }

                    result = NodeAggregationResult.Of(
                        new AggregateRecognitionError(
                            FailedRecognitionError.Of(SymbolPath.Of("bleh"), 10)));
                    return false;
                }));


            var cardinality = Cardinality.Occurs(1, 1);
            var repetition = new Repetition(cardinality, passingElementMock);
            var recognized = repetition.TryRecognize(
                "stuff",
                SymbolPath.Of("root"),
                null!,
                out var result);
            Assert.IsTrue(result.Is(out ISymbolNode agg));
            Assert.IsTrue(agg.Is(out ISymbolNode.Aggregate nseq));
            Assert.AreEqual(1, nseq.Nodes.Length);

            cardinality = Cardinality.Occurs(1, 21);
            repetition = new Repetition(cardinality, passingElementMock);
            recognized = repetition.TryRecognize(
                "stuff",
                SymbolPath.Of("root"),
                null!,
                out result);
            Assert.IsTrue(result.Is(out agg));
            Assert.IsTrue(agg.Is(out nseq));
            Assert.AreEqual(21, nseq.Nodes.Length);

            cardinality = Cardinality.Occurs(1, 21);
            repetition = new Repetition(cardinality, failedRecognitionElementMock);
            recognized = repetition.TryRecognize(
                "stuff",
                SymbolPath.Of("root"),
                null!,
                out result);
            Assert.IsTrue(result.Is(out AggregateRecognitionError ge));
            Assert.AreEqual(0, ge.RecognizedNodes.Length);

            cardinality = Cardinality.Occurs(0, 21);
            repetition = new Repetition(cardinality, failedRecognitionElementMock);
            recognized = repetition.TryRecognize(
                "stuff",
                SymbolPath.Of("root"),
                null!,
                out result);
            Assert.IsTrue(result.Is(out agg));
            Assert.IsTrue(agg.Is(out nseq));
            Assert.AreEqual(0, nseq.Nodes.Length);

            cardinality = Cardinality.Occurs(3, 21);
            passCount = 2;
            repetition = new Repetition(cardinality, conditionedFailureElementMock);
            recognized = repetition.TryRecognize(
                "stuff",
                SymbolPath.Of("root"),
                null!,
                out result);
            Assert.IsTrue(result.Is(out ge));
            Assert.AreEqual(2, ge.RecognizedNodes.Length);
        }

        [TestMethod]
        public void TryRecognizeWithPassingRule_Tests()
        {
            var cardinality = Cardinality.Occurs(2, 2);
            var element = SetupElement(
                NodeAggregationResult.Of(ISymbolNode.Of("name", "tokens")));
            var repetition = new Repetition(cardinality, element);

            var successful = repetition.TryRecognize("bleh", "sym", null!, out var result);

            Assert.IsTrue(successful);
            Assert.IsTrue(result.Is(out ISymbolNode _));
        }

        [TestMethod]
        public void TryRecognizeWithPassingRule_AndOccuringLessThanMinOccurs_Tests()
        {
            var cardinality = Cardinality.Occurs(2, 2);
            var count = 0;
            var element = SetupElement(new AggregateRecognition((
                TokenReader reader,
                SymbolPath path,
                ILanguageContext cxt,
                out NodeAggregationResult result) =>
                {
                    result = ++count switch
                    {
                        1 => NodeAggregationResult.Of(
                            ISymbolNode.Of("name", "tokens")),
                        _ => NodeAggregationResult.Of(
                            AggregateRecognitionError.Of(
                                FailedRecognitionError.Of("abc", 0)))
                    };
                    return result.Is(out ISymbolNode _);
                }));
            var repetition = new Repetition(cardinality, element);

            var successful = repetition.TryRecognize("bleh", "sym", null!, out var result);

            Assert.IsFalse(successful);
            Assert.IsTrue(result.Is(out AggregateRecognitionError are));
            Assert.AreEqual(1, are.RecognizedNodes.Length);
        }

        private IAggregationElement SetupElement(NodeAggregationResult result)
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

        private IAggregationElement SetupElement(AggregateRecognition @delegate)
        {
            var rule = new Mock<IAggregationElement>();
            rule.Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<NodeAggregationResult>.IsAny))
                .Returns(@delegate);

            return rule.Object;
        }
    }
}
