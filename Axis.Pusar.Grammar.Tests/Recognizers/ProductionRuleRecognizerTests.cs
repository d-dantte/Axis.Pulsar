using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Exceptions;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Recognizers;
using Axis.Pulsar.Grammar.Recognizers.Results;
using Moq;

namespace Axis.Pusar.Grammar.Tests.Recognizers
{
    using PulsarGrammar = Pulsar.Grammar.Language.Grammar;
    using MockGrammar = Mock<Pulsar.Grammar.Language.Grammar>;

    [TestClass]
    public class ProductionRuleRecognizerTests
    {
        [TestMethod]
        public void Constructor_ShouldReturnValidInstance()
        {
            var rule = new ProductionRule(
                "meh_literal",
                new Literal("foo"));

            var prodRefRecognizer = new ProductionRuleRecognizer(rule, new MockGrammar().Object);
            Assert.IsNotNull(prodRefRecognizer);
        }

        [TestMethod]
        public void Constructor_WithInvalidArgs_ShouldThrowExceptions()
        {
            Assert.ThrowsException<ArgumentException>(() => new ProductionRuleRecognizer(default, new MockGrammar().Object));
            Assert.ThrowsException<ArgumentNullException>(() => new ProductionRuleRecognizer(
                new ProductionRule(
                    "meh_literal",
                    new Literal("foo")),
                null));
        }

        [TestMethod]
        public void TryRecognize_WithValidArgs()
        {
            #region sample literal rule
            var rule = new ProductionRule(
                "foo_literal",
                new Literal("foo"));

            var recognizer = new ProductionRuleRecognizer(rule, new MockGrammar().Object);

            var recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("foo and then some"),
                out IRecognitionResult result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);
            var success = result as SuccessResult;
            Assert.IsNotNull(success);
            Assert.AreEqual(0, success.Position);
            Assert.AreEqual("foo", success.Symbol.TokenValue());
            #endregion

            #region successful rule, with a mix of branch and leaf nodes in the syntax tree
            /// Node structure
            /// - B0 
            ///   - L00
            ///   - B01
            ///     - B010R
            ///       - B010R0
            ///       - B010R1
            ///     - L011
            ///     - L012
            ///   - L02
            ///   - B03R
            ///     - B03R0
            ///
            var tree = CSTNode.Of(
                "B0",
                CSTNode.Of("L00", "L00-tokens"),
                CSTNode.Of(
                    "B01",
                    CSTNode.Of(
                        "@B010R.Ref",
                        CSTNode.Of("B010R0"),
                        CSTNode.Of("B010R1")),
                    CSTNode.Of("L011", "L011-tokens"),
                    CSTNode.Of("L012", "L012-tokens")),
                CSTNode.Of("L02", "L02-tokens"),
                CSTNode.Of(
                    "@B03R.Ref",
                    CSTNode.Of("B03R0")));
            var mockRule = MockHelper.MockSuccessRecognizerRule<IAggregateRule>("B0", tree);
            rule = new ProductionRule("_root", mockRule.Object);
            recognizer = new ProductionRuleRecognizer(rule, new MockGrammar().Object);

            recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("irrelevant"),
                out result);

            Assert.IsNotNull(result);
            Assert.IsTrue(recognized);
            success = result as SuccessResult;
            Assert.IsNotNull(success);
            var nodeNames = success.Symbol
                .AllChildNodes()
                .Select(n => n.SymbolName)
                .ToArray();
            var expectedNodeNames = new[]
            {
                "L00", "B010R0", "B010R1", "L011", "L012", "L02", "B03R0"
            };

            Assert.IsTrue(expectedNodeNames.SequenceEqual(nodeNames));

            #endregion

            #region invalidated successful rule
            var faultingValidator = MockHelper.MockValidator(new ProductionValidationResult.Error("some syntax error"));
            rule = new ProductionRule(
                "foo_literal",
                new Literal("foo"),
                faultingValidator.Object);

            recognizer = new ProductionRuleRecognizer(rule, new MockGrammar().Object);

            recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("foo and then some"),
                out result);

            Assert.IsNotNull(result);
            Assert.IsFalse(recognized);
            var error = result as ErrorResult;
            Assert.IsNotNull(error);
            Assert.IsTrue(error.Exception is PartialRecognitionException);
            #endregion

            #region Failing rule
            mockRule = MockHelper.MockFailedRecognizerRule<IAggregateRule>("B0", IReason.Of("expected-token"));
            rule = new ProductionRule("_root", mockRule.Object);
            recognizer = new ProductionRuleRecognizer(rule, new MockGrammar().Object);

            recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("irrelevant"),
                out result);

            Assert.IsNotNull(result);
            Assert.IsFalse(recognized);
            Assert.IsTrue(result is FailureResult);

            // aggregation failure, past recognition threshold
            mockRule = MockHelper.MockFailedRecognizerRule<IAggregateRule>("B0", IReason.Of(IReason.Of("expected-token"), tree));
            rule = new ProductionRule("_root", 3, mockRule.Object);
            recognizer = new ProductionRuleRecognizer(rule, new MockGrammar().Object);

            recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("irrelevant"),
                out result);

            Assert.IsNotNull(result);
            Assert.IsFalse(recognized);
            Assert.IsTrue(result is ErrorResult);
            #endregion

            #region Error Rule
            mockRule = MockHelper.MockErroredRecognizerRule<IAggregateRule>(new Exception());
            rule = new ProductionRule("_root", mockRule.Object);
            recognizer = new ProductionRuleRecognizer(rule, new MockGrammar().Object);

            recognized = recognizer.TryRecognize(
                new Pulsar.Grammar.BufferedTokenReader("irrelevant"),
                out result);

            Assert.IsNotNull(result);
            Assert.IsFalse(recognized);
            Assert.IsTrue(result is ErrorResult);
            #endregion
        }
    }
}
