using Axis.Pulsar.Parser.Exceptions;
using Axis.Pulsar.Parser.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Grammar
{
    /// <summary>
    /// A grammar encapsulates productions that together make up a language.
    /// </summary>
    public class Grammar
    {
        private readonly Dictionary<string, IRule> _ruleMap;

        #region Properties
        public string RootSymbol { get; }

        public Production Root => new(RootSymbol, _ruleMap[RootSymbol]);

        public IEnumerable<Production> Productions() => _ruleMap.Select(kvp => new Production(kvp.Key, kvp.Value));

        public IRule this[string symbol]
        {
            get
            {
                if (symbol == null)
                    throw new ArgumentNullException(nameof(symbol));

                else return _ruleMap[symbol];
            }
        }
        #endregion

        internal Grammar(string rootSymbolName, params Production[] productions)
        {
            RootSymbol = rootSymbolName;
            _ruleMap = productions.ToDictionary(
                production => production.Symbol,
                production => production.Rule);
        }

        public IParser CreateParser() => CreateParser(Root);

        private IParser CreateParser(Production production)
        {
            return production.Rule switch
            {
                LiteralRule literal => new LiteralParser(production.Symbol, literal),
                PatternRule pattern => new PatternMatcherParser(production.Symbol, pattern),
                SymbolExpressionRule expressionRule => new ExpressionParser(expressionRule.Value),
                _ => throw new ArgumentException("Invalid rule type: {production.Rule.GetType()}")
            };
        }

        private IRecognizer CreateRecognizer(ISymbolExpression expression)
        {
            return expression switch
            {
                SymbolRef @ref => new ParserRecognizer(
                    CreateParser(
                        new(@ref.ProductionSymbol, this[@ref.ProductionSymbol]))),
                SymbolGroup group => group.Expressions
                    .Select(CreateRecognizer)
                    .Map(recogniers => group.Mode switch
                    {
                        SymbolGroup.GroupingMode.Choice => new ChoiceRegcognizer(group.Cardinality, recogniers.ToArray()),
                        SymbolGroup.GroupingMode.Sequence => new SequenceRecognizer(group.Cardinality, recogniers.ToArray()),
                        SymbolGroup.GroupingMode.Set => new SetRecognizer(group.Cardinality, recogniers.ToArray),
                        _ => throw new ArgumentException($"Invalid {typeof(SymbolGroup.GroupingMode)}: {group.Mode}")
                    }),
                _ => throw new ArgumentException("Invalid expression")
            };
        }
    }

    /// <summary>
    /// Builder for creating new <see cref="Grammar"/> instances
    /// </summary>
    public class GrammarBuilder
    {
        private readonly Dictionary<string, IRule> productions = new();

        private string _rootSymbol;

        public static GrammarBuilder NewBuilder() => new();

        /// <summary>
        /// Adds the root production. This method can be called only once, subsequent calls throw <see cref="ArgumentException"/>.
        /// <para>
        /// Note: This method should be called before adding productions with the <see cref="WithRootProduction(string, IRule)"/> method.
        /// </para>
        /// </summary>
        /// <param name="rootSymbol">The root symbol name</param>
        /// <param name="rule">The rule for the root production</param>
        public GrammarBuilder WithRootProduction(string rootSymbol, IRule rule)
        {
            if (string.IsNullOrWhiteSpace(rootSymbol) || rule == null)
                throw new ArgumentException("Invalid production specified");

            if (!string.IsNullOrEmpty(_rootSymbol))
                throw new ArgumentException($"Root Production already exists: {_rootSymbol}");

            productions[rootSymbol] = rule;

            return this;
        }

        /// <summary>
        /// Adds subsequent productions for this grammer
        /// </summary>
        /// <param name="symbol">The symbol name</param>
        /// <param name="rule">The production rule</param>
        /// <param name="overwriteRule">Indicate what happens if symbolName-collision happens</param>
        public GrammarBuilder WithProduction(string symbol, IRule rule, bool overwriteRule = false)
        {
            if (string.IsNullOrWhiteSpace(symbol) || rule == null)
                throw new ArgumentException("Invalid production specified");

            if (string.IsNullOrWhiteSpace(_rootSymbol))
                throw new InvalidOperationException("A root production must be specified before adding other Productions");

            if (overwriteRule)
                productions[symbol] = rule;

            else if (!productions.TryAdd(symbol, rule))
                throw new ArgumentException("Rules overwriting is not allowed for this call");

            return this;
        }

        /// <summary>
        /// Validates and builds a grammar from the encapsulated productions.
        /// </summary>
        public Grammar Build()
        {
            ValidateGrammar();
            return new Grammar(
                _rootSymbol,
                productions
                    .Select(kvp => new Production(kvp.Key, kvp.Value))
                    .ToArray());
        }

        /// <summary>
        /// Validate the grammar. A valid grammar is one that:
        /// <list type="number">
        ///     <item>Has no unreferenced production. An unreferenced production is one that cannot be traced back to the root</item>
        ///     <item>Has no orphaned symbol-references. An orphaned symbol-reference is one that refers to a non-existent production</item>
        ///     <item>Has terminals that are traceable to the root</item>
        /// </list>
        /// </summary>
        private void ValidateGrammar()
        {
            var grammarSymbols = new HashSet<string>(productions.Keys);
            var ruleSymbolReferences = productions.Values
                .Aggregate(
                    Enumerable.Empty<string>(),
                    (symbols, rule) => symbols.Concat(GetReferencedSymbols(rule)))
                .Map(symbols => new HashSet<string>(symbols));

            //unreferenced productions
            var unreferencedProductions = grammarSymbols
                .Where(symbol => !_rootSymbol.Equals(symbol))
                .Where(symbol => !ruleSymbolReferences.Contains(symbol))
                .ToArray();

            //orphaned symbols
            var orphanedSymbols = ruleSymbolReferences
                .Where(symbol => !grammarSymbols.Contains(symbol))
                .ToArray();

            //has non-terminal
            var hasTerminal = productions.Any(production => production.Value is ITerminal);

            if (unreferencedProductions.Length > 0 || orphanedSymbols.Length > 0 || !hasTerminal)
                throw new GrammarValidatoinException(
                    unreferencedProductions,
                    orphanedSymbols,
                    !hasTerminal);
        }

        private IEnumerable<string> GetReferencedSymbols(IRule rule)
        {
            return rule switch
            {
                ITerminal terminal => Enumerable.Empty<string>(),
                SymbolExpressionRule groupingRule => groupingRule.Value switch
                {
                    SymbolRef sr => new[] { sr.ProductionSymbol },
                    SymbolGroup sg => sg.SymbolRefs.Select(@ref => @ref.ProductionSymbol),
                    _ => throw new Exception($"Invalid SymbolExpression type: {groupingRule.GetType()}")
                },
                _ => throw new Exception($"Invalid rule type: {rule?.GetType()}")
            };
        }
    }
}
