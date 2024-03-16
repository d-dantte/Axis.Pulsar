using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;
using Moq;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Composite.Group;

namespace Axis.Pulsar.Core.Tests.Grammar.Groups
{
    [TestClass]
    public class SequenceTests
    {
        [TestMethod]
        public void TryRecognize_Tests()
        {
            // setup
            var passingElementMock = new Mock<IGroupRule>();
            passingElementMock
                .With(mock => mock
                    .Setup(m => m.Cardinality)
                    .Returns(Cardinality.OccursOnly(1)))
                .With(mock => mock
                    .Setup(m => m.TryRecognize(
                        It.IsAny<TokenReader>(),
                        It.IsAny<SymbolPath>(),
                        It.IsAny<ILanguageContext>(),
                        out It.Ref<GroupRecognitionResult>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        SymbolPath path,
                        ILanguageContext languageContext,
                        out GroupRecognitionResult result) =>
                    {
                        result = GroupRecognitionResult.Of(INodeSequence.Of(ICSTNode.Of("dummy", Tokens.Of("source"))));
                        return true;
                    })));

            var passingOptionalElementMock = new Mock<IGroupRule>();
            passingOptionalElementMock
                .With(mock => mock
                    .Setup(m => m.Cardinality)
                    .Returns(Cardinality.OccursOnly(1)))
                .With(mock => mock
                    .Setup(m => m.TryRecognize(
                        It.IsAny<TokenReader>(),
                        It.IsAny<SymbolPath>(),
                        It.IsAny<ILanguageContext>(),
                        out It.Ref<GroupRecognitionResult>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        SymbolPath path,
                        ILanguageContext languageContext,
                        out GroupRecognitionResult result) =>
                    {
                        result = GroupRecognitionResult.Of(INodeSequence.Of(
                            ICSTNode.Of("dummy", Tokens.Of("source")),
                            true));
                        return true;
                    })));

            var unrecognizedElementMock = new Mock<IGroupRule>();
            unrecognizedElementMock
                .With(mock => mock
                    .Setup(m => m.Cardinality)
                    .Returns(Cardinality.OccursOnly(1)))
                .With(mock => mock
                    .Setup(m => m.TryRecognize(
                        It.IsAny<TokenReader>(),
                        It.IsAny<SymbolPath>(),
                        It.IsAny<ILanguageContext>(),
                        out It.Ref<GroupRecognitionResult>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        SymbolPath path,
                        ILanguageContext languageContext,
                        out GroupRecognitionResult result) =>
                    {
                        result = GroupRecognitionResult.Of(
                            new GroupRecognitionError(
                                elementCount: 0,
                                cause: FailedRecognitionError.Of(
                                    SymbolPath.Of("bleh"),
                                    10)));
                        return false;
                    })));

            var partiallyRecognizedElementMock = new Mock<IGroupRule>();
            partiallyRecognizedElementMock
                .With(mock => mock
                    .Setup(m => m.Cardinality)
                    .Returns(Cardinality.OccursOnly(1)))
                .With(mock => mock
                    .Setup(m => m.TryRecognize(
                        It.IsAny<TokenReader>(),
                        It.IsAny<SymbolPath>(),
                        It.IsAny<ILanguageContext>(),
                        out It.Ref<GroupRecognitionResult>.IsAny))
                    .Returns(new TryRecognizeNodeSequence((
                        TokenReader reader,
                        SymbolPath path,
                        ILanguageContext languageContext,
                        out GroupRecognitionResult result) =>
                    {
                        result = GroupRecognitionResult.Of(
                            new GroupRecognitionError(
                                elementCount: 0,
                                cause: PartialRecognitionError.Of(
                                    SymbolPath.Of("bleh"),
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
            Assert.IsTrue(result.Is(out INodeSequence nseq));
            Assert.AreEqual(2, nseq.Count);

            seq = Sequence.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                unrecognizedElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out GroupRecognitionError ge));
            Assert.IsInstanceOfType<FailedRecognitionError>(ge.Cause);
            Assert.AreEqual(1, ge.ElementCount);

            seq = Sequence.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                partiallyRecognizedElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out ge));
            Assert.AreEqual(1, ge.ElementCount);

            seq = Sequence.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                passingOptionalElementMock.Object,
                passingOptionalElementMock.Object,
                passingElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out nseq));
            Assert.IsFalse(nseq.IsOptional);
            Assert.AreEqual(4, nseq.Count);
            Assert.AreEqual(2, nseq.RequiredNodeCount);

            seq = Sequence.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                passingOptionalElementMock.Object,
                passingOptionalElementMock.Object,
                passingElementMock.Object,
                unrecognizedElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out ge));
            Assert.AreEqual(2, ge.ElementCount);
        }
    }
}
