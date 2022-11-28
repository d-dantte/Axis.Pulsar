using Axis.Pulsar.Grammar.Recognizers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Grammar.Language.Rules
{
    /// <summary>
    /// Representing regular-expression patterns of strings
    /// </summary>
    public struct Pattern: IAtomicRule
    {

        public static readonly string PatternSymbolName = "@Pattern";

        /// <summary>
        /// The regex pattern that defines this rule.
        /// </summary>
        public Regex Regex { get; }

        /// <summary>
        /// Defines how the regular expression will be interpreted.
        /// </summary>
        public MatchType MatchType { get; }

        /// <inheritdoc/>/>
        public string SymbolName => PatternSymbolName;

        /// <summary>
        /// Creates a new Pattern rule instance
        /// </summary>
        /// <param name="regex">The regex to use in matching characters</param>
        /// <param name="matchType">The <see cref="PatternRule.MatchType"/> instance</param>
        public Pattern(
            Regex regex,
            MatchType matchType)
        {
            Regex = regex ?? throw new ArgumentNullException(nameof(regex));
            MatchType = matchType ?? throw new ArgumentNullException(nameof(matchType));
        }

        public Pattern(Regex regex)
            : this(regex, new MatchType.Open(1))
        { }

        public override int GetHashCode() => HashCode.Combine(Regex, MatchType);

        public override bool Equals(object obj)
        {
            return obj is Pattern other
                && EqualityComparer<Regex>.Default.Equals(other.Regex, Regex)
                && EqualityComparer<MatchType>.Default.Equals(other.MatchType, MatchType);
        }

        public override string ToString()
            => Regex is not null
                ? $"/{Regex}/{Flags()}.{MatchType}"
                : "<//>";

        /// <inheritdoc/>
        public IRecognizer ToRecognizer(Grammar grammar) => new PatternRecognizer(this, grammar);

        private string Flags()
        {
            if (Regex is null)
                return null;

            var options = Regex.Options;
            if (options == RegexOptions.Compiled)
                return "";

            var sb = new StringBuilder();

            if (options.HasFlag(RegexOptions.IgnoreCase))
                sb.Append('i');

            if (options.HasFlag(RegexOptions.IgnorePatternWhitespace))
                sb.Append('x');

            if (options.HasFlag(RegexOptions.Multiline))
                sb.Append('m');

            if (options.HasFlag(RegexOptions.Singleline))
                sb.Append('s');

            if (options.HasFlag(RegexOptions.ExplicitCapture))
                sb.Append('n');

            return $".{sb}";
        }

        public static bool operator ==(Pattern first, Pattern second) => first.Equals(second);
        public static bool operator !=(Pattern first, Pattern second) => !(first == second);
    }
}
