using Axis.Luna.Common;
using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Exceptions;
using Axis.Pulsar.Core.Utils;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Core.Grammar.Rules
{
    public class TerminalPattern : IAtomicRule
    {
        public Regex Pattern { get; }

        public IMatchType MatchType { get; }

        public TerminalPattern(Regex pattern, IMatchType matchType)
        {
            Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
            MatchType = matchType ?? throw new ArgumentNullException(nameof(matchType));
        }

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath productionPath,
            out IResult<ICSTNode> result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(productionPath);

            result = MatchType switch
            {
                IMatchType.Closed closed => RecognizeClosedPattern(
                    reader,
                    productionPath,
                    Pattern,
                    closed),

                IMatchType.Open open => RecognizeOpenPattern(
                    reader,
                    productionPath,
                    Pattern,
                    open),

                _ => Result.Of<ICSTNode>(
                    new RecognitionRuntimeError(
                        new InvalidOperationException(
                            $"Invalid match type: {MatchType}")))
            };

            return result.IsDataResult();
        }

        private static IResult<ICSTNode> RecognizeClosedPattern(
            TokenReader reader,
            ProductionPath productionPath,
            Regex pattern,
            IMatchType.Closed matchType)
        {
            var position = reader.Position;
            if (reader.TryGetTokens(matchType.MaxMatch, out var tokens))
            {
                var matchRange = matchType.MaxMatch - matchType.MinMatch;
                for (int cnt = 0; cnt <= matchRange; cnt++)
                {
                    var subtokens = tokens[0..^cnt];

                    if (pattern.IsMatch(subtokens.AsSpan()))
                        return ICSTNode
                            .Of(pattern, subtokens)
                            .ApplyTo(Result.Of<ICSTNode>);
                }
            }

            reader.Reset(position);
            return UnrecognizedTokens
                .Of(productionPath, position)
                .ApplyTo(Result.Of<ICSTNode>);
        }

        private static IResult<ICSTNode> RecognizeOpenPattern(
            TokenReader reader,
            ProductionPath productionPath,
            Regex pattern,
            IMatchType.Open matchType)
        {
            var position = reader.Position;
            var length = 0;
            var mismatchCount = 0;

            while (reader.TryPeekTokens(++length, out var tokens))
            {
                if (pattern.IsMatch(tokens.AsSpan()))
                    mismatchCount = 0;

                mismatchCount++;
                if (mismatchCount > matchType.MaxMismatch)
                    break;
            }

            var trueLength = length - mismatchCount;
            if ((trueLength == 0 && matchType.AllowsEmptyTokens)
                || trueLength > 0)
                return ICSTNode
                    .Of(pattern, reader.GetTokens(trueLength, true))
                    .ApplyTo(Result.Of);

            else 
                return UnrecognizedTokens
                    .Of(productionPath, position)
                    .ApplyTo(Result.Of<ICSTNode>);
        }
    }


    public interface IMatchType
    {
        /// <summary>
        /// Create a closed match-type
        /// </summary>
        /// <param name="minMatch">the minimum character match count</param>
        /// <param name="maxMatch">the maximum character match count</param>
        public static IMatchType Of(int minMatch, int maxMatch) => new Closed(minMatch, maxMatch);

        /// <summary>
        /// Create an open match-type
        /// </summary>
        /// <param name="maxMisMatch">The maximum number of mismatches that fails the match process</param>
        /// <param name="allowsEmptyTokens">Indicates if empty tokens are considered a match</param>
        public static IMatchType Of(int maxMisMatch, bool allowsEmptyTokens = false) => new Open(maxMisMatch, allowsEmptyTokens);


        /// <summary>
        /// Open ended match-type.
        /// With this match-type, tokens will continously be pulled from the reader and matched till the <see cref="Open.MaxMismatch"/>
        /// number of mis-matches is reached.
        /// </summary>
        public readonly struct Open :
            IMatchType,
            IDefaultValueProvider<Open>
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

            public bool IsDefault => MaxMismatch == 0;

            public static Open Default => default;

            public Open(int maxMismatch, bool allowsEmptyTokens = false)
            {
                AllowsEmptyTokens = allowsEmptyTokens;
                MaxMismatch = maxMismatch.ThrowIf(
                        v => v < 1,
                        new ArgumentOutOfRangeException(nameof(maxMismatch)));
            }

            public override int GetHashCode() => HashCode.Combine(MaxMismatch, AllowsEmptyTokens);

            public override bool Equals(object? obj)
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
        public readonly struct Closed :
            IMatchType,
            IDefaultValueProvider<Open>
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

            public bool IsDefault => MinMatch == 0 && MaxMatch == 0;

            public static Open Default => default;

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

            public override bool Equals(object? obj)
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
