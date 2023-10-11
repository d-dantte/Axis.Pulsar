using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Misc.Pulsar.Utils;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Exceptions;
using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Axis.Pulsar.Core.Grammar.CustomRules
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
    public class DelimitedString : ICustomRule
    {
        #region Argument keys
        public static readonly string StartDelimiterArgumentKey = "start-delim";
        public static readonly string EndDelimiterArgumentKey = "end-delim";
        public static readonly string DelimiterArgumentKey = "delim";
        public static readonly string IllegalSequencesArgumentKey = "illegal-sequences";
        public static readonly string LegalSequencesArgumentKey = "legal-sequences";
        #endregion

        private readonly ImmutableDictionary<string, IEscapeSequenceMatcher> _escapeMatchers;
        private readonly ImmutableArray<string> _illegalSequences;
        private readonly ImmutableArray<string> _legalSequences;
        private readonly ImmutableHashSet<CharRange> _illegalRanges;
        private readonly ImmutableHashSet<CharRange> _legalRanges;
        private readonly string _startDelimiter;
        private readonly string _endDelimiter;


        public ImmutableDictionary<string, string> Arguments { get; }

        public DelimitedString(
            IDictionary<string, string> arguments,
            IEnumerable<IEscapeSequenceMatcher> escapeMatchers)
        {
            _escapeMatchers = escapeMatchers
                .ThrowIfNull(new ArgumentNullException(nameof(escapeMatchers)))
                .ToImmutableDictionary(m => m.EscapeDelimiter, m => m);

            Arguments = arguments
                .ThrowIfNull(new ArgumentNullException(nameof(arguments)))
                .ThrowIfAny(
                    kvp => Production.SymbolPattern.IsMatch(kvp.Key),
                    kvp => new ArgumentException($"Invalid key: {kvp.Key}"))
                .ThrowIfAny(
                    kvp => string.IsNullOrEmpty(kvp.Value),
                    kvp => new ArgumentException($"Invalid arg: value missing for arg '{kvp.Key}'"))
                .ApplyTo(ImmutableDictionary.CreateRange);

            ValidateArgs();

            var (startDelim, endDelim) = ExtractDelimiters();
            _startDelimiter = startDelim;
            _endDelimiter = endDelim;

            var (legalSequences, legalRanges, illegalSequences, illegalRanges) = ExtractSequences();
            _illegalSequences = illegalSequences.ToImmutableArray();
            _illegalRanges = illegalRanges.ToImmutableHashSet();
            _legalSequences = legalSequences.ToImmutableArray();
            _legalRanges = legalRanges.ToImmutableHashSet();
        }

        public bool TryRecognize(TokenReader reader, ProductionPath productionPath, out IResult<ICSTNode> result)
        {
            throw new NotImplementedException();
        }

        private void ValidateArgs()
        {
            var hasStartDelim = Arguments.ContainsKey(StartDelimiterArgumentKey);
            var hasEndDelim = Arguments.ContainsKey(EndDelimiterArgumentKey);
            var hasDelim = Arguments.ContainsKey(DelimiterArgumentKey);

            if (hasStartDelim ^ hasEndDelim)
                throw new InvalidOperationException(
                    $"Invalid delimiter combination: start:{hasStartDelim}, end:{hasEndDelim}");

            else if (hasDelim && (hasStartDelim || hasEndDelim))
                throw new InvalidOperationException(
                    $"Invalid delimiter combination: start:{hasStartDelim}, end:{hasEndDelim}, delimiter:{hasDelim}");
        }

        private (string startDelim, string endDelim) ExtractDelimiters()
        {
            if (Arguments.TryGetValue(DelimiterArgumentKey, out var delim))
                return (delim, delim);

            else
                return (
                    Arguments[StartDelimiterArgumentKey],
                    Arguments[EndDelimiterArgumentKey]);
        }

        private (
            HashSet<string> legalSequences, HashSet<CharRange> legalRanges,
            HashSet<string> illegalSequences, HashSet<CharRange> illegalRanges) ExtractSequences()
        {
            var legalSequences = Arguments.TryGetValue(LegalSequencesArgumentKey, out var value)
                    ? ExtractSequences(value)
                    : (new HashSet<string>(), new HashSet<CharRange>());

            var illegalSequences = Arguments.TryGetValue(LegalSequencesArgumentKey, out value)
                    ? ExtractSequences(value)
                    : (new HashSet<string>(), new HashSet<CharRange>());

            return (
                legalSequences.Item1, legalSequences.Item2,
                illegalSequences.Item1, illegalSequences.Item2);
        }

        private static (HashSet<string> sequences, HashSet<CharRange> ranges) ExtractSequences(string sequences)
        {
            if (!SequenceParser.TryParseSequences(sequences, out var sequenceResult))
                throw new FormatException("", sequenceResult.AsError().ActualCause());

            else
            {
                return sequenceResult
                    .Resolve()
                    .Aggregate((seq: new HashSet<string>(), ranges: new HashSet<CharRange>()), (lists, seq) =>
                    {
                        if (seq is CharRange cr)
                            lists.ranges.Add(cr);

                        else if (seq is string s)
                            lists.seq.Add(s);

                        return lists;
                    });
            }
        }

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

        public readonly struct CharRange
        {
            public readonly char Start { get; }

            public readonly char End { get; }

            public CharRange(char start, char end)
            {
                Start = start;
                End = end;

                if (End < Start)
                    throw new ArgumentOutOfRangeException(nameof(end));
            }

            public bool Contains(char c) => Start <= c && End >= c;

            public override int GetHashCode() => HashCode.Combine(Start, End);

            public override bool Equals(object? obj)
            {
                return obj is CharRange other
                    && Start.Equals(other.Start)
                    && End.Equals(other.End);
            }

            public override string ToString() => Start != End
                ? $"{Start}-{End}"
                : Start.ToString();

            public static bool operator ==(CharRange left, CharRange right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(CharRange left, CharRange right)
            {
                return !(left == right);
            }
        }

        internal static class SequenceParser
        {
            internal static bool TryParseSequences(
                TokenReader reader,
                out IResult<IEnumerable<object>> sequencesResult)
            {
                ArgumentNullException.ThrowIfNull(reader);
                var productionPath = ProductionPath.Of("sequences");

                var position = reader.Position;
                var sequences = new List<object>();
                _ = TryParseWhitespaces(reader, productionPath, out _);

                if (TryParseRange(reader, productionPath, out var rangeResult))
                    sequences.Add(rangeResult.Resolve());

                else if (TryParseSequence(reader, productionPath, out var sequenceResult))
                    sequences.Add(rangeResult.Resolve());

                else
                {
                    sequencesResult = Errors.UnrecognizedTokens
                        .Of(productionPath, position)
                        .ApplyTo(Result.Of<IEnumerable<object>>);
                    return false;
                }

                // remaining, comma-separated sequences
                while (true)
                {
                    // consume whitepsaces
                    _ = TryParseWhitespaces(reader, productionPath, out _);

                    // comma
                    if (!reader.TryGetToken(out var commaToken))
                        break;

                    else if (',' != commaToken[0])
                    {
                        sequencesResult = Errors.PartiallyRecognizedTokens
                            .Of(productionPath, position, Tokens.Empty)
                            .ApplyTo(Result.Of<IEnumerable<object>>);
                        return false;
                    }

                    // consume whitespaces
                    _ = TryParseWhitespaces(reader, productionPath, out _);

                    if (TryParseRange(reader, productionPath, out rangeResult))
                        sequences.Add(rangeResult.Resolve());

                    else if (TryParseSequence(reader, productionPath, out var sequenceResult))
                        sequences.Add(rangeResult.Resolve());

                    else
                    {
                        sequencesResult = Errors.PartiallyRecognizedTokens
                            .Of(productionPath, position, Tokens.Empty)
                            .ApplyTo(Result.Of<IEnumerable<object>>);
                        return false;
                    };
                }

                sequencesResult = Result.Of<IEnumerable<object>>(sequences);
                return true;
            }

            internal static bool TryParseRange(
                TokenReader reader,
                ProductionPath parentPath,
                out IResult<CharRange> rangeResult)
            {
                ArgumentNullException.ThrowIfNull(reader);
                ArgumentNullException.ThrowIfNull(parentPath);

                var position = reader.Position;
                var productionPath = parentPath.Next("char-range");

                if (!reader.TryGetToken(out var delimToken)
                    || delimToken[0] != '[')
                {
                    rangeResult = Errors.UnrecognizedTokens
                        .Of(productionPath, position)
                        .ApplyTo(Result.Of<CharRange>);
                    return false;
                }

                if (!TryParseChar(reader, productionPath, out var startCharTokenResult))
                {
                    rangeResult = Errors.PartiallyRecognizedTokens
                        .Of(productionPath, position, delimToken)
                        .ApplyTo(Result.Of<CharRange>);
                    return false;
                }

                if (!reader.TryGetToken(out var dashToken)
                    || dashToken[0] != '-')
                {
                    rangeResult = startCharTokenResult
                        .Map(start => delimToken
                            .CombineWith(start.Tokens))
                        .Map(tokens => Errors.PartiallyRecognizedTokens
                            .Of(productionPath, position, tokens))
                        .MapAs<CharRange>();
                    return false;
                }

                if(!TryParseChar(reader, productionPath, out var endCharTokenResult))
                {
                    rangeResult = startCharTokenResult
                        .Map(start => delimToken
                            .CombineWith(start.Tokens)
                            .CombineWith(dashToken))
                        .Map(tokens => Errors.PartiallyRecognizedTokens
                            .Of(productionPath, position, tokens))
                        .MapAs<CharRange>();
                    return false;
                }

                if (!reader.TryGetToken(out delimToken)
                    || delimToken[0] != ']')
                {
                    rangeResult = startCharTokenResult
                        .Combine(endCharTokenResult, (start, end) => (start, end))
                        .Map(tuple => delimToken
                            .CombineWith(tuple.start.Tokens)
                            .CombineWith(dashToken)
                            .CombineWith(tuple.end.Tokens))
                        .Map(tokens => Errors.PartiallyRecognizedTokens
                            .Of(productionPath, position, tokens))
                        .MapAs<CharRange>();
                    return false;
                }

                rangeResult = startCharTokenResult.Combine(
                    endCharTokenResult,
                    (start, end) => new CharRange(start.Char, end.Char));
                return rangeResult.IsDataResult();
            }

            internal static bool TryParseSequence(
                TokenReader reader,
                ProductionPath parentPath,
                out IResult<string> sequenceResult)
            {
                ArgumentNullException.ThrowIfNull(reader);
                var productionPath = parentPath.Next("sequence");

                var position = reader.Position;
                var sb = new StringBuilder();
                IResult<(char Char, Tokens Tokens)> charResult;
                while (TryParseChar(reader, productionPath, out charResult))
                {
                    sb.Append(charResult.Resolve().Char);
                }

                if (sb.Length > 0)
                {
                    sequenceResult = Result.Of(sb.ToString());
                    return true;
                }

                reader.Reset(position);
                sequenceResult = charResult.MapAs<string>();
                return false;
            }

            internal static bool TryParseChar(
                TokenReader reader,
                ProductionPath parentPath,
                out IResult<(char Char, Tokens Tokens)> charResult)
            {
                ArgumentNullException.ThrowIfNull(reader);
                ArgumentNullException.ThrowIfNull(parentPath);

                var position = reader.Position;
                var productionPath = parentPath.Next("character");
                charResult = Result
                    .Of(() => reader.GetTokens(1, true))
                    .Map(token =>
                    {
                        var @char = token[0];
                        if (@char == '\\')
                        {
                            var escapeToken = reader.GetTokens(1, true);
                            var escapeChar = escapeToken[0];

                            if (char.ToLower(escapeChar) == 'u')
                                return reader
                                    .GetTokens(4, true)
                                    .ApplyTo(_tokens =>
                                    {
                                        if (!ushort.TryParse(
                                            _tokens.AsSpan(),
                                            NumberStyles.HexNumber,
                                            null, out var value))
                                            throw new Errors.PartiallyRecognizedTokens(
                                                productionPath, position, token.CombineWith(escapeToken));

                                        return (
                                            @char: value,
                                            tokens: _tokens);
                                    })
                                    .ApplyTo(_value => (
                                        (char)_value.@char,
                                        token.CombineWith(escapeToken).CombineWith(_value.tokens)));

                            else if (char.ToLower(escapeChar) == 'x')
                                return reader
                                    .GetTokens(2, true)
                                    .ApplyTo(_tokens =>
                                    {
                                        if (!byte.TryParse(
                                            _tokens.AsSpan(),
                                            NumberStyles.HexNumber,
                                            null, out var value))
                                            throw new Errors.PartiallyRecognizedTokens(
                                                productionPath, position, token.CombineWith(escapeToken));

                                        return (
                                            @char: value,
                                            tokens: _tokens);
                                    })
                                    .ApplyTo(_value => (
                                        (char)_value.@char,
                                        token.CombineWith(escapeToken).CombineWith(_value.tokens)));

                            else if (escapeChar == '\\')
                                return ('\\', token.CombineWith(escapeToken));

                            else if (escapeChar == ',')
                                return (',', token.CombineWith(escapeToken));

                            else if (escapeChar == '[')
                                return ('[', token.CombineWith(escapeToken));

                            else if (escapeChar == ']')
                                return (']', token.CombineWith(escapeToken));
                        }

                        else if (@char == ','
                            || @char == '['
                            || @char == ']'
                            || char.IsWhiteSpace(@char))
                            throw new FormatException($"Invalid sequence char: {@char}");

                        return (@char, token);
                    });

                if (charResult.IsErrorResult())
                    reader.Reset(position);

                return charResult.IsDataResult();
            }

            internal static bool TryParseWhitespaces(
                TokenReader reader,
                ProductionPath parentPath,
                out IResult<Tokens> tokensResult)
            {
                ArgumentNullException.ThrowIfNull(reader);
                ArgumentNullException.ThrowIfNull(parentPath);

                Tokens tokens = Tokens.Empty;
                var productionPath = parentPath.Next("whitespaces");
                var position = reader.Position;
                while (reader.TryGetToken(out var token))
                {
                    if (char.IsWhiteSpace(token[0]))
                        tokens = tokens.CombineWith(token);

                    else
                    {
                        reader.Back();
                        break;
                    }
                }

                tokensResult = tokens.Length > 0
                    ? Result.Of(tokens)
                    : Result.Of<Tokens>(Errors.UnrecognizedTokens.Of(productionPath, position));

                return tokensResult.IsDataResult();
            }
        }

        #endregion
    }
}
