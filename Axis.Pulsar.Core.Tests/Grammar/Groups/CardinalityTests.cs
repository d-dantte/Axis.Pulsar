using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Groups;
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
                null!, ProductionPath.Of("stuff"), null!, mockElement, out _));

            Assert.ThrowsException<ArgumentNullException>(() => cardinality.TryRepeat(
                "tokens", null!, null!, mockElement, out _));

            Assert.ThrowsException<ArgumentNullException>(() => cardinality.TryRepeat(
                "tokens", ProductionPath.Of("stuff"), null!, null!, out _));


        }

        [TestMethod]
        public void Recognition_WithValidArgs_Tests()
        {
            // setup
            var passingElementMock = new Mock<IGroupElement>();
            passingElementMock
                .Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<ProductionPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<IResult<NodeSequence>>.IsAny))
                .Returns(new TryRecognizeNodeSequence((
                    TokenReader reader,
                    ProductionPath? path,
                    ILanguageContext context,
                    out IResult<NodeSequence> result) =>
                {
                    result = Result.Of(NodeSequence.Of(ICSTNode.Of("dummy", Tokens.Of("source"))));
                    return true;
                }));

            var unrecognizedElementMock = new Mock<IGroupElement>();
            unrecognizedElementMock
                .Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<ProductionPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<IResult<NodeSequence>>.IsAny))
                .Returns(new TryRecognizeNodeSequence((
                    TokenReader reader,
                    ProductionPath? path,
                    ILanguageContext context,
                    out IResult<NodeSequence> result) =>
                {
                    result = Result.Of<NodeSequence>(
                        new GroupError(
                            nodes: NodeSequence.Empty,
                            error: UnrecognizedTokens.Of(
                                ProductionPath.Of("bleh"),
                                10)));
                    return false;
                }));

            var passCount = 0;
            var conditionedFailureElementMock = new Mock<IGroupElement>();
            conditionedFailureElementMock
                .Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<ProductionPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<IResult<NodeSequence>>.IsAny))
                .Returns(new TryRecognizeNodeSequence((
                    TokenReader reader,
                    ProductionPath? path,
                    ILanguageContext context,
                    out IResult<NodeSequence> result) =>
                {
                    while (passCount-- > 0)
                    {
                        result = Result.Of(NodeSequence.Of(ICSTNode.Of("dumy", Tokens.Of("source"))));
                        return true;
                    }

                    result = Result.Of<NodeSequence>(
                        new GroupError(
                            nodes: NodeSequence.Empty,
                            error: UnrecognizedTokens.Of(
                                ProductionPath.Of("bleh"),
                                10)));
                    return false;
                }));

            var runtimeFailureElementMock = new Mock<IGroupElement>();
            runtimeFailureElementMock
                .Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<ProductionPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<IResult<NodeSequence>>.IsAny))
                .Returns(new TryRecognizeNodeSequence((
                    TokenReader reader,
                    ProductionPath? path,
                    ILanguageContext context,
                    out IResult<NodeSequence> result) =>
                {
                    result = Result.Of<NodeSequence>(new Exception());
                    return false;
                }));


            var cardinality = Cardinality.Occurs(1, 1);
            var recognized = cardinality.TryRepeat(
                "stuff",
                ProductionPath.Of("root"),
                null!,
                passingElementMock.Object,
                out var result);
            Assert.AreEqual(1, result.Resolve().Count);

            cardinality = Cardinality.Occurs(1, 21);
            recognized = cardinality.TryRepeat(
                "stuff",
                ProductionPath.Of("root"),
                null!,
                passingElementMock.Object,
                out result);
            Assert.AreEqual(21, result.Resolve().Count);

            cardinality = Cardinality.Occurs(1, 21);
            recognized = cardinality.TryRepeat(
                "stuff",
                ProductionPath.Of("root"),
                null!,
                unrecognizedElementMock.Object,
                out result);
            var ge = result.AsError().ActualCause() as GroupError;
            var ns = ge.Nodes;
            Assert.AreEqual(0, ns.Count);

            cardinality = Cardinality.Occurs(1, 21);
            recognized = cardinality.TryRepeat(
                "stuff",
                ProductionPath.Of("root"),
                null!,
                runtimeFailureElementMock.Object,
                out result);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsInstanceOfType(
                result.AsError().ActualCause(),
                typeof(RecognitionRuntimeError));

            cardinality = Cardinality.Occurs(0, 21);
            recognized = cardinality.TryRepeat(
                "stuff",
                ProductionPath.Of("root"),
                null!,
                unrecognizedElementMock.Object,
                out result);
            Assert.IsTrue(result.IsDataResult());
            Assert.AreEqual(0, result.Resolve().Count);

            cardinality = Cardinality.Occurs(3, 21);
            passCount = 2;
            recognized = cardinality.TryRepeat(
                "stuff",
                ProductionPath.Of("root"),
                null!,
                conditionedFailureElementMock.Object,
                out result);
            ge = result.AsError().ActualCause() as GroupError;
            ns = ge.Nodes;
            Assert.AreEqual(2, ns.Count);
        }
    }
}
