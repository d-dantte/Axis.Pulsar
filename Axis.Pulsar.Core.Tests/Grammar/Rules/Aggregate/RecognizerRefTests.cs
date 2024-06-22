using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Rules.Aggregate;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using Moq;

namespace Axis.Pulsar.Core.Tests.Grammar.Rules.Aggregate
{
    [TestClass]
    public class RecognizerRefTests
    {
        [TestMethod]
        public void TryRecognizeWithPassingRef_Tests()
        {
            var rule = MockRule(NodeRecognitionResult.Of(ISymbolNode.Of("id", Tokens.Of("bleh"))));
            var arr = new MockRecognizerRef { RecognizerInstance = rule };
            var recognized = arr.TryRecognize("bleh", "sym", null!, out var result);

            Assert.IsTrue(recognized);
            Assert.IsTrue(result.Is(out ISymbolNode _));
        }

        [TestMethod]
        public void TryRecognizeWithFailedRef_Tests()
        {
            var rule = MockRule(NodeRecognitionResult.Of(FailedRecognitionError.Of("sym", 0)));
            var arr = new MockRecognizerRef { RecognizerInstance = rule };
            var recognized = arr.TryRecognize("bleh", "sym", null!, out var result);

            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out AggregateRecognitionError gre));
            Assert.IsTrue(gre.Cause is FailedRecognitionError);
        }

        [TestMethod]
        public void TryRecognizeWithPartialRef_Tests()
        {
            var rule = MockRule(NodeRecognitionResult.Of(PartialRecognitionError.Of("sym", 0, 1)));
            var arr = new MockRecognizerRef { RecognizerInstance = rule };
            var recognized = arr.TryRecognize("bleh", "sym", null!, out var result);

            Assert.IsFalse(recognized);
            Assert.IsTrue(result.Is(out AggregateRecognitionError gre));
            Assert.IsTrue(gre.Cause is PartialRecognitionError);
        }


        internal static IRecognizer<NodeRecognitionResult> MockRule(NodeRecognitionResult recognitionResult)
        {
            var mock = new Mock<IRecognizer<NodeRecognitionResult>>();
            mock.Setup(m => m.TryRecognize(
                    It.IsAny<TokenReader>(),
                    It.IsAny<SymbolPath>(),
                    It.IsAny<ILanguageContext>(),
                    out It.Ref<NodeRecognitionResult>.IsAny))
                .Returns(
                    new NodeRecognition((
                        TokenReader reader,
                        SymbolPath path,
                        ILanguageContext cxt,
                        out NodeRecognitionResult result) =>
                    {
                        result = recognitionResult;
                        return result.Is(out ISymbolNode _);
                    }));

            return mock.Object;
        }

        #region Nested types
        internal class MockRecognizerRef : RecognizerRef
        {
            public IRecognizer<NodeRecognitionResult>? RecognizerInstance { get; set; }

            public override AggregationType Type => AggregationType.Unit;

            internal protected override IRecognizer<NodeRecognitionResult> Recognizer(
                ILanguageContext context)
                => RecognizerInstance!;
        }
        #endregion
    }
}
