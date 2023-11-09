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
                        out It.Ref<IResult<NodeSequence>>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        ProductionPath? path,
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
                        out It.Ref<IResult<NodeSequence>>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        ProductionPath? path,
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
                        out It.Ref<IResult<NodeSequence>>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        ProductionPath? path,
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

            var customErrorElementMock = new Mock<IGroupElement>();
            customErrorElementMock
                .With(mock => mock
                    .Setup(m => m.Cardinality)
                    .Returns(Cardinality.OccursOnly(1)))
                .With(mock => mock
                    .Setup(m => m.TryRecognize(
                        It.IsAny<TokenReader>(),
                        It.IsAny<ProductionPath>(),
                        out It.Ref<IResult<NodeSequence>>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        ProductionPath? path,
                        out IResult<NodeSequence> result) =>
                    {
                        result = Result.Of<NodeSequence>(
                            new GroupError(
                                nodes: NodeSequence.Empty,
                                error: new CustomNodeError()));
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
                        out It.Ref<IResult<NodeSequence>>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        ProductionPath? path,
                        out IResult<NodeSequence> result) =>
                    {
                        result = RecognitionRuntimeError
                            .Of(new Exception())
                            .ApplyTo(Result.Of<NodeSequence>);
                        return false;
                    })));
            #endregion

            var seq = Set.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                passingElementMock.Object);
            var success = seq.TryRecognize("dummy", "dummy", out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.IsDataResult());
            var nseq = result.Resolve();
            Assert.AreEqual(2, nseq.Count);

            seq = Set.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                unrecognizedElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsInstanceOfType<GroupError>(result.AsError().ActualCause());
            var ge = result.AsError().ActualCause() as GroupError;
            Assert.IsInstanceOfType<PartiallyRecognizedTokens>(ge.NodeError);
            Assert.AreEqual(1, ge.Nodes.Count);

            seq = Set.Of(
                Cardinality.OccursOnly(1),
                unrecognizedElementMock.Object,
                unrecognizedElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsInstanceOfType<GroupError>(result.AsError().ActualCause());
            ge = result.AsError().ActualCause() as GroupError;
            Assert.IsInstanceOfType<UnrecognizedTokens>(ge.NodeError);
            Assert.AreEqual(0, ge.Nodes.Count);

            seq = Set.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                partiallyRecognizedElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsInstanceOfType<GroupError>(result.AsError().ActualCause());
            ge = result.AsError().ActualCause() as GroupError;
            Assert.IsInstanceOfType<PartiallyRecognizedTokens>(ge.NodeError);
            Assert.AreEqual(0, ge.Nodes.Count);

            seq = Set.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                customErrorElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsInstanceOfType<RecognitionRuntimeError>(result.AsError().ActualCause());
            var runtimeError = result.AsError().ActualCause() as RecognitionRuntimeError;
            Assert.IsInstanceOfType<CustomNodeError>(runtimeError!.InnerException);

            seq = Set.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                runtimeErrorElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.IsErrorResult());
            Assert.IsInstanceOfType<RecognitionRuntimeError>(result.AsError().ActualCause());
        }

        internal class CustomNodeError : Exception, INodeError
        {
            public ProductionPath ProductionPath { get; set; }

            public int Position { get; set; }
        }
    }
}
