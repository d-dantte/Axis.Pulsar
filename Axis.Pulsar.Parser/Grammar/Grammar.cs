using Axis.Pulsar.Parser.Parsers;
using Axis.Pulsar.Parser.Recognizers;
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
        private readonly Dictionary<string, IParser> _parsers;

        #region Properties
        public string RootSymbol { get; }

        public IEnumerable<Production> Productions => _ruleMap.Select(kvp => new Production(kvp.Key, kvp.Value));

        public IEnumerable<IParser> Parsers => _parsers.Values.ToArray();
        #endregion

        internal Grammar(string rootSymbolName, params Production[] productions)
        {
            RootSymbol = rootSymbolName;

            _ruleMap = productions
                .ThrowIf(
                    Extensions.IsNullOrEmpty,
                    _ => new ArgumentException("Invalid production array"))
                .ToDictionary(
                    production => production.Symbol,
                    production => production.Rule);

            _parsers = productions
                .Select(CreateParser)
                .ToDictionary(
                    parser => parser.SymbolName,
                    parser => parser);
        }

        public IParser RootParser() => _parsers[RootSymbol];

        public IParser GetParser(string symbolName)
        {
            return _parsers.TryGetValue(symbolName, out var parser)
                ? parser
                : null;
        }

        public Production RootProduction() => new(RootSymbol, _ruleMap[RootSymbol]);

        public Production? GetProduction(string symbolName)
        {
            return _ruleMap.TryGetValue(symbolName, out var rule)
                ? new(symbolName, rule)
                : null;
        }

        /// <summary>
        /// Note: possible optimization is to cache parser objects and reference them in the parser-tree structure.
        /// </summary>
        /// <param name="production"></param>
        /// <returns></returns>
        internal IParser CreateParser(Production production)
        {
            return production.Rule switch
            {
                LiteralRule literal => new LiteralParser(production.Symbol, literal),
                PatternRule pattern => new PatternMatcherParser(production.Symbol, pattern),
                SymbolExpressionRule expressionRule => (IParser) new ExpressionParser(
                    production.Symbol,
                    CreateRecognizer(expressionRule.Value)),
                _ => throw new ArgumentException("Invalid rule type: {production.Rule.GetType()}")
            };
        }

        internal IRecognizer CreateRecognizer(ISymbolExpression expression)
        {
            return expression switch
            {
                SymbolRef @ref => new SymbolRefRecognizer(
                    @ref.ProductionSymbol,
                    @ref.Cardinality,
                    this),

                SymbolGroup group => group.Expressions
                    .Select(CreateRecognizer)
                    .Map(recogniers => group.Mode switch
                    {
                        SymbolGroup.GroupingMode.Choice => new ChoiceRecognizer(group.Cardinality, recogniers.ToArray()),
                        SymbolGroup.GroupingMode.Sequence => new SequenceRecognizer(group.Cardinality, recogniers.ToArray()),
                        SymbolGroup.GroupingMode.Set => (IRecognizer) new SetRecognizer(group.Cardinality, recogniers.ToArray()),
                        _ => throw new ArgumentException($"Invalid {typeof(SymbolGroup.GroupingMode)}: {group.Mode}")
                    }),

                _ => throw new ArgumentException("Invalid expression")
            };
        }
    }
}
