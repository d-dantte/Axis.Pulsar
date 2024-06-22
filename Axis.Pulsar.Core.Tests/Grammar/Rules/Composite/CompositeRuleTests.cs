using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Rules.Aggregate;
using Axis.Pulsar.Core.Grammar.Rules.Composite;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using Moq;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules.Composite
{
    [TestClass]
    public class CompositeRuleTests
    {
        [TestMethod]
        public void Construction_Tests()
        {
            var rule = MockRule(
                AggregationType.Sequence,
                NodeAggregationResult.Of(ISymbolNode.Of("id", Tokens.Of("bleh"))));
            var composite = CompositeRule.Of(rule);

            Assert.IsNotNull(composite);
            Assert.IsNull(composite.RecognitionThreshold);
            Assert.AreEqual(rule, composite.Element);

            composite = CompositeRule.Of(1, rule);
            Assert.AreEqual(1u, composite.RecognitionThreshold);

            Assert.ThrowsException<ArgumentNullException>(
                () => new CompositeRule(4, null!));
        }


        [TestMethod]
        public void TryRecognizeWithPassingElement_Tests()
        {
            var rule = MockRule(
                AggregationType.Sequence,
                NodeAggregationResult.Of(ISymbolNode.Of("id", Tokens.Of("bleh"))));
            var arr = new CompositeRule(null, rule);
            var recognized = arr.TryRecognize("bleh", "sym", null!, out var result);

            Assert.IsTrue(recognized);
            Assert.IsTrue(result.Is(out ISymbolNode _));
        }

        [TestMethod]
        public void TryRecognizeWithFailedElement_Tests()
        {
            var rule = MockRule(
                AggregationType.Sequence,
                NodeAggregationResult.Of(
                    AggregateRecognitionError.Of(
                        FailedRecognitionError.Of("sym", 10),
                        ISymbolNode.Of("id", "tokens"))));
            var arr = new CompositeRule(null, rule);
            var recognized = arr.TryRecognize("bleh", "sym", null!, out var result);

            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));
        }

        [TestMethod]
        public void TryRecognizeWithFailedElement_AndUnsurpassedThreshold_Tests()
        {
            var rule = MockRule(
                AggregationType.Sequence,
                NodeAggregationResult.Of(
                    AggregateRecognitionError.Of(
                        FailedRecognitionError.Of("sym", 10),
                        ISymbolNode.Of("id", "tokens"),
                        ISymbolNode.Of("id", "tokens2"))));
            var arr = new CompositeRule(3, rule);
            var recognized = arr.TryRecognize("bleh", "sym", null!, out var result);

            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));
        }

        [TestMethod]
        public void TryRecognizeWithFailedElement_AndBeyondThreshold_Tests()
        {
            var rule = MockRule(
                AggregationType.Sequence,
                NodeAggregationResult.Of(
                    AggregateRecognitionError.Of(
                        FailedRecognitionError.Of("sym", 10),
                        ISymbolNode.Of("id", "tokens"))));
            var arr = new CompositeRule(1, rule);
            var recognized = arr.TryRecognize("bleh", "sym", null!, out var result);

            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out PartialRecognitionError _));
        }

        [TestMethod]
        public void TryRecognizeWithPartialElement_Tests()
        {
            var rule = MockRule(
                AggregationType.Sequence,
                NodeAggregationResult.Of(
                    AggregateRecognitionError.Of(
                        PartialRecognitionError.Of("sym", 0, 1))));
            var arr = new CompositeRule(null, rule);
            var recognized = arr.TryRecognize("bleh", "sym", null!, out var result);

            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out PartialRecognitionError _));
        }


        internal static IAggregationElement MockRule(
            AggregationType type,
            NodeAggregationResult recognitionResult)
        {
            var mock = new Mock<IAggregationElement>();
            mock.Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<NodeAggregationResult>.IsAny))
                .Returns(
                    new AggregateRecognition((
                        TokenReader reader,
                        SymbolPath path,
                        ILanguageContext cxt,
                        out NodeAggregationResult result) =>
                    {
                        result = recognitionResult;
                        return result.Is(out ISymbolNode _);
                    }));
            mock.Setup(m => m.Type).Returns(type);

            return mock.Object;
        }
    }
}
