using Axis.Pulsar.Parser.CST;
using Axis.Pulsar.Parser.Exceptions;
using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Parsers;
using Axis.Pulsar.Parser.Utils;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Axis.Pulsar.Importer.Common.xBNF
{
    /// <summary>
    /// The grammer importer for the xBNF metasyntax language.
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
        public const string SYMBOL_NAME_MATCH_CARDINALITY = "match-cardinality";
        #endregion

        private static readonly IGrammar BnfGrammar;
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

        public IGrammar ImportGrammar(Stream inputStream)
        {
            using var reader = new StreamReader(inputStream);
            var txt = reader.ReadToEnd();

            return ImportRuleInternal(txt);
        }

        public async Task<IGrammar> ImportGrammarAsync(Stream inputStream)
        {
            using var reader = new StreamReader(inputStream);
            var txt = await reader.ReadToEndAsync();

            return ImportRuleInternal(txt);
        }

        private IGrammar ImportRuleInternal(string text)
        {
            if (!BnfParser.TryParse(new BufferedTokenReader(text.Trim()), out var result))
                throw new ParseException(result);

            //build the rule from the symbol-tree
            IResult.Success parseResult = (IResult.Success)result;
            return parseResult.Symbol
                .AllChildNodes()
                .Where(node => node.SymbolName.Equals(SYMBOL_NAME_PRODUCTION))
                .Select(ToProduction)
                .Aggregate(
                    GrammarBuilder.NewBuilder(),
                    (builder, production) => builder.HasRoot
                        ? builder.WithProduction(production)
                        : builder.WithProduction(production).WithRoot(production.Symbol))
                .Build();
        }

        private Production ToProduction(ICSTNode production)
        {
            if (production is ICSTNode.BranchNode productionNode)
            {
                var ruleSymbol = productionNode.LastNode();
                var name = productionNode
                    .FirstNode()
                    .TokenValue()
                    .TrimStart('$');
                return new(name, ToRule(ruleSymbol));
            }

            else throw new ArgumentException($"Supplied node type '{production?.GetType()}' is not a '{nameof(ICSTNode.BranchNode)}'");
        }

        private static IRule ToRule(ICSTNode ruleSymbol) => ruleSymbol.FirstNode().SymbolName switch
        {
            SYMBOL_NAME_LITERAL => ToLiteral(ruleSymbol.FirstNode()),

            SYMBOL_NAME_PATTERN => ToPattern(ruleSymbol.FirstNode()),

            SYMBOL_NAME_SYMBOL_EXPRESSION => ToSymbolExpression(
                ruleSymbol.FirstNode(),
                ExtractRecognitionThreshold(ruleSymbol.LastNode())),

            _ => throw new System.ArgumentException($"unknown rule type: {ruleSymbol.SymbolName}")
        };

        private static SymbolExpressionRule ToSymbolExpression(ICSTNode expression, int? recognitionThreshold)
            => new SymbolExpressionRule(ToExpression(expression), recognitionThreshold);

        private static ISymbolExpression ToExpression(ICSTNode expressionSymbol) => expressionSymbol.FirstNode().SymbolName switch
        {
            SYMBOL_NAME_SYMBOL_REF => ToRef(
                expressionSymbol.FirstNode(),
                ToCardinality(expressionSymbol.LastNode())),

            SYMBOL_NAME_GROUPING => ToGrouping(
                expressionSymbol.FirstNode(),
                ToCardinality(expressionSymbol.LastNode())),

            _ => throw new System.ArgumentException($"Invalid expression-symbol: {expressionSymbol.SymbolName}")
        };

        private static ISymbolExpression ToGrouping(ICSTNode groupingExpression, Cardinality cardinality)
        {
            var groupType = groupingExpression.FirstNode();

            var expressions = groupType
                .FindNodes(SYMBOL_NAME_SYMBOL_EXPRESSION)
                .Select(ToExpression)
                .ToArray();

            return groupType.SymbolName switch
            {
                SYMBOL_NAME_SET => SymbolGroup.Set(cardinality, expressions),
                SYMBOL_NAME_CHOICE => SymbolGroup.Choice(cardinality, expressions),
                SYMBOL_NAME_SEQUENCE => SymbolGroup.Sequence(cardinality, expressions),
                _ => throw new System.ArgumentException($"unknown group type: {groupType.SymbolName}")
            };
        }

        private static ISymbolExpression ToRef(ICSTNode symbolRefExpression, Cardinality cardinality)
        {
            return new ProductionRef(
                symbolRefExpression
                    .TokenValue()
                    .TrimStart('$'),
                cardinality);
        }

        private static LiteralRule ToLiteral(ICSTNode literal) =>  literal.FirstNode().SymbolName switch
        {
            SYMBOL_NAME_CASE_SENSITIVE => new LiteralRule(
                literal.FindNode($"{SYMBOL_NAME_CASE_SENSITIVE}.{SYMBOL_NAME_CASE_LITERAL}").TokenValue().UnescapeSensitive(),
                true),

            SYMBOL_NAME_CASE_INSENSITIVE => new(
                literal.FindNode($"{SYMBOL_NAME_CASE_INSENSITIVE}.{SYMBOL_NAME_NON_CASE_LITERAL}").TokenValue().UnescapeInsensitive(),
                false),

            _ => throw new System.ArgumentException("Invalid literal-case specifier: "+literal.FirstNode().SymbolName)
        };

        private static PatternRule ToPattern(ICSTNode pattern)
        {
            return new PatternRule(
                new System.Text.RegularExpressions.Regex(pattern.FindNode(SYMBOL_NAME_PATTERN_LITERAL).TokenValue().UnescapePattern()),
                ToMatchCardinality(pattern.FindNode(SYMBOL_NAME_MATCH_CARDINALITY)));
        }

        private static Cardinality ToCardinality(ICSTNode cardinality)
        {
            if (string.IsNullOrEmpty(cardinality.TokenValue()))
                return Cardinality.OccursOnlyOnce();

            else
            {
                var min = int.Parse(cardinality.NodeAt(1).TokenValue());
                var max =
                    HasOnlyComma(cardinality) ? default(int?) : //null, meaning unbounded
                    HasCommaAndTrailingCharacter(cardinality) ? int.Parse(cardinality.NodeAt(3).TokenValue()) :
                    min;

                return Cardinality.Occurs(min, max);
            }
        }

        /// <summary>
        /// Match-Cardinality defaults to (min=1, max=unbounded).
        /// </summary>
        private static Cardinality ToMatchCardinality(ICSTNode matchCardinality)
        {
            var cardinality = matchCardinality.FirstNode();
            if (string.IsNullOrEmpty(cardinality.TokenValue()))
                return Cardinality.OccursAtLeastOnce();

            else
            {
                var min = int.Parse(cardinality.NodeAt(1).TokenValue());
                var max =
                    HasOnlyComma(cardinality) ? default(int?) : //null, meaning unbounded
                    HasCommaAndTrailingCharacter(cardinality) ? int.Parse(cardinality.NodeAt(3).TokenValue()) :
                    min;

                return Cardinality.Occurs(min, max);
            }
        }

        private static int ToRecognitionThreshold(ICSTNode recognitionThreshold)
        {
            if (string.IsNullOrEmpty(recognitionThreshold.TokenValue()))
                return 1;

            else
                return recognitionThreshold
                    .NodeAt(1)
                    .TokenValue()
                    .Map(int.Parse);
        }

        private static bool HasOnlyComma(ICSTNode cardinality)
        {
            return ",".Equals(cardinality.NodeAt(2).TokenValue())
                && cardinality.AllChildNodes().Count() == 3;
        }

        private static bool HasCommaAndTrailingCharacter(ICSTNode cardinality)
        {
            return ",".Equals(cardinality.NodeAt(2).TokenValue())
                && !string.IsNullOrEmpty(cardinality.NodeAt(3).TokenValue());
        }

        private static int? ExtractRecognitionThreshold(ICSTNode recognitionThreshold)
        {
            if (recognitionThreshold.TokenValue() == null)
                return null;

            return recognitionThreshold
                .LastNode()?
                .TokenValue()
                .Map(int.Parse);
        }
    }
}
