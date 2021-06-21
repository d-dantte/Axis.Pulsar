using Axis.Pulsar.Parser.Builder;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Language;
using Axis.Pulsar.Parser.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Parser.Tests.Parsers
{
    [TestClass]
    public class SetParserTests
    {

        private static readonly PatternMatcherParser PublicParser = new(
            new PatternTerminal(
                "keyword_public",
                new Regex("^public,\\s*$"),
                Cardinality.OccursAtLeast(7)));

        private static readonly PatternMatcherParser PrivateParser = new(
            new PatternTerminal(
                "keyword_private",
                new Regex("^private,\\s*$"),
                Cardinality.OccursAtLeast(8)));

        private static readonly PatternMatcherParser PackageParser = new(
            new PatternTerminal(
                "keyword_package",
                new Regex("^package,\\s*$"),
                Cardinality.OccursAtLeast(8)));

        private static readonly PatternMatcherParser InternalParser = new(
            new PatternTerminal(
                "keyword_internal",
                new Regex("^internal,\\s*$"),
                Cardinality.OccursAtLeast(9)));

        private static readonly PatternMatcherParser ProtectedParser = new(
            new PatternTerminal(
                "keyword_protected",
                new Regex("^protected,\\s*$"),
                Cardinality.OccursAtLeast(10)));

        private static SetParser CreateSetParser(Cardinality? cardinality = null) => new(
            cardinality ?? Cardinality.OccursOnlyOnce(),
            PublicParser,
            PrivateParser,
            InternalParser,
            ProtectedParser,
            PackageParser);


        [TestMethod]
        public void Constructor_Should_ReturnValidObject()
        {
            var parser = CreateSetParser();

            Assert.IsNotNull(parser);
        }


        [TestMethod]
        public void TryParse_WithValidInput_Should_ReturnValidParseResult()
        {
            var parser = CreateSetParser();

            var reader = new BufferedTokenReader("public, protected, internal, private, package,");
            var succeeded = parser.TryParse(reader, out var result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbol);
            Assert.AreEqual(result.Symbol.Value, "public, protected, internal, private, package,");

            //2
            reader = new BufferedTokenReader("internal, public, package, private, protected,");
            succeeded = parser.TryParse(reader, out result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbol);
            Assert.AreEqual(result.Symbol.Value, "internal, public, package, private, protected,");
        }


        [TestMethod]
        public void TryParse_WithinvalidInput_Should_ReturnErroredParseResult()
        {
            var parser = CreateSetParser();

            var reader = new BufferedTokenReader("public, static, private, protected,");
            var succeeded = parser.TryParse(reader, out var result);

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Error);
            Assert.IsNull(result.Symbol);
        }

    }
}
