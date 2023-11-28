using Axis.Pulsar.Core.XBNF.Definitions;

namespace Axis.Pulsar.Core.XBNF.RuleFactories
{
    /// <summary>
    /// Default out-of-the-box rule definitions
    /// </summary>
    public static class DefaultAtomicRuleDefinitions
    {
        /// <summary>
        /// 
        /// </summary>
        public static readonly AtomicRuleDefinition Literal = AtomicRuleDefinition.Of(
            "Literal",
            AtomicContentDelimiterType.DoubleQuote,
            new LiteralRuleFactory());

        /// <summary>
        /// 
        /// </summary>
        public static readonly AtomicRuleDefinition Pattern = AtomicRuleDefinition.Of(
            "Pattern",
            AtomicContentDelimiterType.Sol,
            new PatternRuleFactory());

        /// <summary>
        /// 
        /// </summary>
        public static readonly AtomicRuleDefinition CharacterRanges = AtomicRuleDefinition.Of(
            "Ranges",
            AtomicContentDelimiterType.Quote,
            new CharRangeRuleFactory());

        /// <summary>
        /// 
        /// </summary>
        public static readonly AtomicRuleDefinition EOF = AtomicRuleDefinition.Of(
            "EOF",
            AtomicContentDelimiterType.None,
            new EOFRuleFactory());

        /// <summary>
        /// 
        /// </summary>
        public static readonly AtomicRuleDefinition DelimitedString = AtomicRuleDefinition.Of(
            "DelimitedString",
            AtomicContentDelimiterType.None,
            new DelimitedStringRuleFactory());

    }
}
