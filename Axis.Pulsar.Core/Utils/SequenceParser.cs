namespace Axis.Pulsar.Core.Utils;

using System.Globalization;
using System.Text;
using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;


/// <summary>
/// Parses a comma-separated list of strings or ranges.
/// <para/>PS: Move this type into the language project
/// </summary>
internal static class SequenceParser
{
    /*
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
    */
}
