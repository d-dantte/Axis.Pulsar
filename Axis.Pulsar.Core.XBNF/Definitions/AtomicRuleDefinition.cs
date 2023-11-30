using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;

namespace Axis.Pulsar.Core.XBNF.Definitions
{
    /// <summary>
    /// 
    /// </summary>
    public class AtomicRuleDefinition
    {
        public string Id { get; }

        public IAtomicRuleFactory Factory { get; }

        public AtomicContentDelimiterType ContentDelimiterType { get; }

        public AtomicRuleDefinition(
            string id,
            AtomicContentDelimiterType contentDelimiterType,
            IAtomicRuleFactory factory)
        {
            Factory = factory.ThrowIfNull(new ArgumentNullException(nameof(factory)));
            ContentDelimiterType = contentDelimiterType.ThrowIfNot(
                Enum.IsDefined,
                new ArgumentException($"Invalid content delimiter type: '{contentDelimiterType}' is undefined"));
            Id = id.ThrowIfNot(
                IProduction.SymbolPattern.IsMatch,
                new FormatException($"Invalid {nameof(id)} format: '{id}'"));
        }

        public static AtomicRuleDefinition Of(
            string symbol,
            AtomicContentDelimiterType contentDelimiterType,
            IAtomicRuleFactory factory)
            => new(symbol, contentDelimiterType, factory);

        public static AtomicRuleDefinition Of(
            string symbol,
            IAtomicRuleFactory factory)
            => new(symbol, AtomicContentDelimiterType.None, factory);

        public static AtomicRuleDefinition Of<TFactory>(
            string symbol,
            AtomicContentDelimiterType contentDelimiterType)
            where TFactory : IAtomicRuleFactory, new()
            => new(symbol, contentDelimiterType, new TFactory());

        public static AtomicRuleDefinition Of<TFactory>(
            string symbol)
            where TFactory : IAtomicRuleFactory, new()
            => new(symbol, AtomicContentDelimiterType.None, new TFactory());
    }
}
