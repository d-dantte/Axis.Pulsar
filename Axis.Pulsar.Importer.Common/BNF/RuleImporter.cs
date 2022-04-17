using Axis.Pulsar.Parser;
using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Syntax;
using Axis.Pulsar.Parser.Utils;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Axis.Pulsar.Importer.Common.BNF
{
    /// <summary>
    /// 
    /// </summary>
    public class GrammarImporter : IGrammarImporter

    {
        #region symbol names
        public const string SYMBOL_NAME_PRODUCTION = "production";
        public const string SYMBOL_NAME_LITERAL = "literal";
        public const string SYMBOL_NAME_PATTERN = "pattern";
        public const string SYMBOL_NAME_SYMBOL_EXPRESSION = "symbol-expression";
        public const string SYMBOL_NAME_GROUPING = "grouping";
        public const string SYMBOL_NAME_SYMBOL_REF = "symbol-ref";
        public const string SYMBOL_NAME_SET = "set";
        public const string SYMBOL_NAME_CHOICE = "choice";
        public const string SYMBOL_NAME_SEQUENCE = "sequence";
        public const string SYMBOL_NAME_CASE_SENSITIVE = "case-sensitive";
        public const string SYMBOL_NAME_CASE_INSENSITIVE = "case-insensitive";
        public const string SYMBOL_NAME_CASE_LITERAL = "case-literal";
        public const string SYMBOL_NAME_NON_CASE_LITERAL = "non-case-literal";
        public const string SYMBOL_NAME_PATTERN_LITERAL = "pattern-literal";
        public const string SYMBOL_NAME_CARDINALITY = "cardinality";
        #endregion

        private static readonly Grammar BnfGrammar;
        private static readonly IParser BnfParser;
        static GrammarImporter()
        {
            using var bnfXmlStream = typeof(GrammarImporter)
                .Assembly
                .GetManifestResourceStream($"{typeof(GrammarImporter).Namespace}.BNFRule.xml");

            BnfGrammar = new Xml
                .GrammarImporter()
                .ImportGrammar(bnfXmlStream);
            BnfParser = BnfGrammar.RootParser();
        }

        public Grammar ImportGrammar(Stream inputStream)
        {
            using var reader = new StreamReader(inputStream);
            var txt = reader.ReadToEnd();

            return ImportRuleInternal(txt);
        }

        public async Task<Grammar> ImportGrammarAsync(Stream inputStream)
        {
            using var reader = new StreamReader(inputStream);
            var txt = await reader.ReadToEndAsync();

            return ImportRuleInternal(txt);
        }

        private Grammar ImportRuleInternal(string text)
        {
            if (!BnfParser.TryParse(new BufferedTokenReader(text), out var result))
                throw new Parser.Exceptions.ParseException(result.Error);

            //build the rule from the symbol-tree
            return result.Symbol.Children
                .Where(child => child.Name.Equals(SYMBOL_NAME_PRODUCTION))
                .Select(ToProduction)
                .Aggregate(
                    GrammarBuilder.NewBuilder(),
                    (builder, production) => builder.HasRoot
                        ? builder.WithProduction(production)
                        : builder.WithRootProduction(production))
                .Build();
        }

        private Production ToProduction(Symbol symbol)
        {
            var name = symbol.Children[0].Value.TrimStart('$');
            var ruleSymbol = symbol.Children.Last().Children[0];
            return new(name, ToRule(ruleSymbol));
        }

        private static IRule ToRule(Symbol ruleSymbol) => ruleSymbol.Name switch
        {
            SYMBOL_NAME_LITERAL => ToLiteral(ruleSymbol),

            SYMBOL_NAME_PATTERN => ToPattern(ruleSymbol),

            SYMBOL_NAME_SYMBOL_EXPRESSION => ToSymbolExpression(ruleSymbol),

            _ => throw new System.ArgumentException($"unknown rule type: {ruleSymbol.Name}")
        };

        private static SymbolExpressionRule ToSymbolExpression(Symbol symbol)
            => new SymbolExpressionRule(ToExpression(symbol));

        private static ISymbolExpression ToExpression(Symbol expressionSymbol) => expressionSymbol.Children[0].Name switch
        {
            SYMBOL_NAME_SYMBOL_REF => ToRef(
                expressionSymbol.Children[0],
                ToCardinality(expressionSymbol.Children[1])),

            SYMBOL_NAME_GROUPING => ToGrouping(
                expressionSymbol.Children[0],
                ToCardinality(expressionSymbol.Children[1])),

            _ => throw new System.ArgumentException($"Invalid expression-symbol: {expressionSymbol.Name}")
        };

        private static ISymbolExpression ToGrouping(Symbol groupingExpression, Cardinality cardinality)
        {
            var groupType = groupingExpression.Children[0];

            var expressions = groupType
                .FindSymbols(SYMBOL_NAME_SYMBOL_EXPRESSION)
                .Select(ToExpression)
                .ToArray();

            return groupType.Name switch
            {
                SYMBOL_NAME_SET => SymbolGroup.Set(cardinality, expressions),
                SYMBOL_NAME_CHOICE => SymbolGroup.Choice(cardinality, expressions),
                SYMBOL_NAME_SEQUENCE => SymbolGroup.Sequence(cardinality, expressions),
                _ => throw new System.ArgumentException($"unknown group type: {groupType.Name}")
            };
        }

        private static ISymbolExpression ToRef(Symbol symbolRefExpression, Cardinality cardinality)
        {
            return new SymbolRef(
                symbolRefExpression
                    .Children[0].Value
                    .TrimStart('$'),
                cardinality);
        }

        private static LiteralRule ToLiteral(Symbol symbol) =>  symbol.Children[0].Name switch
        {
            SYMBOL_NAME_CASE_SENSITIVE => new(
                symbol.FindSymbol($"{SYMBOL_NAME_CASE_SENSITIVE}.{SYMBOL_NAME_CASE_LITERAL}").Value,
                true),

            SYMBOL_NAME_CASE_INSENSITIVE => new(
                symbol.FindSymbol($"{SYMBOL_NAME_CASE_INSENSITIVE}.{SYMBOL_NAME_NON_CASE_LITERAL}").Value,
                false),

            _ => throw new System.ArgumentException("Invalid literal-case specifier: "+symbol.Children[0].Name)
        };

        private static PatternRule ToPattern(Symbol symbol)
        {
            return new PatternRule(
                new System.Text.RegularExpressions.Regex(symbol.FindSymbol(SYMBOL_NAME_PATTERN_LITERAL).Value),
                ToCardinality(symbol.FindSymbol(SYMBOL_NAME_CARDINALITY)));
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
    }
}
