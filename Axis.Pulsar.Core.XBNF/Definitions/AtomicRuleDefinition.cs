using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Rules;
using System.Collections.Immutable;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF.Definitions
{
    /// <summary>
    /// 
    /// </summary>
    public class AtomicRuleDefinition
    {
        public ImmutableHashSet<string> Symbols { get; }

        public IAtomicRuleFactory Factory { get; }

        public ContentArgumentDelimiter ContentDelimiterType { get; }

        public AtomicRuleDefinition(
            ContentArgumentDelimiter contentDelimiterType,
            IAtomicRuleFactory factory,
            params string[] symbols)
        {            
            Factory = factory.ThrowIfNull(() => new ArgumentNullException(nameof(factory)));
            ContentDelimiterType = contentDelimiterType.ThrowIfNot(
                Enum.IsDefined,
                _ => new ArgumentException($"Invalid content delimiter type: '{contentDelimiterType}' is undefined"));

            Symbols = symbols
                .ThrowIfNull(() => new ArgumentNullException(nameof(symbols)))
                .ThrowIf(
                    strs => strs.IsEmpty(),
                    _ => new ArgumentException($"Invalid {nameof(symbols)}: empty"))
                .ThrowIfAny(
                    symbol => !Production.SymbolPattern.IsMatch(symbol),
                    symbol => new FormatException($"Invalid {nameof(symbol)} format: '{symbol}'"))
                .ThrowIfDuplicate(symbol => new InvalidOperationException(
                    $"Invalid state: duplicate symbol found '{symbol}'"))
                .ToImmutableHashSet();
        }

        public static AtomicRuleDefinition Of(
            ContentArgumentDelimiter contentDelimiterType,
            IAtomicRuleFactory factory,
            params string[] symbols)
            => new(contentDelimiterType, factory, symbols);

        public static AtomicRuleDefinition Of(
            IAtomicRuleFactory factory,
            params string[] symbols)
            => new(ContentArgumentDelimiter.None, factory, symbols);

        public static AtomicRuleDefinition Of<TFactory>(
            ContentArgumentDelimiter contentDelimiterType,
            params string[] symbols)
            where TFactory : IAtomicRuleFactory, new()
            => new(contentDelimiterType, new TFactory(), symbols);

        public static AtomicRuleDefinition Of<TFactory>(
            params string[] symbols)
            where TFactory : IAtomicRuleFactory, new()
            => new(ContentArgumentDelimiter.None, new TFactory(), symbols);
    }
}
