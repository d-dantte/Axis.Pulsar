using Axis.Pulsar.Parser.Parsers;
using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Axis.Pulsar.Parser.Recognizers;
using Moq;
using Axis.Pulsar.Parser.Input;
using System.Linq;

namespace Axis.Pulsar.Parser.Tests.Recognizers
{
    [TestClass]
    public class ProductionRecognizerTests
    {

        private static IGrammar CreateGrammar(params (string symbol, IParser parser)[] productionParsers)
        {
            var grammarMock = new Mock<IGrammar>();
            productionParsers.ForAll(parserPair =>
            {
                grammarMock
                    .Setup(g => g.GetParser(parserPair.symbol))
                    .Returns(parserPair.parser);
                grammarMock
                    .Setup(g => g.HasProduction(parserPair.symbol))
                    .Returns(true);
            });

            return grammarMock.Object;
        }

        private static LiteralParser CreateLiteralParser(string symbol, string token)
            => new(symbol, new LiteralRule(token));

        [TestMethod]
        public void Constructor_Should_ReturnValidObject()
        {
            var symbol = "someSymbol";
            var parser = new ProductionRefRecognizer(
                symbol,
                Cardinality.OccursOnlyOnce(),
                CreateGrammar((symbol, CreateLiteralParser(symbol, "blehri torious"))));

            Assert.IsNotNull(parser);
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), parser.Cardinality);
        }


        [TestMethod]
        public void TryRecognize_WithValidInput_Should_ReturnValidParseResult()
        {
            var symbol = "someSymbol";

            // 1
            var parser = new ProductionRefRecognizer(
                symbol,
                Cardinality.OccursOnlyOnce(),
                CreateGrammar((symbol, CreateLiteralParser(symbol, "blehri torious"))));
            var reader = new BufferedTokenReader("blehri torious sturvs");
            var succeeded = parser.TryRecognize(reader, out var result);
            var trueResult = result as Parser.Recognizers.IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(1, trueResult.Symbols.Length);
            Assert.AreEqual(
                "blehri torious",
                trueResult.Symbols.Select(s => s.TokenValue()).Map(Extensions.Concat));

            // 2
            parser = new ProductionRefRecognizer(
                symbol,
                Cardinality.OccursOnly(2),
                CreateGrammar((symbol, CreateLiteralParser(symbol, "blehri torious "))));
            reader = new BufferedTokenReader("blehri torious blehri torious sturvs");
            succeeded = parser.TryRecognize(reader, out result);
            trueResult = result as Parser.Recognizers.IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(2, trueResult.Symbols.Length);
            Assert.AreEqual(
                "blehri torious blehri torious ",
                trueResult.Symbols.Select(s => s.TokenValue()).Map(Extensions.Concat));
        }

        [TestMethod]
        public void TryRecognize_WithFailingInput_ShouldReturnFailedResult()
        {
            var symbol = "someSymbol";
            var innerSymbol = "innerSymbol";

            // 1
            var parserMock = new Mock<IParser>();
            parserMock
                .Setup(p => p.Parse(It.IsAny<BufferedTokenReader>()))
                .Returns(new Parser.Parsers.IResult.FailedRecognition(innerSymbol, 0));
            var parser = new ProductionRefRecognizer(
                symbol,
                Cardinality.OccursOnlyOnce(),
                CreateGrammar((symbol, parserMock.Object)));
            var reader = new BufferedTokenReader("irrelevant");
            var succeeded = parser.TryRecognize(reader, out var result);
            var trueResult = result as Parser.Recognizers.IResult.FailedRecognition;

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(0, trueResult.RecognitionCount);

            // 2
            parserMock = new Mock<IParser>();
            parserMock
                .Setup(p => p.Parse(It.IsAny<BufferedTokenReader>()))
                .Returns(new Parser.Parsers.IResult.PartialRecognition(1, innerSymbol, 0));
            parser = new ProductionRefRecognizer(
                symbol,
                Cardinality.OccursOnlyOnce(),
                CreateGrammar((symbol, parserMock.Object)));
            reader = new BufferedTokenReader("irrelevant");
            succeeded = parser.TryRecognize(reader, out result);
            trueResult = result as Parser.Recognizers.IResult.FailedRecognition;

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(0, trueResult.RecognitionCount);
        }

        [TestMethod]
        public void TryRecognize_WithatalInput_ShouldReturnExceptionResult()
        {
            var symbol = "someSymbol";

            // 1
            var parserMock = new Mock<IParser>();
            parserMock
                .Setup(p => p.Parse(It.IsAny<BufferedTokenReader>()))
                .Returns(new Parser.Parsers.IResult.Exception(new System.Exception(), 0));
            var parser = new ProductionRefRecognizer(
                symbol,
                Cardinality.OccursOnlyOnce(),
                CreateGrammar((symbol, parserMock.Object)));
            var reader = new BufferedTokenReader("irrelevant");
            var succeeded = parser.TryRecognize(reader, out var result);
            var trueResult = result as Parser.Recognizers.IResult.Exception;

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(0, trueResult.InputPosition);
        }
    }
}
