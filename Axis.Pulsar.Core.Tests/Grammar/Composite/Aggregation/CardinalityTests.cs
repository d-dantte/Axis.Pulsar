using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Composite.Group;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using Moq;

namespace Axis.Pulsar.Core.Tests.Grammar.Composite.Groups
{
    [TestClass]
    public class CardinalityTests
    {
        [TestMethod]
        public void Constructor_Tests()
        {
            var cardinality = Cardinality.OccursOnlyOnce();
            Assert.AreEqual(1, cardinality.MinOccurence);
            Assert.AreEqual(1, cardinality.MaxOccurence);
            Assert.IsTrue(cardinality.IsClosed);
            Assert.IsFalse(cardinality.IsOpen);
            Assert.IsFalse(cardinality.IsZeroMinOccurence);
            Assert.IsFalse(cardinality.IsDefault);

            cardinality = Cardinality.OccursAtLeastOnce();
            Assert.IsTrue(cardinality.IsOpen);

            cardinality = Cardinality.OccursAtMost(5);
            Assert.IsTrue(cardinality.IsAny);

            cardinality = Cardinality.OccursNeverOrMore();
            Assert.IsTrue(cardinality.IsProbable);

            cardinality = Cardinality.OccursNeverOrAtMost(1);
            Assert.IsFalse(cardinality.IsProbable);
            Assert.IsTrue(cardinality.IsOptional);

            cardinality = Cardinality.Occurs(1, null);
            Assert.IsFalse(cardinality.IsDefault);

            cardinality = Cardinality.Default;
            Assert.IsTrue(cardinality.IsDefault);

            Assert.ThrowsException<InvalidOperationException>(
                () => Cardinality.Occurs(3, 1));

            Assert.ThrowsException<InvalidOperationException>(
                () => Cardinality.Occurs(0, 0));
        }

        [TestMethod]
        public void IsOptional_Tests()
        {
            var cardinality = Cardinality.OccursOptionally();
            Assert.IsTrue(cardinality.IsOptional);

            cardinality = Cardinality.Occurs(1, 1);
            Assert.IsFalse(cardinality.IsOptional);

            cardinality = Cardinality.Occurs(0, 2);
            Assert.IsFalse(cardinality.IsOptional);
        }

        [TestMethod]
        public void IsProbable_Tests()
        {
            var cardinality = Cardinality.Occurs(0, null);
            Assert.IsTrue(cardinality.IsProbable);

            cardinality = Cardinality.Occurs(0, 1);
            Assert.IsFalse(cardinality.IsProbable);

            cardinality = Cardinality.Occurs(1, null);
            Assert.IsFalse(cardinality.IsProbable);
        }

        [TestMethod]
        public void IsValidRangeTests()
        {
            var zeroOrMore = Cardinality.Occurs(0, null);
            var zeroOrOne = Cardinality.Occurs(0, 1);
            var oneOrMore = Cardinality.Occurs(1, null);
            var oneOrThree = Cardinality.Occurs(1, 3);
            var three = Cardinality.Occurs(3, 3);

            // zero or more
            Assert.IsTrue(zeroOrMore.IsValidCount(0));
            Assert.IsTrue(zeroOrMore.IsValidCount(1));
            Assert.IsTrue(zeroOrMore.IsValidCount(int.MaxValue));
            Assert.IsFalse(zeroOrMore.IsValidCount(-1));

            // zero or one
            Assert.IsTrue(zeroOrOne.IsValidCount(0));
            Assert.IsTrue(zeroOrOne.IsValidCount(1));
            Assert.IsFalse(zeroOrOne.IsValidCount(2));
            Assert.IsFalse(zeroOrOne.IsValidCount(-1));

            // one or more
            Assert.IsFalse(oneOrMore.IsValidCount(0));
            Assert.IsTrue(oneOrMore.IsValidCount(1));
            Assert.IsTrue(oneOrMore.IsValidCount(int.MaxValue));
            Assert.IsFalse(oneOrMore.IsValidCount(-1));

            // one or three
            Assert.IsFalse(oneOrThree.IsValidCount(0));
            Assert.IsFalse(oneOrThree.IsValidCount(4));
            Assert.IsTrue(oneOrThree.IsValidCount(1));
            Assert.IsTrue(oneOrThree.IsValidCount(3));

            // three
            Assert.IsFalse(three.IsValidCount(0));
            Assert.IsFalse(three.IsValidCount(1));
            Assert.IsFalse(three.IsValidCount(2));
            Assert.IsFalse(three.IsValidCount(4));
            Assert.IsTrue(three.IsValidCount(3));
        }

