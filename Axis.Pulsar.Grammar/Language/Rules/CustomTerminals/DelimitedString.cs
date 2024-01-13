using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.Recognizers;
using Axis.Pulsar.Grammar.Recognizers.CustomTerminals;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Grammar.Language.Rules.CustomTerminals
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
    public readonly struct DelimitedString : ICustomTerminal
    {
        private readonly Dictionary<string, IEscapeSequenceMatcher> _escapeMatchers;
        private readonly string[] _illegalSequences;
        private readonly string[] _legalSequences;

        public IReadOnlyDictionary<string, IEscapeSequenceMatcher> EscapeMatchers 
            => _escapeMatchers is not null
                ? new ReadOnlyDictionary<string, IEscapeSequenceMatcher>(_escapeMatchers)
                : new Dictionary<string, IEscapeSequenceMatcher>();

        public string[] IllegalSequences => _illegalSequences?.ToArray() ?? Array.Empty<string>();

        public string[] LegalSequences => _legalSequences?.ToArray() ?? Array.Empty<string>();

        public string StartDelimiter { get; }

        public string EndDelimiter { get; }

        public string SymbolName { get; }

        public IRecognizer ToRecognizer(Grammar grammar) => new DelimitedStringRecognizer(this, grammar);

        #region Constructors
        public DelimitedString(
            string symbolName,
            string delimiter,
            params IEscapeSequenceMatcher[] escapeMatchers)
            : this(symbolName, delimiter, delimiter, Array.Empty<string>(), Array.Empty<string>(), escapeMatchers)
        { }

        public DelimitedString(
            string symbolName,
            string delimiter,
            string[] illegalSequences,
            params IEscapeSequenceMatcher[] escapeMatchers)
            : this(symbolName, delimiter, delimiter, Array.Empty<string>(), illegalSequences, escapeMatchers)
        { }

        public DelimitedString(
            string symbolName,
            string delimiter,
            string[] legalSequences,
            string[] illegalSequences,
            params IEscapeSequenceMatcher[] escapeMatchers)
            : this(symbolName, delimiter, delimiter, legalSequences, illegalSequences, escapeMatchers)
        { }

        public DelimitedString(
            string symbolName,
            string startDelimiter,
            string endDelimiter,
            params IEscapeSequenceMatcher[] escapeMatchers)
            : this(symbolName, startDelimiter, endDelimiter, Array.Empty<string>(), Array.Empty<string>(), escapeMatchers)
        { }

        public DelimitedString(
            string symbolName,
            string startDelimiter,
            string endDelimiter,
            string[] illegalSequences,
            params IEscapeSequenceMatcher[] escapeMatchers)
            : this(symbolName, startDelimiter, endDelimiter, Array.Empty<string>(), illegalSequences, escapeMatchers)
        { }

        public DelimitedString(
            string symbolName,
            string startDelimiter,
            string endDelimiter,
            string[] legalSequences,
            string[] illegalSequences,
            params IEscapeSequenceMatcher[] escapeMatchers)
        {
            StartDelimiter = startDelimiter.ThrowIf(
                string.IsNullOrEmpty,
                _ => new ArgumentException("Null or empty string", nameof(startDelimiter)));

            EndDelimiter = endDelimiter.ThrowIf(
                string.IsNullOrEmpty,
                _ => new ArgumentException("Null or empty string", nameof(endDelimiter)));

            SymbolName = symbolName.ThrowIfNot(
                SymbolHelper.IsValidSymbolName,
                _ => new ArgumentException($"Invalid symbol name: {symbolName}", nameof(symbolName)));

            _illegalSequences = illegalSequences?.ToArray();
            _legalSequences = legalSequences?.ToArray();

            var matchers = _escapeMatchers = new Dictionary<string, IEscapeSequenceMatcher>();

            escapeMatchers
                .ThrowIfNull(() => new ArgumentNullException(nameof(escapeMatchers)))
                .ForAll(transformer =>
                {
                    if (!matchers.TryAdd(transformer.EscapeDelimiter, transformer))
                        throw new ArgumentException($"Duplicate {nameof(IEscapeSequenceMatcher.EscapeDelimiter)} encountered: {transformer.EscapeDelimiter}");
                });
        }
        #endregion

        public override string ToString() => $"@{SymbolName}";

        public override int GetHashCode()
        {
            var escapeMatcherCode = _escapeMatchers?.Keys.Aggregate(
                func: (value, next) => HashCode.Combine(value, next),
                seed: 0)
                ?? 0;

            var illegalSequencesCode = _illegalSequences?.Aggregate(
                func: (value, next) => HashCode.Combine(value, next),
                seed: 0)
                ?? 0;

            return HashCode.Combine(
                StartDelimiter,
                EndDelimiter,
                SymbolName,
                escapeMatcherCode,
                illegalSequencesCode);
        }

        public override bool Equals(object obj)
        {
            var stringEquality = EqualityComparer<string>.Default;
            return obj is DelimitedString other
                && stringEquality.Equals(other.StartDelimiter, StartDelimiter)
                && stringEquality.Equals(other.EndDelimiter, EndDelimiter)
                && stringEquality.Equals(other.SymbolName, SymbolName)
                && other.EscapeMatchers.NullOrTrue(
                    EscapeMatchers,
                    Enumerable.SequenceEqual)
                && other.IllegalSequences.NullOrTrue(
                    IllegalSequences,
                    Enumerable.SequenceEqual);
        }

        public static bool operator ==(DelimitedString first, DelimitedString second) => first.Equals(second);

        public static bool operator !=(DelimitedString first, DelimitedString second) => !first.Equals(second);

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
            bool TryMatchEscapeArgument(BufferedTokenReader reader, out char[] tokens);
        }

        public class BSolBasicEscapeMatcher : IEscapeSequenceMatcher
        {
            public string EscapeDelimiter => "\\";

            public bool TryMatchEscapeArgument(BufferedTokenReader reader, out char[] tokens)
            {
                if (!reader.TryNextTokens(1, out tokens))
                    return false;

                var matches = tokens[0] switch
                {
                    '\'' => true,
                    '\"' => true,
                    '\\' => true,
                    'n' => true,
                    'r' => true,
                    'f' => true,
                    'b' => true,
                    't' => true,
                    'v' => true,
                    '0' => true,
                    'a' => true,
                    _ => false
                };

                if (!matches)
                    reader.Back();

                return matches;
            }
        }

        public class BSolUTFEscapeMatcher : IEscapeSequenceMatcher
        {
            private readonly Regex B2HexPattern = new(@"^u[a-fA-F0-9]{4}$", RegexOptions.Compiled);
            private readonly Regex B4HexPattern = new(@"^U[a-fA-F0-9]{8}$", RegexOptions.Compiled);

            public string EscapeDelimiter => "\\";

            public bool TryMatchEscapeArgument(BufferedTokenReader reader, out char[] tokens)
            {
                var position = reader.Position;

                // utf-b4
                if (reader.TryNextTokens(9, out tokens)
                    && B4HexPattern.IsMatch(new string(tokens)))
                    return true;

                // utf-b2
                if (reader.Reset(position).TryNextTokens(5, out tokens)
                    && B2HexPattern.IsMatch(new string(tokens)))
                    return true;

                reader.Reset(position);
                return false;
            }
        }

        public class BSolGeneralEscapeMatcher: IEscapeSequenceMatcher
        {
            private readonly Regex B1HexPattern = new(@"^x[a-fA-F0-9]{2}$", RegexOptions.Compiled);
            private readonly Regex B2HexPattern = new(@"^u[a-fA-F0-9]{4}$", RegexOptions.Compiled);
            private readonly Regex B4HexPattern = new(@"^U[a-fA-F0-9]{8}$", RegexOptions.Compiled);

            public string EscapeDelimiter => "\\";

            public bool TryMatchEscapeArgument(BufferedTokenReader reader, out char[] tokens)
            {
                var position = reader.Position;

                // utf-b4
                if (reader.TryNextTokens(9, out tokens)
                    && B4HexPattern.IsMatch(new string(tokens)))
                    return true;

                // utf-b2
                if (reader.Reset(position).TryNextTokens(5, out tokens)
                    && B2HexPattern.IsMatch(new string(tokens)))
                    return true;

                // utf-b1
                if (reader.Reset(position).TryNextTokens(3, out tokens)
                    && B1HexPattern.IsMatch(new string(tokens)))
                    return true;

                // regular
                if (reader.Reset(position).TryNextTokens(1, out tokens)
                    && IsBasicEscapeArg(tokens[0]))
                    return true;

                reader.Reset(position);
                return false;
            }

            private static bool IsBasicEscapeArg(char c) => c switch
            {
                '\'' => true,
                '\"' => true,
                '\\' => true,
                'n' => true,
                'r' => true,
                'f' => true,
                'b' => true,
                't' => true,
                'v' => true,
                '0' => true,
                'a' => true,
                _ => false
            };
        }

        public class XmlEscapeMatcher : IEscapeSequenceMatcher
        {
            public string EscapeDelimiter => "&";

            private static readonly string Quotation = "quot;";
            private static readonly string Apostrophe = "apos;";
            private static readonly string Ampersand = "amp;";
            private static readonly string LessThan = "lt;";
            private static readonly string GreaterThan = "gt;";

            public bool TryMatchEscapeArgument(BufferedTokenReader reader, out char[] tokens)
            {
                var position = reader.Position;
                if (reader.TryNextTokens(5, out tokens)
                    && (Quotation.Equals(new string(tokens))
                    || Apostrophe.Equals(new string(tokens))))
                    return true;

                if (reader.Reset(position).TryNextTokens(4, out tokens)
                    && Ampersand.Equals(new string(tokens)))
                    return true;

                if (reader.Reset(position).TryNextTokens(3, out tokens)
                    && (LessThan.Equals(new string(tokens))
                    || GreaterThan.Equals(new string(tokens))))
                    return true;

                reader.Reset(position);
                return false;
            }
        }
        #endregion
    }
}
