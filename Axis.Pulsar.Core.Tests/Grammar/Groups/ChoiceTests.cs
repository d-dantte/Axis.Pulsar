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
                            new GroupError(
                                nodes: NodeSequence.Empty,
                                error: UnrecognizedTokens.Of(
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
                            new GroupError(
                                nodes: NodeSequence.Empty,
                                error: PartiallyRecognizedTokens.Of(
                                    ProductionPath.Of("bleh"),
                                    10,
                                    "partial tokens")));
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
                        out It.Ref<IResult<NodeSequence>>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        ProductionPath? path,
                        ILanguageContext languageContext,
                        out IResult<NodeSequence> result) =>
                    {
                        result = RecognitionRuntimeError
                            .Of(new Exception())
                            .ApplyTo(Result.Of<NodeSequence>);
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
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsInstanceOfType<GroupError>(result.AsError().ActualCause());
            var ge = result.AsError().ActualCause() as GroupError;
            Assert.IsInstanceOfType<PartiallyRecognizedTokens>(ge.NodeError);
            Assert.AreEqual(0, ge.Nodes.Count);

            ch = Choice.Of(
                Cardinality.OccursOnly(1),
                runtimeErrorElementMock.Object,
                passingElementMock.Object);
            success = ch.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsInstanceOfType<RecognitionRuntimeError>(result.AsError().ActualCause());
        }
    }
}