        [TestMethod]
        public void CanRepeatTests()
        {
            var zeroOrMore = Cardinality.Occurs(0, null);
            Assert.IsTrue(zeroOrMore.CanRepeat(0));
            Assert.IsTrue(zeroOrMore.CanRepeat(1));
            Assert.IsTrue(zeroOrMore.CanRepeat(int.MaxValue));
            Assert.IsTrue(zeroOrMore.CanRepeat(-1));

            var zeroOrOne = Cardinality.Occurs(0, 1);
            Assert.IsTrue(zeroOrOne.CanRepeat(-1));
            Assert.IsTrue(zeroOrOne.CanRepeat(0));
            Assert.IsFalse(zeroOrOne.CanRepeat(1));

            var oneOrMore = Cardinality.Occurs(1, null);
            Assert.IsTrue(oneOrMore.CanRepeat(-1));
            Assert.IsTrue(oneOrMore.CanRepeat(0));
            Assert.IsTrue(oneOrMore.CanRepeat(int.MaxValue));

            var oneOrThree = Cardinality.Occurs(1, 3);
            Assert.IsTrue(oneOrThree.CanRepeat(-1));
            Assert.IsTrue(oneOrThree.CanRepeat(0));
            Assert.IsTrue(oneOrThree.CanRepeat(2));
            Assert.IsFalse(oneOrThree.CanRepeat(3));

            var four = Cardinality.Occurs(4, 4);
            Assert.IsTrue(four.CanRepeat(-1));
            Assert.IsTrue(four.CanRepeat(0));
            Assert.IsTrue(four.CanRepeat(2));
            Assert.IsFalse(four.CanRepeat(4));
        }

        [TestMethod]
        public void Recognition_WithNullArgs_Tests()
        {
            // setup
            var mockElement = Mock.Of<IAggregationElementRule>();

            var cardinality = Cardinality.Occurs(1, 1);

            Assert.ThrowsException<ArgumentNullException>(() => cardinality.TryRepeat(
                null!, SymbolPath.Of("stuff"), null!, mockElement, out _));

            Assert.ThrowsException<ArgumentNullException>(() => cardinality.TryRepeat(
                "tokens", SymbolPath.Of("stuff"), null!, null!, out _));
        }

