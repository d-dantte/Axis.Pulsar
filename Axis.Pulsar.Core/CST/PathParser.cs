using Axis.Luna.Common.Results;
using Axis.Luna.Common.Unions;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.CST
{
    using FailedError = FailedRecognitionError;
    using PartialError = PartialRecognitionError;

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

        public static bool TryParse(string pathText, out IResult<NodePath> result)
        {
            if (string.IsNullOrEmpty(pathText))
                throw new ArgumentException("Invalid path: null/empty");

            _ = TryRecognizePath(pathText, out var pathResult);
            result = pathResult.MapMatch(
                data => Result.Of(data),
                _ => PathParser
                    .ToFormatException(pathResult)
                    .ApplyTo(Result.Of<NodePath>),
                _ => PathParser
                    .ToFormatException(pathResult)
                    .ApplyTo(Result.Of<NodePath>));

            return result.IsDataResult();
        }

        public static NodePath Parse(string pathText)
        {
            if (string.IsNullOrEmpty(pathText))
                throw new ArgumentException("Invalid path: null/empty");

            _ = TryRecognizePath(pathText, out var result);
            return result.Is(out NodePath path) ? path : ToFormatException(result).Throw<NodePath>();
        }

        #region Recognizers

        internal static bool TryRecognizeTokens(
            TokenReader reader,
            SymbolPath parentPath,
            object context,
            out PathParserResult<Tokens> result)
        {
            var position = reader.Position;
            var tokensPath = parentPath.Next("tokens");

            #region open delimiter
            if (!reader.TryGetToken(out var openDelim)
                || !'<'.Equals(openDelim[0]))
            {
                reader.Reset(position);
                result = FailedRecognitionError
                    .Of(tokensPath, position)
                    .ApplyTo(cause => PathParserResult<Tokens>.Of(cause));
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
                            .ApplyTo(cause => PathParserResult<Tokens>.Of(cause));
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
                    .ApplyTo(cause => PathParserResult<Tokens>.Of(cause));
                return false;
            }
            #endregion

            result = PathParserResult<Tokens>.Of(tokens);
            return true;
        }

        internal static bool TryRecognizeSymbolName(
            TokenReader reader,
            SymbolPath parentPath,
            object context,
            out PathParserResult<Tokens> result)
        {
            var position = reader.Position;
            var symbolNamePath = parentPath.Next("symbol-name");

            #region delimiter
            var hasDelimiter = false;

            if (!reader.TryGetToken(out var delimiter))
            {
                result = FailedRecognitionError
                    .Of(symbolNamePath, position)
                    .ApplyTo(cause => PathParserResult<Tokens>.Of(cause));
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
                    .ApplyTo(cause => PathParserResult<Tokens>.Of(cause));
                return false;
            }
            else hasDelimiter = true;
            #endregion

            #region Name
            if (!reader.TryGetPattern(Production.SymbolPattern, out var symbolName))
            {
                if (!hasDelimiter)
                    result = FailedRecognitionError
                        .Of(symbolNamePath, position)
                        .ApplyTo(cause => PathParserResult<Tokens>.Of(cause));

                else result = PartialRecognitionError
                    .Of(symbolNamePath,
                        position,
                        delimiter.Segment.EndOffset - position + 1)
                    .ApplyTo(cause => PathParserResult<Tokens>.Of(cause));

                reader.Reset(position);
                return false;
            }
            #endregion

            result = PathParserResult<Tokens>.Of(symbolName);
            return true;
        }

        internal static bool TryRecognizeFilterType(
            TokenReader reader,
            SymbolPath parentPath,
            object context,
            out PathParserResult<NodeType> result)
        {
            var position = reader.Position;
            var filterTypePath = parentPath.Next("filter-type");

            #region delimiter
            if (!reader.TryGetToken(out var delim))
            {
                result = FailedRecognitionError
                    .Of(filterTypePath, position)
                    .ApplyTo(cause => PathParserResult<NodeType>.Of(cause));
                return false;
            }

            if (!'@'.Equals(delim[0]))
            {
                reader.Back();
                result = PathParserResult<NodeType>.Of(NodeType.Unspecified);
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
                    .ApplyTo(cause => PathParserResult<NodeType>.Of(cause)); ;
                return false;
            }
            #endregion

            result = PathParserResult<NodeType>.Of((NodeType)char.ToUpper(typeChar[0]));
            return true;
        }

        internal static bool TryRecognizeFilter(
            TokenReader reader,
            SymbolPath parentPath,
            object context,
            out PathParserResult<NodeFilter> result)
        {
            var position = reader.Position;
            var filterPath = parentPath.Next("filter");
            var accumulatorArgs = NodeRecognitionAccumulator.Args(
                reader,
                filterPath,
                (object)null!);

            result = NodeRecognitionAccumulator
                .Of<(NodeType type, Tokens name, Tokens tokens), SymbolPath, object>(default)

                // filter type
                .ThenTry<NodeType, PathParserResult<NodeType>>(
                    TryRecognizeFilterType,
                    accumulatorArgs,
                    (data, ttype) => (type: ttype, data.name, data.tokens))

                // symbol name
                .ThenTry<Tokens, PathParserResult<Tokens>>(
                    TryRecognizeSymbolName,
                    accumulatorArgs,
                    (data, name) => (data.type, name, data.tokens),
                    (data, fre) => data)

                // tokens
                .ThenTry<Tokens, PathParserResult<Tokens>>(
                    TryRecognizeTokens,
                    accumulatorArgs,
                    (data, tokens) => (data.type, data.name, tokens),
                    (data, fre) => data)

                // errors?
                .MapAll(

                    // data
                    filterInfo => NodeFilter
                        .Of(filterInfo.type,
                            filterInfo.name,
                            filterInfo.tokens)
                        .ApplyTo(PathParserResult<NodeFilter>.Of),

                    // failed recognition
                    (fre, list) => PathParserResult<NodeFilter>.Of(fre),

                    // partial recognition
                    (pre, list) => PathParserResult<NodeFilter>.Of(pre));

            return result.Is(out NodeFilter _);
        }

        internal static bool TryRecognizeSegment(
            TokenReader reader,
            SymbolPath parentPath,
            object context,
            out PathParserResult<PathSegment> result)
        {
            var position = reader.Position;
            var segmentPath = parentPath.Next("segment");
            var accumulatorArgs = NodeRecognitionAccumulator.Args(
                reader,
                segmentPath,
                (object)null!);

            var accumulator = NodeRecognitionAccumulator
                .Of<List<NodeFilter>, SymbolPath, object>(new List<NodeFilter>())

                // filter
                .ThenTry<NodeFilter, PathParserResult<NodeFilter>>(
                    TryRecognizeFilter,
                    accumulatorArgs,
                    (filters, filter) => filters.AddItem(filter));

            while (reader.TryGetTokens("|", out var filterSeparator))
            {
                accumulator = accumulator.ThenTry<NodeFilter, PathParserResult<NodeFilter>>(
                    TryRecognizeFilter,
                    accumulatorArgs,
                    (filters, filter) => filters.AddItem(filter));
            }

            result = accumulator.MapAll(

                // data
                list => list
                    .ToArray()
                    .ApplyTo(PathSegment.Of)
                    .ApplyTo(PathParserResult<PathSegment>.Of),

                // failed recognition
                (fre, list) => list.Count < 1
                    ? PathParserResult<PathSegment>.Of(fre)
                    : PartialRecognitionError
                        .Of(segmentPath,
                            position,
                            reader.Position - position + 1)
                        .ApplyTo(PathParserResult<PathSegment>.Of),

                // partial recognition
                (pre, list) => PathParserResult<PathSegment>.Of(pre));

            return result.Is(out PathSegment _);
        }

        internal static bool TryRecognizePath(
            TokenReader reader,
            out PathParserResult<NodePath> result)
        {
            var position = reader.Position;
            var nodePath = SymbolPath.Of("node-path");
            var accumulatorArgs = NodeRecognitionAccumulator.Args(
                reader,
                nodePath,
                (object)null!);

            var accumulator = NodeRecognitionAccumulator.Of<
                List<PathSegment>,
                SymbolPath,
                object>(
                new List<PathSegment>())

                // segment
                .ThenTry<PathSegment, PathParserResult<PathSegment>>(
                    TryRecognizeSegment,
                    accumulatorArgs,
                    (segments, segment) => segments.AddItem(segment));

            while (reader.TryGetTokens("/", out var segmentSeparator))
            {
                accumulator = accumulator.ThenTry<PathSegment, PathParserResult<PathSegment>>(
                    TryRecognizeSegment,
                    accumulatorArgs,
                    (segments, segment) => segments.AddItem(segment));
            }

            result = accumulator.MapAll(

                // data
                list => list
                    .ToArray()
                    .ApplyTo(NodePath.Of)
                    .ApplyTo(PathParserResult<NodePath>.Of),

                // failed recognition
                (fre, list) => list.Count < 1
                    ? PathParserResult<NodePath>.Of(fre) 
                    : PartialRecognitionError
                        .Of(nodePath,
                            position,
                            reader.Position - position + 1)
                        .ApplyTo(PathParserResult<NodePath>.Of),
                
                // partial recognition
                (pre, list) => PathParserResult<NodePath>.Of(pre));

            return result.Is(out NodePath _);
        }

        #endregion

        private static FormatException ToFormatException(PathParserResult<NodePath> errorResult)
        {
            ArgumentNullException.ThrowIfNull(errorResult);

            INodeRecognitionError? error =
                errorResult.Is(out FailedRecognitionError fre) ? fre :
                errorResult.Is(out PartialRecognitionError pre) ? pre :
                null;

            return error switch
            {
                FailedRecognitionError fe => new FormatException(
                    $"Invalid path format: error detected at position {fe.TokenSegment.Offset}"),
                PartialRecognitionError pe => new FormatException(
                    $"Invalid path format: error detected at position {pe.TokenSegment.Offset}"),
                _ => throw new InvalidOperationException($"Invalid error result: {errorResult}")
            };
        }
    }

    public readonly struct PathParserResult<TResult> :
        INodeRecognitionResultBase<TResult, PathParserResult<TResult>>,
        IUnionOf<TResult, FailedError, PartialError, PathParserResult<TResult>>
    {
        private readonly object? _value;

        object IUnion<TResult, FailedError, PartialError, PathParserResult<TResult>>.Value => _value!;


        public PathParserResult(object value)
        {
            _value = value switch
            {
                null => null,
                FailedRecognitionError
                or PartialError
                or TResult => value,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(value),
                    $"Invalid {nameof(value)} type: '{value.GetType()}'")
            };
        }

        public static PathParserResult<TResult> Of(
            TResult value)
            => new(value!);

        public static PathParserResult<TResult> Of(
            FailedError value)
            => new(value);

        public static PathParserResult<TResult> Of(
            PartialError value)
            => new(value);


        public bool Is(out TResult value)
        {
            if (_value is TResult n)
            {
                value = n;
                return true;
            }

            value = default!;
            return false;
        }

        public bool Is(out FailedError value)
        {
            if (_value is FailedError n)
            {
                value = n;
                return true;
            }

            value = default!;
            return false;
        }

        public bool Is(out PartialError value)
        {
            if (_value is PartialError n)
            {
                value = n;
                return true;
            }

            value = default!;
            return false;
        }

        public bool IsNull() => _value is null;

        public TOut MapMatch<TOut>(
            Func<TResult, TOut> nodeMapper,
            Func<FailedError, TOut> failedErrorMapper,
            Func<PartialError, TOut> partialErrorMapper,
            Func<TOut> nullMapper = null!)
        {
            ArgumentNullException.ThrowIfNull(nodeMapper);
            ArgumentNullException.ThrowIfNull(failedErrorMapper);

            if (_value is TResult t1)
                return nodeMapper.Invoke(t1);

            if (_value is FailedError t2)
                return failedErrorMapper.Invoke(t2);

            if (_value is PartialError t3)
                return partialErrorMapper.Invoke(t3);

            // unknown type, assume null
            return nullMapper switch
            {
                null => default!,
                _ => nullMapper.Invoke()
            };
        }

        public void ConsumeMatch(
            Action<TResult> resultConsumer,
            Action<FailedError> failedErrorConsumer,
            Action<PartialError> partialErrorConsumer,
            Action nullConsumer = null!)
        {
            ArgumentNullException.ThrowIfNull(resultConsumer);
            ArgumentNullException.ThrowIfNull(failedErrorConsumer);
            ArgumentNullException.ThrowIfNull(partialErrorConsumer);

            if (_value is TResult t1)
                resultConsumer.Invoke(t1);

            else if (_value is FailedError t2)
                failedErrorConsumer.Invoke(t2);

            else if (_value is PartialError t3)
                partialErrorConsumer.Invoke(t3);

            else if (_value is null && nullConsumer is not null)
                nullConsumer.Invoke();
        }

        public PathParserResult<TResult> WithMatch(
            Action<TResult> resultConsumer,
            Action<FailedError> failedErrorConsumer,
            Action<PartialError> partialErrorConsumer,
            Action nullConsumer = null!)
        {
            ConsumeMatch(resultConsumer, failedErrorConsumer, partialErrorConsumer, nullConsumer);
            return this;
        }
    }
}
