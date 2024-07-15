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
        /// TODO: complete this method
        /// </summary>
        public static readonly AtomicRuleDefinition DelimitedContent = AtomicRuleDefinition.Of(
            ContentArgumentDelimiter.None,
            new DelimitedContentRuleFactory(
                DelimitedContentRuleFactory.ConstraintQualifierMap
                    .New()
                    .AddQualifiers(
                        new DelimitedContentRuleFactory.LegalCharacterRangesParser(),
                        "lcr", "legal-char-ranges")
                    .AddQualifiers(
                        new DelimitedContentRuleFactory.IllegalCharacterRangesParser(),
                        "icr", "illegal-char-ranges")
                    .AddQualifiers(
                        new DelimitedContentRuleFactory.LegalDiscretePatternsParser(),
                        "ldp", "legal-discrete-patterns")
                    .AddQualifiers(
                        new DelimitedContentRuleFactory.IllegalCharacterRangesParser(),
                        "idp", "illegal-discrete-pattern")
                    .AddQualifiers(
                        DelimitedContentRuleFactory.DefaultDelimitedContentParser.DefaultConstraintParser,
                        "default", "exclude-delims")),
            "DelimitedContent", "delimited-content", "dc");
    }
}
