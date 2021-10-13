using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Parsers;
using Axis.Pulsar.Parser.Syntax;
using Axis.Pulsar.Parser.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Axis.Pulsar.Importer.Common.BNF
{
    public class RuleImporter : IRuleImporter
    {
        private static readonly GrammarContext BnfGrammarContext;

        static RuleImporter()
        {
            using var bnfXmlStream = typeof(RuleImporter)
                .Assembly
                .GetManifestResourceStream($"{typeof(RuleImporter).Namespace}.BNFRule.xml");

            var ruleMap = new Xml
                .RuleImporter()
                .ImportRule(bnfXmlStream);

            BnfGrammarContext = new GrammarContext(ruleMap);
        }

        public RuleMap ImportRule(Stream inputStream)
        {
            using var reader = new StreamReader(inputStream);
            var txt = reader.ReadToEnd();

            return ImportRuleInternal(txt);
        }

        public async Task<RuleMap> ImportRuleAsync(Stream inputStream)
        {
            using var reader = new StreamReader(inputStream);
            var txt = await reader.ReadToEndAsync();

            return ImportRuleInternal(txt);
        }

        private RuleMap ImportRuleInternal(string text)
        {
            if (!BnfGrammarContext
                .RootParser()
                .TryParse(new BufferedTokenReader(text), out var result))
                throw new Parser.Exceptions.ParseException(result.Error);

            //build the rule from the symbol-tree

            return result.Symbol.Children
                .Where(child => child.Name.Equals("production"))
                .Select(ToRuleMap)
                .Map(maps => new RuleMap(maps))
                .Validate();
        }

        private KeyValuePair<string, Rule> ToRuleMap(Symbol symbol)
        {
            var name = symbol.Children[0].Value.TrimStart('$');
            var ruleSymbol = symbol.Children.Last().Children[0];
            return new(name, ToRule(ruleSymbol));
        }

        private static Rule ToRule(Symbol ruleSymbol) => ruleSymbol.Name switch
        {
            "literal" => ToLiteral(ruleSymbol),
            "pattern" => ToPattern(ruleSymbol),
            "grouping" => ToGrouping(ruleSymbol),
            "symbol-ref" => ToRef(ruleSymbol),
            _ => throw new System.Exception("unknown rule type: " + ruleSymbol.Name)
        };

        private static LiteralRule ToLiteral(Symbol symbol)
        {
            return symbol.Children[0].Name switch
            {
                "case-sensitive" => new(
                    symbol.FindSymbol("case-sensitive.case-literal").Value,
                    true),

                "case-insensitive" => new(
                    symbol.FindSymbol("case-insensitive.non-case-literal").Value,
                    false),

                _ => throw new System.Exception("Invalid literal-case specifier: "+symbol.Children[0].Name)
            };
        }

        private static PatternRule ToPattern(Symbol symbol)
        {
            return new PatternRule(
                new System.Text.RegularExpressions.Regex(symbol.FindSymbol("pattern-literal").Value),
                ToCardinality(symbol.FindSymbol("cardinality")));
        }

        private static Cardinality ToCardinality(Symbol symbol)
        {
            if (string.IsNullOrEmpty(symbol?.Value ?? ""))
                return Cardinality.OccursOnlyOnce();

            else
            {
                var min = int.Parse(symbol.Children[1].Value);
                var max =
                    HasOnlyComma(symbol) ? default(int?) :
                    HasCommaAndTrailingCharacter(symbol) ? int.Parse(symbol.Children[3].Value) :
                    min;

                return new Cardinality(min, max);
            }
        }

        private static bool HasOnlyComma(Symbol symbol)
        {
            return ",".Equals(symbol.Children[2].Value)
                && symbol.Children.Length == 3;
        }

        private static bool HasCommaAndTrailingCharacter(Symbol symbol)
        {
            return ",".Equals(symbol.Children[2].Value)
                && !string.IsNullOrEmpty(symbol.Children[3].Value);
        }

        private static GroupingRule ToGrouping(Symbol symbol)
        {
            var grouping = symbol.Children[0];
            var childRules = grouping
                .FindSymbols("value")
                .Select(value => value.Children[0])
                .Select(ToRule);
            var cardinality = symbol.Children[1];

            return grouping.Name switch
            {
                "choice" => GroupingRule.Choice(
                    ToCardinality(cardinality),
                    childRules.ToArray()),

                "set" => GroupingRule.Set(
                    ToCardinality(cardinality),
                    childRules.ToArray()),

                "sequence" => GroupingRule.Sequence(
                    ToCardinality(cardinality),
                    childRules.ToArray()),

                _ => throw new System.Exception("Invalid grouping name: " + grouping)
            };
        }

        private static RuleRef ToRef(Symbol symbol)
        {
            return new RuleRef(
                symbol.Children[0].Value.TrimStart('$'),
                ToCardinality(symbol.Children[1]));
        }
    }
}
