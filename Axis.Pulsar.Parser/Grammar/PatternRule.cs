using System;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Parser.Grammar
{
    /// <summary>
    /// Terminal symbol representing regular-expression patterns of strings
    /// </summary>
    public record PatternRule : ITerminal<Regex>
    {
        /// <summary>
        /// The regular expression pattern.
        /// Note: consider converting this to a function that calls <see cref="Regex.ToString"/>
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// The regex pattern that defines this rule. This regex must recognize at least 1 token.
        /// </summary>
        public Regex Value { get; }

        /// <summary>
        /// Defines how the regular expression will be interpreted.
        /// </para>
        /// </summary>
        public IPatternMatchType MatchType { get; }

        /// <inheritdoc/>
        public int? RecognitionThreshold => null;

        /// <summary>
        /// When present, is called by the corresponding parse after it has parsed a token, but before it reports the parse as successful.
        /// </summary>
        public IRuleValidator<PatternRule> RuleValidator { get; }

        /// <summary>
        /// Creates a new Pattern rule instance
        /// </summary>
        /// <param name="regex">The regex to use in matching characters</param>
        /// <param name="matchType">The <see cref="PatternRule.MatchType"/> instance</param>
        public PatternRule(
            Regex regex,
            IPatternMatchType matchType,
            IRuleValidator<PatternRule> ruleValidator = null)
        {
            Value = regex ?? throw new ArgumentNullException(nameof(regex));
            MatchType = matchType ?? throw new ArgumentNullException(nameof(matchType));
            Pattern = regex.ToString();
            RuleValidator = ruleValidator;
        }

        public PatternRule(Regex regex, IRuleValidator<PatternRule> ruleValidator = null) 
            : this(regex, new IPatternMatchType.Open(1), ruleValidator)
        { }
    }


    /// <summary>
    /// Match type for the <see cref="PatternRule"/>
    /// </summary>
    public interface IPatternMatchType
    {
        /// <summary>
        /// Open ended match-type.
        /// With this match-type, tokens will continously be pulled from the reader and matched till the <see cref="Open.MaxMismatch"/>
        /// number of mis-matches is reached.
        /// </summary>
        public struct Open : IPatternMatchType
        {
            /// <summary>
            /// Default open match type that does not recognize the empty tokens, and has a <see cref="Open.MaxMismatch"/> of 1 
            /// </summary>
            public static readonly Open DefaultMatch = new Open(1, false);

            /// <summary>
            /// Number of mismatches that signals a match failure. Minimum value for this is 1
            /// </summary>
            public int MaxMismatch { get; }

            /// <summary>
            /// Indicates that the rule can match an empty token list (empty string)
            /// </summary>
            public bool MatchesEmptyTokens { get; }

            public Open(int maxMismatch, bool matchesEmptyTokens = false)
            {
                MaxMismatch = maxMismatch.ThrowIf(
                        v => v < 1,
                        new ArgumentException($"Invariant error: {nameof(Open.MaxMismatch)} < 1"));
                MatchesEmptyTokens = matchesEmptyTokens;
            }

            public override int GetHashCode() => HashCode.Combine(MaxMismatch, MatchesEmptyTokens);

            public override bool Equals(object obj)
            {
                return obj is Open other
                    && other.MaxMismatch.Equals(MaxMismatch)
                    && other.MatchesEmptyTokens.Equals(MatchesEmptyTokens);
            }

            public override string ToString() => $"[{MaxMismatch}, {MatchesEmptyTokens}]";

            public static bool operator ==(Open first, Open second) => first.Equals(second);
            public static bool operator !=(Open first, Open second) => !first.Equals(second);
        }

        /// <summary>
        /// Closed-ended match-type.
        /// In this case, at most <see cref="Closed.MaxMatch"/> number of tokens are pulled in, and matched, 
        /// counting backwards to <see cref="Closed.MinMatch"/>, or till a match is found.
        /// </summary>
        public struct Closed : IPatternMatchType
        {
            public static readonly Closed DefaultMatch = new Closed(1, 1);

            /// <summary>
            /// Minimum number of tokens that the rule will match against
            /// </summary>
            public int MinMatch { get; }

            /// <summary>
            /// Maximum number of tokens that the rule will match against
            /// </summary>
            public int MaxMatch { get; }

            public Closed(int minMatch, int maxMatch)
            {
                MinMatch = minMatch.ThrowIf(
                    v => v < 1,
                    new ArgumentException($"Invariant error: {nameof(Closed.MinMatch)} < 1"));

                MaxMatch = maxMatch.ThrowIf(
                    v => v < 1,
                    new ArgumentException($"Invariant error: {nameof(Closed.MaxMatch)} < 1"));

                if (MinMatch > MaxMatch)
                    throw new ArgumentException($"Invariant error: {nameof(Closed.MaxMatch)} < {nameof(Closed.MinMatch)}");
            }

            public override int GetHashCode() => HashCode.Combine(MinMatch, MaxMatch);

            public override bool Equals(object obj)
            {
                return obj is Closed other
                    && other.MinMatch.Equals(MinMatch)
                    && other.MaxMatch.Equals(MaxMatch);
            }

            public override string ToString() => $"[{MinMatch}, {MaxMatch}]";

            public static bool operator ==(Closed first, Closed second) => first.Equals(second);
            public static bool operator !=(Closed first, Closed second) => !first.Equals(second);
        }
    }
}
