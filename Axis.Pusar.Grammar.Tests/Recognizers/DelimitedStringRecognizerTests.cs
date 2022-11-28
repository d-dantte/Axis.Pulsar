using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Language.Rules.CustomTerminals;
using Axis.Pulsar.Grammar.Recognizers;
using Axis.Pulsar.Grammar.Recognizers.Results;
using Axis.Pulsar.Grammar.Recognizers.SpecialTerminals;
using Moq;

namespace Axis.Pusar.Grammar.Tests.Recognizers
{
    [TestClass]
    public class DelimitedStringRecognizerTests
    {

        [TestMethod]
        public void TryRecognize_WithValidArgs_ShouldSucceed()
        {
            // BSolUTF16EscapeMatcher
            Mock<Pulsar.Grammar.Language.Grammar> mockGrammar = new();
            var dsrule = new DelimitedString(
                "bleh",
                "'",
                new DelimitedString.BSolAsciiEscapeMatcher());

            var recognizer = new DelimitedStringRecognizer(dsrule, mockGrammar.Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("'stuff \\n\\''"),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);
            var success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("'stuff \\n\\\''", success.Symbol.TokenValue());

            // BSolUTF16EscapeMatcher
            dsrule = new DelimitedString(
                "bleh",
                "'",
                new DelimitedString.BSolUTF16EscapeMatcher());

            recognizer = new DelimitedStringRecognizer(dsrule, mockGrammar.Object);

            recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("'stuff \\u0a2f'"),
                out result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);
            success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("'stuff \\u0a2f'", success.Symbol.TokenValue());

            // BSolGeneralEscapeMatcher
            dsrule = new DelimitedString(
                "bleh",
                "'",
                new DelimitedString.BSolGeneralEscapeMatcher());

            recognizer = new DelimitedStringRecognizer(dsrule, mockGrammar.Object);

            recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("'stuff \\u0a2f\\\''"),
                out result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);
            success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("'stuff \\u0a2f\\\''", success.Symbol.TokenValue());
        }
    }
}
