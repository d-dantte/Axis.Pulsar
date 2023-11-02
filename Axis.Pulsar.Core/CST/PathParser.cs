using Axis.Luna.Common.Results;
using Axis.Luna.Common.Utils;
using Axis.Luna.Extensions;
using Axis.Pulsar.Utils;
using Axis.Pulsar.Core.Exceptions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.CST
{
    /// <summary>
    /// Parse strings into the <see cref="Path"/> structure.
    /// <para/>
    /// The <see cref="Path"/> syntax takes the form
    /// <code>
    ///     path ::= segment(/segment)*
    ///     segment ::= @{filter-type}:{symbol-name}&lt;{tokens}&gt;
    ///     filter-type ::= (l|r|p|c|n)
    ///     symbol-name ::=identifier
    ///     tokens ::= any string
    /// </code>
    /// </summary>    
    /*
    public static class PathParser__
    {
        private static readonly HashSet<char> FilterTypeCharacters = new HashSet<char>
        {
            'l', 'r', 'p', 'c', 'n'
        };

        public static IResult<Path> Parse(string path)
        {
            try
            {
                TokenReader reader = path;
                _ = TryParsePath(reader, out var pathResult);

                if (pathResult.IsDataResult() && !reader.IsConsumed)
                    return Result.Of<Path>(new PartiallyRecognizedTokens(
                        ProductionPath.Of("path"),
                        0,
                        pathResult.Resolve().Tokens));

                return pathResult.Map(info => info.Path);
            }
            catch(Exception e)
            {
                return Result.Of<Path>(e switch
                {
                    RecognitionRuntimeError => e,
                    _ => new RecognitionRuntimeError(e)
                });
            }
        }

        public static bool TryParse(
            string path,
            out IResult<Path> result)
            => (result = Parse(path)).IsDataResult();

        internal static bool TryParsePath(
            TokenReader reader,
            out IResult<(Path Path, Tokens Tokens)> result)
        {
            var position = reader.Position;
            var segmentInfoList = new List<(Segment Segment, Tokens Tokens)>();
            var tokenList = new List<Tokens>();
            var productionPath = ProductionPath.Of("path");

            try
            {
                // parse the first segment
                if (TryParseSegment(reader, productionPath, out var segmentResult))
                    segmentResult
                        .Resolve()
                        .With(segmentInfoList.Add)
                        .With(t => tokenList.Add(t.Tokens));

                else
                {
                    reader.Reset(position);
                    var errorResult = segmentResult.AsError();
                    result = segmentResult.AsError().map

                    return false;
                }

                while (true)
                {
                    #region Delimiter
                    // read the delimiter
                    if (!reader.TryGetToken(out var delimToken))
                        break;

                    // delimiter character is invalid
                    if (delimToken[0] != '.' && delimToken[0] != '/')
                    {
                        reader.Back(1);
                        break;
                    }
                    else tokenList.Add(delimToken);
                    #endregion

                    #region Segments
                    // read subsequent segments
                    if (TryParseSegment(reader, productionPath, out segmentResult))
                        segmentResult
                            .Resolve()
                            .With(segmentInfoList.Add)
                            .With(t => tokenList.Add(t.Tokens));

                    else
                    {
                        reader.Reset(position);
                        var errorResult = segmentResult.AsError();
                        result = errorResult.MapPartiallyRecognizedTokens<
                            (Segment, Tokens),
                            (Path, Tokens)>(
                            productionPath,
                            position,
                            segmentInfoList.Select(info => info.Tokens));

                        return false;
                    }
                    #endregion
                }

                var path = Path.Of(segmentInfoList.Select(info => info.Segment));
                var tokens = tokenList.Combine();
                result = Result.Of((path, tokens));

                return true;
            }
            catch (Exception e)
            {
                reader.Reset(position);
                result = Result.Of<(Path, Tokens)>(new RecognitionRuntimeError(e));
                return false;
            }
        }

        internal static bool TryParseSegment(
            TokenReader reader,
            ProductionPath parentPath,
            out IResult<(Segment Segment, Tokens Tokens)> result)
        {
            var position = reader.Position;
            var filterInfoList = new List<(NodeFilter Filter, Tokens Tokens)>();
            var tokenList = new List<Tokens>();
            var segmentPath = parentPath.Next("segment");

            try
            {
                // parse the first filter
                if (TryParseFilter(reader, segmentPath, out var filterInfo))
                    _ = filterInfo
                        .Resolve()
                        .With(filterInfoList.Add)
                        .With(t => tokenList.Add(t.Tokens));

                else
                {
                    reader.Reset(position);
                    var errorResult = filterInfo.AsError();
                    result = errorResult.MapUnrecognizedTokens<
                        (NodeFilter, Tokens),
                        (Segment, Tokens)>(segmentPath, position);

                    return false;
                }

                while (true)
                {
                    #region Delimiter
                    // cannot read the delimiter
                    if (!reader.TryGetToken(out var token))
                        break;

                    // delimiter character is invalid
                    if (token[0] != '|')
                    {
                        reader.Back(1);
                        break;
                    }
                    else tokenList.Add(token);
                    #endregion

                    #region Filters
                    // read subsequent filters
                    if (TryParseFilter(reader, segmentPath, out filterInfo))
                        _ = filterInfo
                            .Resolve()
                            .With(filterInfoList.Add)
                            .With(t => tokenList.Add(t.Tokens));

                    else
                    {
                        reader.Reset(position);
                        var errorResult = filterInfo.AsError();
                        result = errorResult.MapPartiallyRecognizedTokens<
                            (NodeFilter, Tokens),
                            (Segment, Tokens)>(
                            segmentPath,
                            position,
                            filterInfoList.Select(info => info.Tokens));

                        return false;
                    }
                    #endregion
                }

                var segment = Segment.Of(filterInfoList.Select(info => info.Filter));
                var tokens = tokenList.Combine();
                result = Result.Of((segment, tokens));

                return true;
            }
            catch (Exception e)
            {
                reader.Reset(position);
                result = Result.Of<(Segment, Tokens)>(new RecognitionRuntimeError(e));
                return false;
            }
        }

        internal static bool TryParseFilter(
            TokenReader reader,
            ProductionPath parentPath,
            out IResult<(NodeFilter Filter, Tokens Tokens)> result)
        {
            var position = reader.Position;
            var filterPath = parentPath.Next("filter");

            try
            {
                if (!TryParseFilterType(reader, filterPath, out var filterType))
                {
                    reader.Reset(position);
                    result = filterType.AsError().ActualCause() switch
                    {
                        PartiallyRecognizedTokens
                        or RecognitionRuntimeError => filterType.MapAs<(NodeFilter, Tokens)>(),
                        _ => filterType.AsError().MapUnrecognizedTokens<
                            (NodeType, Tokens),
                            (NodeFilter, Tokens)>(filterPath, position)
                    };
                    return false;
                }
                
                if (!TryParseSymbolName(reader, filterPath, out var symbolName))
                {
                    var error = symbolName.AsError().ActualCause();
                    if (error is not UnrecognizedTokens)
                    {
                        result = symbolName.MapAs<(NodeFilter, Tokens)>();
                        reader.Reset(position);
                        return false;
                    }
                    else
                    {
                        symbolName = Tokens
                            .Of(reader.Source, reader.Position, 0)
                            .ApplyTo(Result.Of)
                            .Map(r => ("", r));
                    }
                }

                if (!TryParseTokens(reader, filterPath, out var tokens))
                {
                    var actualCause = tokens.AsError().ActualCause();
                    if (actualCause is not UnrecognizedTokens)
                    {
                        reader.Reset(position);
                        result = actualCause switch
                        {
                            RecognitionRuntimeError => tokens.MapAs<(NodeFilter, Tokens)>(),
                            _ => tokens.AsError().MapPartiallyRecognizedTokens<
                                Tokens,
                                (NodeFilter, Tokens)>(
                                filterPath,
                                position,
                                filterType.Resolve().Tokens,
                                symbolName.Resolve().Tokens)
                        };
                        return false;
                    }

                    tokens = Result.Of(Tokens.Of(reader.Source, reader.Position, 0));
                }

                // both symbol name and tokens are empty
                if (symbolName
                    .Combine(tokens, (s, t) => s.Tokens.Length + t.Length == 0)
                    .Resolve())
                {
                    result = PartiallyRecognizedTokens
                        .Of(filterPath, position, filterType.Resolve().Tokens)
                        .ApplyTo(Result.Of<(NodeFilter, Tokens)>);
                    return false;
                }

                result = filterType
                    .Combine(symbolName, (type, name) => (type, name))
                    .Combine(tokens, (tuple, tokens) => (
                        Filter: new NodeFilter(
                            tuple.type.NodeType,
                            tuple.name.Name,
                            !tokens.IsEmpty ? tokens[1..^1].ToString():""),
                        Tokens: ArrayUtil.Of(tuple.type.Tokens, tuple.name.Tokens, tokens).Combine()));
                return true;
            }
            catch (Exception e)
            {
                result = Result.Of<(NodeFilter, Tokens)>(new RecognitionRuntimeError(e));
                return false;
            }
        }

        internal static bool TryParseFilterType(
            TokenReader reader,
            ProductionPath parentPath,
            out IResult<(NodeType NodeType, Tokens Tokens)> result)
        {
            var position = reader.Position;
            var filterTypePath = parentPath.Next("filter-type");

            try
            {
                #region delimiter
                if (!reader.TryGetToken(out var token))
                {
                    result = Result.Of((NodeType.None, Tokens.Empty));
                    return true;
                }

                if (!'@'.Equals(token[0]))
                {
                    reader.Back();
                    result = Result.Of((NodeType.None, Tokens.Empty));
                    return true;
                }
                #endregion

                #region type
                if (!reader.TryGetToken(out var typeChar)
                    || !FilterTypeCharacters.Contains(char.ToLower(typeChar[0])))
                {
                    reader.Reset(position);
                    result = Result.Of<(NodeType, Tokens)>(
                        new PartiallyRecognizedTokens(
                            filterTypePath,
                            position,
                            token));
                    return false;
                }
                #endregion

                var filterType = char.ToLower(typeChar[0]) switch
                {
                    'l' => Result.Of(NodeType.Literal),
                    'r' => Result.Of(NodeType.Ref),
                    'c' => Result.Of(NodeType.Custom),
                    'p' => Result.Of(NodeType.Pattern),
                    'n' or _ => Result.Of(NodeType.None)
                };

                var tokens = token.CombineWith(typeChar);
                result = filterType.Map(ftype => (ftype, tokens));
                return true;
            }
            catch (Exception e)
            {
                result = Result.Of<(NodeType, Tokens)>(new RecognitionRuntimeError(e));
                return false;
            }
        }

        internal static bool TryParseSymbolName(
            TokenReader reader,
            ProductionPath parentPath,
            out IResult<(string Name, Tokens Tokens)> result)
        {
            var position = reader.Position;
            var symbolNamePath = parentPath.Next("symbol-name");

            try
            {
                #region delimiter
                var hasDelimiter = false;

                if (!reader.TryGetToken(out var delimiter))
                {
                    result = Result.Of<(string, Tokens)>(new UnrecognizedTokens(
                        symbolNamePath,
                        position));
                    return false;
                }
                else if (char.IsAsciiLetter(delimiter[0]))
                {
                    reader.Back();
                    delimiter = Tokens.Empty;
                }
                else if (!':'.Equals(delimiter[0]))
                {
                    reader.Reset(position);
                    result = Result.Of<(string, Tokens)>(new UnrecognizedTokens(
                        symbolNamePath,
                        position));
                    return false;
                }

                else hasDelimiter = true;
                #endregion

                #region Name
                if (!reader.TryGetPattern(Production.SymbolPattern, out var symbolName))
                {
                    if (!hasDelimiter)
                        result = Result.Of<(string, Tokens)>(new UnrecognizedTokens(
                            symbolNamePath,
                            position));

                    else result = Result.Of<(string, Tokens)>(
                        new PartiallyRecognizedTokens(
                            symbolNamePath,
                            position,
                            delimiter));

                    reader.Reset(position);
                    return false;
                }
                #endregion

                result = Result.Of((symbolName.ToString(), delimiter.CombineWith(symbolName)));
                return true;
            }
            catch (Exception e)
            {
                result = Result.Of<(string, Tokens)>(new RecognitionRuntimeError(e));
                return false;
            }
        }

        internal static bool TryParseTokens(
            TokenReader reader,
            ProductionPath parentPath,
            out IResult<Tokens> result)
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
                    result = Result.Of<Tokens>(new UnrecognizedTokens(
                        tokensPath,
                        position));
                    return false;
                }
                #endregion

                #region tokens
                var isEscaping = false;
                var tokens = Tokens.Empty;
                while(reader.TryGetToken(out var token))
                {
                    if (!isEscaping)
                    {
                        if ('\\'.Equals(token[0]))
                        {
                            tokens = tokens.CombineWith(token);
                            isEscaping = true;
                        }
                        else if ('>'.Equals(token[0]))
                        {
                            reader.Back(1);
                            break;
                        }

                        else tokens = tokens.CombineWith(token);
                    }
                    else
                    {
                        if ('\\'.Equals(token[0]) || '>'.Equals(token[0]))
                        {
                            tokens = tokens.CombineWith(token);
                            isEscaping = false;
                        }
                        else
                        {
                            reader.Reset(position);
                            result = Result.Of<Tokens>(new PartiallyRecognizedTokens(
                                tokensPath,
                                position,
                                openDelim.CombineWith(tokens)));
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
                    result = Result.Of<Tokens>(new PartiallyRecognizedTokens(
                        tokensPath,
                        position,
                        openDelim.CombineWith(tokens)));
                    return false;
                }
                #endregion

                result = openDelim
                    .CombineWith(tokens)
                    .CombineWith(closeDelim)
                    .ApplyTo(Result.Of);
                return true;
            }
            catch (Exception e)
            {
                reader.Reset(position);
                result = Result.Of<Tokens>(new RecognitionRuntimeError(e));
                return false;
            }
        }
    }
    */


    /// <summary>
    /// Parse strings into the <see cref="Path"/> structure.
    /// <para/>
    /// The <see cref="Path"/> syntax takes the form
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

        private static readonly HashSet<char> FilterTypeCharacters = new HashSet<char>
        {
            't', 'n', 'u'
        };


        public static IResult<Path> Parse(string pathText)
        {
            if (string.IsNullOrEmpty(pathText))
                throw new ArgumentException("Invalid path: null/empty");

            if (!TryRecognizePath(pathText, out var result))
                return Result.Of<Path>(ToFormatException(result.AsError()));

            try
            {
                var root = result.Resolve();
                var path = ToPath(root);

                return Result.Of(path);
            }
            catch (Exception e)
            {
                return Result.Of<Path>(e);
            }

        }

        #region Converters
        internal static Path ToPath(ICSTNode node)
        {
            return node switch
            {
                ICSTNode.NonTerminal root => root.Nodes
                    .Where(n => n.Name.Equals(NodeName_Segment))
                    .Select(ToSegment)
                    .ToArray()
                    .ApplyTo(Path.Of),

                _ => throw new ArgumentException($"Invalid node: {node?.GetType().ToString() ?? "null"}")
            };
        }

        internal static Segment ToSegment(ICSTNode node)
        {
            return node switch
            {
                ICSTNode.NonTerminal root => root.Nodes
                    .Where(n => n.Name.Equals(NodeName_Filter))
                    .Select(ToNodeFilter)
                    .ToArray()
                    .ApplyTo(Segment.Of),

                _ => throw new ArgumentException($"Invalid node: {node?.GetType().ToString() ?? "null"}")
            };
        }
        
        internal static NodeFilter ToNodeFilter(ICSTNode node)
        {

        }
        #endregion

        #region Recognizers
        internal static bool TryRecognizeTokens(
            TokenReader reader,
            ProductionPath parentPath,
            out IResult<ICSTNode> result)
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
                    result = Result.Of<ICSTNode>(new UnrecognizedTokens(
                        tokensPath,
                        position));
                    return false;
                }
                #endregion

                #region tokens
                var isEscaping = false;
                var tokens = Tokens.Empty;
                while (reader.TryGetToken(out var token))
                {
                    if (!isEscaping)
                    {
                        if ('\\'.Equals(token[0]))
                        {
                            tokens = tokens.CombineWith(token);
                            isEscaping = true;
                        }
                        else if ('>'.Equals(token[0]))
                        {
                            reader.Back(1);
                            break;
                        }
                        else tokens = tokens.CombineWith(token);
                    }
                    else
                    {
                        if ('\\'.Equals(token[0]) || '>'.Equals(token[0]))
                        {
                            tokens = tokens.CombineWith(token);
                            isEscaping = false;
                        }
                        else
                        {
                            reader.Reset(position);
                            result = Result.Of<ICSTNode>(new PartiallyRecognizedTokens(
                                tokensPath,
                                position,
                                openDelim.CombineWith(tokens)));
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
                    result = Result.Of<ICSTNode>(new PartiallyRecognizedTokens(
                        tokensPath,
                        position,
                        openDelim.CombineWith(tokens)));
                    return false;
                }
                #endregion

                result = openDelim
                    .CombineWith(tokens)
                    .CombineWith(closeDelim)
                    .ApplyTo(tokens => ICSTNode.Of(tokensPath.Name, tokens))
                    .ApplyTo(Result.Of);
                return true;
            }
            catch (Exception e)
            {
                reader.Reset(position);
                result = Result.Of<ICSTNode>(new RecognitionRuntimeError(e));
                return false;
            }
        }

        internal static bool TryRecognizeSymbolName(
            TokenReader reader,
            ProductionPath parentPath,
            out IResult<ICSTNode> result)
        {
            var position = reader.Position;
            var symbolNamePath = parentPath.Next("symbol-name");

            try
            {
                #region delimiter
                var hasDelimiter = false;

                if (!reader.TryGetToken(out var delimiter))
                {
                    result = Result.Of<ICSTNode>(new UnrecognizedTokens(
                        symbolNamePath,
                        position));
                    return false;
                }
                else if (char.IsAsciiLetter(delimiter[0]))
                {
                    reader.Back();
                    delimiter = Tokens.Empty;
                }
                else if (!':'.Equals(delimiter[0]))
                {
                    reader.Reset(position);
                    result = Result.Of<ICSTNode>(new UnrecognizedTokens(
                        symbolNamePath,
                        position));
                    return false;
                }
                else hasDelimiter = true;
                #endregion

                #region Name
                if (!reader.TryGetPattern(Production.SymbolPattern, out var symbolName))
                {
                    if (!hasDelimiter)
                        result = Result.Of<ICSTNode>(new UnrecognizedTokens(
                            symbolNamePath,
                            position));

                    else result = Result.Of<ICSTNode>(
                        new PartiallyRecognizedTokens(
                            symbolNamePath,
                            position,
                            delimiter));

                    reader.Reset(position);
                    return false;
                }
                #endregion

                result = delimiter
                    .CombineWith(symbolName)
                    .ApplyTo(tokens => ICSTNode.Of(symbolNamePath.Name, tokens))
                    .ApplyTo(Result.Of<ICSTNode>);
                return true;
            }
            catch (Exception e)
            {
                result = Result.Of<ICSTNode>(new RecognitionRuntimeError(e));
                return false;
            }
        }

        internal static bool TryRecognizeFilterType(
            TokenReader reader,
            ProductionPath parentPath,
            out IResult<ICSTNode> result)
        {
            var position = reader.Position;
            var filterTypePath = parentPath.Next("filter-type");

            try
            {
                #region delimiter
                if (!reader.TryGetToken(out var delim))
                {
                    result = UnrecognizedTokens
                        .Of(filterTypePath, position)
                        .ApplyTo(Result.Of<ICSTNode>);
                    return false;
                }

                if (!'@'.Equals(delim[0]))
                {
                    reader.Back();
                    result = ICSTNode
                        .Of(filterTypePath.Name, Tokens.Empty)
                        .ApplyTo(Result.Of);
                    return true;
                }
                #endregion

                #region type
                if (!reader.TryGetToken(out var typeChar)
                    || !FilterTypeCharacters.Contains(char.ToLower(typeChar[0])))
                {
                    reader.Reset(position);
                    result = Result.Of<ICSTNode>(
                        new PartiallyRecognizedTokens(
                            filterTypePath,
                            position,
                            delim));
                    return false;
                }
                #endregion

                result = delim
                    .CombineWith(typeChar)
                    .ApplyTo(tokens => ICSTNode.Of(filterTypePath.Name, tokens))
                    .ApplyTo(Result.Of<ICSTNode>);
                return true;
            }
            catch (Exception e)
            {
                result = Result.Of<ICSTNode>(new RecognitionRuntimeError(e));
                return false;
            }
        }

        internal static bool TryRecognizeFilter(
            TokenReader reader,
            ProductionPath parentPath,
            out IResult<ICSTNode> result)
        {
            var position = reader.Position;
            var filterPath = parentPath.Next("filter");

            try
            {
                if (!TryRecognizeFilterType(reader, filterPath, out var filterType))
                {
                    reader.Reset(position);
                    result = filterType.AsError().MapNodeError(
                        ute => UnrecognizedTokens.Of(filterPath, position),
                        pte => pte);
                    return false;
                }

                var tempPosition = reader.Position;
                if (!TryRecognizeSymbolName(reader, filterPath, out var symbolName))
                {
                    reader.Reset(position);
                    result = symbolName.AsError().MapNodeError(
                        ute => PartiallyRecognizedTokens.Of(
                            filterPath,
                            tempPosition,
                            Tokens.Of(reader.Source, position, tempPosition - position)),
                        pte => pte);
                    return false;
                }

                tempPosition = reader.Position;
                if (!TryRecognizeTokens(reader, filterPath, out var tokens))
                {
                    reader.Reset(position);
                    result = symbolName.AsError().MapNodeError(
                        ute => PartiallyRecognizedTokens.Of(
                            filterPath,
                            tempPosition,
                            Tokens.Of(reader.Source, position, tempPosition - position)),
                        pte => pte);
                    return false;
                }

                // both symbol name and tokens are empty
                if (symbolName
                    .Combine(tokens, (s, t) => s.Tokens.Count + t.Tokens.Count == 0)
                    .Resolve())
                {
                    result = PartiallyRecognizedTokens
                        .Of(filterPath, position, filterType.Resolve().Tokens)
                        .ApplyTo(Result.Of<ICSTNode>);
                    return false;
                }

                result = filterType
                    .Combine(symbolName, (type, name) => (type, name))
                    .Combine(tokens, (tuple, tokens) => (tuple.type, tuple.name, tokens))
                    .Map(tuple => ICSTNode.Of(filterPath.Name, tuple.type, tuple.name, tuple.tokens));
                return true;
            }
            catch (Exception e)
            {
                result = Result.Of<ICSTNode>(new RecognitionRuntimeError(e));
                return false;
            }
        }

        internal static bool TryRecognizeSegment(
            TokenReader reader,
            ProductionPath parentPath,
            out IResult<ICSTNode> result)
        {
            var position = reader.Position;
            var segmentPath = parentPath.Next("segment");

            try
            {
                if (!TryRecognizeFilter(reader, segmentPath, out var filterResult))
                {
                    reader.Reset(position);
                    result = filterResult.AsError().MapNodeError(
                        ute => UnrecognizedTokens.Of(segmentPath, position),
                        pte => pte);
                    return false;
                }

                var alternates = new List<ICSTNode>();
                filterResult.Consume(alternates.Add);

                while (reader.TryGetTokens("|", out var filterSeparator))
                {
                    alternates.Add(ICSTNode.Of("filter-separator", filterSeparator));
                    var tempPosition = reader.Position;

                    if (!TryRecognizeFilter(reader, segmentPath, out var alternateFilterResult))
                    {
                        reader.Reset(position);
                        result = filterResult.AsError().MapNodeError(
                            ute => PartiallyRecognizedTokens.Of(
                                segmentPath,
                                tempPosition,
                                Tokens.Of(reader.Source, position, tempPosition - position)),
                            pte => pte);
                        return false;
                    }
                    alternateFilterResult.Consume(alternates.Add);
                }

                result = ICSTNode
                    .Of(segmentPath.Name, alternates.ToArray())
                    .ApplyTo(Result.Of);
                return true;
            }
            catch (Exception e)
            {
                result = Result.Of<ICSTNode>(new RecognitionRuntimeError(e));
                return false;
            }
        }

        internal static bool TryRecognizePath(
            TokenReader reader,
            out IResult<ICSTNode> result)
        {
            var position = reader.Position;
            var path = ProductionPath.Of("path");

            try
            {
                if (!TryRecognizeSegment(reader, path, out var segmentResult))
                {
                    reader.Reset(position);
                    result = segmentResult.AsError().MapNodeError(
                        ute => UnrecognizedTokens.Of(path, position),
                        pte => pte);
                    return false;
                }

                var alternates = new List<ICSTNode>();
                segmentResult.Consume(alternates.Add);

                while (reader.TryGetTokens("/", out var segmentSeparator))
                {
                    alternates.Add(ICSTNode.Of("segment-separator", segmentSeparator));
                    var tempPosition = reader.Position;

                    if (!TryRecognizeSegment(reader, path, out var alternateFilterResult))
                    {
                        reader.Reset(position);
                        result = segmentResult.AsError().MapNodeError(
                            ute => PartiallyRecognizedTokens.Of(
                                path,
                                tempPosition,
                                Tokens.Of(reader.Source, position, tempPosition - position)),
                            pte => pte);
                        return false;
                    }
                    alternateFilterResult.Consume(alternates.Add);
                }

                result = ICSTNode
                    .Of(path.Name, alternates.ToArray())
                    .ApplyTo(Result.Of);
                return true;
            }
            catch (Exception e)
            {
                result = Result.Of<ICSTNode>(new RecognitionRuntimeError(e));
                return false;
            }
        }
        #endregion
    }
}
