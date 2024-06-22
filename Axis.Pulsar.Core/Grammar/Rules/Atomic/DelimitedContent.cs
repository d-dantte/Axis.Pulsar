using Axis.Luna.Common.Segments;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;
using Axis.Luna.Common;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Grammar.Rules.Atomic
{
    /// <summary>
    /// Rule that defines tokens that can appear between a Start and End delimiter.
    /// </summary>
    public class DelimitedContent : IAtomicRule
    {
        public string Id { get; }
        public DelimiterInfo StartDelimiter { get; }
        public DelimiterInfo EndDelimiter { get; }
        public bool AcceptsEmptyContent { get; }
        public IContentConstraint ContentConstraint { get; }


        public DelimitedContent(
            string id,
            bool acceptsEmptyContent,
            DelimiterInfo startDelimiter,
            DelimiterInfo endDelimiter,
            IContentConstraint contentConstraint)
        {
            Id = id.ThrowIfNot(
                Production.SymbolPattern.IsMatch,
                _ => new ArgumentException($"Invalid atomic rule {nameof(id)}: '{id}'"));

            ContentConstraint = contentConstraint.ThrowIfNull(
                () => new ArgumentNullException(nameof(contentConstraint)));

            StartDelimiter = startDelimiter.ThrowIfDefault(
                _ => new ArgumentException($"Invalid {nameof(startDelimiter)}: default"));

            EndDelimiter = endDelimiter.ThrowIfDefault(
                _ => new ArgumentException($"Invalid {nameof(endDelimiter)}: default"));

            AcceptsEmptyContent = acceptsEmptyContent;
        }

        public DelimitedContent(
            string id,
            bool acceptsEmptyContent,
            DelimiterInfo startDelimiter,
            IContentConstraint contentConstraint)
            : this(id, acceptsEmptyContent, startDelimiter, startDelimiter, contentConstraint)
        {
        }

        public static DelimitedContent Of(
            string id,
            bool acceptsEmptyContent,
            DelimiterInfo startDelimiter,
            DelimiterInfo endDelimiter,
            IContentConstraint contentConstraint)
            => new(id, acceptsEmptyContent, startDelimiter, endDelimiter, contentConstraint);

        public static DelimitedContent Of(
            string id,
            bool acceptsEmptyContent,
            DelimiterInfo startDelimiter,
            IContentConstraint contentConstraint)
            => new(id, acceptsEmptyContent, startDelimiter, contentConstraint);

        public bool TryRecognize(
            TokenReader reader,
            SymbolPath productionPath,
            ILanguageContext context,
            out NodeRecognitionResult result)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var position = reader.Position;
            var delimPath = productionPath.Next(Id);
            var tokens = Tokens.EmptyAt(reader.Source, position);

            // Open Delimiter
            if (!TryRecognizeDelimiter(reader, StartDelimiter, out var startDelimTokens))
            {
                reader.Reset(position);
                result = FailedRecognitionError
                    .Of(delimPath, position)
                    .ApplyTo(NodeRecognitionResult.Of);

                return false;
            }
            tokens += startDelimTokens;

            // Content
            if (!TryRecognizeContent(reader, ContentConstraint, AcceptsEmptyContent, out var contentToken))
            {
                reader.Reset(position);
                result = PartialRecognitionError
                    .Of(delimPath, position, tokens.Segment.Count)
                    .ApplyTo(NodeRecognitionResult.Of);

                return false;
            }
            tokens += contentToken;

            // End Delimiter
            if (!TryRecognizeDelimiter(reader, EndDelimiter, out var endDelimTokens))
            {
                reader.Reset(position);
                result = PartialRecognitionError
                    .Of(delimPath, position, tokens.Segment.Count)
                    .ApplyTo(NodeRecognitionResult.Of);

                return false;
            }
            tokens += endDelimTokens;

            result = ISymbolNode
                .Of(delimPath.Symbol, tokens)
                .ApplyTo(NodeRecognitionResult.Of);
            return true;
        }

        internal static bool TryRecognizeContent(
            TokenReader reader,
            IContentConstraint contentConstraint,
            bool acceptsEmptyContent,
            out Tokens tokens)
        {
            tokens = contentConstraint.ReadValidTokens(reader);

            if (tokens.IsEmpty)
                return acceptsEmptyContent;

            return true;
        }

        internal static bool TryRecognizeDelimiter(
            TokenReader reader,
            DelimiterInfo info,
            out Tokens tokens)
        {
            var position = reader.Position;

            if (!reader.TryGetTokens(info.Delimiter.Length, true, out tokens)
                || !tokens.Equals(info.Delimiter))
            {
                tokens = Tokens.EmptyAt(reader.Source, position);
                reader.Reset(position);
                return false;
            }

            return true;
        }


        #region Nested types
        public readonly struct DelimiterInfo : IDefaultValueProvider<DelimiterInfo>
        {
            public string Delimiter { get; }

            public string? EscapeSequence { get; }

            public bool IsDefault => Delimiter is null && EscapeSequence is null;

            public static DelimiterInfo Default => default;

            public DelimiterInfo(string delimiter, string? escapeSequence = null)
            {
                ArgumentException.ThrowIfNullOrEmpty(delimiter);

                if (escapeSequence is not null && !escapeSequence!.Contains(delimiter))
                    throw new ArgumentException($"Invalid escape sequence: {nameof(delimiter)} must be a substring of {nameof(escapeSequence)}");

                Delimiter = delimiter;
                EscapeSequence = escapeSequence;
            }

            /// <summary>
            /// Checks if the End delimiter matches the end of the <paramref name="tokens"/> instance. The rule is:
            /// <list type="number">
            /// <item>The Delimiter property matches the end of the token</item>
            /// <item> #1 is true, and the Delimiter info has no escape sequence, in which case, return true</item>
            /// <item>#1 is true, and the Delimiter has an escape sequence that is not a superset of the delimter, in which case, return true</item>
            /// <item>#1 is true, and the Delimiter has a superset escape, in which case, ensure the escape does not match the end of the token instance</item>
            /// </list>
            /// </summary>
            /// <param name="tokens"></param>
            /// <param name="endDelimiter"></param>
            /// <returns></returns>
            public bool MatchesEndOfTokens(Tokens tokens)
            {
                var tokenSpan = tokens.AsSpan();

                if (!tokenSpan.EndsWith(Delimiter))
                    return false;

                if (EscapeSequence is null)
                    return true;

                if (tokenSpan.EndsWith(EscapeSequence!))
                    return false;

                return true;
            }

            /// <summary>
            /// Find the first occurence of the "Delimiter" within the <paramref name="tokens"/> instance that is also not
            /// a part of an occurence of the EscapeSequence.
            /// </summary>
            /// <param name="tokens"></param>
            /// <param name="index"></param>
            /// <returns></returns>
            public bool TryIndexOfDelimiterInTokens(Tokens tokens, out int index)
            {
                var offset = 0;
                bool found;
                do
                {
                    found = TryNextIndexOfDelimiterInTokens(tokens, offset, out index);
                    index += index == -1 ? 0 : offset;
                    offset = index + Delimiter.Length;
                }
                while (!found && index >= 0);

                return index >= 0;
            }

            /// <summary>
            /// Finds the index of the next occurence of the <see cref="Delimiter"/>, assigning it to
            /// the <paramref name="delimiterIndex"/> parameter.
            /// </summary>
            /// <param name="tokens">The tokens to search within</param>
            /// <param name="offset">The offset from which to start the search</param>
            /// <param name="delimiterIndex">The parameter to store the index if found. Note that this is the index from the OFFSET, not from the start of the token</param>
            /// <returns>True if no <see cref="EscapeSequence"/> exist, or if the found delimiter isn't a subset of the escape sequence.</returns>
            public bool TryNextIndexOfDelimiterInTokens(Tokens tokens, int offset, out int delimiterIndex)
            {
                var tokenSpan = tokens.AsSpan();
                var comparison = StringComparison.InvariantCulture;

                if (!tokenSpan.TryNextIndexOf(Delimiter, offset, comparison, out delimiterIndex))
                    return false;

                else return EscapeSequence switch
                {
                    null => true,
                    string escape =>
                        !(tokenSpan.TryNextIndexOf(escape, offset, comparison, out var escapeIndex)
                        && Segment.Of(escapeIndex, escape.Length).Contains(Segment.Of(delimiterIndex, Delimiter.Length)))
                };
            }
        }

        public interface IPattern
        {
            bool Matches(Tokens tokens);
            int Length { get; }
        }

        public class LiteralPattern : IPattern
        {
            private readonly string literal;
            private readonly StringComparison comparison;

            public int Length => literal.Length;

            public LiteralPattern(string literal, bool isCaseSensitive = true)
            {
                ArgumentNullException.ThrowIfNull(literal);
                this.literal = literal;
                comparison = isCaseSensitive switch
                {
                    true => StringComparison.InvariantCulture,
                    false => StringComparison.InvariantCultureIgnoreCase
                };
            }

            public bool Matches(Tokens tokens)
            {
                return literal.AsSpan().Equals(tokens.AsSpan(), comparison);
            }
        }

        public class WildcardPattern : IPattern
        {
            private readonly WildcardExpression wildcard;

            public int Length => wildcard.Length;

            public WildcardPattern(WildcardExpression wildcard)
            {
                this.wildcard = wildcard.ThrowIfDefault(_ => new ArgumentException($"Invalid {nameof(wildcard)}: default"));
            }

            public bool Matches(
                Tokens tokens)
                => wildcard.IsMatch(tokens.AsSpan());
        }

        public interface IContentConstraint
        {
            /// <summary>
            /// Reads tokens from the <paramref name="reader"/>, until the encapsulated constraint-rule is violated, returning all valid
            /// tokens read, or an empty <see cref="Tokens"/> instance if no valid tokens were read.
            /// </summary>
            /// <param name="reader">The token reader to read from</param>
            /// <returns>A result of the tokens read</returns>
            Tokens ReadValidTokens(TokenReader reader);
        }

        public class LegalCharacterRanges : IContentConstraint
        {
            public ImmutableArray<CharRange> Ranges { get; }

            public DelimiterInfo EndDelimiter { get; }

            public LegalCharacterRanges(
                DelimiterInfo endDelimiter,
                params CharRange[] ranges)
            {
                ArgumentNullException.ThrowIfNull(ranges);

                EndDelimiter = endDelimiter;
                Ranges = ranges
                    .ThrowIf(
                        arr => arr.IsEmpty(),
                        _ => new InvalidOperationException("Invalid ranges: empty"))
                    .ThrowIfAny(
                        range => range.IsDefault,
                        _ => new InvalidOperationException("Invalid range: default"))
                    .ApplyTo(CharRange.NormalizeRanges)
                    .ToImmutableArray();
            }

            public Tokens ReadValidTokens(TokenReader reader)
            {
                var position = reader.Position;
                var tokens = Tokens.EmptyAt(reader.Source, position);

                while (reader.TryGetToken(out var token))
                {
                    if (Ranges.Any(range => range.Contains(token[0])))
                        tokens += token;

                    else
                    {
                        reader.Back();
                        break;
                    }

                    if (EndDelimiter.MatchesEndOfTokens(tokens))
                    {
                        reader.Back(EndDelimiter.Delimiter.Length);
                        tokens = tokens[..^EndDelimiter.Delimiter.Length];
                        break;
                    }
                }

                return tokens;
            }
        }

        public class IllegalCharacterRanges : IContentConstraint
        {
            public ImmutableArray<CharRange> Ranges { get; }

            public DelimiterInfo EndDelimiter { get; }

            public IllegalCharacterRanges(
                DelimiterInfo endDelimiter,
                params CharRange[] ranges)
            {
                ArgumentNullException.ThrowIfNull(ranges);

                EndDelimiter = endDelimiter;
                Ranges = ranges
                    .ThrowIf(
                        arr => arr.IsEmpty(),
                        _ => new InvalidOperationException("Invalid ranges: empty"))
                    .ThrowIfAny(
                        range => range.IsDefault,
                        _ => new InvalidOperationException("Invalid range: default"))
                    .ApplyTo(CharRange.NormalizeRanges)
                    .ToImmutableArray();
            }

            public Tokens ReadValidTokens(TokenReader reader)
            {
                var position = reader.Position;
                var tokens = Tokens.EmptyAt(reader.Source, position);

                while (reader.TryGetToken(out var token))
                {
                    if (Ranges.Any(range => range.Contains(token[0])))
                    {
                        reader.Back();
                        break;
                    }

                    tokens += token;

                    if (EndDelimiter.MatchesEndOfTokens(tokens))
                    {
                        reader.Back(EndDelimiter.Delimiter.Length);
                        tokens = tokens[..^EndDelimiter.Delimiter.Length];
                        break;
                    }
                }

                return tokens;
            }
        }

        public class LegalDiscretePatterns : IContentConstraint
        {
            public ImmutableArray<IPattern> Patterns { get; }

            public LegalDiscretePatterns(params IPattern[] patterns)
            {
                Patterns = patterns
                    .ThrowIfNull(() => new ArgumentNullException(nameof(patterns)))
                    .ThrowIf(
                        arr => arr.IsEmpty(),
                        _ => new InvalidOperationException("Invalid patterns: empty"))
                    .ThrowIfAny(
                        pattern => pattern is null,
                        _ => new InvalidOperationException("Invalid pattern: null"))
                    .OrderByDescending(pattern => pattern.Length)
                    .ToImmutableArray();
            }

            public Tokens ReadValidTokens(TokenReader reader)
            {
                var position = reader.Position;
                var tokens = Tokens.EmptyAt(reader.Source, position);
                bool found;

                do
                {
                    found = false;
                    foreach (var pattern in Patterns)
                    {
                        if (reader.TryPeekTokens(pattern.Length, true, out var next)
                            && pattern.Matches(next))
                        {
                            tokens += next;
                            reader.Advance(pattern.Length);
                            found = true;
                            break;
                        }
                    }
                }
                while (found);

                return tokens;
            }
        }

        public class IllegalDiscretePatterns : IContentConstraint
        {
            public ImmutableArray<IPattern> Patterns { get; }

            public DelimiterInfo EndDelimiter { get; }

            public IllegalDiscretePatterns(
                DelimiterInfo endDelimiter,
                params IPattern[] patterns)
            {
                EndDelimiter = endDelimiter;
                Patterns = patterns
                    .ThrowIfNull(() => new ArgumentNullException(nameof(patterns)))
                    .ThrowIf(
                        arr => arr.IsEmpty(),
                        _ => new InvalidOperationException("Invalid patterns: empty"))
                    .ThrowIfAny(
                        pattern => pattern is null,
                        _ => new InvalidOperationException("Invalid pattern: null"))
                    .OrderByDescending(pattern => pattern.Length)
                    .ToImmutableArray();
            }

            public Tokens ReadValidTokens(TokenReader reader)
            {
                var position = reader.Position;
                var tokens = Tokens.EmptyAt(reader.Source, position);

                while (reader.TryGetToken(out var token))
                {
                    var tempTokens = tokens + token;

                    if (EndDelimiter.MatchesEndOfTokens(tempTokens))
                    {
                        tokens = tempTokens[..^EndDelimiter.Delimiter.Length];
                        break;
                    }
                    else
                    {
                        // find the pattern that matches the end of the tokens
                        var pattern = Luna.Optional.EnumerableExtensions.FirstOrOptional(
                            Patterns,
                            pattern =>
                                tempTokens.Segment.Count >= pattern.Length
                                && pattern.Matches(tempTokens[^pattern.Length..]));

                        // remove the matched illegal pattern
                        if (pattern.HasValue)
                        {
                            tokens = pattern
                                .Map(pattern => tempTokens[..^pattern.Length])!
                                .Value;
                            break;
                        }
                    }

                    tokens = tempTokens;
                }

                return tokens;
            }
        }

        #endregion
    }
}
