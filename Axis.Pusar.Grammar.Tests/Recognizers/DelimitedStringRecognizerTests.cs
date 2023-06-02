using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Language.Rules.CustomTerminals;
using Axis.Pulsar.Grammar.Recognizers;
using Axis.Pulsar.Grammar.Recognizers.Results;
using Axis.Pulsar.Grammar.Recognizers.CustomTerminals;
using Moq;
using static Axis.Pulsar.Grammar.Language.Rules.CustomTerminals.DelimitedString;
using System.Reflection.Metadata;

namespace Axis.Pusar.Grammar.Tests.Recognizers
{
    [TestClass]
    public class DelimitedStringRecognizerTests
    {

        [TestMethod]
        public void TryRecognize_WithValidArgs_ShouldSucceed()
        {
            // BSolAsciiEscapeMatcher
            Mock<Pulsar.Grammar.Language.Grammar> mockGrammar = new();
            var dsrule = new DelimitedString(
                "bleh",
                "'",
                new DelimitedString.BSolAsciiEscapeMatcher());

            var recognizer = new DelimitedStringRecognizer(dsrule, mockGrammar.Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("'stuff \\t\\''"),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);
            var success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("'stuff \\t\\\''", success.Symbol.TokenValue());

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

        [TestMethod]
        public void Parse_WithIllegalSequence_ShouldFail()
        {
            Mock<Pulsar.Grammar.Language.Grammar> mockGrammar = new();
            var dsrule = new DelimitedString(
                "bleh",
                "'",
                new[] { "\r", "\n" });

            var recognizer = new DelimitedStringRecognizer(dsrule, mockGrammar.Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("'stuff\n'"),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsFalse(recognized);
            var failure = result as FailureResult;
            Assert.IsNotNull(failure);
            Assert.AreEqual(7, failure.Position);
        }

        [TestMethod]
        public void Parse_WithLegalSequence_ShouldPass()
        {
            Mock<Pulsar.Grammar.Language.Grammar> mockGrammar = new();
            var dsrule = new DelimitedString(
                "bleh",
                "'",
                new[] { "a", "b", "c", "d", "e", "f", " " },
                Array.Empty<string>());

            var recognizer = new DelimitedStringRecognizer(dsrule, mockGrammar.Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("'bac cab fad deface bad'"),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);
            var success = result as SuccessResult;
            Assert.IsNotNull(success);
        }

        [TestMethod]
        public void Parse_WithLegalAndEscapeSequence_ShouldPass()
        {
            Mock<Pulsar.Grammar.Language.Grammar> mockGrammar = new();
            var dsrule = new DelimitedString(
                "bleh",
                "'",
                new[] { "a", "b", "c", "d", "e", "f", " " },
                Array.Empty<string>(),
                new BSolAsciiEscapeMatcher());

            var recognizer = new DelimitedStringRecognizer(dsrule, mockGrammar.Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("'bac cab fad \\' deface bad'"),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);
            var success = result as SuccessResult;
            Assert.IsNotNull(success);
        }

        [TestMethod]
        public void Parse_WithLegalAndEscapeAndIllegalSequence_ShouldFail()
        {
            Mock<Pulsar.Grammar.Language.Grammar> mockGrammar = new();
            var dsrule = new DelimitedString(
                "bleh",
                "'",
                new[] { "a", "b", "c", "d", "e", "f", " " },
                new[] { "ce ba" },
                new BSolAsciiEscapeMatcher());

            var recognizer = new DelimitedStringRecognizer(dsrule, mockGrammar.Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("'bac cab fad \\' deface bad'"),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsFalse(recognized);
            var failure = result as FailureResult;
            Assert.IsNotNull(failure);
        }

        [TestMethod]
        public void Parse_WithLegalAndEscapeAndIllegalSequence2_ShouldPass()
        {
            Mock<Pulsar.Grammar.Language.Grammar> mockGrammar = new();
            var dsrule =
                new DelimitedString(
                    "blob",
                    "{{",
                    "}}",
                    new[] 
                    { 
                        "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K",
                        "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V",
                        "W", "X", "Y", "Z",
                        "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k",
                        "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v",
                        "w", "x", "y", "z",
                        "1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
                        "/", "=", "+", " ", "\t", "\n", "\r"
                    },
                    Array.Empty<string>());

            var recognizer = new DelimitedStringRecognizer(dsrule, mockGrammar.Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader(BLOB.Trim()),
                out IRecognitionResult result);

            Console.WriteLine(result);
            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);
            var success = result as SuccessResult;
            Assert.IsNotNull(success);
        }

        private static readonly string BLOB = @"
{{
A
 R E
Z H i
 w 3 P
E h R Y 2
 d 1 f Y u
O n K W x t
 c b M 0 9 /
v 9 v 8 A
}}
";
    }
}
