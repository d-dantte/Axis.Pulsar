using Axis.Pulsar.Grammar.Builders;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Language.Rules.CustomTerminals;
using Axis.Pulsar.Languages.xBNF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Axis.Pulsar.Languges.IO.Tests.xBnf
{
    [TestClass]
    public class ExporterTests
    {
        [TestMethod]
        public void ToLiteralString()
        {
            var exporter = new Exporter();

            var value = "abcd";
            var rule = new Literal(value, true);
            var @string = exporter.ToLiteralString(rule);

            Assert.AreEqual($"\"{value}\"", @string);

            rule = new Literal(value, false);
            @string = rule.ToString();

            Assert.AreEqual($"\'{value}\'", @string);
        }

        [TestMethod]
        public void ToPatternStringTest()
        {
            var exporter = new Exporter();
            var pattern = new Regex("abc[a-z]*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            var matchType = Grammar.Language.MatchType.Of(3, false);
            var rule = new Pattern(pattern, matchType);
            var @string = exporter.ToPatternString(rule);

            Assert.AreEqual($"/{pattern}/.ni.{matchType}", @string);


            matchType = Grammar.Language.MatchType.Of(2, 5);
            pattern = new Regex("xyz");
            rule = new Pattern(pattern, matchType);
            @string = exporter.ToPatternString(rule);

            Assert.AreEqual($"/{pattern}/.{matchType}", @string);


            pattern = new Regex("xyz");
            rule = new Pattern(pattern);
            @string = exporter.ToPatternString(rule);

            Assert.AreEqual($"/{pattern}/.{new Grammar.Language.MatchType.Open(1)}", @string);
        }

        [TestMethod]
        public void ToEOFStringTest()
        {
            Assert.AreEqual("EOF", new Exporter().ToEOFString(new EOF()));
        }

        [TestMethod]
        public void ToCustomTerminalStringTest()
        {
            var exporter = new Exporter();
            var dstring = new DelimitedString("dqd", "\"");
            Assert.AreEqual("@dqd", exporter.ToCustomTerminalString(dstring));
        }

        [TestMethod]
        public void ToProductionRefStringTest()
        {
            var exporter = new Exporter();
            var rule = new ProductionRef("xyz-symbol");
            var @string = exporter.ToProductionRefString(rule);
            Assert.AreEqual("$xyz-symbol", @string);

            rule = new ProductionRef("xyz-symbol", Grammar.Language.Cardinality.OccursAtLeast(3));
            @string = exporter.ToProductionRefString(rule);
            Assert.AreEqual("$xyz-symbol.3,", @string);
        }

        [TestMethod]
        public void ToChoiceStringTest()
        {
            var exporter = new Exporter();
            var rule = new Choice(
                new Literal("stuff"),
                new Literal("other stuff"));
            var @string = exporter.ToChoiceString(rule);
            Assert.AreEqual("?[\"stuff\" \"other stuff\"]", @string);

            rule = new Choice(
                Grammar.Language.Cardinality.OccursOptionally(),
                new Literal("stuff"),
                new Literal("other stuff"));
            @string = exporter.ToChoiceString(rule);
            Assert.AreEqual("?[\"stuff\" \"other stuff\"].?", @string);
        }

        [TestMethod]
        public void ToSequenceStringTest()
        {
            var exporter = new Exporter();
            var rule = new Sequence(
                new Literal("stuff"),
                new Literal("other stuff"));
            var @string = exporter.ToSequenceString(rule);
            Assert.AreEqual("+[\"stuff\" \"other stuff\"]", @string);

            rule = new Sequence(
                Grammar.Language.Cardinality.OccursNeverOrMore(),
                new Literal("stuff"),
                new Literal("other stuff"));
            @string = exporter.ToSequenceString(rule);
            Assert.AreEqual("+[\"stuff\" \"other stuff\"].*", @string);
        }

        [TestMethod]
        public void ToSetStringTest()
        {
            var exporter = new Exporter();
            var rule = new Set(
                new Literal("stuff"),
                new Literal("other stuff"));
            var @string = exporter.ToSetString(rule);
            Assert.AreEqual("#[\"stuff\" \"other stuff\"]", @string);

            rule = new Set(
                Grammar.Language.Cardinality.Occurs(2, 5),
                3,
                new Literal("stuff"),
                new Literal("other stuff"));
            @string = exporter.ToSetString(rule);
            Assert.AreEqual("#3[\"stuff\" \"other stuff\"].2,5", @string);
        }

        [TestMethod]
        public void ToProductionLineTest()
        {
            var exporter = new Exporter();
            var rule = new Production(new ProductionRule(
                "aaaa",
                new Literal("something like aaa", false)));
            var @string = exporter.ToProductionString(rule);

            Assert.AreEqual("$aaaa -> 'something like aaa'", @string);
        }

        [TestMethod]
        public void ToGrammarTextTest()
        {
            var grammar = new TestGrammar();
            var appender = (IProductionAppender)grammar;
            _ = appender
                .AddProduction(new ProductionRule(
                    "single-dash1",
                    new Literal("first")))
                .AddProduction(new ProductionRule(
                    "single-dash2",
                    new Literal("second")))
                .AddProduction(new ProductionRule(
                    "double-dash-1",
                    new Literal("third")))
                .AddProduction(new ProductionRule(
                    "double-dash-2",
                    new Literal("fourth")));

            var exporter = new Exporter(
                new Exporter.GroupFilter(
                    "single dash",
                    "single dash grouping comment",
                    p => p.Symbol.Count(c => c == '-') == 1),
                new Exporter.GroupFilter(
                    "double dash",
                    "double dash grouping comment",
                    p => p.Symbol.Count(c => c == '-') == 2));

            var @string = exporter.ToGrammarString(grammar);
            var expected =
@"# single dash grouping comment
$single-dash1 -> ""first""
$single-dash2 -> ""second""

# double dash grouping comment
$double-dash-1 -> ""third""
$double-dash-2 -> ""fourth""";
            Assert.AreEqual(expected, @string);


            exporter = new Exporter();
            @string = exporter.ToGrammarString(grammar);
            expected =
@"$single-dash1 -> ""first""
$single-dash2 -> ""second""
$double-dash-1 -> ""third""
$double-dash-2 -> ""fourth""";
            Assert.AreEqual(expected, @string);
        }

        class TestGrammar: Grammar.Language.Grammar
        { }
    }
}
