using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Groups;
using Axis.Pulsar.Core.Utils;
using Moq;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Results;

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

            var unrecognizedElementMock = new Mock<IGroupElement>();
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
                                cause: PartialRecognitionError.Of("bleh", 10, 5)));
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
            Assert.IsTrue(result.Is(out INodeSequence nseq));
            Assert.AreEqual(2, nseq.Count);

            seq = Set.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                unrecognizedElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out GroupRecognitionError gre));
            Assert.IsInstanceOfType<PartialRecognitionError>(gre.Cause);
            Assert.AreEqual(1, gre.ElementCount);

            seq = Set.Of(
                Cardinality.OccursOnly(1),
                1,
                unrecognizedElementMock.Object,
                unrecognizedElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out gre));
            Assert.IsInstanceOfType<FailedRecognitionError>(gre.Cause);
            Assert.AreEqual(0, gre.ElementCount);

            seq = Set.Of(
                Cardinality.OccursOnly(1),
                1,
                passingElementMock.Object,
                partiallyRecognizedElementMock.Object);
            success = seq.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out gre));
            Assert.IsInstanceOfType<PartialRecognitionError>(gre.Cause);
            Assert.AreEqual(0, gre.ElementCount);
        }
    }
}
