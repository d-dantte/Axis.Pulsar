using Axis.Pulsar.Parser.Parsers;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Grammar;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Axis.Pulsar.Parser.Tests.Parsers
{
    [TestClass]
    public class LiteralParserTests
    {
        [TestMethod]
        public void Constructor_Should_ReturnValidObject()
        {
            var parser = new LiteralParser(
                "catch",
                new LiteralRule(
                    "catch",
                    false));

            Assert.IsNotNull(parser);
        }

        [TestMethod]
        public void TryParse_WithValidInput_Should_ReturnValidParseResult()
        {
            var terminal = new LiteralRule("catch");
            var parser = new LiteralParser("catch", terminal);

            var reader = new BufferedTokenReader("catch (Exception e){}");
            var succeeded = parser.TryParse(reader, out var result);
            var success = result as IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsTrue(result is IResult.Success);
            Assert.IsNotNull(success.Symbol);
            Assert.AreEqual(terminal.Value, success.Symbol.TokenValue());
            Assert.AreEqual(4, reader.Position);

            //case insensitivity test
            terminal =
                new LiteralRule(
                    "catch",
                    false);
            parser = new LiteralParser("catch", terminal);

            reader = new BufferedTokenReader("CATCH (Exception e){}");
            succeeded = parser.TryParse(reader, out result);
            success = result as IResult.Success;

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsTrue(result is IResult.Success);
            Assert.IsNotNull(success.Symbol);
            Assert.AreEqual("CATCH", success.Symbol.TokenValue());
            Assert.AreEqual(4, reader.Position);
        }

        [TestMethod]
        public void TryParse_WithInvalidInput_Should_ReturnErroredResult()
        {
            var symbolName = "_catch";
            var terminal = new LiteralRule("catch");
            var parser = new LiteralParser(symbolName, terminal);

            var reader = new BufferedTokenReader("}\n\tcatch (Exception e){}");
            var succeeded = parser.TryParse(reader, out var result);
            var failure = result as IResult.FailedRecognition;

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsTrue(result is IResult.FailedRecognition);
            Assert.AreEqual(symbolName, failure.ExpectedSymbolName);
            Assert.AreEqual(0, failure.InputPosition);
        }

        [TestMethod]
        public void TryParse_WithInsufficientBuffer_Should_ReturnErroredResult()
        {
            var symbolName = "_catch";
            var terminal = new LiteralRule("catch");
            var parser = new LiteralParser(symbolName, terminal);

            var reader = new BufferedTokenReader("}");
            var succeeded = parser.TryParse(reader, out var result);
            var failure = result as IResult.FailedRecognition;

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsTrue(result is IResult.FailedRecognition);
            Assert.AreEqual(symbolName, failure.ExpectedSymbolName);
            Assert.AreEqual(0, failure.InputPosition);
        }

        [TestMethod]
        public void TryParse_WithInvalidMethodInput_Should_ReturnErroredResult()
        {
            var symbolName = "_catch";
            var terminal = new LiteralRule("catch");
            var parser = new LiteralParser(symbolName, terminal);

            var reader = new BufferedTokenReader("}");
            var succeeded = parser.TryParse(reader, out var result);
            var failure = result as IResult.FailedRecognition;

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsTrue(result is IResult.FailedRecognition);
            Assert.AreEqual(symbolName, failure.ExpectedSymbolName);
            Assert.AreEqual(0, failure.InputPosition);
        }
    }
}
