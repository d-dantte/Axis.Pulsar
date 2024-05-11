using Axis.Luna.Common.Segments;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Composite;
using Axis.Pulsar.Core.Grammar.Composite.Group;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using Moq;

namespace Axis.Pulsar.Core.Tests.Grammar.Composite
{
    [TestClass]
    public class NonTerminalTests
    {
        internal static IAggregationElementRule MockElement(
            Cardinality cardinality,
            bool recognitionStatus,
            SymbolAggregationResult recognitionResult)
        {
            var mock = new Mock<IAggregationElementRule>();

            mock.Setup(m => m.Cardinality).Returns(cardinality);
            mock.Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<SymbolAggregationResult>.IsAny))
                .Returns(
                    new TryRecognizeNodeSequence((
                        TokenReader reader,
                        SymbolPath path,
                        ILanguageContext cxt,
                        out SymbolAggregationResult result) =>
                {
                    result = recognitionResult;
                    return recognitionStatus;
                }));

            return mock.Object;
        }

        [TestMethod]
        public void Constructor_Tests()
        {
            var element = MockElement(
                Cardinality.OccursOnlyOnce(),
                true,
                SymbolAggregationResult.Of(ISymbolNodeAggregation.Sequence.Empty()));

            var nt = new CompositeRule(null, element);
            Assert.IsNotNull(nt);

            Assert.ThrowsException<ArgumentNullException>(
                () => new CompositeRule(null, null!));
        }

        [TestMethod]
        public void TryRecognizeWithPassingElement_Tests()
        {
            var path = SymbolPath.Of("parent");

            // passing element
            var element = MockElement(
                Cardinality.OccursOnlyOnce(),
                true,
                SymbolAggregationResult.Of(ISymbolNodeAggregation.Sequence.Empty()));
            var nt = CompositeRule.Of(element);
            var success = nt.TryRecognize("stuff", path, null!, out var result);
            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out ISymbolNode _));
        }

        [TestMethod]
        public void TryRecognizeWithUnrecognizedElementAndNullThreshold_Tests()
        {
            var path = SymbolPath.Of("parent");

            // unrecognized element
            var element = MockElement(
                Cardinality.OccursOnlyOnce(),
                false,
                FailedRecognitionError
                    .Of(path, 0)
                    .ApplyTo(err => SymbolAggregationError.Of(err, 0))
                    .ApplyTo(SymbolAggregationResult.Of));
            var nt = CompositeRule.Of(element);
            var success = nt.TryRecognize("stuff", path, null!, out var result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));
        }

        [TestMethod]
        public void TryRecognizeWithUnrecognizedElementAndThresholdLessThanElementCount_Tests()
        {
            var path = SymbolPath.Of("parent");

            // unrecognized element
            var element = MockElement(
                Cardinality.OccursOnlyOnce(),
                false,
                FailedRecognitionError
                    .Of(path, 0)
                    .ApplyTo(err => SymbolAggregationError.Of(err, 5))
                    .ApplyTo(SymbolAggregationResult.Of));
            var nt = CompositeRule.Of(1, element);
            var success = nt.TryRecognize("stuff", path, null!, out var result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out PartialRecognitionError _));
        }

        [TestMethod]
        public void TryRecognizeWithUnrecognizedElementAndThresholdNotLessThanElementCount_Tests()
        {
            var path = SymbolPath.Of("parent");

            // unrecognized element
            var element = MockElement(
                Cardinality.OccursOnlyOnce(),
                false,
                FailedRecognitionError
                    .Of(path, 0)
                    .ApplyTo(err => SymbolAggregationError.Of(err, 5))
                    .ApplyTo(SymbolAggregationResult.Of));
            var nt = CompositeRule.Of(6, element);
            var success = nt.TryRecognize("stuff", path, null!, out var result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out FailedRecognitionError _));
        }

        [TestMethod]
        public void TryRecognizeWithPartiallyRecognizedElement_Tests()
        {
            var path = SymbolPath.Of("parent");

            // partially recognized element
            var element = MockElement(
                Cardinality.OccursOnlyOnce(),
                false,
                PartialRecognitionError
                    .Of(path, 0, 11)
                    .ApplyTo(p => SymbolAggregationError.Of(p, 2))
                    .ApplyTo(SymbolAggregationResult.Of));
            var nt = CompositeRule.Of(element);
            var success = nt.TryRecognize("stuff", path, null!, out var result);
            Assert.IsFalse(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Is(out PartialRecognitionError _));
        }

        internal class FauxError : INodeRecognitionError
        {
            public Segment TokenSegment => throw new NotImplementedException();

            public SymbolPath Symbol => throw new NotImplementedException();
        }
    }
}
