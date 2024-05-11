using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Grammar.Atomic
{
    /// <summary>
    /// Represents parsing tokens with the following properties:
    /// <list type="number">
    /// <item>Has a start-delimiter - a sequence of characters marking the beginning of the token group</item>
    /// <item>
    ///     Has an optional end-delimiter - a sequence of characters marking the end of the token group.
    ///     If the end-delimiter is absent, then there must be either illegal sequences, or illegal ranges, or both.
    /// </item>
    /// <item>
    ///     Has an optoinal list of legal sequences - a list of strings that make up the only substrings allowed to
    ///     exist within the bounding delimiters. Any combination of the legal sequences is allowed.
    /// </item>
    /// <item>
    ///     Has an optional list of illegal sequences - a list of strings that must not be present in the bounded string.
    /// </item>
    /// <item>
    ///     An optional list of Legal character ranges - a list of character ranges that make up the subset of
    ///     allowable characters. Even legal sequences will be tested for legal characters, if specified, and if any is
    ///     found to not be in the legal range, the string is rejected.
    /// </item>
    /// <item>
    ///     An optional list of illegal character ranges - a list of ranges of characters that must not appear in the
    ///     bounded string.
    /// </item>
    /// <item>
    ///     Optional end-delimiter escape sequence. When end-delimiters are present, they usually signify a rejection of
    ///     the currently injested substring, if matched. End-delimiter escape sequences are used to by-pass the rejection.
    ///     If end-delimiters are absent, this property is ignored.
    /// </item>
    /// </list>
    /// <para/>
    /// The search/match algorithm used by this class is split into two categories: One for when Legal sequences are
    /// present, and the other for when they are absent. This is because when present, tokens are injested based on chunks
    /// of legal characters; and if absent, tokens are injested one character at a time.
    /// </summary>
    [Obsolete("Use DelimitedContent instead")]
    public class DelimitedString : IAtomicRule
    {
        public string Id { get; }
        public ImmutableArray<Tokens> IllegalSequences { get; }
        public ImmutableArray<Tokens> LegalSequences { get; }
        public ImmutableHashSet<CharRange> IllegalRanges { get; }
        public ImmutableHashSet<CharRange> LegalRanges { get; }
        public Tokens EndDelimiterEscapeSequence { get; }
        public string StartDelimiter { get; }
        public string? EndDelimiter { get; }
        public bool AcceptsEmptyString { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="startDelimiter"></param>
        /// <param name="endDelimiter">The optional end-delimiter</param>
        /// <param name="legalSequences">The collection of legal character sequences. Note: an empty collection means all sequences are legal</param>
        /// <param name="illegalSequences"></param>
        /// <param name="legalRanges">The collection of legal character ranges. Note: an empty collection means all ranges are legal</param>
        /// <param name="illegalRanges"></param>
        /// <param name="endDelimiterEscapeSequence">A special escape sequence for the end delimiter that the recognizer will accept and not recognize as the end deliiter.</param>
        public DelimitedString(
            string id,
            bool acceptsEmptyString,
            string startDelimiter,
            string? endDelimiter,
            IEnumerable<Tokens> legalSequences,
            IEnumerable<Tokens> illegalSequences,
            IEnumerable<CharRange> legalRanges,
            IEnumerable<CharRange> illegalRanges,
            Tokens endDelimiterEscapeSequence = default)
        {
            AcceptsEmptyString = acceptsEmptyString;
            EndDelimiterEscapeSequence = endDelimiterEscapeSequence;

            StartDelimiter = startDelimiter.ThrowIf(
                string.IsNullOrEmpty,
                _ => new ArgumentException($"Invalid start delimiter"));
            EndDelimiter = endDelimiter;

            LegalRanges = legalRanges
                .ThrowIfNull(() => new ArgumentNullException(nameof(legalRanges)))
                .ApplyTo(CharRange.NormalizeRanges)
                .ToImmutableHashSet();
            IllegalRanges = illegalRanges
                .ThrowIfNull(() => new ArgumentNullException(nameof(illegalRanges)))
                .ApplyTo(CharRange.NormalizeRanges)
                .ToImmutableHashSet();

            LegalSequences = legalSequences
                .ThrowIfNull(() => new ArgumentNullException(nameof(legalSequences)))
                .ThrowIfAny(t => t.IsDefault || t.IsEmpty, _ => new ArgumentException("Invalid legal sequence: default/empty"))
                .Distinct()
                .OrderByDescending(t => t.Segment.Count)
                .ToImmutableArray();
            IllegalSequences = illegalSequences
                .ThrowIfNull(() => new ArgumentNullException(nameof(illegalSequences)))
                .ThrowIfAny(t => t.IsDefault || t.IsEmpty, _ => new ArgumentException("Invalid legal sequence: default/empty"))
                .Concat(EndDelimiter!) // <-- add the end-delimiter to the illegal sequence list
                .Distinct()
                .Where(seq => !seq.IsDefaultOrEmpty)
                .OrderByDescending(t => t.Segment.Count)
                .ToImmutableArray();

            Id = id.ThrowIfNot(
                Production.SymbolPattern.IsMatch,
                _ => new ArgumentException($"Invalid atomic rule {nameof(id)}: '{id}'"));

            if (EndDelimiter is null
                && IllegalRanges.IsEmpty
                && IllegalSequences.IsEmpty)
                throw new InvalidOperationException(
                    $"Invalid {nameof(DelimitedString)}: with a null '{nameof(EndDelimiter)}', "
                    + $"there must be '{nameof(IllegalRanges)}' and/or '{nameof(IllegalSequences)}'");
        }

        public static DelimitedString Of(
            string id,
            bool acceptsEmptyString,
            string startDelimiter,
            string? endDelimiter,
            IEnumerable<Tokens> legalSequences,
            IEnumerable<Tokens> illegalSequences,
            IEnumerable<CharRange> legalRanges,
            IEnumerable<CharRange> illegalRanges,
            Tokens endDelimiterEscapeSequence = default)
            => new(id, acceptsEmptyString, startDelimiter, endDelimiter,
                legalSequences, illegalSequences,
                legalRanges, illegalRanges,
                endDelimiterEscapeSequence);

        #region Procedural implementation
        public bool TryRecognize(
            TokenReader reader,
            SymbolPath productionPath,
            ILanguageContext context,
            out NodeRecognitionResult result)
        {
            var position = reader.Position;
            var delimPath = productionPath.Next(Id);
            var tokens = Tokens.EmptyAt(reader.Source, position);

            // Open Delimiter
            if (!TryRecognizeStartDelimiter(reader, out var startDelimTokens))
            {
                reader.Reset(position);
                result = FailedRecognitionError
                    .Of(delimPath, position)
                    .ApplyTo(error => NodeRecognitionResult.Of(error));

                return false;
            }
            tokens += startDelimTokens;

            // String
            if (!TryRecognizeString(reader, out var stringTokens))
            {
                reader.Reset(position);
                result = PartialRecognitionError
                    .Of(delimPath, position, tokens.Segment.EndOffset - position - 1)
                    .ApplyTo(error => NodeRecognitionResult.Of(error));

                return false;
            }
            tokens += stringTokens;

            // End Delimiter
            if (EndDelimiter is not null)
            {
                if (!TryRecognizeEndDelimiter(reader, out var endDelimTokens))
                {
                    reader.Reset(position);
                    result = PartialRecognitionError
                        .Of(delimPath, position, tokens.Segment.EndOffset - position - 1)
                        .ApplyTo(error => NodeRecognitionResult.Of(error));

                    return false;
                }
                tokens += endDelimTokens;
            }

            result = ISymbolNode
                .Of(delimPath.Symbol, tokens)
                .ApplyTo(NodeRecognitionResult.Of);
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

            if (!reader.TryGetTokens(EndDelimiter!.Length, true, out tokens)
                || !tokens.Equals(EndDelimiter))
            {
                reader.Reset(position);
                return false;
            }

            return true;
        }

        internal bool TryRecognizeString(TokenReader reader, out Tokens tokens)
        {
            return LegalSequences.IsEmpty
                ? TryRecognizeStringWithoutLegalSequence(reader, out tokens)
                : TryRecognizeStringWithLegalSequence(reader, out tokens);
        }

        internal bool TryRecognizeStringWithoutLegalSequence(TokenReader reader, out Tokens tokens)
        {
            var position = reader.Position;
            tokens = Tokens.EmptyAt(reader.Source, position);

            // end delimiter escape
            var endDelimiterEscapeMatcher = EndDelimiter is null || EndDelimiterEscapeSequence.IsDefaultOrEmpty
                ? null
                : SubstringMatcher.LookBehindMatcher.Of(
                    EndDelimiterEscapeSequence,
                    reader.Source,
                    position);

            // descending-ordered array of illegal matchers
            var illegalMatchers = IllegalSequences
                .Select(sequence => SubstringMatcher.LookBehindMatcher.Of(
                    sequence,
                    reader.Source,
                    position))
                .ToArray();

            while (reader.TryGetToken(out var token))
            {
                tokens += token;

                if ((!LegalRanges.IsEmpty && !IsLegalCharacter(token[0])) // character is not in legal range                    
                    || IsIllegalCharacter(token[0]))                      // or character is in illegal range
                {
                    reader.Back();
                    tokens = tokens[..^1];
                    break;
                }

                // end-delimiter escape
                var isEndDelimiterEscapeMatched = false;
                _ = endDelimiterEscapeMatcher?.TryNextWindow(out isEndDelimiterEscapeMatched);

                // illegal sequence
                var (containsIllegalSequences, illegalMatchLength) = illegalMatchers
                    .Select(matcher => (
                        IsContained: ContainsIllegalSequence(matcher, 1, out var illegalLength),
                        Length: illegalLength))
                    .ToArray() // ensure all matchers are executed
                    .Where(tuple => tuple.IsContained)
                    .FirstOrDefault();

                // an illegal sequence was matched, and it is not the escaped end delimiter...
                if (containsIllegalSequences && !isEndDelimiterEscapeMatched)
                {
                    reader.Back(illegalMatchLength);
                    tokens = tokens[..^illegalMatchLength];
                    break;
                }
            }

            return !tokens.IsEmpty || AcceptsEmptyString;
        }

        internal bool TryRecognizeStringWithLegalSequence(
            TokenReader reader,
            out Tokens tokens)
        {
            var position = reader.Position;
            tokens = Tokens.EmptyAt(reader.Source, position);

            // descending-ordered array of illegal matchers
            var illegalMatchers = IllegalSequences
                .Select(sequence => SubstringMatcher.LookBehindMatcher.Of(
                    sequence,
                    reader.Source,
                    position))
                .ToArray();

            while (true)
            {
                // find the next legal sequence
                var legalToken = LegalSequences
                    .Select(seq => (
                        IsMatch: reader.TryPeekTokens(seq, out var foundTokens),
                        Tokens: foundTokens))
                    .ToArray() // ensure all matchers are executed
                    .Where(tuple => tuple.IsMatch)
                    .FirstOrDefault();

                // no legal token found
                if (!legalToken.IsMatch)
                    break;

                // all characters are within legal range
                if (!LegalRanges.IsEmpty && !legalToken.Tokens.All(IsLegalCharacter))
                    break;

                // illegal ranges present?
                if (legalToken.Tokens.Any(IsIllegalCharacter))
                    break;

                // illegal sequence
                if (illegalMatchers.Any(matcher => ContainsIllegalSequence(
                    windowLength: legalToken.Tokens.Segment.Count,
                    matcher: matcher,
                    sequenceLength: out _)))
                    break;

                // no illegal ranges or sequences
                tokens += legalToken.Tokens;
                reader.Advance(legalToken.Tokens.Segment.Count);
            }

            return !tokens.IsEmpty || AcceptsEmptyString;
        }

        #endregion

        private bool IsIllegalCharacter(
            char @char)
            => IllegalRanges.Any(range => range.Contains(@char));

        private bool IsLegalCharacter(
            char @char)
            => LegalRanges.Any(range => range.Contains(@char));

        private static bool ContainsIllegalSequence(
            SubstringMatcher matcher,
            int windowLength,
            out int sequenceLength)
        {
            for (int cnt = 0; cnt < windowLength; cnt++)
            {
                if (matcher.TryNextWindow(out var isMatch) && isMatch)
                {
                    sequenceLength = matcher.Pattern.Segment.Count;
                    return true;
                }
            }

            sequenceLength = 0;
            return false;
        }
    }
}
