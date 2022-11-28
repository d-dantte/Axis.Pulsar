using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Recognizers;
using Axis.Pulsar.Grammar.Recognizers.Results;
using Moq;

namespace Axis.Pusar.Grammar.Tests.Recognizers
{
    using PulsarGrammar = Pulsar.Grammar.Language.Grammar;

    [TestClass]
    public class ChoiceRecognizerTests
    {
        private static Mock<PulsarGrammar> MockGrammar = new Mock<PulsarGrammar>();

        [TestMethod]
        public void Constructor_ShouldReturnValidInstance()
        {
            var choice = new Choice(
                new Literal("meh"),
                new Literal("bleh"));

            var recognizer = new ChoiceRecognizer(choice, MockGrammar.Object);
            Assert.IsNotNull(recognizer);

            choice = new Choice(
                Cardinality.OccursOnlyOnce(),
                new Literal("meh"),
                new Literal("bleh"));

            recognizer = new ChoiceRecognizer(choice, MockGrammar.Object);
            Assert.IsNotNull(recognizer);
        }

        [TestMethod]
        public void Constructor_WithInvalidArgs_ShouldThrowExceptions()
        {
            Assert.ThrowsException<ArgumentException>(() => new ChoiceRecognizer(default, MockGrammar.Object));
            Assert.ThrowsException<ArgumentNullException>(() => new ChoiceRecognizer(
                new Choice(
                    new Literal("meh"),
                    new Literal("bleh")),
                null));
        }

        [TestMethod]
        public void TryRecognize_WithValidArgs()
        {
            var choice = new Choice(
                new Literal("meh"),
                new Literal("bleh"));
            var recognizer = new ChoiceRecognizer(choice, MockGrammar.Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("bleh"),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);

            var success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("bleh", success.Symbol.TokenValue());

            // with cardinality
            choice = new Choice(
                Cardinality.OccursOnly(2),
                new Literal("meh"),
                new Literal("bleh"));
            recognizer = new ChoiceRecognizer(choice, MockGrammar.Object);

            recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("blehmeh"),
                out result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);

            success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("blehmeh", success.Symbol.TokenValue());
        }

        [TestMethod]
        public void TryRecognize_WithFatalRecognizer_ShouldAbortRecognition()
        {
            var exception = new InvalidOperationException();
            var mockFatalRecognizer = MockHelper.MockErroredRecognizerRule<IAtomicRule>(exception);
            var choice = new Choice(
                new Literal("meh"),
                mockFatalRecognizer.Object);
            var recognizer = new ChoiceRecognizer(choice, MockGrammar.Object);

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
            var choice = new Choice(
                Cardinality.OccursAtLeast(1),
                new Literal("meh"),
                mockFatalRecognizer.Object);
            var recognizer = new ChoiceRecognizer(choice, MockGrammar.Object);

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
            var choice = new Choice(
                Cardinality.OccursAtLeast(3),
                new Literal("meh "),
                new Literal("bleh "));
            var recognizer = new ChoiceRecognizer(choice, MockGrammar.Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("bleh meh dlkaf"),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsFalse(recognized);

            var failure = result as FailureResult;
            Assert.IsNotNull(failure);
            Assert.AreEqual(9, failure.Position);
        }
    }
}
