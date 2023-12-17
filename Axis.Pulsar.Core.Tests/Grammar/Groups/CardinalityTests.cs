using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Groups;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using Moq;

namespace Axis.Pulsar.Core.Tests.Grammar.Groups
{
    [TestClass]
    public class CardinalityTests
    {

        [TestMethod]
        public void IsValidRangeTests()
        {
            var zeroOrMore = Cardinality.Occurs(0, null);
            var zeroOrOne = Cardinality.Occurs(0, 1);
            var oneOrMore = Cardinality.Occurs(1, null);
            var oneOrThree = Cardinality.Occurs(1, 3);
            var three = Cardinality.Occurs(3, 3);

            // zero or more
            Assert.IsTrue(zeroOrMore.IsValidRange(0));
            Assert.IsTrue(zeroOrMore.IsValidRange(1));
            Assert.IsTrue(zeroOrMore.IsValidRange(int.MaxValue));
            Assert.IsFalse(zeroOrMore.IsValidRange(-1));

            // zero or one
            Assert.IsTrue(zeroOrOne.IsValidRange(0));
            Assert.IsTrue(zeroOrOne.IsValidRange(1));
            Assert.IsFalse(zeroOrOne.IsValidRange(2));
            Assert.IsFalse(zeroOrOne.IsValidRange(-1));

            // one or more
            Assert.IsFalse(oneOrMore.IsValidRange(0));
            Assert.IsTrue(oneOrMore.IsValidRange(1));
            Assert.IsTrue(oneOrMore.IsValidRange(int.MaxValue));
            Assert.IsFalse(oneOrMore.IsValidRange(-1));

            // one or three
            Assert.IsFalse(oneOrThree.IsValidRange(0));
            Assert.IsFalse(oneOrThree.IsValidRange(4));
            Assert.IsTrue(oneOrThree.IsValidRange(1));
            Assert.IsTrue(oneOrThree.IsValidRange(3));

            // three
            Assert.IsFalse(three.IsValidRange(0));
            Assert.IsFalse(three.IsValidRange(1));
            Assert.IsFalse(three.IsValidRange(2));
            Assert.IsFalse(three.IsValidRange(4));
            Assert.IsTrue(three.IsValidRange(3));
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
            var mockElement = Mock.Of<IGroupElement>();

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
            var passingElementMock = new Mock<IGroupElement>();
            passingElementMock
                .Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<GroupRecognitionResult>.IsAny))
                .Returns(new TryRecognizeNodeSequence((
                    TokenReader reader,
                    SymbolPath path,
                    ILanguageContext context,
                    out GroupRecognitionResult result) =>
                {
                    result = GroupRecognitionResult.Of(INodeSequence.Of(ICSTNode.Of("dummy", Tokens.Of("source"))));
                    return true;
                }));

            var failedRecognitionElementMock = new Mock<IGroupElement>();
            failedRecognitionElementMock
                .Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<GroupRecognitionResult>.IsAny))
                .Returns(new TryRecognizeNodeSequence((
                    TokenReader reader,
                    SymbolPath path,
                    ILanguageContext context,
                    out GroupRecognitionResult result) =>
                {
                    result = GroupRecognitionResult.Of(
                        new GroupRecognitionError(
                            elementCount: 0,
                            cause: FailedRecognitionError.Of(
                                SymbolPath.Of("bleh"),
                                10)));
                    return false;
                }));

            var passCount = 0;
            var conditionedFailureElementMock = new Mock<IGroupElement>();
            conditionedFailureElementMock
                .Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<GroupRecognitionResult>.IsAny))
                .Returns(new TryRecognizeNodeSequence((
                    TokenReader reader,
                    SymbolPath path,
                    ILanguageContext context,
                    out GroupRecognitionResult result) =>
                {
                    while (passCount-- > 0)
                    {
                        result = GroupRecognitionResult.Of(INodeSequence.Of(ICSTNode.Of("dumy", Tokens.Of("source"))));
                        return true;
                    }

                    result = GroupRecognitionResult.Of(
                        new GroupRecognitionError(
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
            Assert.IsTrue(result.Is(out INodeSequence nseq));
            Assert.AreEqual(1, nseq.Count);

            cardinality = Cardinality.Occurs(1, 21);
            recognized = cardinality.TryRepeat(
                "stuff",
                SymbolPath.Of("root"),
                null!,
                passingElementMock.Object,
                out result);
            Assert.IsTrue(result.Is(out nseq));
            Assert.AreEqual(21, nseq.Count);

            cardinality = Cardinality.Occurs(1, 21);
            recognized = cardinality.TryRepeat(
                "stuff",
                SymbolPath.Of("root"),
                null!,
                failedRecognitionElementMock.Object,
                out result);
            Assert.IsTrue(result.Is(out GroupRecognitionError ge));
            Assert.AreEqual(0, ge.ElementCount);

            cardinality = Cardinality.Occurs(0, 21);
            recognized = cardinality.TryRepeat(
                "stuff",
                SymbolPath.Of("root"),
                null!,
                failedRecognitionElementMock.Object,
                out result);
            Assert.IsTrue(result.Is(out nseq));
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
    }
}