        [TestMethod]
        public void Recognition_WithValidArgs_Tests()
        {
            // setup
            var passingElementMock = new Mock<IAggregationElementRule>();
            passingElementMock
                .Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<SymbolAggregationResult>.IsAny))
                .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        SymbolPath path,
                        ILanguageContext cxt,
                        out SymbolAggregationResult result) =>
                {
                    result = SymbolAggregationResult.Of(ISymbolNodeAggregation.Of(ISymbolNode.Of("dummy", Tokens.Of("source"))));
                    return true;
                }));

            var failedRecognitionElementMock = new Mock<IAggregationElementRule>();
            failedRecognitionElementMock
                .Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<SymbolAggregationResult>.IsAny))
                .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        SymbolPath path,
                        ILanguageContext cxt,
                        out SymbolAggregationResult result) =>
                {
                    result = SymbolAggregationResult.Of(
                        new SymbolAggregationError(
                            elementCount: 0,
                            cause: FailedRecognitionError.Of(
                                SymbolPath.Of("bleh"),
                                10)));
                    return false;
                }));

            var passCount = 0;
            var conditionedFailureElementMock = new Mock<IAggregationElementRule>();
            conditionedFailureElementMock
                .Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<SymbolAggregationResult>.IsAny))
                .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        SymbolPath path,
                        ILanguageContext cxt,
                        out SymbolAggregationResult result) =>
                {
                    while (passCount-- > 0)
                    {
                        result = SymbolAggregationResult.Of(ISymbolNodeAggregation.Of(ISymbolNode.Of("dumy", Tokens.Of("source"))));
                        return true;
                    }

                    result = SymbolAggregationResult.Of(
                        new SymbolAggregationError(
                            elementCount: 0,
                            cause: FailedRecognitionError.Of(
                                SymbolPath.Of("bleh"),
                                10)));
                    return false;
                }));


            var cardinality = Cardinality.Occurs(1, 1);
            var recognized = cardinality.TryRepeat(
                "stuff",
                SymbolPath.Of("root"),
                null!,
                passingElementMock.Object,
                out var result);
            Assert.IsTrue(result.Is(out ISymbolNodeAggregation agg));
            Assert.IsTrue(agg.Is(out ISymbolNodeAggregation.Sequence nseq));
            Assert.AreEqual(1, nseq.Count);

            cardinality = Cardinality.Occurs(1, 21);
            recognized = cardinality.TryRepeat(
                "stuff",
                SymbolPath.Of("root"),
                null!,
                passingElementMock.Object,
                out result);
            Assert.IsTrue(result.Is(out agg));
            Assert.IsTrue(agg.Is(out nseq));
            Assert.AreEqual(21, nseq.Count);

            cardinality = Cardinality.Occurs(1, 21);
            recognized = cardinality.TryRepeat(
                "stuff",
                SymbolPath.Of("root"),
                null!,
                failedRecognitionElementMock.Object,
                out result);
            Assert.IsTrue(result.Is(out SymbolAggregationError ge));
            Assert.AreEqual(0, ge.ElementCount);

            cardinality = Cardinality.Occurs(0, 21);
            recognized = cardinality.TryRepeat(
                "stuff",
                SymbolPath.Of("root"),
                null!,
                failedRecognitionElementMock.Object,
                out result);
            Assert.IsTrue(result.Is(out agg));
            Assert.IsTrue(agg.Is(out nseq));
            Assert.AreEqual(0, nseq.Count);

            cardinality = Cardinality.Occurs(3, 21);
            passCount = 2;
            recognized = cardinality.TryRepeat(
                "stuff",
                SymbolPath.Of("root"),
                null!,
                conditionedFailureElementMock.Object,
                out result);
            Assert.IsTrue(result.Is(out ge));
            Assert.AreEqual(2, ge.ElementCount);
        }

        [TestMethod]
        public void TryRepeatWithPassingRule_Tests()
        {
            var cardinality = Cardinality.Occurs(2, 2);
            var element = SetupRule(
                SymbolAggregationResult.Of(
                    ISymbolNodeAggregation.Of(ISymbolNode.Of("name", "tokens"))));

            var successful = cardinality.TryRepeat(
                "bleh", "sym", null!,
                element, out var result);

            Assert.IsTrue(successful);
            Assert.IsTrue(result.Is(out ISymbolNodeAggregation _));
        }

        [TestMethod]
        public void TryRepeatWithPassingRule_AndOccuringLessThanMinOccurs_Tests()
        {
            var cardinality = Cardinality.Occurs(2, 2);
            var count = 0;
            var element = SetupRule(new TryRecognizeNodeSequence((
                TokenReader reader,
                SymbolPath path,
                ILanguageContext cxt,
                out SymbolAggregationResult innerResult) =>
                {
                    innerResult = (++count) switch
                    {
                        1 => SymbolAggregationResult.Of(
                            ISymbolNodeAggregation.Of(ISymbolNode.Of("name", "tokens"))),
                        _ => SymbolAggregationResult.Of(
                            SymbolAggregationError.Of(
                                FailedRecognitionError.Of("abc", 0),
                                0))
                    };
                    return innerResult.Is(out ISymbolNodeAggregation _);
                }));

            var successful = cardinality.TryRepeat(
                "bleh", "sym", null!,
                element, out var result);

            Assert.IsFalse(successful);
        }

        [TestMethod]
        public void Equals_WithObject_Tests()
        {
            var cardinality = Cardinality.OccursOnlyOnce();
            Assert.IsFalse(cardinality.Equals(new object()));
            Assert.IsTrue(cardinality.Equals((object)cardinality));
        }

        [TestMethod]
        public void Equals_Tests()
        {
            var cardinality = Cardinality.OccursOnlyOnce();
            Assert.IsTrue(cardinality.Equals(cardinality));
            Assert.IsFalse(cardinality.Equals(Cardinality.Occurs(1, 2)));
            Assert.IsFalse(cardinality.Equals(Cardinality.Occurs(2, 2)));

            Assert.IsFalse(cardinality == Cardinality.Occurs(1, 2));
            Assert.IsTrue(cardinality != Cardinality.Occurs(1, 2));
        }

        [TestMethod]
        public void HashCode_Tests()
        {
            var cardinality = Cardinality.OccursOnlyOnce();

            var expected = HashCode.Combine(cardinality.MinOccurence, cardinality.MaxOccurence);
            var hash = cardinality.GetHashCode();
            Assert.AreEqual(expected, hash);
        }

        [TestMethod]
        public void ToString_Tests()
        {
            Assert.AreEqual("", Cardinality.Occurs(1, 1).ToString());
            Assert.AreEqual(".2", Cardinality.Occurs(2, 2).ToString());
            Assert.AreEqual(".?", Cardinality.Occurs(0, 1).ToString());
            Assert.AreEqual(".*", Cardinality.Occurs(0, null).ToString());
            Assert.AreEqual(".+", Cardinality.Occurs(1, null).ToString());
            Assert.AreEqual(".2+", Cardinality.Occurs(2, null).ToString());
            Assert.AreEqual(".2,5", Cardinality.Occurs(2, 5).ToString());
        }

        [TestMethod]
        public void CanRepeat_Tests()
        {
            var cardinality = Cardinality.OccursOnly(2);
            Assert.IsTrue(Cardinality.OccursNeverOrMore().CanRepeat(1));
            Assert.IsTrue(cardinality.CanRepeat(1));
            Assert.IsFalse(cardinality.CanRepeat(2));
        }

        [TestMethod]
        public void IsValidRange_Tests()
        {
            var cardinality = Cardinality.OccursOnly(2);
            Assert.IsTrue(cardinality.IsValidCount(2));
            Assert.IsFalse(cardinality.IsValidCount(3));

            cardinality = Cardinality.OccursAtLeast(3);
            Assert.IsTrue(cardinality.IsValidCount(21));
        }

        private IAggregationElementRule SetupRule(SymbolAggregationResult result)
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
                    innerResult = result;
                    return innerResult.Is(out ISymbolNodeAggregation _);
                }));

            return rule.Object;
        }

        private IAggregationElementRule SetupRule(TryRecognizeNodeSequence @delegate)
        {
            var rule = new Mock<IAggregationElementRule>();
            rule.Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<SymbolAggregationResult>.IsAny))
                .Returns(@delegate);

            return rule.Object;
        }
    }
}
