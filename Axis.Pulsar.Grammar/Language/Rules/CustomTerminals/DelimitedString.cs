using Axis.Pulsar.Grammar.Recognizers;
using Axis.Pulsar.Grammar.Recognizers.SpecialTerminals;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Grammar.Language.Rules.CustomTerminals
{
    public struct DelimitedString : ICustomTerminal
    {

        private readonly Dictionary<string, IEscapeSequenceMatcher> _escapeMatchers;

        public IReadOnlyDictionary<string, IEscapeSequenceMatcher> EscapeMatchers 
            => _escapeMatchers is not null
                ? new ReadOnlyDictionary<string, IEscapeSequenceMatcher>(_escapeMatchers)
                : null;

        public string StartDelimiter { get; }

        public string EndDelimiter { get; }

        public string SymbolName { get; }

        public IRecognizer ToRecognizer(Grammar grammar) => new DelimitedStringRecognizer(this, grammar);


        public DelimitedString(
            string symbolName,
            string startDelimiter,
            string endDelimiter,
            params IEscapeSequenceMatcher[] escapeMatchers)
        {
            StartDelimiter = startDelimiter.ThrowIf(
                string.IsNullOrEmpty,
                new ArgumentException(nameof(startDelimiter)));

            EndDelimiter = endDelimiter.ThrowIf(
                string.IsNullOrEmpty,
                new ArgumentException(nameof(endDelimiter)));

            SymbolName = symbolName.ThrowIfNot(
                SymbolHelper.IsValidSymbolName,
                new ArgumentException($"Invalid symbol name: {symbolName}"));

            var matchers = _escapeMatchers = new Dictionary<string, IEscapeSequenceMatcher>();

            escapeMatchers
                .ThrowIfNull(new ArgumentNullException(nameof(escapeMatchers)))
                .ForAll(transformer =>
                {
                    if (!matchers.TryAdd(transformer.EscapeDelimiter, transformer))
                        throw new ArgumentException($"Duplicate {nameof(IEscapeSequenceMatcher.EscapeDelimiter)} encountered: {transformer.EscapeDelimiter}");
                });
        }

        public DelimitedString(
            string symbolName,
            string delimiter,
            params IEscapeSequenceMatcher[] escapeMatchers)
            : this(symbolName, delimiter, delimiter, escapeMatchers)
        {
        }

        public override string ToString() => $"%{SymbolName}";

        public override int GetHashCode()
        {
            var transformersCode = _escapeMatchers?.Keys.Aggregate(
                func: (value, next) => HashCode.Combine(value, next),
                seed: 0)
                ?? 0;

            return HashCode.Combine(
                StartDelimiter,
                EndDelimiter,
                SymbolName,
                transformersCode);
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
                    Enumerable.SequenceEqual);
        }

        public static bool operator ==(DelimitedString first, DelimitedString second) => first.Equals(second);

        public static bool operator !=(DelimitedString first, DelimitedString second) => !first.Equals(second);


        /// <summary>
        /// Matches tokens against the encapsulated escape sequence.
        /// <para>
        /// Note of caution: When using/combining escape matchers, abstain from combinding matchers that may have
        /// <see cref="IEscapeSequenceMatcher.EscapeDelimiter"/>s that are subsets of other matchers. A good example of 
        /// this is the <see cref="BSolAsciiEscapeMatcher"/> and the <see cref="BSolUTF16EscapeMatcher"/>.
        /// The former has a delimiter of <c>"\"</c>, while the latter has a delimiter of <c>"\u"</c>. This means that the 
        /// <see cref="BSolAsciiEscapeMatcher"/> will greedily match the delimiter, and fail instances of the
        /// unicode-escape - e.g <c>\u21F5</c>. 
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
            /// Once the escape delimiter for this matcher is recognized, this matcher is called repeatedly with a subset of tokens,
            /// starting from the token after the matcher delimiter, till the matcher returns false.
            /// <para>
            /// e.g, for the unicode escapes (\u0A2E), once the "\u" delimiter is matched, this method is called with:
            /// <list type="number">
            /// <item>0</item>
            /// <item>0A</item>
            /// <item>0A2</item>
            /// <item>0A2E</item>
            /// </list>
            /// On the fifth call, the unicode matcher should return false.
            /// </para>
            /// </summary>
            /// <param name="subTokens">The sub tokens</param>
            /// <returns>true if the sub-tokens matched, false otherwise</returns>
            bool IsSubMatch(ReadOnlySpan<char> subTokens);

            /// <summary>
            /// After <see cref="IsSubMatch(ReadOnlySpan{char})"/> returns false, all the previously matched tokens are passed into this
            /// method to do a full match. False from this method indicates that the tokens were not matched.
            /// </summary>
            /// <param name="escapeTokens">The escape tokens</param>
            /// <returns>true if the tokens are a match, false otherwise</returns>
            bool IsMatch(ReadOnlySpan<char> escapeTokens);
        }

        public class BSolAsciiEscapeMatcher : IEscapeSequenceMatcher
        {
            public string EscapeDelimiter => "\\";

            public bool IsSubMatch(ReadOnlySpan<char> tokens)
            {
                return tokens.Length == 1 && tokens[0] switch
                {
                    '\'' => true,
                    '\"' => true,
                    '\\' => true,
                    'n' =>  true,
                    'r' =>  true,
                    'f' =>  true,
                    'b' =>  true,
                    't' =>  true,
                    'v' =>  true,
                    '0' =>  true,
                    'a' =>  true,
                    _ => false
                };
            }

            public bool IsMatch(ReadOnlySpan<char> tokens) => IsSubMatch(tokens);
        }

        public class BSolUTF16EscapeMatcher : IEscapeSequenceMatcher
        {
            private readonly Regex HexPattern = new(@"^[a-fA-F0-9]{1,4}$", RegexOptions.Compiled);

            public string EscapeDelimiter => "\\u";

            public bool IsSubMatch(ReadOnlySpan<char> subTokens)
                => subTokens.Length <= 4 && HexPattern.IsMatch(new string(subTokens));

            public bool IsMatch(ReadOnlySpan<char> tokens)
                => tokens.Length == 4 && HexPattern.IsMatch(new string(tokens));
        }

        public class BSolGeneralEscapeMatcher: IEscapeSequenceMatcher
        {
            private readonly Regex HexPattern = new(@"^u[a-fA-F0-9]{0,4}$", RegexOptions.Compiled);

            public string EscapeDelimiter => "\\";

            public bool IsSubMatch(ReadOnlySpan<char> subTokens)
            {
                if (subTokens[0] == 'u')
                    return subTokens.Length <= 5 
                        && HexPattern.IsMatch(new string(subTokens));

                return subTokens.Length == 1 && subTokens[0] switch
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

            public bool IsMatch(ReadOnlySpan<char> escapeTokens)
            {
                if (escapeTokens.Length == 5)
                    return HexPattern.IsMatch(new string(escapeTokens));

                if (escapeTokens.Length == 1 && escapeTokens[0] != 'u')
                    return IsSubMatch(escapeTokens);

                return false;
            }
        }

        public class XmlEscapeMatcher : IEscapeSequenceMatcher
        {
            public string EscapeDelimiter => "&";

            private static readonly string Quotation = "quot;";
            private static readonly string Apostrophe = "apos;";
            private static readonly string Ampersand = "amp;";
            private static readonly string LessThan = "lt;";
            private static readonly string GreaterThan = "gt;";

            public bool IsSubMatch(ReadOnlySpan<char> subTokens)
            {
                var tokenString = new string(subTokens);
                return Quotation.StartsWith(tokenString)
                    || Apostrophe.StartsWith(tokenString)
                    || Ampersand.StartsWith(tokenString)
                    || LessThan.StartsWith(tokenString)
                    || GreaterThan.StartsWith(tokenString);
            }

            public bool IsMatch(ReadOnlySpan<char> tokens)
            {
                var tokenString = new string(tokens);
                return Quotation.Equals(tokenString)
                    || Apostrophe.Equals(tokenString)
                    || Ampersand.Equals(tokenString)
                    || LessThan.Equals(tokenString)
                    || GreaterThan.Equals(tokenString);
            }
        }
    }
}
