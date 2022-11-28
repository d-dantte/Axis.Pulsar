using Axis.Pulsar.Grammar;
using Axis.Pulsar.Grammar.Builders;
using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Exceptions;
using Axis.Pulsar.Grammar.IO;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Language.Rules.CustomTerminals;
using Axis.Pulsar.Grammar.Recognizers.Results;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Axis.Pulsar.Languages.xAntlr
{
    using AntlrGrammar = Grammar.Language.Grammar;
    using MatchType = Grammar.Language.MatchType;

    public class Importer: IImporter, ICustomTerminalRegistry, IValidatorRegistry
    {
        #region symbol names
        public const string SYMBOL_NAME_PRODUCTION = "production";
        public const string SYMBOL_NAME_PRODUCTION_OPTIONS = "production-options";
        public const string SYMBOL_NAME_PRODUCTION_OPTION_SET = "production-option-set";
        public const string SYMBOL_NAME_THRESHOLD = "threshold";
        public const string SYMBOL_NAME_INITIAL_RULE_LIST = "initial-rule-list";
        public const string SYMBOL_NAME_ALTERNATE_RULE_LIST = "alternate-rule-list";
        public const string SYMBOL_NAME_RULE_LIST = "rule-list";
        public const string SYMBOL_NAME_RULE_GROUP = "rule-group";
        public const string SYMBOL_NAME_RULE_ITEM = "rule-item";
        public const string SYMBOL_NAME_TERMINAL = "terminal";
        public const string SYMBOL_NAME_REF = "ref";
        public const string SYMBOL_NAME_PATTERN = "pattern";
        public const string SYMBOL_NAME_PATTERN_VALUE = "pattern-value";
        public const string SYMBOL_NAME_SENSITIVE_LITERAL = "sensitive-literal";
        public const string SYMBOL_NAME_INSENSITIVE_LITERAL = "insensitive-literal";
        public const string SYMBOL_NAME_LINE_INDENT_SPACE = "line-indent-space";
        public const string SYMBOL_NAME_LINE_SPACE = "line-space";
        public const string SYMBOL_NAME_RULE_END = "rule-end";
        public const string SYMBOL_NAME_SYMBOL_NAME = "symbol-name";
        public const string SYMBOL_NAME_CARDINALITY = "cardinality";
        public const string SYMBOL_NAME_MATCH_CARDINALITY = "match-cardinality";
        public const string SYMBOL_NAME_SPACE = "space";
        public const string SYMBOL_NAME_TAB = "tab";
        public const string SYMBOL_NAME_WHITE_SPACE = "white-space";
        public const string SYMBOL_NAME_SENSITIVE_VALUE = "sensitive-value";
        public const string SYMBOL_NAME_INSENSITIVE_VALUE = "insensitive-value";
        public const string SYMBOL_NAME_NEW_LINE = "new-line";
        public const string SYMBOL_NAME_COLON = "colon";
        public const string SYMBOL_NAME_SEMI_COLON = "semi-colon";
        public const string SYMBOL_NAME_V_BAR = "v-bar";
        public const string SYMBOL_NAME_L_BRACKET = "l-bracket";
        public const string SYMBOL_NAME_R_BRACKET = "r-bracket";
        public const string SYMBOL_NAME_F_SLASH = "f-slash";
        public const string SYMBOL_NAME_S_QUOTE = "s-quote";
        public const string SYMBOL_NAME_D_QUOTE = "d-quote";
        public const string SYMBOL_NAME_DIGITS = "digits";
        public const string SYMBOL_NAME_PATTERN_FLAGS = "pattern-flags";
        public const string SYMBOL_NAME_EOF = "eof";
        #endregion

        private static readonly AntlrGrammar AntlrGrammar;

        private ConcurrentDictionary<string, IProductionValidator> _validatorMap = new();

        static Importer()
        {
            using var antlrxBnfStream = typeof(Importer)
                .Assembly
                .GetManifestResourceStream($"{typeof(Importer).Namespace}.xAntlr.xbnf");

            // TODO: add a validator for the '$line-indent-space' production to verify that they all have the same space, or tabs
            AntlrGrammar = new xBNF
                .Importer()
                .ImportGrammar(antlrxBnfStream);
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
        public AntlrGrammar ImportGrammar(Stream inputStream)
        {
            using var reader = new StreamReader(inputStream);
            var txt = reader.ReadToEnd();

            return ToGrammar(txt);
        }

        /// <inheritdoc/>
        public async Task<AntlrGrammar> ImportGrammarAsync(Stream inputStream)
        {
            using var reader = new StreamReader(inputStream);
            var txt = await reader.ReadToEndAsync();

            return ToGrammar(txt);
        }


        internal AntlrGrammar ToGrammar(string text)
        {
            var tokenReader = new BufferedTokenReader(text.Trim());
            if (!AntlrGrammar.RootRecognizer().TryRecognize(tokenReader, out var result))
                throw new RecognitionException(result);

            var successResult = result as SuccessResult;
            return successResult.Symbol
                .AllChildNodes()
                .Where(node => node.SymbolName.Equals(SYMBOL_NAME_PRODUCTION))
                .Aggregate(GrammarBuilder.NewBuilder(), (builder, symbol) =>
                {
                    _ = builder.HavingProduction(_builder => ConfigureProduction(_builder, symbol));

                    if (!builder.HasRoot)
                        builder.WithRoot(ExtractProductionSymbol(symbol));

                    return builder;
                })
                .Build();
        }

        internal void ConfigureProduction(
            ProductionBuilder productionBuilder,
            CSTNode productionNode)
        {
            var name = ExtractProductionSymbol(productionNode);
            var ruleListPath = $"{SYMBOL_NAME_INITIAL_RULE_LIST}|{SYMBOL_NAME_ALTERNATE_RULE_LIST}.{SYMBOL_NAME_RULE_LIST}";
            var alternatives = productionNode
                .FindNodes(ruleListPath)
                .ToArray();

            var productionRule = alternatives.Length > 1
                ? ToAlternativesChoice(alternatives)
                : ToRuleListSequence(alternatives[0]);

            productionBuilder
                .WithSymbol(name)
                .WithRecognitionThreshold(ExtractThreshold(productionNode))
                .WithValidator(RegisteredValidator(name))
                .WithRule(productionRule);
        }

        internal IRule ToRuleListSequence(CSTNode ruleListNode, CSTNode cardinalityNode = null)
        {
            var cardinality = ExtractCardinality(cardinalityNode);
            return ruleListNode
                .FindNodes(SYMBOL_NAME_RULE_ITEM)
                .Select(ToRule)
                .Map(rules => new Sequence(
                    cardinality,
                    rules.ToArray()));
        }

        internal IRule ToAlternativesChoice(CSTNode[] ruleListNodes)
        {
            return ruleListNodes
                .Select(node => ToRuleListSequence(node))
                .Map(rules => new Choice(rules.ToArray()));
        }

        internal IRule ToRule(CSTNode ruleItemNode)
        {
            var ruleTypeNode = ruleItemNode.FirstNode();
            return ruleTypeNode.SymbolName switch
            {
                SYMBOL_NAME_EOF => new EOF(),

                SYMBOL_NAME_TERMINAL => ToTerminal(ruleTypeNode),

                SYMBOL_NAME_REF => new ProductionRef(
                    ruleTypeNode.TokenValue(),
                    ExtractCardinality(ruleItemNode.LastNode())),

                SYMBOL_NAME_RULE_GROUP => ToRuleListSequence(
                    ruleTypeNode.FindNode(SYMBOL_NAME_RULE_LIST),
                    ruleItemNode.LastNode()),

                _ => throw new InvalidOperationException($"Invaid rule item type: {ruleTypeNode.SymbolName}")
            };
        }

        internal IAtomicRule ToTerminal(CSTNode terminalNode)
        {
            var terminalType = terminalNode.FirstNode();
            return terminalType.SymbolName switch
            {
                SYMBOL_NAME_INSENSITIVE_LITERAL => new Literal(
                    isCaseSensitive: false,
                    value: terminalType
                        .FindNode(SYMBOL_NAME_INSENSITIVE_VALUE)
                        .TokenValue()
                        .ApplyEscape()),

                SYMBOL_NAME_SENSITIVE_LITERAL => new Literal(
                    isCaseSensitive: true,
                    value: terminalType
                        .FindNode(SYMBOL_NAME_SENSITIVE_VALUE)
                        .TokenValue()
                        .ApplyEscape()),

                SYMBOL_NAME_PATTERN => new Pattern(
                    matchType: ExtractMatchType(terminalNode.FindNode($"{SYMBOL_NAME_PATTERN}.{SYMBOL_NAME_MATCH_CARDINALITY}")),
                    regex: new Regex(
                        options: ExtractRegexOptions(terminalType.FindNode(SYMBOL_NAME_PATTERN_FLAGS)),
                        pattern: terminalNode
                            .FindNode($"{SYMBOL_NAME_PATTERN}.{SYMBOL_NAME_PATTERN_VALUE}")
                            .TokenValue())),

                _ => throw new ArgumentException($"Invalid symbol name: {terminalNode.FirstNode().SymbolName}")
            };
        }


        internal string ExtractProductionSymbol(CSTNode productionNode) => productionNode.FirstNode().TokenValue();

        internal int? ExtractThreshold(CSTNode productionNode)
        {
            var nodePath = new StringBuilder()
                .Append(SYMBOL_NAME_PRODUCTION_OPTIONS)
                .Append($".{SYMBOL_NAME_PRODUCTION_OPTION_SET}")
                .Append($".{SYMBOL_NAME_THRESHOLD}")
                .Append($".{SYMBOL_NAME_DIGITS}")
                .ToString();

            var thresholdNode = productionNode.FindNode(nodePath);

            return int.TryParse(thresholdNode?.TokenValue(), out var value)
                ? value
                : null;
        }

        internal Cardinality ExtractCardinality(CSTNode cardinalityNode)
        {
            return cardinalityNode?.SymbolName switch
            {
                SYMBOL_NAME_CARDINALITY => cardinalityNode.TokenValue() switch
                {
                    "?" => Cardinality.OccursOptionally(),
                    "*" => Cardinality.OccursNeverOrMore(),
                    "+" => Cardinality.OccursAtLeastOnce(),
                    _ => throw new InvalidOperationException($"Invalid cardinality symbol encountered: {cardinalityNode.TokenValue()}")
                },
                _ => Cardinality.OccursOnlyOnce()
            };
        }

        private MatchType ExtractMatchType(CSTNode matchCardinality)
        {
            if (matchCardinality == null)
                return MatchType.Open.DefaultMatch;

            var first = matchCardinality.NodeAt(1).TokenValue();
            var second = matchCardinality.NodeAt(3)?.TokenValue();

            return second switch
            {
                null => MatchType.Open.DefaultMatch,
                "*" => new MatchType.Open(int.Parse(first), true),
                "+" => new MatchType.Open(int.Parse(first), false),
                _ => new MatchType.Closed(int.Parse(first), int.Parse(second))
            };
        }

        private static RegexOptions ExtractRegexOptions(CSTNode patternFlags)
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
