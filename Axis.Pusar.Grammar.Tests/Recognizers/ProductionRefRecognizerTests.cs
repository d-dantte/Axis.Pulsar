using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Recognizers;
using Axis.Pulsar.Grammar.Recognizers.Results;
using Moq;

namespace Axis.Pusar.Grammar.Tests.Recognizers
{
    [TestClass]
    public class ProductionRefRecognizerTests
    {

        [TestMethod]
        public void Constructor_ShouldReturnValidInstance()
        {
            Mock<Pulsar.Grammar.Language.Grammar> MockGrammar = new();
            var prodRef = new ProductionRef(
                "meh_literal");

            var prodRefRecognizer = new ProductionRefRecognizer(prodRef, MockGrammar.Object);

            Assert.IsNotNull(prodRefRecognizer);
        }

        [TestMethod]
        public void Constructor_WithInvalidArgs_ShouldThrowExceptions()
        {
            Assert.ThrowsException<ArgumentException>(() => new ProductionRefRecognizer(default, new Mock<Pulsar.Grammar.Language.Grammar>().Object));
            Assert.ThrowsException<ArgumentNullException>(() => new ProductionRefRecognizer(new ProductionRef("beh"), null));
        }

        [TestMethod]
        public void TryRecognize_WithValidArgs()
        {
            Mock<Pulsar.Grammar.Language.Grammar> mockGrammar = new();
            _ = MockHelper.MockRefGrammar(
                mockGrammar,
                ("meh_literal", new Literal("meh ").ToRecognizer(mockGrammar.Object)));

            var prodRef = new ProductionRef(
                "meh_literal");

            var recognizer = new ProductionRefRecognizer(prodRef, mockGrammar.Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("meh bleh "),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);
            var success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("meh ", success.Symbol.TokenValue());

            // with cardinality
            prodRef = new ProductionRef(
                "meh_literal",
                Cardinality.OccursAtLeast(2));

            recognizer = new ProductionRefRecognizer(prodRef, mockGrammar.Object);

            recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("meh meh meh mex "),
                out result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);
            success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("meh meh meh ", success.Symbol.TokenValue());

            // with optional cardinality
            prodRef = new ProductionRef(
                "meh_literal",
                Cardinality.OccursOptionally());

            recognizer = new ProductionRefRecognizer(prodRef, mockGrammar.Object);

            recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("mex meh meh mex "),
                out result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);
            success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("", success.Symbol.TokenValue());

        }

        [TestMethod]
        public void TryRecognize_WithErrorRecognition_ShouldAbortRecognition()
        {
            var expectedException = new InvalidOperationException("any exception, including partial recognition");
            var mockFatalRule = MockHelper.MockErroredRecognizerRule<IAtomicRule>(expectedException);

            var mockGrammar = new Mock<Pulsar.Grammar.Language.Grammar>();
            _ = MockHelper.MockRefGrammar(
                mockGrammar,
                ("meh_literal", mockFatalRule.Object.ToRecognizer(mockGrammar.Object)));

            var prodRef = new ProductionRef(
                "meh_literal");

            var recognizer = new ProductionRefRecognizer(prodRef, mockGrammar.Object);

            var reader = new Pulsar.Grammar.BufferedTokenReader("mehhelb");
            var recognized = recognizer.TryRecognize(
                reader,
                out IRecognitionResult result);

            Assert.IsFalse(recognized);
            Assert.IsNotNull(result);
            var partial = result as ErrorResult;
            Assert.IsNotNull(partial);
            Assert.AreEqual(expectedException, partial.Exception);
        }

        [TestMethod]
        public void TryRecognize_WithFailingRecognizer_ShouldFail()
        {
            var mockFailingRule = MockHelper.MockFailedRecognizerRule<IAtomicRule>("meh_literal");
            var mockGrammar = new Mock<Pulsar.Grammar.Language.Grammar>();
            _ = MockHelper.MockRefGrammar(
                mockGrammar,
                ("meh_literal", mockFailingRule.Object.ToRecognizer(mockGrammar.Object)));

            var prodRef = new ProductionRef(
                "meh_literal");

            var recognizer = new ProductionRefRecognizer(prodRef, mockGrammar.Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("meh bleh "),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsFalse(recognized);

            var failure = result as FailureResult;
            Assert.IsNotNull(failure);
            Assert.AreEqual(0, failure.Position);
        }
    }
}
