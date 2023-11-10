using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.XBNF.Definitions
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
                IProduction.SymbolPattern.IsMatch,
                new FormatException($"Invalid escape name: '{name}'"));
        }

        public static EscapeMatcherDefinition Of(
            string name,
            IEscapeSequenceMatcher matcher)
            => new(name, matcher);
    }
}
