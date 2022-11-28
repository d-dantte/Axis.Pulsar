using Axis.Pulsar.Parser.Exceptions;
using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Parsers;
using Axis.Pulsar.Parser.Recognizers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Builders
{
    /// <summary>
    /// Builder for creating new <see cref="Grammar"/> instances
    /// </summary>
    public class GrammarBuilder
    {
        private readonly Dictionary<string, IRule> productions = new();

        private string _rootSymbol;

        /// <summary>
        /// Indicates if the root symbol has been set.
        /// </summary>
        public bool HasRoot => !string.IsNullOrEmpty(_rootSymbol);

        /// <summary>
        /// Creates a new instance of the <see cref="GrammarBuilder"/> class.
        /// </summary>
        public static GrammarBuilder NewBuilder() => new();

        /// <summary>
        /// Adds subsequent productions for this grammer.
        /// </summary>
        /// <param name="symbol">The symbol name</param>
        /// <param name="rule">The production rule</param>
        /// <param name="overwriteDuplicate">Indicate what happens if symbolName-collision happens</param>
        /// <returns>This builder instance</returns>
        public GrammarBuilder WithProduction(
            string symbol,
            IRule rule,
            bool overwriteDuplicate = false) 
            => WithProduction(new Production(symbol, rule), overwriteDuplicate);

        /// <summary>
        /// Adds a list of productions to this builder. Collissions (duplicates) throw <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="productions">An array of production instances</param>
        /// <returns>This builder instance</returns>
        public GrammarBuilder WithProductions(params Production[] productions)
            => productions.Aggregate(this, (builder, production) => builder.WithProduction(production));

        /// <summary>
        /// Adds a single production to the builder.
        /// </summary>
        /// <param name="production">The production</param>
        /// <param name="overwriteDuplicate">Indicates if duplicates should be overwritten, or exceptions should be thrown</param>
        /// <returns>This builder instance</returns>
        /// <exception cref="ArgumentException"></exception>
        public GrammarBuilder WithProduction(Production production, bool overwriteDuplicate = false)
        {
            if (overwriteDuplicate)
                productions[production.Symbol] = production.Rule;

            else if (!productions.TryAdd(production.Symbol, production.Rule))
                throw new ArgumentException("Rule overwriting is not allowed for this call");

            return this;
        }

        public GrammarBuilder WithProduction(
            string productionName,
            Action<ProductionBuilder> productionBuilder)
            => new ProductionBuilder(productionName)
                .Use(productionBuilder.Invoke)
                .Build()
                .Map(production => WithProduction(production));

        /// <summary>
        /// Updates the root symbol for the grammar being built. The root symbol must already exist in the internal list of productions for this method to be successful.
        /// </summary>
        /// <param name="rootSymbol"></param>
        /// <returns>This builder instance</returns>
        /// <exception cref="ArgumentException"></exception>
        public GrammarBuilder WithRoot(string rootSymbol)
        {
            if (!productions.ContainsKey(rootSymbol))
                throw new ArgumentException($"{nameof(rootSymbol)}: {rootSymbol} does not exist in the list of productions");

            _rootSymbol = rootSymbol;
            return this;
        }

        /// <summary>
        /// Validates and builds a grammar from the encapsulated productions.
        /// </summary>
        public IGrammar Build()
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
            var hasTerminal = productions.Any(production => production.Value switch
            {
                LiteralRule => true,
                PatternRule => true,
                _ => false
            });

            if (unreferencedProductions.Length > 0 || orphanedSymbols.Length > 0 || !hasTerminal)
                throw new GrammarValidationException(
                    unreferencedProductions,
                    orphanedSymbols,
                    !hasTerminal,
                    productions);
        }

        private IEnumerable<string> GetReferencedSymbols(IRule rule)
        {
            return rule switch
            {
                LiteralRule => Enumerable.Empty<string>(),
                PatternRule => Enumerable.Empty<string>(),
                SymbolExpressionRule groupingRule => groupingRule.Value switch
                {
                    ProductionRef sr => new[] { sr.ProductionSymbol },
                    SymbolGroup sg => sg.SymbolRefs.Select(@ref => @ref.ProductionSymbol),
                    _ => throw new Exception($"Invalid SymbolExpression type: {groupingRule.GetType()}")
                },
                _ => throw new Exception($"Invalid rule type: {rule?.GetType()}")
            };
        }


        /// <summary>
        /// Default implementation for the <see cref="IGrammar"/> interface.
        /// </summary>
        public class Grammar : IGrammar
        {
            private readonly Dictionary<string, IRule> _ruleMap;
            private readonly Dictionary<string, IParser> _parsers;

            #region Properties
            /// <inheritdoc/>
            public string RootSymbol { get; }

            /// <inheritdoc/>
            public IEnumerable<Production> Productions => _ruleMap.Select(kvp => new Production(kvp.Key, kvp.Value));

            /// <inheritdoc/>
            public IEnumerable<IParser> Parsers => _parsers.Values.ToArray();
            #endregion

            internal Grammar(string rootSymbolName, params Production[] productions)
            {
                RootSymbol = rootSymbolName;

                _ruleMap = productions
                    .ThrowIf(
                        Extensions.IsNullOrEmpty,
                        new ArgumentException("Invalid production array"))
                    .ToDictionary(
                        production => production.Symbol,
                        production => production.Rule);

                _parsers = productions
                    .Select(CreateParser)
                    .ToDictionary(
                        parser => parser.SymbolName,
                        parser => parser);
            }

            /// <inheritdoc/>
            public IParser RootParser() => _parsers[RootSymbol];

            /// <inheritdoc/>
            public IParser GetParser(string symbolName)
            {
                return _parsers.TryGetValue(symbolName, out var parser)
                    ? parser
                    : throw new SymbolNotFoundException(symbolName);
            }

            /// <inheritdoc/>
            public Production RootProduction() => new(RootSymbol, _ruleMap[RootSymbol]);

            /// <inheritdoc/>
            public Production GetProduction(string symbolName)
            {
                return _ruleMap.TryGetValue(symbolName, out var rule)
                    ? new(symbolName, rule)
                    : throw new SymbolNotFoundException(symbolName);
            }

            /// <inheritdoc/>
            public bool HasProduction(string symbolName) => _ruleMap.ContainsKey(symbolName);

            internal IParser CreateParser(Production production)
            {
                return production.Rule switch
                {
                    LiteralRule literal => new LiteralParser(production.Symbol, literal),
                    PatternRule pattern => new PatternMatcherParser(production.Symbol, pattern),
                    SymbolExpressionRule expression => new ExpressionParser(
                        production.Symbol,
                        expression,
                        CreateRecognizer(expression.Value)),
                    _ => throw new ArgumentException("Invalid rule type: {production.Rule.GetType()}")
                };
            }

            internal IRecognizer CreateRecognizer(ISymbolExpression expression)
            {
                return expression switch
                {
                    EOF => new EOFRecognizer(),

                    ProductionRef @ref => new ProductionRefRecognizer(
                        @ref,
                        this),

                    SymbolGroup group => group.Expressions
                        .Select(CreateRecognizer)
                        .Map(recogniers => group switch
                        {
                            SymbolGroup.Choice choice => new ChoiceRecognizer(
                                choice,
                                recogniers.ToArray()),

                            SymbolGroup.Sequence sequence => new SequenceRecognizer(
                                sequence,
                                recogniers.ToArray()),

                            SymbolGroup.Set set => (IRecognizer)new SetRecognizer(
                                set,
                                recogniers.ToArray()),

                            _ => throw new ArgumentException($"Invalid {typeof(SymbolGroup.GroupingMode)}: {group.Mode}")
                        }),

                    _ => throw new ArgumentException("Invalid expression")
                };
            }
        }
    }
}
