using Axis.Pulsar.Parser.Parsers;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;
using Axis.Pulsar.Parser.Recognizers;
using System.Linq;

namespace Axis.Pulsar.Parser.Tests.Parsers
{
    [TestClass]
    public class SetParserTests
    {
        private static readonly Grammar.Grammar Grammar = GrammarBuilder
            .NewBuilder()
            .WithRootProduction(
                new Production(
                    "language",
                    new SymbolExpressionRule(
                        SymbolGroup.Set(
                            ProductionRef.Create(
                                "public",
                                "private",
                                "package",
                                "internal",
                                "protected")))))
            .WithProductions(
                new Production(
                    "public",
                    new PatternRule(
                        new Regex("^public\\s*$"),
                        Cardinality.OccursAtLeast(6))),
                new Production(
                    "private",
                    new PatternRule(
                        new Regex("^private\\s*$"),
                        Cardinality.OccursAtLeast(7))),
                new Production(
                    "package",
                    new PatternRule(
                        new Regex("^pacakge\\s*$"),
                        Cardinality.OccursAtLeast(7))),
                new Production(
                    "internal",
                    new PatternRule(
                        new Regex("^internal\\s*$"),
                        Cardinality.OccursAtLeast(8))),
                new Production(
                    "protected",
                    new PatternRule(
                        new Regex("^protected\\s*$"),
                        Cardinality.OccursAtLeast(9))))
            .Build();

        private static SetRecognizer CreateSetRecognizer(Cardinality? cardinality = null)
        {
            var set = SymbolGroup.Set(
                    cardinality ?? Cardinality.OccursOnlyOnce(),
                    ProductionRef.Create(
                        "public",
                        "private",
                        "package",
                        "internal",
                        "protected"));

            return (SetRecognizer)Grammar.CreateRecognizer(set);
        }


        [TestMethod]
        public void Constructor_Should_ReturnValidObject()
        {
            var parser = CreateSetRecognizer();

            Assert.IsNotNull(parser);
        }


        [TestMethod]
        public void TryParse_WithValidInput_Should_ReturnValidParseResult()
        {
            var parser = CreateSetRecognizer();

            var reader = new BufferedTokenReader("public protected internal private package");
            var succeeded = parser.TryRecognize(reader, out var result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbols);
            Assert.AreEqual(result.Symbols.Select(s => s.Value).Map(s => string.Join("", s)), "public, protected, internal, private, package,");

            //2
            reader = new BufferedTokenReader("internal, public, package, private, protected,");
            succeeded = parser.TryRecognize(reader, out result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbols);
            Assert.AreEqual(result.Symbols.Select(s => s.Value).Map(s => string.Join("", s)), "internal, public, package, private, protected,");
        }


        [TestMethod]
        public void TryParse_WithinvalidInput_Should_ReturnErroredParseResult()
        {
            var parser = CreateSetRecognizer();

            var reader = new BufferedTokenReader("public, static, private, protected,");
            var succeeded = parser.TryRecognize(reader, out var result);

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Error);
            Assert.IsNull(result.Symbols);
        }

    }
}
