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

namespace Axis.Pulsar.Importer.Common.Antlr
{
    public class GrammarImporter : IGrammarImporter
    {
        #region symbol names
        public const string SYMBOL_NAME_PRODUCTION = "production";
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
        public const string SYMBOL_NAME_PATTERN_FLAGS = "pattern-flags";
        public const string SYMBOL_NAME_EOF = "eof";
        #endregion

        private static readonly IGrammar AntlrGrammar;

        static GrammarImporter()
        {
            using var antlrxBnfStream = typeof(GrammarImporter)
                .Assembly
                .GetManifestResourceStream($"{typeof(GrammarImporter).Namespace}.Antlr.xbnf");

            AntlrGrammar = new xBNF
                .GrammarImporter()
                .ImportGrammar(antlrxBnfStream);
        }

        public IGrammar ImportGrammar(
            Stream inputStream,
            Dictionary<string, IRuleValidator<IRule>> validators = null)
        {
            using var reader = new StreamReader(inputStream);
            var txt = reader.ReadToEnd();

            return new BnfTreeWalker(validators).ExtractGrammar(txt);
        }

        public async Task<IGrammar> ImportGrammarAsync(
            Stream inputStream,
            Dictionary<string, IRuleValidator<IRule>> validators = null)
        {
            using var reader = new StreamReader(inputStream);
            var txt = await reader.ReadToEndAsync();

            return new BnfTreeWalker(validators).ExtractGrammar(txt);
        }


        internal class BnfTreeWalker
        {
            private readonly Dictionary<string, (string name, IRule terminal)> _terminals = new Dictionary<string, (string name, IRule terminal)>();
            private readonly Dictionary<string, IRuleValidator<IRule>> _validators;

            internal BnfTreeWalker(Dictionary<string, IRuleValidator<IRule>> validators = null)
            {
                _validators = validators ?? new Dictionary<string, IRuleValidator<IRule>>();
            }

            internal IGrammar ExtractGrammar(string text)
            {
                if (!AntlrGrammar
                    .RootParser()
                    .TryParse(new BufferedTokenReader(text.Trim()), out var result))
                    throw new ParseException(result);

                // consume the symbol tree
                IResult.Success parseResult = (IResult.Success)result;
                var builder = parseResult.Symbol
                    .AllChildNodes()
                    .Where(node => node.SymbolName.Equals(SYMBOL_NAME_PRODUCTION))
                    .Select(ToProduction)
                    .Aggregate(
                        GrammarBuilder.NewBuilder(),
                        (builder, production) => builder.HasRoot
                            ? builder.WithProduction(production)
                            : builder.WithProduction(production).WithRoot(production.Symbol));

                // consume the generated terminal productions
                return _terminals.Values
                    .Aggregate(builder, (builder, tuple) => builder.WithProduction(tuple.name, tuple.terminal))
                    .Build();

                // TODO: validate line-indent-spaces are all of the same indentation count and type. This is trivial, but nice to have.
            }

            private Production ToProduction(ICSTNode node)
            {
                var rule = node.FindNodes(SYMBOL_NAME_RULE_LIST);
                var name = node
                    .FirstNode()
                    .TokenValue();
                return new Production(name, ToRule(rule, _validators.TryGetValue(name, out var validator) ? validator : null));
            }

            private IRule ToRule(
                IEnumerable<ICSTNode> ruleAlternatives,
                IRuleValidator<IRule> validator = null)
            {
                return ruleAlternatives
                    .ToArray()
                    .Map(array => array.Length switch
                    {
                        0 => throw new Exception($"{SYMBOL_NAME_PRODUCTION} must have at least 1 {SYMBOL_NAME_RULE_LIST}"),
                        1 => ToSequence(array[0]),
                        _ => ToChoice(array)
                    })
                    .Map(expression => new SymbolExpressionRule(expression, null, validator));
            }

            private SymbolGroup ToSequence(ICSTNode ruleElement, Cardinality? cardinality = null)
            {
                cardinality ??= Cardinality.OccursOnlyOnce();
                return ruleElement.SymbolName switch
                {
                    SYMBOL_NAME_RULE_LIST => ruleElement
                        .FindNodes($"{SYMBOL_NAME_RULE_ITEM}")
                        .Select(ToExpression)
                        .ToArray()
                        .Map(expressions => new SymbolGroup.Sequence(cardinality.Value, expressions)),

                    SYMBOL_NAME_RULE_GROUP => ruleElement
                        .FindNode($"{SYMBOL_NAME_RULE_LIST}")
                        .Map(list => ToSequence(list, cardinality)),

                    _ => throw new ArgumentException($"Invalid symbol encountered: {ruleElement.SymbolName}")
                };
            }

