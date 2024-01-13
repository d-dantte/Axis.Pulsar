using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.Recognizers;
using System;
using System.Linq;

namespace Axis.Pulsar.Grammar.Language.Rules
{
    /// <summary>
    /// Represents an unordered set of expressions, each of which MUST be recognized successfully.
    /// <para>
    /// </para>
    /// </summary>
    public struct Set : IRuleExpression
    {
        private readonly IRule[] _rules;

        /// <inheritdoc/>/>
        public Cardinality Cardinality { get; }

        /// <inheritdoc/>/>
        public IRule[] Rules => _rules.ToArray();

        /// <inheritdoc/>/>
        public string SymbolName => "@Set";

        /// <summary>
        /// Minimum number of recognized items that can exist for this group to be deemed recognized.
        /// Default value is <see cref="Set.Rules.Length"/>.
        /// </summary>
        public int? MinRecognitionCount { get; }

        public Set(
            params IRule[] rules)
            : this(Cardinality.OccursOnlyOnce(), null, rules)
        { }

        public Set(
            Cardinality cardinality,
            params IRule[] rules)
            : this(cardinality, null, rules)
        { }

        public Set(
            Cardinality cardinality,
            int? minRecognitionCount = null,
            params IRule[] rules)
        {
            Cardinality = cardinality;
            MinRecognitionCount = minRecognitionCount;

            _rules = rules
                .ThrowIfNull(() => new ArgumentNullException(nameof(rules)))
                .ThrowIf(Extensions.IsEmpty, _ => new ArgumentException($"{nameof(rules)} is empty"))
                .WithEach(r => r.ThrowIfNull(() => new ArgumentException("Cannot contain null rules")))
                .WithEach(r => r.ThrowIf(
                    Extensions.Is<ProductionRule>,
                    _ => new ArgumentException($"Cannot contain {typeof(ProductionRule).FullName} rules")))
                .WithEach(r => r.ThrowIfNot(
                    Extensions.IsTerminal,
                    new ArgumentException($"Cannot contain non-terminating rules")))
                .ToArray();
        }

        public override bool Equals(object obj)
        {
            return obj is Set other
                && other.Cardinality == Cardinality
                && other.MinRecognitionCount == MinRecognitionCount
                && other._rules.NullOrTrue(_rules, Enumerable.SequenceEqual);
        }

        public override int GetHashCode()
        {
            return _rules.Aggregate(
                HashCode.Combine(Cardinality, MinRecognitionCount),
                (code, expression) => HashCode.Combine(code, expression));
        }

        public override string ToString()
        {
            if (_rules is null)
                return null;

            return $"#{MinRecognitionCount}{this.AsRuleExpressionString()}";
        }

        /// <inheritdoc/>
        public IRecognizer ToRecognizer(Grammar grammar) => new SetRecognizer(this, grammar);

        public static bool operator ==(Set first, Set second) => first.Equals(second);
        public static bool operator !=(Set first, Set second) => !(first == second);
    }
}
