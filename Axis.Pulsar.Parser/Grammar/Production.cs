using System;

namespace Axis.Pulsar.Parser.Grammar
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
        public IRule Rule { get; }

        public Production(string symbolName, IRule productionRule)
        {
            Symbol = symbolName.ThrowIf(
                string.IsNullOrWhiteSpace,
                _ => new ArgumentNullException(nameof(symbolName)));
            Rule = productionRule ?? throw new ArgumentNullException(nameof(productionRule));
        }

        public override int GetHashCode() => HashCode.Combine(Symbol, Rule);

        public override bool Equals(object obj)
        {
            return obj is Production p
                && Symbol.NullOrEquals(p.Symbol)
                && Rule.NullOrEquals(p.Rule);
        }

        public static bool operator ==(Production arg1, Production arg2) => arg1.Equals(arg2);

        public static bool operator !=(Production arg1, Production arg2) => !(arg1 == arg2);
    }
}
