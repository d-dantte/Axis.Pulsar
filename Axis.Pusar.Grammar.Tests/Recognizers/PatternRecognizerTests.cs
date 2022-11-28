using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Recognizers;
using Axis.Pulsar.Grammar.Recognizers.Results;
using Moq;
using System.Text.RegularExpressions;

namespace Axis.Pusar.Grammar.Tests.Recognizers
{
    using PatternMatchType = Axis.Pulsar.Grammar.Language.MatchType;

    [TestClass]
    public class PatternRecognizerTests
    {
        private static Mock<Pulsar.Grammar.Language.Grammar> MockGrammar = new Mock<Pulsar.Grammar.Language.Grammar>();
        [TestMethod]
        public void Constructor_ShouldReturnValidInstance()
        {
            var pattern = new Pattern(new Regex("meh"));
            var recognizer = new PatternRecognizer(pattern, MockGrammar.Object);
            Assert.IsNotNull(recognizer);

            pattern = new Pattern(new Regex("meh"), PatternMatchType.Of(1, false));
            recognizer = new PatternRecognizer(pattern, MockGrammar.Object);
            Assert.IsNotNull(recognizer);

            pattern = new Pattern(new Regex("meh"), PatternMatchType.Of(1, 3));
            recognizer = new PatternRecognizer(pattern, MockGrammar.Object);
            Assert.IsNotNull(recognizer);
        }

        [TestMethod]
        public void Constructor_WithInvalidArgs_ShouldThrowExceptions()
        {
            Assert.ThrowsException<ArgumentException>(() => new PatternRecognizer(default, MockGrammar.Object));
            Assert.ThrowsException<ArgumentNullException>(() => new PatternRecognizer(new Pattern(new Regex("")), null));
        }

        [TestMethod]
        public void TryRecognize_WithValidArgs_ShouldSucceed()
        {
            var recognizer = new PatternRecognizer(new Pattern(new Regex(@"^\d+")), MockGrammar.Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("77654"),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);
            var success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("77654", success.Symbol.TokenValue());

            // with open match-set
            recognizer = new PatternRecognizer(
                new Pattern(
                    new Regex(@"^([A-Z]\d )+$"),
                    PatternMatchType.Of(3, false)),
                MockGrammar.Object);

            recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("A6 Q5 W1 W0 "),
                out result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);
            success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("A6 Q5 W1 W0 ", success.Symbol.TokenValue());

            // with closed match-set
            recognizer = new PatternRecognizer(
                new Pattern(
                    new Regex(@"^Caladan Brood|Dragnipur$"),
                    PatternMatchType.Of(9, 13)),
                MockGrammar.Object);

            recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("Dragnipurake Pureblood$"),
                out result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);
            success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("Dragnipur", success.Symbol.TokenValue());
        }

        [TestMethod]
        public void TryRecognize_WithInvalidTokens_ShouldFail()
        {
            var recognizer = new PatternRecognizer(new Pattern(new Regex(@"^\d+")), MockGrammar.Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("y77654"),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsFalse(recognized);
            var failure = result as FailureResult;
            Assert.IsNotNull(failure);
            Assert.AreEqual(0, failure.Position);
        }
    }
}
