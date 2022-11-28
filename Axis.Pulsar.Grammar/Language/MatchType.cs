using System;

namespace Axis.Pulsar.Grammar.Language
{
    public interface MatchType
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="minMatch"></param>
        /// <param name="maxMatch"></param>
        /// <returns></returns>
        public static MatchType Of(int minMatch, int maxMatch) => new Closed(minMatch, maxMatch);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxMisMatch"></param>
        /// <param name="allowsEmptyTokens"></param>
        /// <returns></returns>
        public static MatchType Of(int maxMisMatch, bool allowsEmptyTokens = false) => new Open(maxMisMatch, allowsEmptyTokens);


        /// <summary>
        /// Open ended match-type.
        /// With this match-type, tokens will continously be pulled from the reader and matched till the <see cref="Open.MaxMismatch"/>
        /// number of mis-matches is reached.
        /// </summary>
        public struct Open : MatchType
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
            public bool AllowsEmptyTokens { get; }

            public Open(int maxMismatch, bool allowsEmptyTokens = false)
            {
                MaxMismatch = maxMismatch.ThrowIf(
                        v => v < 1,
                        new ArgumentException($"Invariant error: {nameof(Open.MaxMismatch)} < 1"));
                AllowsEmptyTokens = allowsEmptyTokens;
            }

            public override int GetHashCode() => HashCode.Combine(MaxMismatch, AllowsEmptyTokens);

            public override bool Equals(object obj)
            {
                return obj is Open other
                    && other.MaxMismatch.Equals(MaxMismatch)
                    && other.AllowsEmptyTokens.Equals(AllowsEmptyTokens);
            }

            public override string ToString() => $"{MaxMismatch},{(AllowsEmptyTokens ? "*" : "+")}";

            public static bool operator ==(Open first, Open second) => first.Equals(second);
            public static bool operator !=(Open first, Open second) => !first.Equals(second);
        }

        /// <summary>
        /// Closed-ended match-type.
        /// In this case, at most <see cref="Closed.MaxMatch"/> number of tokens are pulled in, and matched, 
        /// counting backwards to <see cref="Closed.MinMatch"/>, or till a match is found.
        /// </summary>
        public struct Closed : MatchType
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

            public override string ToString() => $"{MinMatch},{MaxMatch}";

            public static bool operator ==(Closed first, Closed second) => first.Equals(second);
            public static bool operator !=(Closed first, Closed second) => !first.Equals(second);
        }
    }
}
