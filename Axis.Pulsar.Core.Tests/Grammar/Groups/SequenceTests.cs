using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Groups;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;
using Moq;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Tests.Grammar.Groups
{
    [TestClass]
    public class SequenceTests
    {
        [TestMethod]
        public void TryRecognize_Tests()
        {
            // setup
            var passingElementMock = new Mock<IGroupElement>();
            passingElementMock
                .With(mock => mock
                    .Setup(m => m.Cardinality)
                    .Returns(Cardinality.OccursOnly(1)))
                .With(mock => mock
                    .Setup(m => m.TryRecognize(
                        It.IsAny<TokenReader>(),
                        It.IsAny<ProductionPath>(),
                        It.IsAny<ILanguageContext>(),
                        out It.Ref<IResult<NodeSequence>>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        ProductionPath? path,
                        ILanguageContext languageContext,
                        out IResult<NodeSequence> result) =>
                    {
                        result = Result.Of(NodeSequence.Of(ICSTNode.Of("dummy", Tokens.Of("source"))));
                        return true;
                    })));

            var unrecognizedElementMock = new Mock<IGroupElement>();
            unrecognizedElementMock
                .With(mock => mock
                    .Setup(m => m.Cardinality)
                    .Returns(Cardinality.OccursOnly(1)))
                .With(mock => mock
                    .Setup(m => m.TryRecognize(
                        It.IsAny<TokenReader>(),
                        It.IsAny<ProductionPath>(),
                        It.IsAny<ILanguageContext>(),
                        out It.Ref<IResult<NodeSequence>>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        ProductionPath? path,
                        ILanguageContext languageContext,
                        out IResult<NodeSequence> result) =>
                    {
                        result = Result.Of<NodeSequence>(
                            new GroupRecognitionError(
                                elementCount: 0,
                                cause: FailedRecognitionError.Of(
                                    ProductionPath.Of("bleh"),
                                    10)));
                        return false;
                    })));

            var partiallyRecognizedElementMock = new Mock<IGroupElement>();
            partiallyRecognizedElementMock
                .With(mock => mock
                    .Setup(m => m.Cardinality)
                    .Returns(Cardinality.OccursOnly(1)))
                .With(mock => mock
                    .Setup(m => m.TryRecognize(
                        It.IsAny<TokenReader>(),
                        It.IsAny<ProductionPath>(),
                        It.IsAny<ILanguageContext>(),
                        out It.Ref<IResult<NodeSequence>>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        ProductionPath? path,
                        ILanguageContext languageContext,
                        out IResult<NodeSequence> result) =>
                    {
                        result = Result.Of<NodeSequence>(
                            new GroupRecognitionError(
                                elementCount: 0,
                                cause: PartialRecognitionError.Of(
                                    ProductionPath.Of("bleh"),
                                    10,
                                    3)));
                        return false;
                    })));

            var seq = Sequence.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                passingElementMock.Object);
            var success = seq.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var nseq = result.Resolve();
            Assert.AreEqual(2, nseq.Count);

            seq = Sequence.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                unrecognizedElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out GroupRecognitionError ge));
            Assert.IsInstanceOfType<FailedRecognitionError>(ge.Cause);
            Assert.AreEqual(1, ge.ElementCount);

            seq = Sequence.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                partiallyRecognizedElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out ge));
            Assert.AreEqual(1, ge.ElementCount);
        }
    }
}
