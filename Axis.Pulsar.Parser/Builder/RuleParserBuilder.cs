using Axis.Pulsar.Parser.Language;
using System;

namespace Axis.Pulsar.Parser.Builder
{
    public static class RuleParserBuilder
    {
        public static IParser BuildParser(IRule rule)
        {
            return rule switch
            {
                PatternTerminal p => new PatternMatcherParser(p),

                StringTerminal s => new StringMatcherParser(s),

                NonTerminal n => new NonTerminalParser(n),

                _ => throw new ArgumentException($"Invalid rule type: {typeof(RuleParserBuilder)}")
            };
        }
    }
}
