using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Recognizers;
using Axis.Pulsar.Grammar.Recognizers.Results;
using Moq;

namespace Axis.Pusar.Grammar.Tests.Recognizers
{
    [TestClass]
    public class LiteralRecognizerTests
    {
        private static Mock<Pulsar.Grammar.Language.Grammar> MockGrammar = new Mock<Pulsar.Grammar.Language.Grammar>();

        [TestMethod]
        public void Constructor_ShouldReturnValidInstance()
        {
            var literal = new Literal("meh");

            var recognizer = new LiteralRecognizer(literal, MockGrammar.Object);
            Assert.IsNotNull(recognizer);
        }

        [TestMethod]
        public void Constructor_WithInvalidArgs_ShouldThrowExceptions()
        {
            Assert.ThrowsException<ArgumentException>(() => new LiteralRecognizer(default, MockGrammar.Object));
            Assert.ThrowsException<ArgumentNullException>(() => new LiteralRecognizer(new Literal("meh"), null));
        }

        [TestMethod]
        public void TryRecognize_WithValidArgs_ShouldSucceed()
        {
            var recognizer = new LiteralRecognizer(new Literal("stuff "), MockGrammar.Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("stuff "),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);

            var success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("stuff ", success.Symbol.TokenValue());


            recognizer = new LiteralRecognizer(
                new Literal("stuff\nother stuff\u00a9"),
                MockGrammar.Object);

            recognized = recognizer.TryRecognize(
                "stuff\nother stuff\u00a9",
                out result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);

            success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("stuff\nother stuff\u00a9", success.Symbol.TokenValue());
        }

        [TestMethod]
        public void TryRecognize_WithInvalidTokens_ShouldFail()
        {
            var recognizer = new LiteralRecognizer(new Literal("stuff "), MockGrammar.Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("stuf "),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsFalse(recognized);

            var failed = result as FailureResult;
            Assert.IsNotNull(failed);
            Assert.AreEqual(0, failed.Position);
        }
    }
}
