using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Recognizers;
using Axis.Pulsar.Grammar.Recognizers.Results;
using Moq;

namespace Axis.Pusar.Grammar.Tests.Recognizers
{
    using MockGrammar = Mock<Pulsar.Grammar.Language.Grammar>;


    [TestClass]
    public class SetRecognizerTests
    {
        [TestMethod]
        public void Constructor_ShouldReturnValidInstance()
        {
            var set = new Set(
                new Literal("meh"),
                new Literal("bleh"));

            var recognizer = new SetRecognizer(set, new MockGrammar().Object);
            Assert.IsNotNull(recognizer);

            set = new Set(
                Cardinality.OccursOnlyOnce(),
                1,
                new Literal("meh"),
                new Literal("bleh"));

            recognizer = new SetRecognizer(set, new MockGrammar().Object);
            Assert.IsNotNull(recognizer);
        }

        [TestMethod]
        public void Constructor_WithInvalidArgs_ShouldThrowExceptions()
        {
            Assert.ThrowsException<ArgumentException>(() => new SetRecognizer(default, new MockGrammar().Object));
            Assert.ThrowsException<ArgumentNullException>(() => new SetRecognizer(
                new Set(
                    new Literal("meh"),
                    new Literal("bleh")),
                null));
        }

        [TestMethod]
        public void TryRecognize_WithValidArgs()
        {
            var set = new Set(
                new Literal("meh "),
                new Literal("bleh "));
            var recognizer = new SetRecognizer(set, new MockGrammar().Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("bleh meh "),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);

            var success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("bleh meh ", success.Symbol.TokenValue());

            // with cardinality
            set = new Set(
                Cardinality.OccursOnly(2),
                new Literal("meh"),
                new Literal("bleh"));
            recognizer = new SetRecognizer(set, new MockGrammar().Object);

            recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("blehmehmehbleh"),
                out result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);

            success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("blehmehmehbleh", success.Symbol.TokenValue());

            // with optional rule
            var set2 = new Set(
                Cardinality.OccursOptionally(),
                new Literal("bleh"));

            set = new Set(
                new Literal("meh"),
                set2);
            recognizer = new SetRecognizer(set, new MockGrammar().Object);

            recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("meh"),
                out result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);

            success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("meh", success.Symbol.TokenValue());

            // with min recognition count
            set = new Set(
                Cardinality.OccursOnly(1),
                1,
                new Literal("meh"),
                new Literal("bleh"));
            recognizer = new SetRecognizer(set, new MockGrammar().Object);

            recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("meh"),
                out result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);

            success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("meh", success.Symbol.TokenValue());
        }

        [TestMethod]
        public void TryRecognize_WithFatalRecognizer_ShouldAbortRecognition()
        {
            var exception = new InvalidOperationException();
            var mockFatalRecognizer = MockHelper.MockErroredRecognizerRule<IAtomicRule>(exception);
            var set = new Set(
                new Literal("meh"),
                mockFatalRecognizer.Object);
            var recognizer = new SetRecognizer(set, new MockGrammar().Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("helb"),
                out IRecognitionResult result);

            Assert.IsFalse(recognized);
            Assert.IsNotNull(result);
            var error = result as ErrorResult;
            Assert.IsNotNull(error);
            Assert.AreEqual(exception, error.Exception);
        }

        [TestMethod]
        public void TryRecognize_WithPartialRecognition_ShouldAbortRecognition()
        {
            var exception = new InvalidOperationException();
            var mockFatalRecognizer = MockHelper.MockPartialRecognizerRule<IAtomicRule>(
                "expected_symbol",
                0,
                IReason.Of("expected tokens"),
                new[] {
                    CSTNode.Of("_symbol_1", "partial-1"),
                    CSTNode.Of("_symbol_2", "partial-2")
                });
            var set = new Set(
                Cardinality.OccursAtLeast(1),
                new Literal("meh"),
                mockFatalRecognizer.Object);
            var recognizer = new SetRecognizer(set, new MockGrammar().Object);

            var reader = new Pulsar.Grammar.BufferedTokenReader("mehhelb");
            var recognized = recognizer.TryRecognize(
                reader,
                out IRecognitionResult result);

            Assert.IsFalse(recognized);
            Assert.IsNotNull(result);
            var partial = result as ErrorResult;
            Assert.IsNotNull(partial);
            var partialRecognition = partial.Exception;
            Assert.IsNotNull(partialRecognition);
        }

        [TestMethod]
        public void TryRecognize_WithFailingRecognizer_ShouldFail()
        {
            var set = new Set(
                new Literal("meh "),
                new Literal("bleh "),
                new Literal("deh "));
            var recognizer = new SetRecognizer(set, new MockGrammar().Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("bleh meh dlkaf"),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsFalse(recognized);

            var failure = result as FailureResult;
            Assert.IsNotNull(failure);
            Assert.AreEqual(0, failure.Position);
        }
    }
}
