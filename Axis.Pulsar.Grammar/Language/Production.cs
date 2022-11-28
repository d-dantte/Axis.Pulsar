using Axis.Pulsar.Grammar.Language.Rules;
using System;
using System.Collections.Generic;

namespace Axis.Pulsar.Grammar.Language
{
    /// <summary>
    /// The mapping of <c>symbol-name</c> to <c>rule</c>, is a production
    /// </summary>
    public readonly struct Production
    {
        /// <summary>
        /// The symbol name
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// The rule for this production
        /// </summary>
        public ProductionRule Rule { get; }

        public Production(string symbolName, ProductionRule productionRule)
        {
            Symbol = symbolName.ThrowIfNot(
                SymbolHelper.IsValidSymbolName,
                new ArgumentException($"Invalid symbol name: {symbolName}"));

            Rule = productionRule.ThrowIfDefault(
                new ArgumentNullException(nameof(productionRule)));
        }

        public override int GetHashCode() => HashCode.Combine(Symbol, Rule);

        public override bool Equals(object obj)
        {
            return obj is Production other
                && other.Rule.Equals(Rule)
                && EqualityComparer<string>.Default.Equals(other.Symbol, Symbol);
        }

        public override string ToString()
        {
            if (Symbol is null)
                return null;

            return $"{Symbol} -> {Rule}";
        }

        public static bool operator ==(Production first, Production second) => first.Equals(second);

        public static bool operator !=(Production first, Production second) => !(first == second);
    }
}