            private SymbolGroup ToChoice(ICSTNode[] ruleLists)
            {
                return ruleLists
                    .Select(list => ToSequence(list))
                    .ToArray()
                    .Map(expressions => new SymbolGroup.Choice(Cardinality.OccursOnlyOnce(), expressions));
            }

            private ISymbolExpression ToExpression(ICSTNode ruleItem)
            {
                var cardinality = ruleItem
                    .LastNode()
                    .Map(ToCardinality);
                var item = ruleItem.FirstNode();

                return item.SymbolName switch
                {
                    SYMBOL_NAME_EOF => new EOF(),
                    SYMBOL_NAME_RULE_GROUP => ToSequence(item, cardinality),
                    SYMBOL_NAME_REF => new ProductionRef(item.TokenValue(), cardinality),
                    SYMBOL_NAME_TERMINAL => _terminals
                        .GetOrAdd(
                            item.TokenValue(),
                            key => (name: $"____terminal-{_terminals.Count}", terminal: ToTerminal(item)))
                        .Map(terminalInfo => new ProductionRef(terminalInfo.name, cardinality)),

                    _ => throw new ArgumentException($"Invalid symbol name: {item.SymbolName}")
                };
            }

            private IRule ToTerminal(ICSTNode terminal)
            {
                var terminalType = terminal.FirstNode();
                return terminalType.SymbolName switch
                {
                    SYMBOL_NAME_INSENSITIVE_LITERAL => new LiteralRule(
                        isCaseSensitive: false,
                        ruleValidator: null,
                        value: terminalType
                            .FindNode(SYMBOL_NAME_INSENSITIVE_VALUE)
                            .TokenValue()
                            .ApplyEscape()),

                    SYMBOL_NAME_SENSITIVE_LITERAL => new LiteralRule(
                        isCaseSensitive: true,
                        ruleValidator: null,
                        value: terminalType
                            .FindNode(SYMBOL_NAME_SENSITIVE_VALUE)
                            .TokenValue()
                            .ApplyEscape()),

                    SYMBOL_NAME_PATTERN => new PatternRule(
                        ruleValidator: null,
                        matchType: ToMatchType(terminal.FindNode($"{SYMBOL_NAME_PATTERN}.{SYMBOL_NAME_MATCH_CARDINALITY}")),
                        regex: new Regex(
                            options: ToRegexOptions(terminalType.FindNode(SYMBOL_NAME_PATTERN_FLAGS)),
                            pattern: terminal
                                .FindNode($"{SYMBOL_NAME_PATTERN}.{SYMBOL_NAME_PATTERN_VALUE}")
                                .TokenValue())),

                    _ => throw new ArgumentException($"Invalid symbol name: {terminal.FirstNode().SymbolName}")
                };
            }

            private Cardinality ToCardinality(ICSTNode cardinality)
            {
                return cardinality.SymbolName switch
                {
                    SYMBOL_NAME_CARDINALITY => cardinality.TokenValue() switch
                    {
                        "?" => Cardinality.OccursOptionally(),
                        "*" => Cardinality.OccursNeverOrMore(),
                        "+" => Cardinality.OccursAtLeastOnce(),
                        _ => throw new ArgumentException($"Invalid cardinality symbol encountered: {cardinality.TokenValue()}")
                    },
                    _ => Cardinality.OccursOnlyOnce()
                };
            }

            private IPatternMatchType ToMatchType(ICSTNode matchCardinality)
            {
                if (matchCardinality == null)
                    return IPatternMatchType.Open.DefaultMatch;

                var first = matchCardinality.NodeAt(1).TokenValue();
                var second = matchCardinality.NodeAt(3)?.TokenValue();

                return second switch
                {
                    null => IPatternMatchType.Open.DefaultMatch,
                    "*" => new IPatternMatchType.Open(int.Parse(first), true),
                    "+" => new IPatternMatchType.Open(int.Parse(first), false),
                    _ => new IPatternMatchType.Closed(int.Parse(first), int.Parse(second))
                };
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
}
