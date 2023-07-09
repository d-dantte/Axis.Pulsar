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
    public class SequenceRecognizerTests
    {
        [TestMethod]
        public void Constructor_ShouldReturnValidInstance()
        {
            var sequence = new Sequence(
                new Literal("meh"),
                new Literal("bleh"));

            var recognizer = new SequenceRecognizer(sequence, new MockGrammar().Object);
            Assert.IsNotNull(recognizer);

            sequence = new Sequence(
                Cardinality.OccursOnlyOnce(),
                new Literal("meh"),
                new Literal("bleh"));

            recognizer = new SequenceRecognizer(sequence, new MockGrammar().Object);
            Assert.IsNotNull(recognizer);
        }

        [TestMethod]
        public void Constructor_WithInvalidArgs_ShouldThrowExceptions()
        {
            Assert.ThrowsException<ArgumentException>(() => new SequenceRecognizer(default, new MockGrammar().Object));
            Assert.ThrowsException<ArgumentNullException>(() => new SequenceRecognizer(
                new Sequence(
                    new Literal("meh")),
                null));
        }

        [TestMethod]
        public void TryRecognize_WithValidArgs()
        {
            var sequence = new Sequence(
                new Literal("meh "),
                new Literal("bleh "));
            var recognizer = new SequenceRecognizer(sequence, new MockGrammar().Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("meh bleh "),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);

            var success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("meh bleh ", success.Symbol.TokenValue());

            // with cardinality
            sequence = new Sequence(
                Cardinality.OccursOnly(2),
                new Literal("meh"),
                new Literal("bleh"));
            recognizer = new SequenceRecognizer(sequence, new MockGrammar().Object);

            recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("mehblehmehbleh"),
                out result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);

            success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("mehblehmehbleh", success.Symbol.TokenValue());

            // with optional rule
            var sequence2 = new Sequence(
                Cardinality.OccursOptionally(),
                new Literal("bleh"));

            sequence = new Sequence(
                new Literal("meh"),
                sequence2);
            recognizer = new SequenceRecognizer(sequence, new MockGrammar().Object);

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
        public void TryRecognize_WithPartialRecognition_ShouldAbortRecognition()
        {
            var exception = new InvalidOperationException();
            var mockFatalRecognizer = MockHelper.MockPartialRecognizerRule<IAtomicRule>(
                "expected_symbol",
                0,
                IReason.Of("expected tokens"),
                new[] {
                    CSTNode.Of(CSTNode.TerminalType.Literal, "_symbol_1", "partial-1"),
                    CSTNode.Of(CSTNode.TerminalType.Literal, "_symbol_2", "partial-2")
                });
            var sequence = new Sequence(
                Cardinality.OccursAtLeast(1),
                new Literal("meh"),
                mockFatalRecognizer.Object);
            var recognizer = new SequenceRecognizer(sequence, new MockGrammar().Object);

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
            var sequence = new Sequence(
                new Literal("meh "),
                new Literal("bleh "),
                new Literal("deh "));
            var recognizer = new SequenceRecognizer(sequence, new MockGrammar().Object);

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
