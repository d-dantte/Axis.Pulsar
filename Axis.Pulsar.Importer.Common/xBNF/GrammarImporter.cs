using Axis.Pulsar.Parser;
using Axis.Pulsar.Parser.Builders;
using Axis.Pulsar.Parser.CST;
using Axis.Pulsar.Parser.Exceptions;
using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Parsers;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        public const string SYMBOL_NAME_PATTERN_FLAGS = "pattern-flags";
        public const string SYMBOL_NAME_IGNORE_CASE_FLAG = "ignore-case-flag";
        public const string SYMBOL_NAME_DIGITS = "digits";
        public const string SYMBOL_NAME_EOF = "eof";
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

        public IGrammar ImportGrammar(
            Stream inputStream,
            Dictionary<string, IRuleValidator<IRule>> validators = null)
        {
            using var reader = new StreamReader(inputStream);
            var txt = reader.ReadToEnd();

            return ImportRuleInternal(txt, validators);
        }

        public async Task<IGrammar> ImportGrammarAsync(
            Stream inputStream,
            Dictionary<string, IRuleValidator<IRule>> validators = null)
        {
            using var reader = new StreamReader(inputStream);
            var txt = await reader.ReadToEndAsync();

            return ImportRuleInternal(txt, validators);
        }

        private IGrammar ImportRuleInternal(
            string text,
            Dictionary<string, IRuleValidator<IRule>> validators = null)
        {
            if (!BnfParser.TryParse(new BufferedTokenReader(text.Trim()), out var result))
                throw new ParseException(result);

            //build the rule from the symbol-tree
            IResult.Success parseResult = (IResult.Success)result;
            return parseResult.Symbol
                .AllChildNodes()
                .Where(node => node.SymbolName.Equals(SYMBOL_NAME_PRODUCTION))
                .Select(node => ToProduction(node, validators))
                .Aggregate(
                    GrammarBuilder.NewBuilder(),
                    (builder, production) => builder.HasRoot
                        ? builder.WithProduction(production)
                        : builder.WithProduction(production).WithRoot(production.Symbol))
                .Build();
        }

        private Production ToProduction(
            ICSTNode production,
            Dictionary<string, IRuleValidator<IRule>> validators = null)
        {
            if (production is ICSTNode.BranchNode productionNode)
            {
                var ruleSymbol = productionNode.LastNode();
                var name = productionNode
                    .FirstNode()
                    .TokenValue()
                    .TrimStart('$');

                return new(name, ToRule(ruleSymbol, validators?.TryGetValue(name, out var validator) == true ? validator : null));
            }

            else throw new ArgumentException($"Supplied node type '{production?.GetType()}' is not a '{nameof(ICSTNode.BranchNode)}'");
        }

        private static IRule ToRule(
            ICSTNode ruleSymbol,
            IRuleValidator<IRule> validator = null)
            => ruleSymbol.FirstNode().SymbolName switch
            {
                SYMBOL_NAME_LITERAL => ToLiteral(ruleSymbol.FirstNode(), validator),

                SYMBOL_NAME_PATTERN => ToPattern(ruleSymbol.FirstNode(), validator),

                SYMBOL_NAME_SYMBOL_EXPRESSION => ToSymbolExpression(
                    ruleSymbol.FirstNode(),
                    ExtractRecognitionThreshold(ruleSymbol.LastNode()),
                    validator),

                _ => throw new System.ArgumentException($"unknown rule type: {ruleSymbol.SymbolName}")
            };

        private static SymbolExpressionRule ToSymbolExpression(
            ICSTNode expression,
            int? recognitionThreshold,
            IRuleValidator<IRule> validator)
            => new SymbolExpressionRule(ToExpression(expression), recognitionThreshold, validator);

        private static ISymbolExpression ToExpression(ICSTNode expressionSymbol) => expressionSymbol.FirstNode().SymbolName switch
        {
            SYMBOL_NAME_SYMBOL_REF => ToRef(
                expressionSymbol.FirstNode(),
                ToCardinality(expressionSymbol.LastNode())),

            SYMBOL_NAME_GROUPING => ToGrouping(
                expressionSymbol.FirstNode(),
                ToCardinality(expressionSymbol.LastNode())),

            SYMBOL_NAME_EOF => new EOF(),

            _ => throw new System.ArgumentException($"Invalid expression-symbol: {expressionSymbol.SymbolName}")
        };

        private static ISymbolExpression ToGrouping(ICSTNode groupingExpression, Cardinality cardinality)
        {
            var groupType = groupingExpression.FirstNode();

            var expressions = groupType
                .FindNodes(SYMBOL_NAME_SYMBOL_EXPRESSION)
                .Select(ToExpression)
                .ToArray();

            var minSetCount = groupType
                .FindNode(SYMBOL_NAME_DIGITS)?
                .Map(node => node.TokenValue())
                .Map(int.Parse);

            return groupType.SymbolName switch
            {
                SYMBOL_NAME_SET => new SymbolGroup.Set(cardinality, minSetCount, expressions),
                SYMBOL_NAME_CHOICE => new SymbolGroup.Choice(cardinality, expressions),
                SYMBOL_NAME_SEQUENCE => new SymbolGroup.Sequence(cardinality, expressions),
                _ => throw new ArgumentException($"unknown group type: {groupType.SymbolName}")
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

        private static LiteralRule ToLiteral(ICSTNode literal, IRuleValidator<IRule> validator = null) 
            =>  literal.FirstNode().SymbolName switch
            {
                SYMBOL_NAME_CASE_SENSITIVE => new LiteralRule(
                    literal.FindNode($"{SYMBOL_NAME_CASE_SENSITIVE}.{SYMBOL_NAME_CASE_LITERAL}").TokenValue().ApplyEscape(),
                    true,
                    validator),

                SYMBOL_NAME_CASE_INSENSITIVE => new(
                    literal.FindNode($"{SYMBOL_NAME_CASE_INSENSITIVE}.{SYMBOL_NAME_NON_CASE_LITERAL}").TokenValue().ApplyEscape(),
                    false,
                    validator),

                _ => throw new System.ArgumentException("Invalid literal-case specifier: "+literal.FirstNode().SymbolName)
            };

        private static PatternRule ToPattern(ICSTNode pattern, IRuleValidator<IRule> validator = null)
        {
            return new PatternRule(
                new Regex(
                    pattern.FindNode(SYMBOL_NAME_PATTERN_LITERAL).TokenValue().ApplyPatternEscape(),
                    ToRegexOptions(pattern.FindNode(SYMBOL_NAME_PATTERN_FLAGS))),
                ToMatchCardinality(pattern.FindNode(SYMBOL_NAME_MATCH_CARDINALITY)),
                validator);
        }

        private static Cardinality ToCardinality(ICSTNode cardinality)
        {
            if (string.IsNullOrEmpty(cardinality.TokenValue()))
                return Cardinality.OccursOnlyOnce();

            else if (cardinality.NodeAt(1).SymbolName == "numeric-cardinality")
            {
                var ncardinality = cardinality.NodeAt(1);
                var min = int.Parse(ncardinality.NodeAt(0).TokenValue());
                var max =
                    EndsWithComma(ncardinality) ? default(int?) : //null, meaning unbounded
                    EndsWithCommaAndTrailingCharacter(ncardinality) ? int.Parse(ncardinality.NodeAt(2).TokenValue()) :
                    min;

                return Cardinality.Occurs(min, max);
            }
            else //if (cardinality.NodeAt(1).SymbolName == "symbolic-cardinality")
            {
                var scardinality = cardinality.NodeAt(1);
                return scardinality.TokenValue() switch
                {
                    "*" => Cardinality.OccursNeverOrMore(),
                    "?" => Cardinality.OccursOptionally(),
                    "+" => Cardinality.OccursAtLeastOnce(),
                    _ => throw new InvalidOperationException($"Invalid cardinality symbol: {scardinality.TokenValue()}")
                };
            }
        }

        /// <summary>
        /// Match-Cardinality defaults to (min=1, max=unbounded).
        /// </summary>
        private static IPatternMatchType ToMatchCardinality(ICSTNode matchCardinality)
        {
            if (string.IsNullOrEmpty(matchCardinality.TokenValue()))
                return IPatternMatchType.Open.DefaultMatch;

            var first = int.Parse(matchCardinality.NodeAt(1).TokenValue());
            var second =
                EndsWithComma(matchCardinality) ? default : //null, meaning unbounded
                EndsWithCommaAndTrailingCharacter(matchCardinality) ? matchCardinality.NodeAt(3).TokenValue() :
                $"{first}"; // has only initial digits

            return second switch
            {
                "*" => new IPatternMatchType.Open(first, true),
                "+" => new IPatternMatchType.Open(first),
                null => new IPatternMatchType.Open(first),
                _ => new IPatternMatchType.Closed(first, int.Parse(second))
            };
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

        private static bool EndsWithComma(ICSTNode cardinality) => cardinality.TokenValue().EndsWith(",");

        private static bool EndsWithCommaAndTrailingCharacter(ICSTNode cardinality)
        {
            var tokens = cardinality.TokenValue();
            return tokens.Contains(',') && tokens[^1] != ',';
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

        private static RegexOptions ToRegexOptions(ICSTNode patternFlags)
        {
            var options = RegexOptions.Compiled;
            if (patternFlags == null)
                return options;

            var flags = patternFlags
                .AllChildNodes()
                .Select(node => node.SymbolName)
                .Map(names => new HashSet<string>(names));

            if (flags.Contains("ignore-case-flag"))
                options |= RegexOptions.IgnoreCase;

            if (flags.Contains("multi-line-flag"))
                options |= RegexOptions.Multiline;

            if (flags.Contains("single-line-flag"))
                options |= RegexOptions.Singleline;

            if (flags.Contains("explicit-capture-flag"))
                options |= RegexOptions.ExplicitCapture;

            if (flags.Contains("ignore-whitespace-flag"))
                options |= RegexOptions.IgnorePatternWhitespace;

            return options;
        }
    }
}
