using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Groups;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;
using Moq;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Luna.Common.Segments;
using Axis.Pulsar.Core.Lang;

namespace Axis.Pulsar.Core.Tests.Grammar.Groups
{
    [TestClass]
    public class SetTests
    {
        [TestMethod]
        public void TryRecognize_Tests()
        {
            #region Setup
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
                                    "bleh",
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
                                    "bleh",
                                    10,
                                    5)));
                        return false;
                    })));

            var customErrorElementMock = new Mock<IGroupElement>();
            customErrorElementMock
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
                        result = Result.Of<INodeSequence>(new CustomNodeError());
                        return false;
                    })));

            var runtimeErrorElementMock = new Mock<IGroupElement>();
            runtimeErrorElementMock
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
                        result = Result.Of<INodeSequence>(new Exception("non-IRecognitionError Exception"));
                        return false;
                    })));
            #endregion

            var seq = Set.Of(
                Cardinality.OccursOnly(1),
                1,
                passingElementMock.Object,
                passingElementMock.Object);
            var success = seq.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var nseq = result.Resolve();
            Assert.AreEqual(2, nseq.Count);

            seq = Set.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                unrecognizedElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out GroupRecognitionError gre));
            Assert.IsInstanceOfType<PartialRecognitionError>(gre.Cause);
            Assert.AreEqual(1, gre.ElementCount);

            seq = Set.Of(
                Cardinality.OccursOnly(1),
                1,
                unrecognizedElementMock.Object,
                unrecognizedElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out gre));
            Assert.IsInstanceOfType<FailedRecognitionError>(gre.Cause);
            Assert.AreEqual(0, gre.ElementCount);

            seq = Set.Of(
                Cardinality.OccursOnly(1),
                1,
                passingElementMock.Object,
                partiallyRecognizedElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out gre));
            Assert.IsInstanceOfType<PartialRecognitionError>(gre.Cause);
            Assert.AreEqual(0, gre.ElementCount);

            seq = Set.Of(
                Cardinality.OccursOnly(1),
                1,
                passingElementMock.Object,
                customErrorElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out CustomNodeError cne));

            seq = Set.Of(
                Cardinality.OccursOnly(1),
                1,
                passingElementMock.Object,
                runtimeErrorElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult(out Exception _));
        }

        internal class CustomNodeError : Exception, IRecognitionError__
        {
            public string Symbol => "custom-node-error-symbol";

            Segment IRecognitionError__.TokenSegment => default;
        }
    }
}
