using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;
using Axis.Pulsar.Parser.Recognizers;

namespace Axis.Pulsar.Parser.Tests.Parsers
{
    [TestClass]
    public class ChoiceParserTests
    {
        private static readonly Grammar.Grammar Grammar = GrammarBuilder
            .NewBuilder()
            .WithRootProduction(
                new Production(
                    "language",
                    new SymbolExpressionRule(
                        SymbolGroup.Choice(
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

        private static ChoiceRecognizer CreateChoiceRecognizer(Cardinality? cardinality = null)
        {
            var choice = SymbolGroup.Choice(
                    cardinality ?? Cardinality.OccursOnlyOnce(),
                    ProductionRef.Create(
                        "public",
                        "private",
                        "package",
                        "internal",
                        "protected"));

            return (ChoiceRecognizer)Grammar.CreateRecognizer(choice);
        }

        [TestMethod]
        public void Constructor_Should_ReturnValidObject()
        {
            var parser = CreateChoiceRecognizer();

            Assert.IsNotNull(parser);
        }


        [TestMethod]
        public void TryParse_WithValidInput_Should_ReturnValidParseResult()
        {
            var parser = CreateChoiceRecognizer();

            var reader = new BufferedTokenReader("public static class SomeClass{}");
            var succeeded = parser.TryRecognize(reader, out var result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbols);
            Assert.AreEqual(result.Symbols[0].Value, "public ");

            //2
            reader = new BufferedTokenReader("internal string SomeProp{ get; }");
            succeeded = parser.TryRecognize(reader, out result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbols);
            Assert.AreEqual(result.Symbols[0].Value, "internal ");

            //3
            parser = CreateChoiceRecognizer(Cardinality.OccursOnly(2));
            reader = new BufferedTokenReader("internal public string SomeProp{ get; }");
            succeeded = parser.TryRecognize(reader, out result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbols);
            Assert.AreEqual(result.Symbols[0].Value, "internal ");
            Assert.AreEqual(result.Symbols[1].Value, "public ");
        }


        [TestMethod]
        public void TryParse_WithinvalidInput_Should_ReturnErroredParseResult()
        {
            var parser = CreateChoiceRecognizer();

            var reader = new BufferedTokenReader("pure brid static SomeProp{get;}");
            var succeeded = parser.TryRecognize(reader, out var result);

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Error);
            Assert.IsNull(result.Symbols);
        }
    }
}
