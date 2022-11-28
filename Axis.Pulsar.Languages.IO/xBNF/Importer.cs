using Axis.Pulsar.Grammar;
using Axis.Pulsar.Grammar.Builders;
using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Exceptions;
using Axis.Pulsar.Grammar.IO;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Recognizers.Results;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Axis.Pulsar.Languages.xBNF
{
    using BNFGrammar = Grammar.Language.Grammar;
    using MatchType = Grammar.Language.MatchType;

    /// <summary>
    /// The grammer importer for the xBNF metasyntax language.
    /// </summary>
    public class Importer : IImporter, ICustomTerminalRegistry, IValidatorRegistry
    {
        #region symbol names
        public const string SYMBOL_NAME_PRODUCTION = "production";
        public const string SYMBOL_NAME_LITERAL = "literal";
        public const string SYMBOL_NAME_PATTERN = "pattern";
        public const string SYMBOL_NAME_EXPRESSION_RULE = "expression-rule";
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
        public const string SYMBOL_NAME_RECOGNITION_THRESHOLD = "recognition-threshold";
        #endregion

        private static readonly BNFGrammar BnfGrammar;

        private ConcurrentDictionary<string, IProductionValidator> _validatorMap = new();

        static Importer()
        {
            using var bnfXmlStream = typeof(Importer)
                .Assembly
                .GetManifestResourceStream($"{typeof(Importer).Namespace}.xBNFRule.xml");

            BnfGrammar = new Xml
                .Importer()
                .ImportGrammar(bnfXmlStream);
        }

        #region Validator API
        public IValidatorRegistry RegisterValidator(string symbolName, IProductionValidator validator)
        {
            if (validator is null)
                throw new ArgumentNullException(nameof(validator));

            if (symbolName is null)
                throw new ArgumentNullException(nameof(symbolName));

            if (!SymbolHelper.SymbolPattern.IsMatch(symbolName))
                throw new ArgumentException($"Invalid {nameof(symbolName)}: {symbolName}");

            _ = _validatorMap.AddOrUpdate(symbolName, validator, (_, _) => validator);

            return this;
        }

        public string[] RegisteredSymbols() => _validatorMap.Keys.ToArray();

        public IProductionValidator RegisteredValidator(string symbolName)
            => _validatorMap.TryGetValue(symbolName, out var validator)
                ? validator
                : null;
        #endregion

        #region Custom Terminal API
        ICustomTerminalRegistry RegisterTerminal(ICustomTerminal validator);

        bool TryRegister(ICustomTerminal terminal);

        string[] RegisteredSymbols();

        ICustomTerminal RegisteredTerminal(string symbolName);
        #endregion

        /// <inheritdoc/>
        public BNFGrammar ImportGrammar(Stream inputStream)
        {
            using var reader = new StreamReader(inputStream);
            var txt = reader.ReadToEnd();

            return ToGrammar(txt);
        }

        /// <inheritdoc/>
        public async Task<BNFGrammar> ImportGrammarAsync(Stream inputStream)
        {
            using var reader = new StreamReader(inputStream);
            var txt = await reader.ReadToEndAsync();

            return ToGrammar(txt);
        }

        internal BNFGrammar ToGrammar(string text)
        {
            var tokenReader = new BufferedTokenReader(text.Trim());
            if (!BnfGrammar.RootRecognizer().TryRecognize(tokenReader, out var result))
                throw new RecognitionException(result);

            var successResult = result as SuccessResult;
            return successResult.Symbol
                .AllChildNodes()
                .Where(node => node.SymbolName.Equals(SYMBOL_NAME_PRODUCTION))
                .Aggregate(GrammarBuilder.NewBuilder(), (builder, symbol) =>
                {
                    _ = builder.HavingProduction(_builder => BuildProduction(_builder, symbol));

                    if (!builder.HasRoot)
                        builder.WithRoot(ExtractProductionSymbol(symbol));

                    return builder;
                })
                .Build();
        }

        internal void BuildProduction(
            ProductionBuilder productionBuilder,
            CSTNode productionNode)
        {
            var ruleNode = productionNode.LastNode();
            var name = ExtractProductionSymbol(productionNode);

            productionBuilder
                .WithSymbol(name)
                .WithRecognitionThreshold(ExtractThreshold(ruleNode))
                .WithValidator(RegisteredValidator(name))
                .WithRule(_builder => ConfigureRule(_builder, ruleNode));
        }

        internal void ConfigureRule(RuleBuilder builder, CSTNode ruleNode)
        {
            var ruleTypeNode = ruleNode.FirstNode();
            _ = ruleTypeNode.SymbolName switch
            {
                SYMBOL_NAME_EOF => builder.WithEOF(),

                SYMBOL_NAME_PATTERN => WithPattern(builder, ruleTypeNode),

                SYMBOL_NAME_LITERAL => WithLiteral(builder, ruleTypeNode),

                SYMBOL_NAME_SYMBOL_REF => builder.WithRef(
                    ExtractSymbolName(ruleTypeNode.FirstNode()),
                    ExtractCardinality(ruleTypeNode.NodeAt(1))),

                SYMBOL_NAME_GROUPING => WithGrouping(
                    builder,
                    ruleTypeNode.FirstNode(),
                    ruleTypeNode.NodeAt(1)),

                _ => throw new ArgumentException($"Invalid element: {ruleTypeNode.SymbolName}")
            };
        }

        internal void ConfigureRuleList(RuleListBuilder builder, CSTNode[] ruleNodes)
        {
            foreach (var ruleNode in ruleNodes)
            {
                var ruleTypeNode = ruleNode.FirstNode();
                _ = ruleTypeNode.SymbolName switch
                {
                    SYMBOL_NAME_EOF => builder.HavingEOF(),

                    SYMBOL_NAME_PATTERN => HavingPattern(builder, ruleTypeNode),

                    SYMBOL_NAME_LITERAL => HavingLiteral(builder, ruleTypeNode),

                    SYMBOL_NAME_SYMBOL_REF => builder.HavingRef(
                        ExtractSymbolName(ruleTypeNode.FirstNode()),
                        ExtractCardinality(ruleTypeNode.NodeAt(1))),

                    SYMBOL_NAME_GROUPING => HavingGrouping(
                        builder,
                        ruleTypeNode.FirstNode(),
                        ruleTypeNode.NodeAt(1)),

                    _ => throw new ArgumentException($"Invalid element: {ruleTypeNode.SymbolName}")
                };
            }
        }

        internal RuleBuilder WithPattern(RuleBuilder builder, CSTNode patternNode)
        {
            var pattern = ExtractPattern(patternNode);
            var regexOptions = ExtractRegexOptions(patternNode);
            var matchType = ExtractMatchType(patternNode);

            return builder.WithPattern(
                new Regex(pattern, regexOptions),
                matchType);
        }

        internal RuleListBuilder HavingPattern(RuleListBuilder builder, CSTNode patternNode)
        {
            var pattern = ExtractPattern(patternNode);
            var regexOptions = ExtractRegexOptions(patternNode);
            var matchType = ExtractMatchType(patternNode);

            return builder.HavingPattern(
                new Regex(pattern, regexOptions),
                matchType);
        }

        internal RuleBuilder WithLiteral(RuleBuilder builder, CSTNode literalNode)
        {
            var literalTypeNode = literalNode.FirstNode();
            var isCaseSensitive = literalTypeNode.SymbolName switch
            {
                SYMBOL_NAME_CASE_SENSITIVE => true,
                SYMBOL_NAME_CASE_INSENSITIVE => false,
                _ => throw new InvalidOperationException($"Invalid literal type: {literalTypeNode.SymbolName}")
            };
            var literalValue = literalTypeNode.NodeAt(1).TokenValue();

            return builder.WithLiteral(literalValue, isCaseSensitive);
        }

        internal RuleListBuilder HavingLiteral(RuleListBuilder builder, CSTNode literalNode)
        {
            var literalTypeNode = literalNode.FirstNode();
            var isCaseSensitive = literalTypeNode.SymbolName switch
            {
                SYMBOL_NAME_CASE_SENSITIVE => true,
                SYMBOL_NAME_CASE_INSENSITIVE => false,
                _ => throw new InvalidOperationException($"Invalid literal type: {literalTypeNode.SymbolName}")
            };
            var literalValue = literalTypeNode.NodeAt(1).TokenValue();

            return builder.HavingLiteral(literalValue, isCaseSensitive);
        }

        internal RuleBuilder WithGrouping(RuleBuilder builder, CSTNode groupingTypeNode, CSTNode cardinalityNode)
        {
            var cardinality = ExtractCardinality(cardinalityNode);
            var rules = groupingTypeNode
                .FindNodes(SYMBOL_NAME_EXPRESSION_RULE)
                .ToArray();

            return groupingTypeNode.SymbolName switch
            {
                SYMBOL_NAME_CHOICE => builder.WithChoice(
                    cardinality: cardinality,
                    ruleListBuilderAction: _builder => ConfigureRuleList(_builder, rules)),

                SYMBOL_NAME_SEQUENCE => builder.WithSequence(
                    cardinality: cardinality,
                    ruleListBuilderAction: _builder => ConfigureRuleList(_builder, rules)),

                SYMBOL_NAME_SET => builder.WithSet(
                    cardinality: cardinality,
                    minRecognitionCount: ExtractMinRecognitionCount(groupingTypeNode),
                    ruleListBuilderAction: _builder => ConfigureRuleList(_builder, rules)),

                _ => throw new InvalidOperationException($"Invalid grouping type: {groupingTypeNode.SymbolName}")
            };
        }

        internal RuleListBuilder HavingGrouping(RuleListBuilder builder, CSTNode groupingTypeNode, CSTNode cardinalityNode)
        {
            var cardinality = ExtractCardinality(cardinalityNode);
            var rules = groupingTypeNode
                .FindNodes(SYMBOL_NAME_EXPRESSION_RULE)
                .ToArray();

            return groupingTypeNode.SymbolName switch
            {
                SYMBOL_NAME_CHOICE => builder.HavingChoice(
                    cardinality: cardinality,
                    ruleListBuilderAction: _builder => ConfigureRuleList(_builder, rules)),

                SYMBOL_NAME_SEQUENCE => builder.HavingSequence(
                    cardinality: cardinality,
                    ruleListBuilderAction: _builder => ConfigureRuleList(_builder, rules)),

                SYMBOL_NAME_SET => builder.HavingSet(
                    cardinality: cardinality,
                    minRecognitionCount: ExtractMinRecognitionCount(groupingTypeNode),
                    ruleListBuilderAction: _builder => ConfigureRuleList(_builder, rules)),

                _ => throw new InvalidOperationException($"Invalid grouping type: {groupingTypeNode.SymbolName}")
            };
        }


        internal string ExtractProductionSymbol(CSTNode productionNode)
        {
            return productionNode
                .FirstNode()
                .TokenValue()
                .TrimStart('$');
        }

        internal int? ExtractThreshold(CSTNode ruleNode)
        {
            var thresholdNode = ruleNode.LastNode();
            if (thresholdNode.SymbolName == SYMBOL_NAME_RECOGNITION_THRESHOLD)
                return int.TryParse(thresholdNode.TokenValue().TrimStart('>'), out var threshold)
                    ? threshold
                    : null;

            return null;
        }

        internal static RegexOptions ExtractRegexOptions(CSTNode patternNode)
        {
            var patternFlags = patternNode.FindNode(SYMBOL_NAME_MATCH_CARDINALITY);
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

        internal static string ExtractPattern(CSTNode patternNode)
        {
            return patternNode
                .FindNode(SYMBOL_NAME_PATTERN_LITERAL)
                .TokenValue()
                .ApplyPatternEscape();
        }

        internal static MatchType ExtractMatchType(CSTNode patternNode)
        {
            var matchCardinality = patternNode.FindNode(SYMBOL_NAME_MATCH_CARDINALITY);

            if (string.IsNullOrEmpty(matchCardinality.TokenValue()))
                return MatchType.Open.DefaultMatch;

            var first = int.Parse(matchCardinality.NodeAt(1).TokenValue());
            var second =
                EndsWithComma(matchCardinality) ? default : //null, meaning unbounded
                EndsWithCommaAndTrailingCharacter(matchCardinality) ? matchCardinality.NodeAt(3).TokenValue() :
                $"{first}"; // has only initial digits

            return second switch
            {
                "*" => new MatchType.Open(first, true),
                "+" => new MatchType.Open(first),
                null => new MatchType.Open(first),
                _ => new MatchType.Closed(first, int.Parse(second))
            };
        }

        internal static string ExtractSymbolName(CSTNode symbolRefNode)
        {
            return symbolRefNode.FirstNode().TokenValue().TrimStart('$');
        }

        internal static Cardinality ExtractCardinality(CSTNode cardinalityNode)
        {
            if (string.IsNullOrEmpty(cardinalityNode.TokenValue()))
                return Cardinality.OccursOnlyOnce();

            else if (cardinalityNode.NodeAt(1).SymbolName == "numeric-cardinality")
            {
                var ncardinality = cardinalityNode.NodeAt(1);
                var min = int.Parse(ncardinality.NodeAt(0).TokenValue());
                var max =
                    EndsWithComma(ncardinality) ? default(int?) : //null, meaning unbounded
                    EndsWithCommaAndTrailingCharacter(ncardinality) ? int.Parse(ncardinality.NodeAt(2).TokenValue()) :
                    min;

                return Cardinality.Occurs(min, max);
            }
            else //if (cardinality.NodeAt(1).SymbolName == "symbolic-cardinality")
            {
                var scardinality = cardinalityNode.NodeAt(1);
                return scardinality.TokenValue() switch
                {
                    "*" => Cardinality.OccursNeverOrMore(),
                    "?" => Cardinality.OccursOptionally(),
                    "+" => Cardinality.OccursAtLeastOnce(),
                    _ => throw new InvalidOperationException($"Invalid cardinality symbol: {scardinality.TokenValue()}")
                };
            }
        }

        internal static int? ExtractMinRecognitionCount(CSTNode setNode)
        {
            return int.TryParse(setNode.FindNode(SYMBOL_NAME_DIGITS)?.TokenValue(), out var value)
                ? value
                : null;
        }


        internal static bool EndsWithComma(CSTNode cardinality) => cardinality.TokenValue().EndsWith(",");

        internal static bool EndsWithCommaAndTrailingCharacter(CSTNode cardinality)
        {
            var tokens = cardinality.TokenValue();
            return tokens.Contains(',') && tokens[^1] != ',';
        }
    }
}
