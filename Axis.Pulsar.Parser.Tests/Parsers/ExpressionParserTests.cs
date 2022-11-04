using Axis.Pulsar.Parser.CST;
using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Parsers;
using Axis.Pulsar.Parser.Recognizers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace Axis.Pulsar.Parser.Tests.Parsers
{
    [TestClass]
    public class ExpressionParserTests
    {
        [TestMethod]
        public void Constructor_Should_ReturnValidObject()
        {
            // arrange
            var mockRecognizer = Mock.Of<IRecognizer>();
            var symbolName = "some_Non_Terminal";
            var rule = new Grammar.SymbolExpressionRule(null, 1, null);
            var parser =  new ExpressionParser(symbolName, rule, mockRecognizer);

            Assert.IsNotNull(parser);
            Assert.AreEqual(symbolName, parser.SymbolName);
            Assert.AreEqual(1, parser.RecognitionThreshold);
        }

        [TestMethod]
        public void TryParse_WithPassingRecognizer_ShouldReturnSuccessResult()
        {
            // arrange
            var symbolName = "exSymbol";
            var recognizerMocker = new Mock<IRecognizer>();
            var mockValidator = new Mock<IRuleValidator<SymbolExpressionRule>>();
            var successResult = new Parser.Recognizers.IResult.Success(ICSTNode.Of("stuff", "tokens"));
            recognizerMocker
                .Setup(r => r.Recognize(It.IsAny<BufferedTokenReader>()))
                .Returns(successResult);
            mockValidator
                .Setup(v => v.IsValidCSTNode(It.IsAny<SymbolExpressionRule>(), It.IsAny<ICSTNode>()))
                .Returns(true)
                .Verifiable();
            var rule = new SymbolExpressionRule(null, 1, mockValidator.Object);
            var parser = new ExpressionParser(symbolName, rule, recognizerMocker.Object);
            var reader = new BufferedTokenReader("ble bleh ble");

            // assert
            Assert.IsTrue(parser.TryParse(reader, out var result));
            Assert.IsNotNull(result);

            var trueResult = result as Parser.Parsers.IResult.Success;
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(symbolName, trueResult.Symbol.SymbolName);
            Assert.AreEqual("stuff", trueResult.Symbol.FirstNode().SymbolName);
            mockValidator.Verify();
        }

        [TestMethod]
        public void TryParse_WithFaultingRecognizer_ShouldReturnExceptionResult()
        {
            // arrange
            var symbolName = "exSymbol";
            var recognizerMocker = new Mock<IRecognizer>();
            var exceptionResult = new Parser.Recognizers.IResult.Exception(new Exception(), 2);
            recognizerMocker
                .Setup(r => r.Recognize(It.IsAny<BufferedTokenReader>()))
                .Returns(exceptionResult);
            var rule = new Grammar.SymbolExpressionRule(null, 1, null);
            var parser = new ExpressionParser(symbolName, rule, recognizerMocker.Object);
            var reader = new BufferedTokenReader("ble bleh ble");

            // assert
            Assert.IsFalse(parser.TryParse(reader, out var result));
            Assert.IsNotNull(result);

            var trueResult = result as Parser.Parsers.IResult.Exception;
            Assert.IsNotNull(trueResult);
            Assert.IsNotNull(trueResult.Error);
            Assert.AreEqual(2, trueResult.InputPosition);
        }

        [TestMethod]
        public void TryParse_WithFailingRecognizerAndBelowThreshold_ShouldReturnFailureResult()
        {
            // arrange
            var symbolName = "exSymbol";
            var recognizerMocker = new Mock<IRecognizer>();
            var exceptionResult = new Parser.Recognizers.IResult.FailedRecognition(1, 4);
            recognizerMocker
                .Setup(r => r.Recognize(It.IsAny<BufferedTokenReader>()))
                .Returns(exceptionResult);
            var rule = new Grammar.SymbolExpressionRule(null, 2, null);
            var parser = new ExpressionParser(symbolName, rule, recognizerMocker.Object);
            var reader = new BufferedTokenReader("ble bleh ble");

            // assert
            Assert.IsFalse(parser.TryParse(reader, out var result));
            Assert.IsNotNull(result);

            var trueResult = result as Parser.Parsers.IResult.FailedRecognition;
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(symbolName, trueResult.ExpectedSymbolName);
            Assert.AreEqual(4, trueResult.InputPosition);
        }

        [TestMethod]
        public void TryParse_WithFailingRecognizerAndAcceptableThreshold_ShouldReturnPartialResult()
        {
            // arrange
            var symbolName = "exSymbol";
            var recognizerMocker = new Mock<IRecognizer>();
            var exceptionResult = new Parser.Recognizers.IResult.FailedRecognition(2, 4);
            recognizerMocker
                .Setup(r => r.Recognize(It.IsAny<BufferedTokenReader>()))
                .Returns(exceptionResult);
            var rule = new Grammar.SymbolExpressionRule(null, 2, null);
            var parser = new ExpressionParser(symbolName, rule, recognizerMocker.Object);
            var reader = new BufferedTokenReader("ble bleh ble");

            // assert
            Assert.IsFalse(parser.TryParse(reader, out var result));
            Assert.IsNotNull(result);

            var trueResult = result as Parser.Parsers.IResult.PartialRecognition;
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(symbolName, trueResult.ExpectedSymbolName);
            Assert.AreEqual(4, trueResult.InputPosition);
        }
    }
}
