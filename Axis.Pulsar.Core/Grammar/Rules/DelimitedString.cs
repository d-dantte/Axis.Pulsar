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
        public ImmutableArray<Tokens> IllegalSequences { get; }
        public ImmutableArray<Tokens> LegalSequences { get; }
        public ImmutableHashSet<CharRange> IllegalRanges { get; }
        public ImmutableHashSet<CharRange> LegalRanges { get; }
        public Tokens EndDelimiterEscapeSequence { get; }
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
        /// <param name="endDelimiterEscapeSequence"></param>
        public DelimitedString(
            bool acceptsEmptyString,
            string startDelimiter,
            string endDelimiter,
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
                .Distinct()
                .OrderByDescending(t => t.SourceSegment.Length)
                .ToImmutableArray();
            IllegalSequences = illegalSequences
                .ThrowIfNull(new ArgumentNullException(nameof(illegalSequences)))
                .ThrowIfAny(t => t.IsDefault || t.IsEmpty, new ArgumentException("Invalid legal sequence: default/empty"))
                .Distinct()
                .ToImmutableArray();
        }

        public static DelimitedString Of(
            bool acceptsEmptyString,
            string startDelimiter,
            string endDelimiter,
            IEnumerable<Tokens> legalSequences,
            IEnumerable<Tokens> illegalSequences,
            IEnumerable<CharRange> legalRanges,
            IEnumerable<CharRange> illegalRanges,
            Tokens endDelimiterEscapeSequence = default)
            => new(acceptsEmptyString, startDelimiter, endDelimiter,
                legalSequences, illegalSequences,
                legalRanges, illegalRanges,
                endDelimiterEscapeSequence);

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
                result = FailedRecognitionError
                    .Of(productionPath, position)
                    .ApplyTo(Result.Of<ICSTNode>);

                return false;
            }
            tokens = tokens.ConJoin(startDelimTokens);

            // String
            if (!TryRecognizeString(reader, out var stringTokens))
            {
                reader.Reset(position);
                result = PartialRecognitionError
                    .Of(productionPath, position, tokens.SourceSegment.EndOffset - position - 1)
                    .ApplyTo(Result.Of<ICSTNode>);

                return false;
            }
            tokens = tokens.ConJoin(stringTokens);

            // End Delimiter
            if (!TryRecognizeEndDelimiter(reader, out var endDelimTokens))
            {
                reader.Reset(position);
                result = PartialRecognitionError
                    .Of(productionPath, position, tokens.SourceSegment.EndOffset - position - 1)
                    .ApplyTo(Result.Of<ICSTNode>);

                return false;
            }
            tokens = tokens.ConJoin(endDelimTokens);

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

        internal bool TryRecognizeString(TokenReader reader, out Tokens tokens)
        {
            tokens = Tokens.Empty;
            var position = reader.Position;

            #region First run: find the end delimiter, and validate char ranges

            while (!reader.IsConsumed)
            {
                // escaped delimiter
                if (!EndDelimiterEscapeSequence.IsDefault && reader.TryGetTokens(
                    EndDelimiterEscapeSequence,
                    out var escapedDelimTokens))
                {
                    tokens += escapedDelimTokens;
                    continue;
                }

                // end delimiter
                if (reader.TryPeekTokens(EndDelimiter, out _))
                    break;

                if (reader.TryPeekToken(out var token))
                {
                    // illegal range
                    if (IllegalRanges.Any(range => range.Contains(token[0])))
                        return false;

                    // legal range
                    if (LegalRanges.IsEmpty || LegalRanges.Any(range => range.Contains(token[0])))
                    {
                        tokens += token;
                        reader.Advance();
                    }
                }
            }

            if (tokens.IsEmpty && AcceptsEmptyString)
                return true;

            #endregion

            #region Second run: find illegal sequences

            var capturedTokens = tokens;
            var illegalMatchers = IllegalSequences
                .Select(illegalSequence => SubstringMatcher.OfLookBehind(
                    illegalSequence,
                    capturedTokens))
                .OrderByDescending(m => m.PatternLength)
                .ToArray();

            foreach (var matcher in illegalMatchers)
            {
                while (matcher.TryNextWindow(out var isMatch))
                {
                    if (isMatch)
                    {
                        tokens = tokens[..matcher.CurrentOffset];
                        return false;
                    }
                }
            }

            #endregion

            #region Third run: find legal sequences

            if (LegalSequences.IsEmpty)
                return true;

            for (int index = 0; index < tokens.SourceSegment.Length; index++)
            {
                var shift = LegalSequences
                    .Where(legalSequence =>
                    {
                        var boundaryIndex = legalSequence.SourceSegment.Length + index;
                        if (boundaryIndex > capturedTokens.SourceSegment.Length)
                            return false;

                        var subtoken = capturedTokens[index..boundaryIndex];
                        if (legalSequence.Equals(subtoken))
                            return true;

                        return false;
                    })
                    .Select(legalSequence => legalSequence.SourceSegment.Length)
                    .FirstOrDefault();

                if (shift > 0)
                    index += (shift - 1);

                else
                {
                    tokens = tokens[..index];
                    return false;
                }
            }
            return true;

            #endregion
        }

        #endregion
    }
}
