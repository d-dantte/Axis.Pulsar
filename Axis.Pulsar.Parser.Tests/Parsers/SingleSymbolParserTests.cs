using Axis.Pulsar.Parser.Builder;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Language;
using Axis.Pulsar.Parser.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Parser.Tests.Parsers
{
    [TestClass]
    public class SingleSymbolParserTests
    {
        private static readonly StringMatcherParser CatchKeyWordParser = new(
            new StringTerminal(
                "catch_keyword",
                "catch",
                false));

        private static readonly PatternMatcherParser VariableParser = new(
            new PatternTerminal(
                "identifier_name",
                new Regex("^[a-z_\\$]\\w{0,4}$", RegexOptions.IgnoreCase),
                new(1, 5)));

        [TestMethod]
        public void Constructor_Should_ReturnValidObject()
        {
            var parser = new SingleSymbolParser(
                Cardinality.OccursOnlyOnce(),
                CatchKeyWordParser);
            Assert.IsNotNull(parser);
        }


        [TestMethod]
        public void TryParse_WithValidInput_Should_ReturnValidParseResult()
        {
            var parser = new SingleSymbolParser(
                Cardinality.OccursOnlyOnce(),
                VariableParser);

            var reader = new BufferedTokenReader("varia = 5;");
            var succeeded = parser.TryParse(reader, out var result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbol);
            Assert.AreEqual(result.Symbol.Value, "varia");

            //test 2
            parser = new SingleSymbolParser(
            Cardinality.OccursAtLeast(2),
            VariableParser);

            reader = new BufferedTokenReader("varianuria = 5;");
            succeeded = parser.TryParse(reader, out result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbol);
            Assert.AreEqual(result.Symbol.Value, "varianuria");

            //test 3
            parser = new SingleSymbolParser(
            Cardinality.OccursAtLeast(2),
            VariableParser);

            reader = new BufferedTokenReader("varianuriatamaria = 5;");
            succeeded = parser.TryParse(reader, out result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbol);
            Assert.AreEqual(result.Symbol.Value, "varianuriatamaria");

            //test 4
            parser = new SingleSymbolParser(
            Cardinality.OccursNeverOrAtMost(3),
            VariableParser);

            reader = new BufferedTokenReader("{varianuriatamaria = 5;}");
            succeeded = parser.TryParse(reader, out result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbol);
            Assert.AreEqual(result.Symbol.Value, "");

            //test 5
            parser = new SingleSymbolParser(
            Cardinality.OccursNeverOrAtMost(3),
            VariableParser);

            reader = new BufferedTokenReader("varianuriatamaria = 5;");
            succeeded = parser.TryParse(reader, out result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbol);
            Assert.AreEqual(result.Symbol.Value, "varianuriatamar");
        }


        [TestMethod]
        public void TryParse_WithinvalidInput_Should_ReturnErroredParseResult()
        {
            var parser = new SingleSymbolParser(
                Cardinality.OccursOnlyOnce(),
                VariableParser);

            var reader = new BufferedTokenReader("= 5;");
            var succeeded = parser.TryParse(reader, out var result);

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Error);
            Assert.IsNull(result.Symbol);

            //test 2
            parser = new SingleSymbolParser(
            Cardinality.OccursAtLeast(2),
            VariableParser);

            reader = new BufferedTokenReader(" = 5;");
            succeeded = parser.TryParse(reader, out result);

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Error);
            Assert.IsNull(result.Symbol);

            //test 3
            parser = new SingleSymbolParser(
            Cardinality.OccursAtLeast(2),
            VariableParser);

            reader = new BufferedTokenReader("varia = 5;");
            succeeded = parser.TryParse(reader, out result);

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Error);
            Assert.IsNull(result.Symbol);
        }
    }
}
