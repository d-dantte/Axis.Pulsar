using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Utils;
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
            // throw new NotImplementedException("NOTE: Ensure that the escape delimiters do not clash with the Illegal ranges/sequences");
        }

        public static DelimitedString Of(
            bool acceptsEmptyString,
            string startDelimiter,
            string endDelimiter,
            IEnumerable<Tokens> legalSequences,
            IEnumerable<Tokens> illegalSequences,
            IEnumerable<CharRange> legalRanges,
            IEnumerable<CharRange> illegalRanges,
            IEnumerable<IEscapeSequenceMatcher> escapeMatchers)
            => new(acceptsEmptyString, startDelimiter, endDelimiter,
                legalSequences, illegalSequences,
                legalRanges, illegalRanges,
                escapeMatchers);

        #region Procedural implementation
        public bool TryRecognize(
            TokenReader reader,
            ProductionPath productionPath,
            ILanguageContext context,
            out IResult<ICSTNode> result)
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
            tokens = tokens.Join(startDelimTokens);

            // String
            if (!TryRecognizeString(reader, out var stringTokens))
            {
                reader.Reset(position);
                result = PartiallyRecognizedTokens
                    .Of(productionPath, position, tokens)
                    .ApplyTo(Result.Of<ICSTNode>);

                return false;
            }
            tokens = tokens.Join(stringTokens);

            // End Delimiter
            if (!TryRecognizeEndDelimiter(reader, out var endDelimTokens))
            {
                reader.Reset(position);
                result = PartiallyRecognizedTokens
                    .Of(productionPath, position, tokens)
                    .ApplyTo(Result.Of<ICSTNode>);

                return false;
            }
            tokens = tokens.Join(endDelimTokens);

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
                    tokens = tokens.Join(escapeDelim);
                    var matcher = EscapeMatchers[delim];

                    if (matcher.TryMatchEscapeArgument(reader, out var argResult))
                    {
                        tokens = tokens.Join(argResult);
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

                    tokens = tokens.Join(token).Join(escapeArgs);
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

                tokens = tokens.Join(token);
            }

            reader.Reset(position);
            return false;
        }

        internal bool TryRecognizeString_(TokenReader reader, out Tokens tokens)
        {
            var position = reader.Position;
            tokens = Tokens.Empty;

            var endDelimiterMatcher = SubstringMatcher.OfLookAhead(
                EndDelimiter,
                reader.Source,
                reader.Position);

            var illegalSequenceMatchers = IllegalSequences
                .Select(illegalSequence => SubstringMatcher.OfLookBehind(
                    illegalSequence,
                    reader.Source,
                    reader.Position))
                .ToArray();

            var legalSequenceMatchers = LegalSequences
                .Select(legalSequence => SubstringMatcher.OfLookBehind(
                    legalSequence,
                    reader.Source,
                    reader.Position))
                .ToArray();

            // Seems the only full-proof implementation will involve:
            // 1. reading the input token by token till we reach the (non-escaped) end delimiter.
            // 2. during #1, check for legal/illegal ranges
            // 3. in a second loop, check for illegal sequences
            // 4. in a third loop, ensure the whole string consists only of legal sequences
            while (reader.TryPeekToken(out var token))
            {
                var newTokens = Tokens.Empty;

                #region End delimiter

                // could not read any other token from the reader
                if (!endDelimiterMatcher.TryNextWindow(out var isEndDelimiterMatch))
                    return false;

                else if (isEndDelimiterMatch)
                {
                    reader.Advance(endDelimiterMatcher.PatternLength);
                    return true;
                }
                
                #endregion

                #region Illegal sequence match

                if (MatchesAny(illegalSequenceMatchers, out _))
                    return false;

                #endregion

                #region Legal sequence match

                if (legalSequenceMatchers.Length == 0)
                    newTokens = token;

                else if (MatchesAny(legalSequenceMatchers, out var matchCount))
                {
                    
                }

                #endregion

                // illegal tokens
            }
        }

        internal static bool MatchesAny(
            IEnumerable<SubstringMatcher> matchers,
            out int matchCount)
        {
            matchCount = 0;
            foreach (var matcher in matchers)
            {
                if (matcher.TryNextWindow(out bool isMatch) && isMatch)
                {
                    matchCount = matcher.PatternLength;
                    return true;
                }
            }

            return false;
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
    }
}
