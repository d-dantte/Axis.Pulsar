using Axis.Pulsar.Core.XBNF.RuleFactories;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF.Definitions
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
            ContentArgumentDelimiter.DoubleQuote,
            new LiteralRuleFactory());

        /// <summary>
        /// 
        /// </summary>
        public static readonly AtomicRuleDefinition Pattern = AtomicRuleDefinition.Of(
            "Pattern",
            ContentArgumentDelimiter.Sol,
            new PatternRuleFactory());

        /// <summary>
        /// 
        /// </summary>
        public static readonly AtomicRuleDefinition CharacterRanges = AtomicRuleDefinition.Of(
            "Ranges",
            ContentArgumentDelimiter.Quote,
            new CharRangeRuleFactory());

        /// <summary>
        /// 
        /// </summary>
        public static readonly AtomicRuleDefinition EOF = AtomicRuleDefinition.Of(
            "EOF",
            ContentArgumentDelimiter.None,
            new EOFRuleFactory());

        /// <summary>
        /// 
        /// </summary>
        public static readonly AtomicRuleDefinition DelimitedString = AtomicRuleDefinition.Of(
            "DelimitedString",
            ContentArgumentDelimiter.None,
            new DelimitedStringRuleFactory());

    }
}
