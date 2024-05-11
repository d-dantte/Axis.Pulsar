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
            ContentArgumentDelimiter.DoubleQuote,
            new LiteralRuleFactory(),
            "Literal", "literal");

        /// <summary>
        /// 
        /// </summary>
        public static readonly AtomicRuleDefinition Pattern = AtomicRuleDefinition.Of(
            ContentArgumentDelimiter.Sol,
            new PatternRuleFactory(),
            "Pattern", "pattern");

        /// <summary>
        /// 
        /// </summary>
        public static readonly AtomicRuleDefinition CharacterRanges = AtomicRuleDefinition.Of(
            ContentArgumentDelimiter.Quote,
            new CharRangeRuleFactory(),
            "Ranges", "ranges");

        /// <summary>
        /// 
        /// </summary>
        public static readonly AtomicRuleDefinition EOF = AtomicRuleDefinition.Of(
            ContentArgumentDelimiter.None,
            new EOFRuleFactory(),
            "EOF", "eof");

        /// <summary>
        /// 
        /// </summary>
        public static readonly AtomicRuleDefinition DelimitedString = AtomicRuleDefinition.Of(
            ContentArgumentDelimiter.None,
            new DelimitedStringRuleFactory(),
            "DelimitedString", "delimited-string");

    }
}
