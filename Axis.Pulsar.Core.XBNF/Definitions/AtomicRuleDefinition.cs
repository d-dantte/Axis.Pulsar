using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF.Definitions
{
    /// <summary>
    /// 
    /// </summary>
    public class AtomicRuleDefinition
    {
        public string Id { get; }

        public IAtomicRuleFactory Factory { get; }

        public ContentArgumentDelimiter ContentDelimiterType { get; }

        public AtomicRuleDefinition(
            string id,
            ContentArgumentDelimiter contentDelimiterType,
            IAtomicRuleFactory factory)
        {
            Factory = factory.ThrowIfNull(() => new ArgumentNullException(nameof(factory)));
            ContentDelimiterType = contentDelimiterType.ThrowIfNot(
                Enum.IsDefined,
                _ => new ArgumentException($"Invalid content delimiter type: '{contentDelimiterType}' is undefined"));
            Id = id.ThrowIfNot(
                IProduction.SymbolPattern.IsMatch,
                _ => new FormatException($"Invalid {nameof(id)} format: '{id}'"));
        }

        public static AtomicRuleDefinition Of(
            string symbol,
            ContentArgumentDelimiter contentDelimiterType,
            IAtomicRuleFactory factory)
            => new(symbol, contentDelimiterType, factory);

        public static AtomicRuleDefinition Of(
            string symbol,
            IAtomicRuleFactory factory)
            => new(symbol, ContentArgumentDelimiter.None, factory);

        public static AtomicRuleDefinition Of<TFactory>(
            string symbol,
            ContentArgumentDelimiter contentDelimiterType)
            where TFactory : IAtomicRuleFactory, new()
            => new(symbol, contentDelimiterType, new TFactory());

        public static AtomicRuleDefinition Of<TFactory>(
            string symbol)
            where TFactory : IAtomicRuleFactory, new()
            => new(symbol, ContentArgumentDelimiter.None, new TFactory());
    }
}
