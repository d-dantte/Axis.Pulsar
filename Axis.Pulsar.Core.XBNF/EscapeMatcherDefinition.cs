using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using static Axis.Pulsar.Core.Grammar.Rules.DelimitedString;

namespace Axis.Pulsar.Core.XBNF
{
    /// <summary>
    /// 
    /// </summary>
    public class EscapeMatcherDefinition
    {
        public string Name { get; }

        public IEscapeSequenceMatcher Matcher { get; }

        public EscapeMatcherDefinition(
            string name,
            IEscapeSequenceMatcher matcher)
        {
            Matcher = matcher.ThrowIfNull(new ArgumentNullException(nameof(matcher)));
            Name = name.ThrowIfNot(
                Production.SymbolPattern.IsMatch,
                new FormatException($"Invalid escape name: '{name}'"));
        }

        public static AtomicRuleDefinition Of(
            string symbol,
            AtomicContentDelimiterType contentDelimiterType,
            IAtomicRuleFactory factory)
            => new(symbol, contentDelimiterType, factory);
    }
}
