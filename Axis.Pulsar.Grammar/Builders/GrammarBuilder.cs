using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.Exceptions;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Grammar.Builders
{
    /// <summary>
    /// Builder for creating new <see cref="Grammar"/> instances
    /// </summary>
    public class GrammarBuilder : AbstractBuiler<Language.Grammar>
    {
        private readonly Language.Grammar _grammar = new();

        /// <summary>
        /// Indicates if the root symbol has been set.
        /// </summary>
        public bool HasRoot => !string.IsNullOrEmpty(_grammar.RootSymbol);

        /// <summary>
        /// Creates a new instance of the <see cref="GrammarBuilder"/> class.
        /// </summary>
        public static GrammarBuilder NewBuilder() => new();

        /// <summary>
        /// 
        /// </summary>
        internal Language.Grammar Grammar => _grammar;

        /// <summary>
        /// Adds subsequent productions for this grammer.
        /// </summary>
        /// <param name="rule">The production rule</param>
        /// <param name="overwriteDuplicate">Indicate what happens if symbolName-collision happens</param>
        /// <returns>This builder instance</returns>
        public GrammarBuilder HavingProduction(
            ProductionRule rule,
            bool overwriteDuplicate = false)
            => HavingProduction(new Production(rule), overwriteDuplicate);

        /// <summary>
        /// Adds a list of productions to this builder. Collissions (duplicates) throw <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="productions">An array of production instances</param>
        /// <returns>This builder instance</returns>
        public GrammarBuilder HavingProductions(params Production[] productions)
            => productions.Aggregate(this, (builder, production) => builder.HavingProduction(production));

        /// <summary>
        /// Adds a single production to the builder.
        /// </summary>
        /// <param name="production">The production</param>
        /// <param name="overwriteDuplicate">Indicates if duplicates should be overwritten, or exceptions should be thrown</param>
        /// <returns>This builder instance</returns>
        /// <exception cref="ArgumentException"></exception>
        public GrammarBuilder HavingProduction(Production production, bool overwriteDuplicate = false)
        {
            AssertNotBuilt();

            if (production == default)
                throw new ArgumentException($"Invalid {nameof(production)}");

            var appender = _grammar as IProductionAppender;

            if (overwriteDuplicate)
                appender.AddProduction(production);

            else if (!appender.TryAddProduction(production))
                throw new ArgumentException("Rule overwriting is not allowed for this call");

            return this;
        }

        public GrammarBuilder HavingProduction(
            Action<ProductionBuilder> productionBuilderAction)
            => ProductionBuilder
                .NewBuilder()
                .With(productionBuilderAction.Invoke)
                .Build()
                .Map(production => HavingProduction(production));

        /// <summary>
        /// Updates the root symbol for the grammar being built. The root symbol must already exist in the internal list of productions for this method to be successful.
        /// </summary>
        /// <param name="rootSymbol"></param>
        /// <returns>This builder instance</returns>
        /// <exception cref="ArgumentException"></exception>
        public GrammarBuilder WithRoot(string rootSymbol)
        {
            AssertNotBuilt();

            if (!_grammar.HasProduction(rootSymbol))
                throw new ArgumentException($"{nameof(rootSymbol)}: {rootSymbol} does not exist in the list of productions");

            _grammar.RootSymbol = rootSymbol;
            return this;
        }

        /// <summary>
        /// Validates and builds a grammar from the encapsulated productions.
        /// </summary>
        protected override Language.Grammar BuildTarget() => _grammar;

        /// <summary>
        /// Validate the grammar. A valid grammar is one that:
        /// <list type="number">
        ///     <item>Has no unreferenced production. An unreferenced production is one that cannot be traced back to the root</item>
        ///     <item>Has no orphaned symbol-references. An orphaned symbol-reference is one that refers to a non-existent production</item>
        ///     <item>All symbols terminate in terminals - this doesn't check for possible infinite recursions</item>
        /// </list>
        /// </summary>
        protected override void ValidateTarget()
        {
            var grammarSymbols = new HashSet<string>(_grammar.Symbols);
            var ruleSymbolReferences = _grammar.Productions
                .Aggregate(
                    Enumerable.Empty<string>(),
                    (symbols, production) => symbols.Concat(GetReferencedSymbols(production.Rule)))
                .Select(symbolRef => SymbolHelper.SymbolRefPattern.Match(symbolRef))
                .Select(match => match.Groups["symbol"].Value)
                .Map(symbols => new HashSet<string>(symbols));

            // unreferenced productions - production symbols that are not referenced (except the root symbol)
            var unreferencedProductions = grammarSymbols
                .Where(symbol => !_grammar.RootSymbol.Equals(symbol))
                .Where(symbol => !ruleSymbolReferences.Contains(symbol))
                .ToArray();

            // orphaned symbols - referenced symbols that have no production
            var orphanedSymbols = ruleSymbolReferences
                .Where(symbol => !grammarSymbols.Contains(symbol))
                .ToArray();

            // ensure that all symbol-refs terminate in terminal productions
            var nonTerminatingProductions = this.ResolveSymbols();

            if (unreferencedProductions.Length > 0
                || orphanedSymbols.Length > 0
                || nonTerminatingProductions.Length > 0)
                throw new GrammarValidationException(
                    unreferencedProductions,
                    orphanedSymbols,
                    nonTerminatingProductions,
                    _grammar.Productions);
        }

        private IEnumerable<string> GetReferencedSymbols(IRule rule)
        {
            return rule switch
            {
                ProductionRef @ref => new[] { @ref.SymbolName },
                IAtomicRule => Enumerable.Empty<string>(),
                ICompositeRule composite => GetReferencedSymbols(composite.Rule),
                IAggregateRule aggregateRule => aggregateRule.Rules.SelectMany(GetReferencedSymbols),
                _ => throw new Exception($"Invalid rule type: {rule?.GetType()}")
            };
        }

        private string[] ResolveSymbols()
        {
            var ruleSymbols = _grammar.Productions
                .Select(production => (production.Symbol, Refs: ResolveRule(production.Rule)))
                .ToDictionary(
                    tuple => tuple.Symbol,
                    tuple => tuple.Refs.Distinct().ToArray());

            while (ResolveRefs(ruleSymbols)) ;

            return ruleSymbols
                .Where(kvp => kvp.Value.Length > 0)
                .Select(kvp => kvp.Key)
                .ToArray();
        }

        private bool ResolveRefs(Dictionary<string, string[]> ruleSymbols)
        {
            var found = false;
            var visited = new HashSet<string>();
            ruleSymbols.Keys
                .Where(key => ruleSymbols[key].Length > 0)
                .ForAll(symbol =>
                {
                    var transformedRefs = ruleSymbols[symbol]
                        .Where(s => ruleSymbols[s].Length > 0)
                        .Where(s => !visited.Contains(s))
                        .ToArray();

                    if (transformedRefs.Length < ruleSymbols[symbol].Length)
                    {
                        found = true;
                        ruleSymbols[symbol] = transformedRefs;
                    }

                    visited.Add(symbol);
                });

            return found;
        }


        private string[] ResolveRule(IRule rule)
        {
            return rule switch
            {
                ProductionRef @ref => new[] { @ref.ProductionSymbol },
                ProductionRule productionRule => ResolveRule(productionRule.Rule),
                IAtomicRule => Array.Empty<string>(),
                IAggregateRule aggregate => aggregate.Rules
                    .SelectMany(ResolveRule)
                    .ToArray(),

                _ => throw new InvalidOperationException($"Invalid rule@ {rule}")
            };
        }
    }
}
