using Axis.Pulsar.Parser.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Grammar
{
    /// <summary>
    /// Builder for creating new <see cref="Grammar"/> instances
    /// </summary>
    public class GrammarBuilder
    {
        private readonly Dictionary<string, IRule> productions = new();

        private string _rootSymbol;

        public bool HasRoot => !string.IsNullOrEmpty(_rootSymbol);

        public static GrammarBuilder NewBuilder() => new();

        /// <summary>
        /// Adds the root production. This method can be called only once, subsequent calls throw <see cref="ArgumentException"/>.
        /// <para>
        /// Note: This method should be called before adding productions with the <see cref="WithRootProduction(string, IRule)"/> method.
        /// </para>
        /// </summary>
        /// <param name="rootSymbol">The root symbol name</param>
        /// <param name="rule">The rule for the root production</param>
        public GrammarBuilder WithRootProduction(string rootSymbol, IRule rule) => WithRootProduction(new Production(rootSymbol, rule));

        /// <summary>
        /// Adds the root production. This method can be called only once, subsequent calls throw <see cref="ArgumentException"/>.
        /// <para>
        /// Note: This method should be called before adding productions with the <see cref="WithRootProduction(string, IRule)"/> method.
        /// </summary>
        /// <param name="production">The production</param>
        public GrammarBuilder WithRootProduction(Production production)
        {
            if (!string.IsNullOrEmpty(_rootSymbol))
                throw new ArgumentException($"Root Production already exists: {_rootSymbol}");

            _rootSymbol = production.Symbol;
            productions[production.Symbol] = production.Rule;

            return this;
        }

        /// <summary>
        /// Adds subsequent productions for this grammer
        /// </summary>
        /// <param name="symbol">The symbol name</param>
        /// <param name="rule">The production rule</param>
        /// <param name="overwriteRule">Indicate what happens if symbolName-collision happens</param>
        public GrammarBuilder WithProduction(string symbol, IRule rule, bool overwriteRule = false) => WithProduction(new Production(symbol, rule), overwriteRule);


        public GrammarBuilder WithProductions(params Production[] productions)
            => productions.Aggregate(this, (builder, production) => builder.WithProduction(production));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="production"></param>
        /// <param name="overwriteRule"></param>
        /// <returns></returns>
        public GrammarBuilder WithProduction(Production production, bool overwriteRule = false)
        {
            if (string.IsNullOrWhiteSpace(_rootSymbol))
                throw new InvalidOperationException("A root production must be specified before adding other Productions");

            if (overwriteRule)
                productions[production.Symbol] = production.Rule;

            else if (!productions.TryAdd(production.Symbol, production.Rule))
                throw new ArgumentException("Rule overwriting is not allowed for this call");

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
