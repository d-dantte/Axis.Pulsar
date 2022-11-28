using Axis.Pulsar.Grammar.Recognizers;
using System;
using System.Linq;

namespace Axis.Pulsar.Grammar.Language.Rules
{
    /// <summary>
    /// Represents an ordered list of expressions, each of which MUST be recognized successfully.
    /// </summary>
    public struct Sequence : IRuleExpression
    {
        private readonly IRule[] _rules;

        /// <inheritdoc/>/>
        public Cardinality Cardinality { get; }

        /// <inheritdoc/>/>
        public IRule[] Rules => _rules.ToArray();

        /// <inheritdoc/>/>
        public string SymbolName => "@Sequence";

        public Sequence(
            params IRule[] rules)
            : this(Cardinality.OccursOnlyOnce(), rules)
        { }

        public Sequence(
            Cardinality cardinality,
            params IRule[] rules)
        {
            Cardinality = cardinality;

            _rules = rules
                .ThrowIfNull(new ArgumentNullException(nameof(rules)))
                .ThrowIf(Extensions.IsEmpty, new ArgumentException($"{nameof(rules)} is empty"))
                .WithEach(r => r.ThrowIfNull(new ArgumentException("Cannot contain null rules")))
                .WithEach(r => r.ThrowIf(
                    Extensions.Is<ProductionRule>,
                    new ArgumentException($"Cannot contain {typeof(ProductionRule).FullName} rules")))
                .WithEach(r => r.ThrowIfNot(
                    Extensions.IsTerminal,
                    new ArgumentException($"Cannot contain non-terminating rules")))
                .ToArray();
        }

        public override bool Equals(object obj)
        {
            return obj is Sequence other
                && other.Cardinality == Cardinality
                && other._rules.NullOrTrue(_rules, Enumerable.SequenceEqual);
        }

        public override int GetHashCode()
        {
            return _rules.Aggregate(
                HashCode.Combine(Cardinality),
                (code, expression) => HashCode.Combine(code, expression));
        }

        public override string ToString()
        {
            if (_rules is null)
                return null;

            return $"+{this.AsRuleExpressionString()}";
        }

        /// <inheritdoc/>
        public IRecognizer ToRecognizer(Grammar grammar) => new SequenceRecognizer(this, grammar);

        public static bool operator ==(Sequence first, Sequence second) => first.Equals(second);
        public static bool operator !=(Sequence first, Sequence second) => !(first == second);
    }
}
