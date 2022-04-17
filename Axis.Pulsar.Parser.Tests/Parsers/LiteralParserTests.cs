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
            var terminal =
                new LiteralRule("catch");
            var parser = new LiteralParser("catch", terminal);

            var reader = new BufferedTokenReader("catch (Exception e){}");
            var succeeded = parser.TryParse(reader, out var result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbol);
            Assert.AreEqual(terminal.Value, result.Symbol.Value);
            Assert.AreEqual(4, reader.Position);

            //case insensitivity test
            terminal =
                new LiteralRule(
                    "catch",
                    false);
            parser = new LiteralParser("catch", terminal);

            reader = new BufferedTokenReader("CATCH (Exception e){}");
            succeeded = parser.TryParse(reader, out result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbol);
            Assert.AreEqual("CATCH", result.Symbol.Value);
            Assert.AreEqual(4, reader.Position);
        }

        [TestMethod]
        public void TryParse_WithInvalidInput_Should_ReturnErroredResult()
        {
            var terminal =
                new LiteralRule(
                    "catch");
            var parser = new LiteralParser("catch", terminal);

            var reader = new BufferedTokenReader("}\n\tcatch (Exception e){}");
            var succeeded = parser.TryParse(reader, out var result);

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Error);
            Assert.IsNull(result.Symbol);
            Assert.AreEqual(0, result.Error.CharacterIndex);

            //case insensitivity test
            terminal =
                new LiteralRule(
                    "catch");
            parser = new LiteralParser("catch", terminal);

            reader = new BufferedTokenReader("CATCH (Exception e){}");
            succeeded = parser.TryParse(reader, out result);

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Error);
            Assert.IsNull(result.Symbol);
            Assert.AreEqual(0, result.Error.CharacterIndex);
        }
    }
}
