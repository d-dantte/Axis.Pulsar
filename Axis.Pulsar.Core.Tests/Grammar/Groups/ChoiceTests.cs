using Axis.Luna.Common.Results;
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
    public class ChoiceTests
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
                                    SymbolPath.Of("bleh"), 10, 5)));
                        return false;
                    })));

            var ch = Choice.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                passingElementMock.Object);
            var success = ch.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out INodeSequence nseq));
            Assert.AreEqual(1, nseq.Count);

            ch = Choice.Of(
                Cardinality.OccursOnly(1),
                unrecognizedElementMock.Object,
                passingElementMock.Object);
            success = ch.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out nseq));
            Assert.AreEqual(1, nseq.Count);

            ch = Choice.Of(
                Cardinality.OccursOnly(1),
                partiallyRecognizedElementMock.Object,
                passingElementMock.Object);
            success = ch.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out GroupRecognitionError ge));
            Assert.AreEqual(0, ge.ElementCount);
        }
    }
}
