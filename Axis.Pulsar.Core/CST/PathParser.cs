using Axis.Luna.Common.Results;
using Axis.Luna.Common.Utils;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.CST
{
    /// <summary>
    /// Parse strings into the <see cref="NodePath"/> structure.
    /// <para/>
    /// The <see cref="NodePath"/> syntax takes the form
    /// <code>
    ///     path ::= segment(/segment)*
    ///     segment ::= filter(|filter)*;
    ///     filter ::= @{filter-type}:{symbol-name}&lt;{tokens}&gt;
    ///     filter-type ::= (t|n|u)
    ///     symbol-name ::=identifier
    ///     tokens ::= any string
    /// </code>
    /// </summary>    
    public static class PathParser
    {
        internal static readonly string NodeName_Path = "path";
        internal static readonly string NodeName_Segment = "segment";
        internal static readonly string NodeName_Filter = "filter";
        internal static readonly string NodeName_Filter_Type = "filter-type";
        internal static readonly string NodeName_Symbol_Name = "symbol-name";
        internal static readonly string NodeName_Tokens = "tokens";

        private static readonly HashSet<char> FilterTypeCharacters = Enum
            .GetValues<NodeType>()
            .Select(nt => (char)nt)
            .ToHashSet();

        public static bool TryParse(string pathText, out IRecognitionResult<NodePath> result)
        {
            result = Parse(pathText);
            return result.IsSuccess();
        }

        public static IRecognitionResult<NodePath> Parse(string pathText)
        {
            if (string.IsNullOrEmpty(pathText))
                throw new ArgumentException("Invalid path: null/empty");

            if (!TryRecognizePath(pathText, out var result))
                return RecognitionResult.Of<NodePath>(ToFormatException(result));

            return result;
        }

        #region Recognizers

        internal static bool TryRecognizeTokens(
            TokenReader reader,
            ProductionPath parentPath,
            object context,
            out IRecognitionResult<Tokens> result)
        {
            var position = reader.Position;
            var tokensPath = parentPath.Next("tokens");

            try
            {
                #region open delimiter
                if (!reader.TryGetToken(out var openDelim)
                    || !'<'.Equals(openDelim[0]))
                {
                    reader.Reset(position);
                    result = FailedRecognitionError
                        .Of(tokensPath, position)
                        .ApplyTo(RecognitionResult.Of<Tokens>);
                    return false;
                }
                #endregion

                #region tokens
                var isEscaping = false;
                var tokens = Tokens.Default;
                while (reader.TryGetToken(out var token))
                {
                    if (!isEscaping)
                    {
                        if ('\\'.Equals(token[0]))
                        {
                            tokens += token;
                            isEscaping = true;
                        }
                        else if ('>'.Equals(token[0]))
                        {
                            reader.Back(1);
                            break;
                        }
                        else tokens += token;
                    }
                    else
                    {
                        if ('\\'.Equals(token[0]) || '>'.Equals(token[0]))
                        {
                            tokens += token;
                            isEscaping = false;
                        }
                        else
                        {
                            reader.Reset(position);
                            var newLength = (openDelim.Segment + tokens.Segment).EndOffset - position + 1;
                            result = PartialRecognitionError
                                .Of(tokensPath, position, newLength)
                                .ApplyTo(RecognitionResult.Of<Tokens>);
                            return false;
                        }
                    }
                }
                #endregion

                #region close delimiter
                if (!reader.TryGetToken(out var closeDelim)
                    || !'>'.Equals(closeDelim[0]))
                {
                    reader.Reset(position);
                    var newLength = (openDelim.Segment + tokens.Segment).EndOffset - position + 1;
                    result = PartialRecognitionError
                        .Of(tokensPath, position, newLength)
                        .ApplyTo(RecognitionResult.Of<Tokens>);
                    return false;
                }
                #endregion

                result = RecognitionResult.Of(tokens);
                return true;
            }
            catch (Exception e)
            {
                reader.Reset(position);
                result = RecognitionResult.Of<Tokens>(e);
                return false;
            }
        }

        internal static bool TryRecognizeSymbolName(
            TokenReader reader,
            ProductionPath parentPath,
            object context,
            out IRecognitionResult<Tokens> result)
        {
            var position = reader.Position;
            var symbolNamePath = parentPath.Next("symbol-name");

            try
            {
                #region delimiter
                var hasDelimiter = false;

                if (!reader.TryGetToken(out var delimiter))
                {
                    result = FailedRecognitionError
                        .Of(symbolNamePath, position)
                        .ApplyTo(RecognitionResult.Of<Tokens>);
                    return false;
                }
                else if (char.IsAsciiLetter(delimiter[0]))
                {
                    reader.Back();
                    delimiter = Tokens.EmptyAt(reader.Source, position);
                }
                else if (!':'.Equals(delimiter[0]))
                {
                    reader.Reset(position);
                    result = FailedRecognitionError
                        .Of(symbolNamePath, position)
                        .ApplyTo(RecognitionResult.Of<Tokens>);
                    return false;
                }
                else hasDelimiter = true;
                #endregion

                #region Name
                if (!reader.TryGetPattern(IProduction.SymbolPattern, out var symbolName))
                {
                    if (!hasDelimiter)
                        result = FailedRecognitionError
                            .Of(symbolNamePath, position)
                            .ApplyTo(RecognitionResult.Of<Tokens>);

                    else result = PartialRecognitionError
                        .Of(symbolNamePath,
                            position,
                            delimiter.Segment.EndOffset - position + 1)
                        .ApplyTo(RecognitionResult.Of<Tokens>);

                    reader.Reset(position);
                    return false;
                }
                #endregion

                result = RecognitionResult.Of(symbolName);
                return true;
            }
            catch (Exception e)
            {
                result = RecognitionResult.Of<Tokens>(e);
                return false;
            }
        }

        internal static bool TryRecognizeFilterType(
            TokenReader reader,
            ProductionPath parentPath,
            object context,
            out IRecognitionResult<NodeType> result)
        {
            var position = reader.Position;
            var filterTypePath = parentPath.Next("filter-type");

            try
            {
                #region delimiter
                if (!reader.TryGetToken(out var delim))
                {
                    result = FailedRecognitionError
                        .Of(filterTypePath, position)
                        .ApplyTo(RecognitionResult.Of<NodeType>);
                    return false;
                }

                if (!'@'.Equals(delim[0]))
                {
                    reader.Back();
                    result = RecognitionResult.Of(NodeType.Unspecified);
                    return true;
                }
                #endregion

                #region type
                if (!reader.TryGetToken(out var typeChar)
                    || !FilterTypeCharacters.Contains(char.ToUpper(typeChar[0])))
                {
                    reader.Reset(position);
                    result = PartialRecognitionError
                        .Of(filterTypePath,
                            position,
                            delim.Segment.EndOffset - position + 1)
                        .ApplyTo(RecognitionResult.Of<NodeType>);;
                    return false;
                }
                #endregion

                result = RecognitionResult.Of((NodeType)char.ToUpper(typeChar[0]));
                return true;
            }
            catch (Exception e)
            {
                result = RecognitionResult.Of<NodeType>(e);
                return false;
            }
        }

        internal static bool TryRecognizeFilter(
            TokenReader reader,
            ProductionPath parentPath,
            object context,
            out IRecognitionResult<NodeFilter> result)
        {
            var position = reader.Position;
            var filterPath = parentPath.Next("filter");

            try
            {
                result = ParserAccumulator
                    .Of(reader,
                        filterPath,
                        context,
                        (type: default(NodeType), name: default(Tokens), tokens: default(Tokens)))

                    // filter type
                    .ThenTry<NodeType>(
                        TryRecognizeFilterType,
                        (data, ttype) => (type: ttype, data.name, data.tokens))

                    // symbol name
                    .ThenTry<Tokens>(
                        TryRecognizeSymbolName,
                        (data, name) => (data.type, name, data.tokens),
                        data => data)

                    // tokens
                    .ThenTry<Tokens>(
                        TryRecognizeTokens,
                        (data, tokens) => (data.type, data.name, tokens),
                        data => data)

                    // errors?
                    .TransformError((data, err, recognitionCount) => (err, recognitionCount) switch
                    {
                        (FailedRecognitionError, >= 1) => PartialRecognitionError.Of(
                            filterPath,
                            position,
                            reader.Position - position + 1),

                        _ => err
                    })

                    // map to result
                    .ToResult((Func<(NodeType type, Tokens name, Tokens tokens), NodeFilter>)(data => NodeFilter.Of(
                        data.type,
                        data.name,
                        data.tokens)));

                return result.IsSuccess();
            }
            catch (Exception e)
            {
                result = RecognitionResult.Of<NodeFilter>(e);
                return false;
            }
        }

        internal static bool TryRecognizeSegment(
            TokenReader reader,
            ProductionPath parentPath,
            object context,
            out IRecognitionResult<PathSegment> result)
        {
            var position = reader.Position;
            var segmentPath = parentPath.Next("segment");

            try
            {
                var accumulator = ParserAccumulator
                    .Of(reader,
                        segmentPath,
                        context,
                        new List<NodeFilter>())

                    // filter
                    .ThenTry<NodeFilter>(
                        TryRecognizeFilter,
                        (filters, filter) => filters.AddItem(filter));

                while (reader.TryGetTokens("|", out var filterSeparator))
                {
                    _ = accumulator.ThenTry<NodeFilter>(
                        TryRecognizeFilter,
                        (filters, filter) => filters.AddItem(filter));
                }

                result = accumulator

                    // errors?
                    .TransformError((data, err, recognitionCount) => (err, recognitionCount) switch
                    {
                        (FailedRecognitionError, >= 1) => PartialRecognitionError.Of(
                            segmentPath,
                            position,
                            reader.Position - position + 1),

                        _ => err
                    })

                    // map to result
                    .ToResult(data => PathSegment.Of(data.ToArray()));

                return result.IsSuccess();
            }
            catch (Exception e)
            {
                result = RecognitionResult.Of<PathSegment>(e);
                return false;
            }
        }

        internal static bool TryRecognizePath(
            TokenReader reader,
            out IRecognitionResult<NodePath> result)
        {
            var position = reader.Position;
            var nodePath = ProductionPath.Of("node-path");

            try
            {
                var accumulator = ParserAccumulator
                    .Of(reader,
                        nodePath,
                        new object(),
                        new List<PathSegment>())

                    // segment
                    .ThenTry<PathSegment>(
                        TryRecognizeSegment,
                        (segments, segment) => segments.AddItem(segment));

                while (reader.TryGetTokens("/", out var segmentSeparator))
                {
                    _ = accumulator.ThenTry<PathSegment>(
                        TryRecognizeSegment,
                        (segments, segment) => segments.AddItem(segment));
                }

                result = accumulator

                    // errors?
                    .TransformError((data, err, recognitionCount) => (err, recognitionCount) switch
                    {
                        (FailedRecognitionError, >= 1) => PartialRecognitionError.Of(
                            nodePath,
                            position,
                            reader.Position - position + 1),

                        _ => err
                    })

                    // map to result
                    .ToResult(data => NodePath.Of(data.ToArray()));

                return result.IsSuccess();
            }
            catch (Exception e)
            {
                result = RecognitionResult.Of<NodePath>(e);
                return false;
            }
        }

        #endregion

        private static FormatException ToFormatException(IRecognitionResult<NodePath> errorResult)
        {
            ArgumentNullException.ThrowIfNull(errorResult);

            if (errorResult.IsError(out Exception error))
            {
                return error switch
                {
                    IRecognitionError__ re => new FormatException(
                        $"Invalid path format: error detected at position {re.TokenSegment.Offset}"),
                    Exception e => new FormatException($"Invalid path format: unclassified error", e)
                };
            }

            throw new ArgumentException($"Invalid result: not error-reuslt");
        }
    }
}
