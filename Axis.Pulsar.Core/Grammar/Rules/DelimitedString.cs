using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Exceptions;
using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Utils;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Grammar.Rules
{
    /// <summary>
    /// Represents parsing tokens with the following properties:
    /// <list type="number">
    /// <item>Has a left delimiter - a sequence of characters marking the beginning of the token group</item>
    /// <item>Has a right delimiter - a sequence of characters marking the end of the token group. This may be the same as the left delimiter.</item>
    /// <item>Has an optional list of legal sequences allowed between the left and right delimiters. If this list is absent, any character is allowed, EXCEPT the right delimiter</item>
    /// <item>Has an optional list of illegal sequences prohibited from appearing between the left and right delimiters. This by default contains the right delimiter, so it is never absent.</item>
    /// <item>
    /// Has an optional list of escape matchers. Escape matchers are comprised of:
    /// <list type="number">
    ///     <item>a delimiter marking the start of the escape</item>
    ///     <item>a list of sequences following the escape delimiter</item>
    /// </list>
    /// <para>Note, when recognizing escape sequences, parsing no longer considers the legal/illegal/delimiter sequences.</para> 
    /// </item>
    /// </list>
    /// </summary>
    public class DelimitedString : IAtomicRule
    {
        private readonly string[] _orderedMatcherDelimiters;

        public ImmutableDictionary<string, IEscapeSequenceMatcher> EscapeMatchers { get; }
        public ImmutableHashSet<Tokens> IllegalSequences { get; }
        public ImmutableHashSet<Tokens> LegalSequences { get; }
        public ImmutableHashSet<CharRange> IllegalRanges { get; }
        public ImmutableHashSet<CharRange> LegalRanges { get; }
        public string StartDelimiter { get; }
        public string EndDelimiter { get; }
        public bool AcceptsEmptyString { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startDelimiter"></param>
        /// <param name="endDelimiter"></param>
        /// <param name="legalSequences">The collection of legal character sequences. Note: an empty collection means all sequences are legal</param>
        /// <param name="illegalSequences"></param>
        /// <param name="legalRanges">The collection of legal character ranges. Note: an empty collection means all ranges are legal</param>
        /// <param name="illegalRanges"></param>
        /// <param name="escapeMatchers"></param>
        public DelimitedString(
            bool acceptsEmptyString,
            string startDelimiter,
            string endDelimiter,
            IEnumerable<Tokens> legalSequences,
            IEnumerable<Tokens> illegalSequences,
            IEnumerable<CharRange> legalRanges,
            IEnumerable<CharRange> illegalRanges,
            IEnumerable<IEscapeSequenceMatcher> escapeMatchers)
        {
            AcceptsEmptyString = acceptsEmptyString;
            EscapeMatchers = escapeMatchers
                .ThrowIfNull(new ArgumentNullException(nameof(escapeMatchers)))
                .ThrowIfAny(e => e is null, new ArgumentException("Invalid escape matcher: null"))
                .ToImmutableDictionary(m => m.EscapeDelimiter, m => m);

            _orderedMatcherDelimiters = EscapeMatchers.Keys
                .OrderByDescending(delim => delim.Length)
                .ToArray();

            StartDelimiter = startDelimiter.ThrowIf(
                string.IsNullOrEmpty,
                new ArgumentException($"Invalid start delimiter"));
            EndDelimiter = endDelimiter ?? startDelimiter;

            LegalRanges = legalRanges
                .ThrowIfNull(new ArgumentNullException(nameof(legalRanges)))
                .ApplyTo(CharRange.NormalizeRanges)
                .ToImmutableHashSet();
            IllegalRanges = illegalRanges
                .ThrowIfNull(new ArgumentNullException(nameof(illegalRanges)))
                .ApplyTo(CharRange.NormalizeRanges)
                .ToImmutableHashSet();

            LegalSequences = legalSequences
                .ThrowIfNull(new ArgumentNullException(nameof(legalSequences)))
                .ThrowIfAny(t => t.IsDefault || t.IsEmpty, new ArgumentException("Invalid legal sequence: default/empty"))
                .ToImmutableHashSet();
            IllegalSequences = illegalSequences
                .ThrowIfNull(new ArgumentNullException(nameof(illegalSequences)))
                .ThrowIfAny(t => t.IsDefault || t.IsEmpty, new ArgumentException("Invalid legal sequence: default/empty"))
                .ToImmutableHashSet();

            // NOTE: Ensure that the escape delimiters do not clash with the Illegal ranges/sequences.
            throw new NotImplementedException("NOTE: Ensure that the escape delimiters do not clash with the Illegal ranges/sequences");
        }

        #region Procedural implementation
        public bool TryRecognize(TokenReader reader, ProductionPath productionPath, out IResult<ICSTNode> result)
        {
            var position = reader.Position;
            var tokens = Tokens.Empty;

            // Open Delimiter
            if (!TryRecognizeStartDelimiter(reader, out var startDelimTokens))
            {
                reader.Reset(position);
                result = UnrecognizedTokens
                    .Of(productionPath, position)
                    .ApplyTo(Result.Of<ICSTNode>);

                return false;
            }
            tokens = tokens.CombineWith(startDelimTokens);

            // String
            if (!TryRecognizeString(reader, out var stringTokens))
            {
                reader.Reset(position);
                result = PartiallyRecognizedTokens
                    .Of(productionPath, position, tokens)
                    .ApplyTo(Result.Of<ICSTNode>);

                return false;
            }
            tokens = tokens.CombineWith(stringTokens);

            // End Delimiter
            if (!TryRecognizeEndDelimiter(reader, out var endDelimTokens))
            {
                reader.Reset(position);
                result = PartiallyRecognizedTokens
                    .Of(productionPath, position, tokens)
                    .ApplyTo(Result.Of<ICSTNode>);

                return false;
            }
            tokens = tokens.CombineWith(endDelimTokens);

            result = ICSTNode
                .Of(productionPath.Name, tokens)
                .ApplyTo(Result.Of);
            return true;
        }

        internal bool TryRecognizeStartDelimiter(TokenReader reader, out Tokens tokens)
        {
            var position = reader.Position;

            if (!reader.TryGetTokens(StartDelimiter.Length, true, out tokens)
                || !tokens.Equals(StartDelimiter))
            {
                reader.Reset(position);
                return false;
            }

            return true;
        }

        internal bool TryRecognizeEndDelimiter(TokenReader reader, out Tokens tokens)
        {
            var position = reader.Position;

            if (!reader.TryGetTokens(EndDelimiter.Length, true, out tokens)
                || !tokens.Equals(EndDelimiter))
            {
                reader.Reset(position);
                return false;
            }

            return true;
        }

        internal bool TryRecognizeEscapeSequence(TokenReader reader, out Tokens tokens)
        {
            var position = reader.Position;
            tokens = Tokens.Empty;

            for (int index = 0; index < _orderedMatcherDelimiters.Length; index++)
            {
                var delim = _orderedMatcherDelimiters[index];
                if (reader.TryGetTokens(delim.Length, true, out var escapeDelim)
                    && escapeDelim.Equals(delim))
                {
                    tokens = tokens.CombineWith(escapeDelim);
                    var matcher = EscapeMatchers[delim];

                    if (matcher.TryMatchEscapeArgument(reader, out var argResult))
                    {
                        tokens = argResult
                            .Map(tokens.CombineWith)
                            .Resolve();
                        return true;
                    }

                    reader.Reset(position);
                    return false;
                }

                reader.Reset(position);
            }

            return false;
        }

        /// <summary>
        /// Attempts to recognize the string between the start and end delimiters. The algorithm works as follows:
        /// <list type="number">
        /// <item><paramref name="tokens"/> represents all currently valid string tokens</item>
        /// <item>Check that the tail end of the tokens does not match with the <see cref="DelimitedString.EndDelimiter"/>. If it does, exit.</item>
        /// <item>Next, check that the last token does not match any given illegal character range.</item>
        /// <item>Next, check that the last token matches any given legal character range. If no legal range exists, then all non-illegal tokens are legal</item>
        /// <item>Next, check that the tail end of the tokens does not match any illegal sequence.</item>
        /// <item>Next, check that the tail end of the tokens matches any given legal sequence. If no legal sequence exists, then all non-illegal sequences are legal</item>
        /// <item>If we got this far, we have a legal token. Append it to <paramref name="tokens"/>.</item>
        /// </list>
        /// </summary>
        /// <param name="reader">The token reader</param>
        /// <param name="tokens">The token instance, holding all valid tokens</param>
        /// <returns>True if recognition succeeds, false otherwise</returns>
        internal bool TryRecognizeString(TokenReader reader, out Tokens tokens)
        {
            var position = reader.Position;
            tokens = Tokens.Empty;

            // build sequence matchers
            var endDelimiterMatcher = SequenceMatcher.Of(
                EndDelimiter,
                reader.Source,
                position);

            var escapeDelimMatchers = _orderedMatcherDelimiters
                .Select(delim => new SequenceMatcher(delim, reader.Source, position))
                .ToArray();

            var illegalSequenceMatchers = IllegalSequences
                .Select(seq => new SequenceMatcher(seq, reader.Source, position))
                .ToArray();

            var legalSequenceMatchers = LegalSequences
                .Select(seq => new SequenceMatcher(seq, reader.Source, position))
                .ToArray();

            while (reader.TryGetToken(out var token))
            {
                var tempPosition = reader.Position;

                // end delimiter?
                if (endDelimiterMatcher.TryNextWindow(out var isMatch) && isMatch)
                    return ResetReader(reader, tempPosition, true);

                #region Ranges
                // illegal ranges?
                if (IllegalRanges.Any(range => range.Contains(token[0])))
                    return ResetReader(reader, tempPosition, false);

                // legal ranges?
                if (!LegalRanges.IsEmpty
                    && !LegalRanges.Any(range => range.Contains(token[0])))
                    return ResetReader(reader, tempPosition, false);
                #endregion

                #region Sequences

                // escape sequences?
                if (TryMatch(escapeDelimMatchers, out var matcher))
                {
                    var escapeDelimString = matcher!.Pattern.ToString()!;
                    var escapeMatcher = EscapeMatchers[escapeDelimString];

                    if (!escapeMatcher.TryMatchEscapeArgument(reader, out var escapeArgs))
                        return ResetReader(reader, tempPosition, false);

                    tokens = tokens.CombineWith(token).CombineWith(escapeArgs.Resolve());
                    continue;
                }

                // illegal sequences?
                if (TryMatch(illegalSequenceMatchers, out _))
                    return ResetReader(reader, tempPosition, false);

                // legal sequences?
                if (!legalSequenceMatchers.IsEmpty()
                    && !TryMatch(legalSequenceMatchers, out matcher))
                    return ResetReader(reader, tempPosition, false);
                #endregion

                tokens = tokens.CombineWith(token);
            }

            reader.Reset(position);
            return false;
        }

        /// <summary>
        /// Advances all matchers by one token, then returns the first one whose window matches.
        /// </summary>
        /// <param name="matchers">Sequence of matchers</param>
        /// <param name="matcher">The delimiter that matched.</param>
        /// <returns>True if a match was made, false otherwise</returns>
        internal static bool TryMatch(IEnumerable<SequenceMatcher> matchers, out SequenceMatcher? matcher)
        {
            matcher = null;
            foreach (var m in matchers)
            {
                _ = m.TryNextWindow(out bool isMatch);
                matcher = isMatch && matcher is null ? m : matcher;
            }
            return matcher is not null;
        }

        /// <summary>
        /// Convenience method for resetting the reader and returning a boolean value
        /// </summary>
        internal static bool ResetReader(TokenReader reader, int position, bool returnValue)
        {
            _ = reader.Reset(position);
            return returnValue;
        }

        #endregion

        #region Nested types

        /// <summary>
        /// Matches tokens against the escape sequence.
        /// <para>
        /// An escape sequence comprises 2 parts: <c>{delimiter}{argument}</c>. e.g: <c>{\}{n}</c>, <c>{\u}{0A2E}</c>, <c>{&amp;}{lt;}</c>.
        /// </para>
        /// <para>
        /// Escape matchers are grouped into sets by their <see cref="IEscapeSequenceMatcher.EscapeDelimiter"/> property, meaning matchers with
        /// duplicate escape delimiters cannot be used together in a <see cref="DelimitedString"/>. The recognition algorithm scans through
        /// the matchers based on the length of their "Delimiters", finding which metchers delimiter matches with incoming tokens from the
        /// <see cref="BufferedTokenReader"/>, then calling <see cref="IEscapeSequenceMatcher.TryMatch(BufferedTokenReader, out char[])"/>
        /// to match the escape arguments
        /// </para>
        /// <para>
        /// A classic solution for this is to combine both into another matcher, as is done with the
        /// <see cref="BSolGeneralEscapeMatcher"/>.
        /// </para>
        /// </summary>
        public interface IEscapeSequenceMatcher
        {
            /// <summary>
            /// The escape delimiter.
            /// </summary>
            public string EscapeDelimiter { get; }

            /// <summary>
            /// Attempts to match the escape-argument.
            /// If matching fails, this method MUST reset the reader to the position before it started reading.
            /// </summary>
            /// <param name="reader">the token reader</param>
            /// <param name="tokens">returns the matched arguments if sucessful, or the unmatched tokens</param>
            /// <returns>true if matched, otherwise false</returns>
            bool TryMatchEscapeArgument(TokenReader reader, out IResult<Tokens> tokens);
        }

        /// <summary>
        /// Uses a RollingHash implementation to check if a moving window of the source string matches a given pattern.
        /// </summary>
        internal class SequenceMatcher
        {
            private int _cursor;
            private RollingHash.Hash _matchSequenceHash;
            private RollingHash? _sourceRollingHash;

            /// <summary>
            /// The pattern to find within the source string
            /// </summary>
            public Tokens Pattern { get; }

            /// <summary>
            /// The source string
            /// </summary>
            public string Source { get; }

            /// <summary>
            /// The start offset in the source string from which matches are to be found
            /// </summary>
            public int StartOffset { get; }

            public SequenceMatcher(Tokens sequence, string source, int startOffset)
            {
                Pattern = sequence.ThrowIf(
                    seq => seq.IsEmpty || seq.IsDefault,
                    new ArgumentException($"Invalid {nameof(sequence)}: default/empty"));

                Source = source.ThrowIf(
                    string.IsNullOrEmpty,
                    new ArgumentException($"Invalid {nameof(source)}: null/empty"));

                StartOffset = startOffset.ThrowIf(
                    offset => offset < 0 || offset >= source.Length,
                    new ArgumentOutOfRangeException(
                        nameof(startOffset),
                        $"Value '{startOffset}' is < 0 or > {nameof(source)}.Length"));

                _sourceRollingHash = null;
                _matchSequenceHash = RollingHash
                    .Of(Pattern.Source,
                        Pattern.Offset,
                        Pattern.Count)
                    .WindowHash;
            }

            public static SequenceMatcher Of(
                Tokens sequence,
                string source,
                int startOffset)
                => new(sequence, source, startOffset);

            /// <summary>
            /// Consumes the next token from the source, from the current offset, then attempts to match the new window with the <see cref="SequenceMatcher.Pattern"/>.
            /// </summary>
            /// <param name="isMatch">True if the match succeeded, false otherwise</param>
            /// <returns>True if a new token could be consumed, false otherwise</returns>
            public bool TryNextWindow(out bool isMatch)
            {
                isMatch = false;
                var newCursor = _cursor + 1;

                if (newCursor >= Source.Length)
                    return false;

                _cursor = newCursor;
                if (_cursor - StartOffset < Pattern.Count)
                    return true;

                if (_sourceRollingHash is null)
                {
                    _sourceRollingHash = RollingHash.Of(Source, StartOffset, Pattern.Count);
                    isMatch = _sourceRollingHash.WindowHash.Equals(_matchSequenceHash);
                }
                else
                {
                    isMatch =
                        _sourceRollingHash.TryNext(out var hash)
                        && hash.Equals(_matchSequenceHash);
                }

                return true;
            }
        }

        #endregion
    }
}
