using Axis.Pulsar.Parser.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Grammar
{
    public class RuleMap
    {
        private readonly Dictionary<string, Rule> _ruleMap = new();

        private string _rootSymbol;

        public string RootSymbol => _rootSymbol;

        public KeyValuePair<string, Rule> RootMap => new(_rootSymbol, _ruleMap[_rootSymbol]);

        public IEnumerable<KeyValuePair<string, Rule>> Rules() => _ruleMap;

        public RuleMap()
        { }

        public RuleMap(IEnumerable<KeyValuePair<string, Rule>>rules)
            :this(rules.First().Key, rules.ToArray())
        {
        }

        public RuleMap(string root, params KeyValuePair<string, Rule>[] rules)
        {
            if (rules == null || rules.Length == 0)
                throw new ArgumentException("Invalid rule list");

            root.ThrowIf(
                string.IsNullOrWhiteSpace,
                s => new ArgumentException("Invalid root name"));

            rules.ForAll(rule =>
            {
                AddRule(
                    rule.Key,
                    rule.Value,
                    rule.Key.Equals(root, StringComparison.InvariantCulture));
            });
        }


        public Rule this[string ruleName]
        {
            get
            {
                if (ruleName == null)
                    throw new ArgumentNullException(nameof(ruleName));

                else return _ruleMap[ruleName];
            }
        }

        public RuleMap AddRule(string symbol, Rule rule, bool isRoot)
        {
            if (isRoot && !string.IsNullOrEmpty(_rootSymbol))
                throw new ArgumentException("Map cannot have multiple roots. Current root: " + _rootSymbol);

            _ruleMap[symbol] = rule;

            if (isRoot)
                _rootSymbol = symbol;

            NormalizeRule(rule);

            return this;
        }

        public bool TryAddRule(string symbol, Rule rule, bool isRoot)
        {
            if (isRoot && !string.IsNullOrEmpty(_rootSymbol))
                throw new ArgumentException("Map cannot have multiple roots. Current root: " + _rootSymbol);

            if (_ruleMap.ContainsKey(symbol))
                return false;

            _ruleMap[symbol] = rule;

            if (isRoot)
                _rootSymbol = symbol;

            return true;
        }


        public RuleMap Validate()
        {
            var productions = new HashSet<string>(_ruleMap.Keys);
            var symbols = new HashSet<string>(_ruleMap.Values.Aggregate(
                Enumerable.Empty<string>(),
                (symbols, rule) => symbols.Concat(ExtractSymbolNames(rule))));

            //unreferenced productions
            var unreferencedProductions = productions
                .Where(p => !RootSymbol.Equals(p))
                .Where(p => !symbols.Contains(p))
                .ToArray();

            //orphaned symbols
            var orphanedSymbols = symbols
                .Where(s => !productions.Contains(s))
                .ToArray();

            if (unreferencedProductions.Length > 0 || orphanedSymbols.Length > 0)
                throw new RuleMapValidatoinException(
                    unreferencedProductions,
                    orphanedSymbols);

            return this;
        }

        /// <summary>
        /// Searches the rule tree for all Refs and adds this rule map as the rule map for them.
        /// </summary>
        /// <param name="rule"></param>
        private void NormalizeRule(Rule rule)
        {
            switch(rule)
            {
                case GroupingRule g: 
                    g.Rules.ForAll(NormalizeRule);
                    break;

                case RuleRef r:
                    r.SetRuleMap(this);
                    break;

                default:
                    break;
            }
        }

        private IEnumerable<string> ExtractSymbolNames(Rule rule)
        {
            var empty = Enumerable.Empty<string>();
            return rule switch
            {
                LiteralRule lr => empty,
                PatternRule pr => empty,
                RuleRef rr => new[] {rr.Symbol},
                GroupingRule gr => gr.Rules.Aggregate(empty, (symbols, rule) => symbols.Concat(ExtractSymbolNames(rule))),

                _ => throw new Exception($"Invalid rule: {rule?.GetType()}")
            };
        }
    }
}
