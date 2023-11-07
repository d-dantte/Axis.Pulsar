using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;

namespace Axis.Pulsar.Core.XBNF.RuleFactories
{
    public static class DefaultAtomicRuleDefinitions
    {
        public static readonly RuleDefinition Literal = RuleDefinition.Of(
            "Literal",
            AtomicContentDelimiterType.DoubleQuote,
            new LiteralRuleFactory());

        public static readonly RuleDefinition Pattern = RuleDefinition.Of(
            "Pattern",
            AtomicContentDelimiterType.Sol,
            new PatternRuleFactory());

        public static readonly RuleDefinition CharacterRanges = RuleDefinition.Of(
            "Ranges",
            AtomicContentDelimiterType.Quote,
            new CharRangeRuleFactory());


        #region Nested Types
        public readonly struct RuleDefinition
        {
            public string Symbol { get; }

            public IDelimitedContentAtomicRuleFactory Factory { get; }

            public AtomicContentDelimiterType ContentDelimiterType { get; }

            internal RuleDefinition(
                string symbol,
                AtomicContentDelimiterType contentDelimiterType,
                IDelimitedContentAtomicRuleFactory factory)
            {
                ContentDelimiterType = contentDelimiterType;
                Factory = factory.ThrowIfNull(new ArgumentNullException(nameof(factory)));
                Symbol = symbol.ThrowIfNot(
                    Production.SymbolPattern.IsMatch,
                    new FormatException($"Invalid symbol format: '{symbol}'"));
            }

            internal static RuleDefinition Of(
                string symbol,
                AtomicContentDelimiterType contentDelimiterType,
                IDelimitedContentAtomicRuleFactory factory)
                => new(symbol, contentDelimiterType, factory);
        }
        #endregion
    }
}
