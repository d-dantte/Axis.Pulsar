using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;

namespace Axis.Pulsar.Parser.XLanguage
{
    public abstract class Rule
    {
        public Cardinality Cardinality { get; }

        public Rule(Cardinality cardinality = default)
        {
            Cardinality = cardinality == default
                ? Cardinality.OccursOnlyOnce()
                : cardinality;
        }
    }

    public class TerminalRule: Rule
    {
        public RuleRef Symbol { get; }

        public TerminalRule(Cardinality cardinality, RuleRef symbol)
            :base(cardinality)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        }

        public TerminalRule(RuleRef symbol)
            : this(default, symbol)
        { }
    }

    public class NonTerminalRule: Rule
    {
        private readonly List<Rule> _rules = new();

        public Rule[] Rules => _rules.ToArray();

        public GroupingMode GroupingMode { get; }

        public NonTerminalRule(Cardinality cardinality, params Rule[] rules)
            :base(cardinality)
        {
            if (rules == null || rules.Length == 0)
                throw new ArgumentException($"Invalid {rules} array");

            _rules.AddRange(rules);
        }

        public NonTerminalRule(params Rule[] rules)
            : this(default, rules)
        { }
    }

    public enum GroupingMode
    {
        Choice,
        Set,
        Sequence
    }
}
