using Axis.Pulsar.Parser.Builder;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Language;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Axis.Pulsar.Parser.Tests.Parsers
{
    [TestClass]
    public class StringParserTests
    {
        [TestMethod]
        public void Constructor_Should_ReturnValidObject()
        {
            var parser = new StringMatcherParser(
                new StringTerminal(
                    "catch_keyword",
                    "catch",
                    false));

            Assert.IsNotNull(parser);
        }

        [TestMethod]
        public void TryParse_WithValidInput_Should_ReturnValidParseResult()
        {
            var terminal =
                new StringTerminal(
                    "catch_keyword",
                    "catch");
            var parser = new StringMatcherParser(terminal);

            var reader = new BufferedTokenReader("catch (Exception e){}");
            var succeeded = parser.TryParse(reader, out var result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbol);
            Assert.AreEqual(terminal.Name, result.Symbol.Name);
            Assert.AreEqual(terminal.Value, result.Symbol.Value);
            Assert.AreEqual(4, reader.Position);

            //case insensitivity test
            terminal =
                new StringTerminal(
                    "catch_keyword",
                    "catch",
                    false);
            parser = new StringMatcherParser(terminal);

            reader = new BufferedTokenReader("CATCH (Exception e){}");
            succeeded = parser.TryParse(reader, out result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbol);
            Assert.AreEqual(terminal.Name, result.Symbol.Name);
            Assert.AreEqual("CATCH", result.Symbol.Value);
            Assert.AreEqual(4, reader.Position);
        }

        [TestMethod]
        public void TryParse_WithInvalidInput_Should_ReturnErroredResult()
        {
            var terminal =
                new StringTerminal(
                    "catch_keyword",
                    "catch");
            var parser = new StringMatcherParser(terminal);

            var reader = new BufferedTokenReader("}\n\tcatch (Exception e){}");
            var succeeded = parser.TryParse(reader, out var result);

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Error);
            Assert.IsNull(result.Symbol);
            Assert.AreEqual(terminal.Name, result.Error.SymbolName);
            Assert.AreEqual(0, result.Error.CharacterIndex);

            //case insensitivity test
            terminal =
                new StringTerminal(
                    "catch_keyword",
                    "catch");
            parser = new StringMatcherParser(terminal);

            reader = new BufferedTokenReader("CATCH (Exception e){}");
            succeeded = parser.TryParse(reader, out result);

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Error);
            Assert.IsNull(result.Symbol);
            Assert.AreEqual(terminal.Name, result.Error.SymbolName);
            Assert.AreEqual(0, result.Error.CharacterIndex);
        }
    }
}
