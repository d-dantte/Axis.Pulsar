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

    }
}
