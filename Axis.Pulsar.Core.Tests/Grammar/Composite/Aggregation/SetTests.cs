using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Utils;
using Moq;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Composite.Group;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Tests.Grammar.Composite.Groups
{
    [TestClass]
    public class SetTests
    {
        [TestMethod]
        public void TryRecognize_Tests()
        {
            #region Setup
            // setup
            var passingElementMock = new Mock<IAggregationElementRule>();
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
                        result = GroupRecognitionResult.Of(INodeSequence.Of(ISymbolNode.Of("dummy", Tokens.Of("source"))));
                        return true;
                    })));

            var passingOptionalElementMock = new Mock<IAggregationElementRule>();
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
                            ISymbolNode.Of("dummy", Tokens.Of("source")),
                            true));
                        return true;
                    })));

            var unrecognizedElementMock = new Mock<IAggregationElementRule>();
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

            var partiallyRecognizedElementMock = new Mock<IAggregationElementRule>();
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

            var set = Set.Of(
                Cardinality.OccursOnly(1),
                1,
                passingElementMock.Object,
                passingElementMock.Object);
            var success = set.TryRecognize("dummy", "dummy", null!, out var result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out INodeSequence nseq));
            Assert.AreEqual(2, nseq.Count);

            set = Set.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                unrecognizedElementMock.Object);
            success = set.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out GroupRecognitionError gre));
            Assert.IsInstanceOfType<FailedRecognitionError>(gre.Cause);
            Assert.AreEqual(1, gre.ElementCount);

            set = Set.Of(
                Cardinality.OccursOnly(1),
                1,
                unrecognizedElementMock.Object,
                unrecognizedElementMock.Object);
            success = set.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out gre));
            Assert.IsInstanceOfType<FailedRecognitionError>(gre.Cause);
            Assert.AreEqual(0, gre.ElementCount);

            set = Set.Of(
                Cardinality.OccursOnly(1),
                1,
                passingElementMock.Object,
                partiallyRecognizedElementMock.Object);
            success = set.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out gre));
            Assert.IsInstanceOfType<PartialRecognitionError>(gre.Cause);
            Assert.AreEqual(0, gre.ElementCount);

            set = Set.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                passingOptionalElementMock.Object,
                passingOptionalElementMock.Object,
                passingElementMock.Object);
            success = set.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsTrue(success);
            Assert.IsTrue(result.Is(out nseq));
            Assert.IsFalse(nseq.IsOptional);
            Assert.AreEqual(4, nseq.Count);
            Assert.AreEqual(2, nseq.RequiredNodeCount);

            set = Set.Of(
                Cardinality.OccursOnly(1),
                passingElementMock.Object,
                passingOptionalElementMock.Object,
                passingOptionalElementMock.Object,
                passingElementMock.Object,
                unrecognizedElementMock.Object);
            success = set.TryRecognize("dummy", "dummy", null!, out result);
            Assert.IsFalse(success);
            Assert.IsTrue(result.Is(out gre));
            Assert.AreEqual(2, gre.ElementCount);
        }
    }
}
