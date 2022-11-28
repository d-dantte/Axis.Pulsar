using Axis.Pulsar.Parser.Parsers;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;
using Axis.Pulsar.Parser.Recognizers;
using System.Linq;
using System;
using Axis.Pulsar.Parser.CST;

namespace Axis.Pulsar.Parser.Tests.Recognizers
{
    using ParserResult = Pulsar.Parser.Parsers.IResult;
    using RecognizerResult = Pulsar.Parser.Recognizers.IResult;

    [TestClass]
    public class SetRecognizerTests
    {
        private IRecognizer CreateLiteralRecognizer(string symbol, string token)
        {
            return new LiteralRecognizer(
                new LiteralParser(
                    symbol,
                    new LiteralRule(token, true)));
        }

        [TestMethod]
        public void Constructor_Should_ReturnValidObject()
        {
            var parser = new SetRecognizer(
                new SymbolGroup.Set(Cardinality.OccursOnlyOnce(), null, DummyExpression.Instance),
                CreateLiteralRecognizer("symbol-1", "token"));

            Assert.IsNotNull(parser);
            Assert.AreEqual(Cardinality.OccursOnlyOnce(), parser.Cardinality);
        }

        [TestMethod]
        public void TryRecognize_WithValidInput_Should_ReturnValidParseResult()
        {
            var parser = new SetRecognizer(
                new SymbolGroup.Set(Cardinality.OccursOnlyOnce(), null, DummyExpression.Instance),
                CreateLiteralRecognizer("public-symbol", "public "),
                CreateLiteralRecognizer("static-symbol", "static "),
                CreateLiteralRecognizer("abstract-symbol", "abstract "),
                CreateLiteralRecognizer("sealed-symbol", "sealed "));

            // 1
            var reader = new BufferedTokenReader("public static abstract sealed void Main(string[] args){}");
            var succeeded = parser.TryRecognize(reader, out var result);
            var trueResult = result as RecognizerResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(4, trueResult.Symbols.Length);
            Assert.AreEqual(
                "public static abstract sealed ",
                trueResult.Symbols.Select(s => s.TokenValue()).Map(Extensions.Concat));

            // 2
            reader = new BufferedTokenReader("static abstract sealed public void Main(string[] args){}");
            succeeded = parser.TryRecognize(reader, out result);
            trueResult = result as RecognizerResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(4, trueResult.Symbols.Length);
            Assert.AreEqual(
                "static abstract sealed public ",
                trueResult.Symbols.Select(s => s.TokenValue()).Map(Extensions.Concat));
        }

        [TestMethod]
        public void TryRecognize_WithInvalidInput_Should_ReturnFailedParseResult()
        {
            var parser = new SetRecognizer(
                new SymbolGroup.Set(Cardinality.OccursOnlyOnce(), null, DummyExpression.Instance),
                CreateLiteralRecognizer("public-symbol", "public "),
                CreateLiteralRecognizer("static-symbol", "static "),
                CreateLiteralRecognizer("abstract-symbol", "abstract "),
                CreateLiteralRecognizer("sealed-symbol", "sealed "));

            // 1
            var reader = new BufferedTokenReader("public void Main(string[] args){}");
            var succeeded = parser.TryRecognize(reader, out var result);
            var trueResult = result as RecognizerResult.FailedRecognition;

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(1, trueResult.RecognitionCount);

            // 2
            reader = new BufferedTokenReader("static abstract void Main(string[] args){}");
            succeeded = parser.TryRecognize(reader, out result);
            trueResult = result as RecognizerResult.FailedRecognition;

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(trueResult);
            Assert.AreEqual(2, trueResult.RecognitionCount);
        }

        [TestMethod]
        public void TryRecognize_WithFatalInput_Should_ReturnExceptionParseResult()
        {
            var parser = new SetRecognizer(
                new SymbolGroup.Set(Cardinality.OccursOnlyOnce(), null, DummyExpression.Instance),
                CreateLiteralRecognizer("public-symbol", "public "),
                new RecognizerResult.Exception(new Exception(), 2).CreateRecognizer(),
                CreateLiteralRecognizer("static-symbol", "static "),
                CreateLiteralRecognizer("abstract-symbol", "abstract "),
                CreateLiteralRecognizer("sealed-symbol", "sealed "));

            // 1
            var reader = new BufferedTokenReader("public void Main(string[] args){}");
            var succeeded = parser.TryRecognize(reader, out var result);
            var exceptionResult = result as RecognizerResult.Exception;

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(exceptionResult);
            Assert.AreEqual(2, exceptionResult.InputPosition);

            // 2
            reader = new BufferedTokenReader("static abstract void Main(string[] args){}");
            succeeded = parser.TryRecognize(reader, out result);
            exceptionResult = result as RecognizerResult.Exception;

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(exceptionResult);
            Assert.AreEqual(2, exceptionResult.InputPosition);
        }
    }

    internal class LiteralRecognizer : IRecognizer
    {
        public Cardinality Cardinality => Cardinality.OccursOnlyOnce();

        public Parser.Recognizers.IResult Recognize(BufferedTokenReader tokenReader)
        {
            _ = TryRecognize(tokenReader, out var result);
            return result;
        }

        private LiteralParser Parser { get; }

        public LiteralRecognizer(LiteralParser parser)
        {
            Parser = parser;
        }

        public bool TryRecognize(BufferedTokenReader tokenReader, out RecognizerResult result)
        {
            result = Parser.Parse(tokenReader) switch
            {
                ParserResult.Success success => new RecognizerResult.Success(success.Symbol),

                ParserResult.FailedRecognition failed => new RecognizerResult.FailedRecognition(0, failed.InputPosition),

                ParserResult.Exception exception => new RecognizerResult.Exception(exception.Error, exception.InputPosition),

                ParserResult.PartialRecognition partial => new RecognizerResult.FailedRecognition(0, partial.InputPosition),

                _ => new RecognizerResult.Exception(new Exception(), tokenReader.Position)
            };

            return result is RecognizerResult.Success;
        }
    }
}
