using Axis.Luna.Common.Automata.Sync;
using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Misc.Pulsar.Utils;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Exceptions;
using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

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
        private string[] _orderedMatcherDelimiters;

        public ImmutableDictionary<string, IEscapeSequenceMatcher> EscapeMatchers { get; }
        public ImmutableHashSet<Tokens> IllegalSequences { get; }
        public ImmutableHashSet<Tokens> LegalSequences { get; }
        public ImmutableHashSet<CharRange> IllegalRanges { get; }
        public ImmutableHashSet<CharRange> LegalRanges { get; }
        public string StartDelimiter { get; }
        public string EndDelimiter { get; }
        public bool AcceptsEmptyString{ get; }

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

        internal bool TryRecognizeString(TokenReader reader, out Tokens tokens)
        {
            var position = reader.Position;
            tokens = Tokens.Empty;

            // build sequence matchers
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
                #region Ranges
                // illegal ranges
                if (IllegalRanges.Any(range => range.Contains(token[0])))
                    return false;

                // legal ranges
                if (!LegalRanges.IsEmpty
                    && !LegalRanges.Any(range => range.Contains(token[0])))
                    return false;
                #endregion

                #region Sequences
                // escape sequences
                foreach (var matcher in escapeDelimMatchers)
                {
                    if (matcher.TryNextWindow(out var isMatch) && isMatch)
                    {
                        var escapeDelimString = matcher.MatchSequence.ToString()!;
                        var escapeMatcher = EscapeMatchers[escapeDelimString];

                        if (!escapeMatcher.TryMatchEscapeArgument(reader, out var escapeArgs))
                            return false;
                        
                        token = tokens.CombineWith(token).CombineWith(escapeArgs.Resolve());
                    }
                }

                // illegal sequences

                // legal sequences
                #endregion
            }

            reader.Reset(position);
            return false;
        }

        #endregion

        #region State Machine Implementation
        private static StateMachine<RecognitionContext> CreateStateMachine(RecognitionContext context)
        {
            var startDelimiterState = new LambdaState<RecognitionContext>(
                StateNames.StartDelimiter.ToString(),
                RecognizeStartDelimiter);

            var endDelimiterState = new LambdaState<RecognitionContext>(
                StateNames.EndDelimiter.ToString(),
                RecognizeEndDelimiter);

            var stringCharacterState = new LambdaState<RecognitionContext>(
                StateNames.StringCharacters.ToString(),
                RecognizeStringCharacters);

            var escapeCharacterState = new LambdaState<RecognitionContext>(
                StateNames.EscapeCharacters.ToString(),
                RecognizeEscapeCharacters);

            return new StateMachine<RecognitionContext>(
                context,
                StateNames.StartDelimiter.ToString(),
                startDelimiterState,
                endDelimiterState,
                stringCharacterState,
                escapeCharacterState);
        }

        private static string? RecognizeStartDelimiter(RecognitionContext context)
        {
            var position = context.TokenReader.Position;
            if (!context.TokenReader.TryGetTokens(
                context.Rule.StartDelimiter.Length,
                out var tokens)
                || !tokens.Equals(context.Rule.StartDelimiter))
            {
                context.TokenReader.Reset(position);
                context.Error = UnrecognizedTokens.Of(
                    context.ProductionPath,
                    position);

                return null;
            }

            context.AppendTokens(tokens);
            return StateNames.StringCharacters.ToString();
        }

        private static string? RecognizeEndDelimiter(RecognitionContext context)
        {
            var position = context.TokenReader.Position;
            if (!context.TokenReader.TryGetTokens(
                context.Rule.EndDelimiter.Length,
                out var tokens)
                || !tokens.Equals(context.Rule.EndDelimiter))
            {
                context.TokenReader.Reset(position);
                context.Error = PartiallyRecognizedTokens.Of(
                    context.ProductionPath,
                    position,
                    context.Tokens);

                return null;
            }
            else context.AppendTokens(tokens);

            return null;
        }

        private static string? RecognizeStringCharacters(RecognitionContext context)
        {
            var tokenCount = context.LegalSequences.OrderedSequenceLengths[0];
            while (true)
            {
                // read tokens
                var position = context.TokenReader.Position;
                if (!context.TokenReader.TryGetTokens(tokenCount, false, out var tokens))
                {
                    context.Error = PartiallyRecognizedTokens.Of(
                        context.ProductionPath,
                        position,
                        context.Tokens);

                    return null;
                }

                // legal tokens
                if (!TryFindLegalSequence(context, tokens, out var legalSequenceTokens))
                {
                    context.TokenReader.Back(tokens.Count);
                    return StateNames.EscapeCharacters.ToString();
                }

                // reset any excess tokens
                if (tokens.Count > legalSequenceTokens.Count)
                {
                    context.TokenReader.Back(tokens.Count - legalSequenceTokens.Count);
                    tokens = legalSequenceTokens;
                }

                // illegal sequence?
                if (TryFindIllegalSequence(context, tokens, out var illegalSequenceTokens))
                {
                    context.TokenReader.Back(illegalLength);
                    context.TokenBuffer.RemoveLast(illegalLength - tokens.Count);
                    return StateNames.EscapeCharacters.ToString();
                }

                // finally, add the tokens
                _ = context.TokenBuffer.Append(tokens);
            }
        }

        private static string RecognizeEscapeCharacters(RecognitionContext context)
        {
            var position = context.TokenReader.Position;
            var index = context.TokenBuffer.Length;

            // no escape matchers?
            if (context.Rule.EscapeMatchers.Count <= 0)
                return StateNames.EndDelimiter.ToString();

            // read escape delimiter
            var delimLengths = context.Rule.EscapeMatchers.Keys
                .Select(delim => delim.Length)
                .OrderByDescending(l => l)
                .Distinct()
                .ToArray();

            if (context.TokenReader.TryGetTokens(delimLengths[0], out var delimTokens, false)
                && delimTokens.Length <= 0)
            {
                context.Result = new FailureResult(
                    context.TokenReader.Position + 1,
                    IReason.Of("EOF error. Expected escape tokens."));
                context.TokenReader.Reset(position);
                return null;
            }

            // find the matcher
            var matcher = delimLengths
                .Select(length => context.Rule.EscapeMatchers
                    .TryGetValue(new string(delimTokens[..length]), out var _matcher)
                    ? _matcher : null)
                .FirstOrDefault(m => m is not null);

            if (matcher is null)
            {
                context.TokenReader.Reset(position);
                return StateNames.EndDelimiter.ToString();
            }

            while (true)
            {
                if (matcher.TryMatchEscapeArgument(context.TokenReader, out var argTokens))
                {
                    context.TokenBuffer.Append(delimTokens).Append(argTokens);
                    context.EscapeSpan = new EscapeSpan(index, delimTokens.Length + argTokens.Length);
                    return StateNames.StringCharacters.ToString();
                }
                else // fail
                {
                    var failedEscape = new StringBuilder()
                        .Append(delimTokens)
                        .Append(argTokens);

                    context.Result = new FailureResult(
                        context.TokenReader.Position + 1,
                        IReason.Of($"Invalid escape characters: {failedEscape}"));
                    context.TokenReader.Reset(position);
                    return null;
                }
            }
        }

        /// <summary>
        /// verifies that the given <paramref name="tokens"/> contains at least one subset that is present in the LegalSequence set.
        /// </summary>
        /// <param name="context">the context</param>
        /// <param name="tokens">the tokens</param>
        /// <param name="legalSequenceTokens">the legal token sequence found</param>
        /// <returns>true if a subset is found in LegalSequence, false otherwise</returns>
        private static bool TryFindLegalSequence(
            RecognitionContext context,
            Tokens tokens,
            out Tokens legalSequenceTokens)
        {
            foreach (var length in context.LegalSequences.OrderedSequenceLengths)
            {
                legalSequenceTokens = tokens[..length];
                if (context.LegalSequences.Matches(legalSequenceTokens))
                    return true;
            }

            legalSequenceTokens = default;
            return false;
        }

        /// <summary>
        /// verifies that the given concatenation of the <c>context.TokenBuffer</c> and the<paramref name="tokens"/> contains at least one
        /// right-most subset that is present in the IllegalSequence set.
        /// <para>
        /// Note that concatenation begins from the end of the last escape sequence in the buffer, or the beginning of the buffer.
        /// </para>
        /// </summary>
        /// <param name="context">the context</param>
        /// <param name="tokens">the tokens</param>
        /// <returns>true if no illegal subsets are found, false otherwise</returns>
        private static bool TryFindIllegalSequence(
            RecognitionContext context,
            Tokens tokens,
            out Tokens illegalSequenceTokens)
        {
            var lastEscapeEndIndex =
                (context.EscapeSpan?.Index ?? 0)
                + (context.EscapeSpan?.Length ?? 0);

            var tbuff = context.TokenBuffer.ToString(lastEscapeEndIndex..) + new string(tokens);

            foreach (var length in context.IllegalSequences.OrderedSequenceLengths)
            {
                if (tbuff.Length < length)
                    continue;

                var index = tbuff.Length - length;
                var potentialIllegalSequence = tbuff[index..];

                if (context.IllegalSequences.Matches(potentialIllegalSequence))
                {
                    illegalLength = length;
                    return true;
                }
            }

            illegalLength = -1;
            return false;
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


        public class SequenceMatcher
        {
            private int _cursor;
            private RollingHash.Hash _matchSequenceHash;
            private RollingHash? _sourceRollingHash;

            public Tokens MatchSequence { get; }

            public string Source { get; }

            public int StartOffset { get; }


            public SequenceMatcher(Tokens sequence, string source, int startOffset)
            {
                MatchSequence = sequence.ThrowIf(
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
                _matchSequenceHash = RollingHash.ComputeHash(
                    MatchSequence.Source,
                    MatchSequence.Offset,
                    MatchSequence.Count);
            }

            /// <summary>
            /// Consumes the next token from the source, from the current offset, then attempts to match the new window with the <see cref="SequenceMatcher.MatchSequence"/>.
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
                if (_cursor - StartOffset < MatchSequence.Count)
                    return true;

                if(_sourceRollingHash is null)
                {
                    _sourceRollingHash = new RollingHash(Source, StartOffset, MatchSequence.Count);
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


        /// <summary>
        /// Parses a comma-separated list of strings or ranges.
        /// <para/>PS: Move this type into the language project
        /// </summary>
        internal static class SequenceParser
        {
            internal static bool TryParseSequences(
                TokenReader reader,
                out IResult<(HashSet<string> Sequences, HashSet<CharRange> Ranges)> sequencesResult)
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
                    sequencesResult = UnrecognizedTokens
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
                        sequencesResult = PartiallyRecognizedTokens
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
                        sequencesResult = PartiallyRecognizedTokens
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
                    rangeResult = UnrecognizedTokens
                        .Of(productionPath, position)
                        .ApplyTo(Result.Of<CharRange>);
                    return false;
                }

                if (!TryParseChar(reader, productionPath, out var startCharTokenResult))
                {
                    rangeResult = PartiallyRecognizedTokens
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
                        .Map(tokens => PartiallyRecognizedTokens
                            .Of(productionPath, position, tokens))
                        .MapAs<CharRange>();
                    return false;
                }

                if (!TryParseChar(reader, productionPath, out var endCharTokenResult))
                {
                    rangeResult = startCharTokenResult
                        .Map(start => delimToken
                            .CombineWith(start.Tokens)
                            .CombineWith(dashToken))
                        .Map(tokens => PartiallyRecognizedTokens
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
                        .Map(tokens => PartiallyRecognizedTokens
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
                                            throw new PartiallyRecognizedTokens(
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
                                            throw new PartiallyRecognizedTokens(
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

                tokensResult = tokens.Count > 0
                    ? Result.Of(tokens)
                    : Result.Of<Tokens>(UnrecognizedTokens.Of(productionPath, position));

                return tokensResult.IsDataResult();
            }
        }

        #endregion
    }
}
