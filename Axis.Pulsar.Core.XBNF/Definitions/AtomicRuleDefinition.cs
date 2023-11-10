using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;

namespace Axis.Pulsar.Core.XBNF.Definitions
{
    /// <summary>
    /// 
    /// </summary>
    public class AtomicRuleDefinition
    {
        private static readonly HashSet<AtomicContentDelimiterType> _contentTypes = Enum
            .GetValues<AtomicContentDelimiterType>()
            .ApplyTo(v => new HashSet<AtomicContentDelimiterType>(v));

        public string Symbol { get; }

        public IAtomicRuleFactory Factory { get; }

        public AtomicContentDelimiterType ContentDelimiterType { get; }

        public AtomicRuleDefinition(
            string symbol,
            AtomicContentDelimiterType contentDelimiterType,
            IAtomicRuleFactory factory)
        {
            Factory = factory.ThrowIfNull(new ArgumentNullException(nameof(factory)));
            ContentDelimiterType = contentDelimiterType.ThrowIfNot(
                _contentTypes.Contains,
                new ArgumentException($"Invalid content delimiter type: {contentDelimiterType}"));
            Symbol = symbol.ThrowIfNot(
                IProduction.SymbolPattern.IsMatch,
                new FormatException($"Invalid symbol format: '{symbol}'"));
        }

        public static AtomicRuleDefinition Of(
            string symbol,
            AtomicContentDelimiterType contentDelimiterType,
            IAtomicRuleFactory factory)
            => new(symbol, contentDelimiterType, factory);
    }
}
