using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Groups;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;
using Moq;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Lang;

namespace Axis.Pulsar.Core.Tests.Grammar.Groups
{
    [TestClass]
    public class ChoiceTests
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
                        out It.Ref<IResult<INodeSequence>>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        ProductionPath? path,
                        ILanguageContext languageContext,
                        out IResult<INodeSequence> result) =>
                    {
                        result = Result.Of(INodeSequence.Of(ICSTNode.Of("dummy", Tokens.Of("source"))));
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
                        out It.Ref<IResult<INodeSequence>>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        ProductionPath? path,
                        ILanguageContext languageContext,
                        out IResult<INodeSequence> result) =>
                    {
                        result = Result.Of<INodeSequence>(
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
                        out It.Ref<IResult<INodeSequence>>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        ProductionPath? path,
                        ILanguageContext languageContext,
                        out IResult<INodeSequence> result) =>
                    {
                        result = Result.Of<INodeSequence>(
                            new GroupRecognitionError(
                                elementCount: 0,
                                cause: PartialRecognitionError.Of(
                                    ProductionPath.Of("bleh"),
                                    10,
                                    5)));
                        return false;
                    })));

            var nonRecognitionErrorElementMock = new Mock<IGroupElement>();
            nonRecognitionErrorElementMock
                .With(mock => mock
                    .Setup(m => m.Cardinality)
                    .Returns(Cardinality.OccursOnly(1)))
                .With(mock => mock
                    .Setup(m => m.TryRecognize(
                        It.IsAny<TokenReader>(),
                        It.IsAny<ProductionPath>(),
                        It.IsAny<ILanguageContext>(),
                        out It.Ref<IResult<INodeSequence>>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        ProductionPath? path,
                        ILanguageContext languageContext,
                        out IResult<INodeSequence> result) =>
                    {
                        result = Result.Of<INodeSequence>(new Exception("non-IRecognitionError"));
                        return false;
                    })));

            var ch = Choice.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                passingElementMock.Object);
            var success = ch.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var nseq = result.Resolve();
            Assert.AreEqual(1, nseq.Count);

            ch = Choice.Of(
                Cardinality.OccursOnly(1),
                unrecognizedElementMock.Object,
                passingElementMock.Object);
            success = ch.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            nseq = result.Resolve();
            Assert.AreEqual(1, nseq.Count);

            ch = Choice.Of(
                Cardinality.OccursOnly(1),
                partiallyRecognizedElementMock.Object,
                passingElementMock.Object);
            success = ch.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out GroupRecognitionError ge));
            Assert.AreEqual(0, ge.ElementCount);

            ch = Choice.Of(
                Cardinality.OccursOnly(1),
                nonRecognitionErrorElementMock.Object,
                passingElementMock.Object);
            success = ch.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out Exception _));
        }
    }
}
