using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.Recognizers;
using System;
using System.Linq;

namespace Axis.Pulsar.Grammar.Language.Rules
{
    /// <summary>
    /// Represents an ordered list of choices of expressions, only one of which MUST be recognied successfully.
    /// </summary>
    public struct Choice : IRuleExpression
    {
        private readonly IRule[] _rules;

        /// <inheritdoc/>/>
        public Cardinality Cardinality { get; }

        /// <inheritdoc/>/>
        public IRule[] Rules => _rules?.ToArray();

        /// <inheritdoc/>/>
        public string SymbolName => "@Choice";

        public Choice(
            params IRule[] rules)
            : this(Cardinality.OccursOnlyOnce(), rules)
        { }

        public Choice(
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
            return obj is Choice other
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

            return $"?{this.AsRuleExpressionString()}";
        }

        /// <inheritdoc/>
        public IRecognizer ToRecognizer(Grammar grammar) => new ChoiceRecognizer(this, grammar);

        public static bool operator ==(Choice first, Choice second) => first.Equals(second);
        public static bool operator !=(Choice first, Choice second) => !(first == second);
    }
}
