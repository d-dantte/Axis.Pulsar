using Axis.Pulsar.Grammar.Recognizers;
using System;
using System.Collections.Generic;

namespace Axis.Pulsar.Grammar.Language.Rules
{
    /// <summary>
    /// A wrapper that encapsulates the actual rule for this production
    /// </summary>
    public struct ProductionRule: ICompositeRule
    {
        /// <summary>
        /// Optional validator for validating all recognized symbols
        /// </summary>
        public IProductionValidator Validator { get; }

        /// <summary>
        /// The encapsulated production
        /// </summary>
        public IRule Rule { get; }

        /// <inheritdoc/>/>
        public string SymbolName { get; }


        /// <summary>
        /// <para>
        /// Represents the least number of symbols that need to match for this Rule to be UNIQUELY identified.
        /// In other words, if this threshold number is reached, and recognition still fails, any recognizer higher up
        /// in the call stack that would find alternatives (e.g, <see cref="Language.Rules.Choice"/>) would fail, rather
        /// than seek an alternative.
        /// </para>
        /// Note:
        /// <list type="number">
        /// <item>Values &lt;= 0 are considered invalid.</item>
        /// <item>If the value is null, then no threshold is assumed.</item>
        /// </list>
        /// <para>
        /// Note: 
        /// </para>
        /// </summary>
        public int? RecognitionThreshold { get; }

        public ProductionRule(
            string symbol,
            IRule rule,
            IProductionValidator validator = null)
            : this(symbol, null, rule, validator)
        { }

        public ProductionRule(
            string symbolName,
            int? recognitionThreshold,
            IRule rule,
            IProductionValidator validator = null)
        {
            Validator = validator;

            RecognitionThreshold = recognitionThreshold.ThrowIf(
                v => v < 1,
                new ArgumentException($"Invalid {nameof(recognitionThreshold)}: {recognitionThreshold}"));

            SymbolName = symbolName.ThrowIfNot(
                SymbolHelper.IsValidSymbolName,
                new ArgumentException($"Invalid symbol name: {symbolName}"));

            Rule = rule
                .ThrowIfNull(new ArgumentNullException(nameof(rule)))
                .ThrowIf(
                    Extensions.Is<ProductionRule>,
                    new ArgumentException($"Cannot encapsulate a {typeof(ProductionRule).FullName}"));
        }

        public override string ToString()
        {
            var rule = Rule.ToString();

            var threshold = RecognitionThreshold > 0
                ? $">{RecognitionThreshold}"
                : "";

            return $"{rule}{threshold}";
        }

        /// <inheritdoc/>
        public IRecognizer ToRecognizer(Grammar grammar) => new ProductionRuleRecognizer(this, grammar);

        public override int GetHashCode() => HashCode.Combine(Rule, SymbolName, RecognitionThreshold, Validator);

        public override bool Equals(object obj)
        {
            return obj is ProductionRule other
                && other.SymbolName == SymbolName
                && other.RecognitionThreshold == RecognitionThreshold
                && EqualityComparer<IRule>.Default.Equals(other.Rule, Rule)
                && EqualityComparer<IProductionValidator>.Default.Equals(other.Validator, Validator);
        }

        public static bool operator ==(ProductionRule first, ProductionRule second) => first.Equals(second);
        public static bool operator !=(ProductionRule first, ProductionRule second) => !(first == second);
    }
}
