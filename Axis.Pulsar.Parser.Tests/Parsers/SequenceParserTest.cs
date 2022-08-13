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
    public class SequenceParserTest
    {
        private static readonly Grammar.Grammar Grammar = GrammarBuilder
            .NewBuilder()
            .WithRootProduction(
                new Production(
                    "language",
                    new SymbolExpressionRule(
                        SymbolGroup.Sequence(
                            ProductionRef.Create(
                                "open-brace",
                                "white-space",
                                "variable",
                                "white-space",
                                "closed-brace")))))
            .WithProductions(
                new Production(
                    "open-brace",
                    new LiteralRule("(")),
                new Production(
                    "closed-brace",
                    new LiteralRule(")")),
                new Production(
                    "white-space",
                    new PatternRule(
                        new Regex("^\\s*$", RegexOptions.IgnoreCase),
                        Cardinality.OccursNeverOrMore())),
                new Production(
                    "variable",
                    new PatternRule(
                        new Regex("^[a-z_\\$]\\w*$", RegexOptions.IgnoreCase),
                        Cardinality.OccursAtLeast(1))))
            .Build();

        private static SequenceRecognizer CreateSequenceRecognizer(Cardinality? cardinality = null)
        {
            var sequence = SymbolGroup.Sequence(
                            cardinality ?? Cardinality.OccursOnlyOnce(),
                            ProductionRef.Create(
                                "open-brace",
                                "white-space",
                                "variable",
                                "white-space",
                                "closed-brace"));

            return (SequenceRecognizer)Grammar.CreateRecognizer(sequence);
        }

        [TestMethod]
        public void Constructor_Should_ReturnValidObject()
        {
            var parser = CreateSequenceRecognizer();

            Assert.IsNotNull(parser);
        }


        [TestMethod]
        public void TryParse_WithValidInput_Should_ReturnValidParseResult()
        {
            var parser = CreateSequenceRecognizer();

            var reader = new BufferedTokenReader("(some_identifier)");
            var succeeded = parser.TryRecognize(reader, out var result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbols);
            Assert.AreEqual(result.Symbols.Select(s => s.Value).Map(s => string.Join("", s)), "(some_identifier)");

            //2
            reader = new BufferedTokenReader("(some_identifier )");
            succeeded = parser.TryRecognize(reader, out result);

            Assert.IsTrue(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNull(result.Error);
            Assert.IsNotNull(result.Symbols);
            Assert.AreEqual(result.Symbols.Select(s => s.Value).Map(s => string.Join("", s)), "(some_identifier )");
        }


        [TestMethod]
        public void TryParse_WithinvalidInput_Should_ReturnErroredParseResult()
        {
            var parser = CreateSequenceRecognizer();

            var reader = new BufferedTokenReader(" (some_identifier )");
            var succeeded = parser.TryRecognize(reader, out var result);

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Error);
            Assert.IsNull(result.Symbols);

            //2
            reader = new BufferedTokenReader("((some_identifier )");
            succeeded = parser.TryRecognize(reader, out result);

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Error);
            Assert.IsNull(result.Symbols);

            //3
            reader = new BufferedTokenReader("(some_ identifier)");
            succeeded = parser.TryRecognize(reader, out result);

            Assert.IsFalse(succeeded);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Error);
            Assert.IsNull(result.Symbols);
        }
    }
}
