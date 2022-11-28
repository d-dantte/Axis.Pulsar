using Axis.Pulsar.Grammar;
using Axis.Pulsar.Grammar.Exceptions;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Recognizers;
using Axis.Pulsar.Grammar.Recognizers.Results;
using Moq;

namespace Axis.Pusar.Grammar.Tests
{
    using GrammarCSTNode = Pulsar.Grammar.CST.CSTNode;
    using PulsarGrammar = Pulsar.Grammar.Language.Grammar;

    public static class MockHelper
    {
        public static Mock<TRule> MockSuccessRecognizerRule<TRule>(
            string symbolName,
            GrammarCSTNode symbolNode,
            Action<Mock<TRule>>? extraSetups = null)
            where TRule : class, IRule
        {
            var mockRule = new Mock<TRule>();
            var mockRecognizer = new Mock<IRecognizer>();

            mockRule
                .Setup(r => r.ToRecognizer(It.IsAny<PulsarGrammar>()))
                .Returns(mockRecognizer.Object);

            if (extraSetups is not null)
                extraSetups.Invoke(mockRule);

            mockRule
                .Setup(r => r.SymbolName)
                .Returns(symbolName);

            mockRecognizer
                .Setup(r => r.Recognize(It.IsAny<BufferedTokenReader>()))
                .Returns<BufferedTokenReader>(btr => new SuccessResult(btr.Position + 1, symbolNode))
                .Verifiable();

            mockRecognizer
                .Setup(r => r.TryRecognize(
                    It.IsAny<BufferedTokenReader>(),
                    out It.Ref<IRecognitionResult>.IsAny))
                .Returns((BufferedTokenReader btr, out IRecognitionResult result) =>
                {
                    result = new SuccessResult(btr.Position + 1, symbolNode);
                    return false;
                });

            return mockRule;
        }

        public static Mock<TRule> MockFailedRecognizerRule<TRule>(
            string symbolName,
            IReason? failureReason = null,
            Action<Mock<TRule>>? extraSetups = null)
            where TRule: class, IRule
        {
            var mockRule = new Mock<TRule>();
            var mockRecognizer = new Mock<IRecognizer>();

            mockRule
                .Setup(r => r.ToRecognizer(It.IsAny<PulsarGrammar>()))
                .Returns(mockRecognizer.Object);

            if (extraSetups is not null)
                extraSetups.Invoke(mockRule);

            mockRule
                .Setup(r => r.SymbolName)
                .Returns(symbolName);

            mockRecognizer
                .Setup(r => r.Recognize(It.IsAny<BufferedTokenReader>()))
                .Returns<BufferedTokenReader>(btr => new FailureResult(btr.Position+1, reason: failureReason ?? IReason.Of(symbolName)))
                .Verifiable();

            mockRecognizer
                .Setup(r => r.TryRecognize(
                    It.IsAny<BufferedTokenReader>(),
                    out It.Ref<IRecognitionResult>.IsAny))
                .Returns((BufferedTokenReader btr, out IRecognitionResult result) =>
                {
                    result = new FailureResult(btr.Position + 1, reason: null);
                    return false;
                });

            return mockRule;
        }

        public static Mock<TRule> MockErroredRecognizerRule<TRule>(
            Exception? exception = null,
            Action<Mock<TRule>>? extraSetups = null)
            where TRule : class, IRule
        {
            var mockRule = new Mock<TRule>();
            var mockRecognizer = new Mock<IRecognizer>();

            mockRule
                .Setup(r => r.ToRecognizer(It.IsAny<PulsarGrammar>()))
                .Returns(mockRecognizer.Object);

            if (extraSetups is not null)
                extraSetups.Invoke(mockRule);

            mockRule
                .Setup(r => r.SymbolName)
                .Returns("__symbol__");

            mockRecognizer
                .Setup(r => r.Recognize(It.IsAny<BufferedTokenReader>()))
                .Returns<BufferedTokenReader>(btr => new ErrorResult(btr.Position + 1, exception: exception ?? new Exception()))
                .Verifiable();

            mockRecognizer
                .Setup(r => r.TryRecognize(
                    It.IsAny<BufferedTokenReader>(),
                    out It.Ref<IRecognitionResult>.IsAny))
                .Returns((BufferedTokenReader btr, out IRecognitionResult result) =>
                {
                    result = new ErrorResult(btr.Position + 1, exception: exception ?? new Exception());
                    return false;
                });

            return mockRule;
        }

        public static Mock<TRule> MockPartialRecognizerRule<TRule>(
            string expectedSymbol,
            int position,
            IReason failureReason,
            GrammarCSTNode[] partials,
            Action<Mock<TRule>>? extraSetups = null)
            where TRule: class, IRule
        {
            return MockErroredRecognizerRule<TRule>(
                extraSetups: extraSetups,
                exception: new PartialRecognitionException(
                    expectedSymbol,
                    position,
                    failureReason));
        }

        public static Mock<PulsarGrammar> MockRefGrammar(
            Mock<PulsarGrammar> mockGrammar,
            params (string symbol, IRecognizer recognizer)[] recognizers)
        {
            foreach(var (symbol, recognizer) in recognizers)
            {
                mockGrammar
                    .Setup(g => g.GetRecognizer(symbol))
                    .Returns(recognizer)
                    .Verifiable();
            }

            return mockGrammar; ;
        }

        public static Mock<PulsarGrammar> MockRefGrammar(
            params (string symbol, IRecognizer recognizer)[] recognizers)
            => MockRefGrammar(new(), recognizers);

        public static Mock<IProductionValidator> MockValidator(
        ProductionValidationResult result)
        {
            var mockValidator = new Mock<IProductionValidator>();
            mockValidator
                .Setup(v => v.ValidateCSTNode(
                    It.IsAny<ProductionRule>(),
                    It.IsAny<GrammarCSTNode>()))
                .Returns(result)
                .Verifiable();

            return mockValidator;
        }
    }
}
