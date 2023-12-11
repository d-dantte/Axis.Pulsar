using Axis.Luna.Common;
using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Utils;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Core.Grammar.Rules
{
    public class TerminalPattern : IAtomicRule
    {
        public string Id { get; }

        public Regex Pattern { get; }

        public IMatchType MatchType { get; }

        public TerminalPattern(string id, Regex pattern, IMatchType matchType)
        {
            Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
            MatchType = matchType ?? throw new ArgumentNullException(nameof(matchType));
            Id = id.ThrowIfNot(
                IProduction.SymbolPattern.IsMatch,
                _ => new ArgumentException($"Invalid atomic rule {nameof(id)}: '{id}'"));
        }

        public static TerminalPattern Of(
            string id,
            Regex pattern,
            IMatchType matchType)
            => new(id, pattern, matchType);

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath productionPath,
            ILanguageContext context,
            out IRecognitionResult<ICSTNode> result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(productionPath);

            var patternPath = productionPath.Next(Id);
            result = MatchType switch
            {
                IMatchType.Closed closed => RecognizeClosedPattern(
                    reader,
                    patternPath,
                    Pattern,
                    closed),

                IMatchType.Open open => RecognizeOpenPattern(
                    reader,
                    patternPath,
                    Pattern,
                    open),

                _ => throw new InvalidOperationException(
                        $"Invalid match type: {MatchType}")
            };

            return result.IsSuccess();
        }

        private static IRecognitionResult<ICSTNode> RecognizeClosedPattern(
            TokenReader reader,
            ProductionPath productionPath,
            Regex pattern,
            IMatchType.Closed matchType)
        {
            var position = reader.Position;
            if (reader.TryGetTokens(matchType.MaxMatch, out var tokens))
            {
                //var matchRange = matchType.MaxMatch - matchType.MinMatch;
                for (int length = matchType.MaxMatch; length >= matchType.MinMatch; length--)
                {
                    var match = pattern.Match(
                        tokens.Source!,
                        tokens.Segment.Offset,
                        length);

                    if (match.Success && match.Length == length)
                        return ICSTNode
                            .Of(productionPath.Name, tokens[..length])
                            .ApplyTo(RecognitionResult.Of);
                }
            }

            reader.Reset(position);
            return FailedRecognitionError
                .Of(productionPath, position)
                .ApplyTo(error => RecognitionResult.Of<ICSTNode>(error));
        }

        private static IRecognitionResult<ICSTNode> RecognizeOpenPattern(
            TokenReader reader,
            ProductionPath productionPath,
            Regex pattern,
            IMatchType.Open matchType)
        {
            var position = reader.Position;
            var length = 0;
            var mismatchCount = 0;
            Tokens tokens = default;

            while (reader.TryPeekTokens(++length, true, out tokens))
            {
                if (pattern.IsMatch(tokens.AsSpan()))
                    mismatchCount = 0;

                else
                {
                    mismatchCount++;
                    if (mismatchCount > matchType.MaxMismatch)
                        break;
                }
            }

            var trueLength = tokens.Segment.Count - mismatchCount;
            if ((trueLength == 0 && matchType.AllowsEmptyTokens)
                || trueLength > 0)
                return ICSTNode
                    .Of(productionPath.Name, reader.GetTokens(trueLength, true))
                    .ApplyTo(RecognitionResult.Of);

            else 
                return FailedRecognitionError
                    .Of(productionPath, position)
                    .ApplyTo(error => RecognitionResult.Of<ICSTNode>(error));
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
                        _ => new ArgumentOutOfRangeException(nameof(maxMismatch)));
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
            public static readonly Closed DefaultMatch = new(1, 1);

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
                    _ => new ArgumentException($"Invariant error: {nameof(Closed.MinMatch)} < 1"));

                MaxMatch = maxMatch.ThrowIf(
                    v => v < 1,
                    _ => new ArgumentException($"Invariant error: {nameof(Closed.MaxMatch)} < 1"));

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
