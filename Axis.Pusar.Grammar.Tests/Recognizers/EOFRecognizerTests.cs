using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Recognizers;
using Axis.Pulsar.Grammar.Recognizers.Results;
using Moq;

namespace Axis.Pusar.Grammar.Tests.Recognizers
{
    [TestClass]
    public class EOFRecognizerTests
    {
        [TestMethod]
        public void TryRecognize_WithValidArgs_ShouldSucceed()
        {
            var recognizer = new EOFRecognizer(default, new Mock<Pulsar.Grammar.Language.Grammar>().Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader(""),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);

            var success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("", success.Symbol.TokenValue());
        }

        [TestMethod]
        public void TryRecognize_WithInvalidTokens_ShouldFail()
        {
            var recognizer = new EOFRecognizer(default, new Mock<Pulsar.Grammar.Language.Grammar>().Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("abcd "),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsFalse(recognized);

            var failed = result as FailureResult;
            Assert.IsNotNull(failed);
            Assert.AreEqual(0, failed.Position);
        }
    }
}
