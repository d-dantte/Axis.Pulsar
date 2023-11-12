using Axis.Luna.Common.Results;
using Axis.Luna.Common.Utils;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.CST
{
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
        internal static readonly string NodeName_Filter_Type = "filter-type";
        internal static readonly string NodeName_Symbol_Name = "symbol-name";
        internal static readonly string NodeName_Tokens = "tokens";

        private static readonly HashSet<char> FilterTypeCharacters = new()
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

        public static bool TryParse(string pathText, out  IResult<Path> result)
        {
            result = Parse(pathText);
            return result is IResult<Path>.DataResult;
        }

        #region Converters
        internal static Path ToPath(ICSTNode pathNode)
        {
            return pathNode switch
            {
                ICSTNode.NonTerminal root => root.Nodes
                    .Where(n => n.Name.Equals(NodeName_Segment))
                    .Select(ToSegment)
                    .ToArray()
                    .ApplyTo(Path.Of),

                _ => throw new ArgumentException($"Invalid node: {pathNode?.GetType().ToString() ?? "null"}")
            };
        }

        internal static Segment ToSegment(ICSTNode segmentNode)
        {
            return segmentNode switch
            {
                ICSTNode.NonTerminal root => root.Nodes
                    .Where(n => n.Name.Equals(NodeName_Filter))
                    .Select(ToNodeFilter)
                    .ToArray()
                    .ApplyTo(Segment.Of),

                _ => throw new ArgumentException($"Invalid node: {segmentNode?.GetType().ToString() ?? "null"}")
            };
        }

        internal static NodeFilter ToNodeFilter(ICSTNode filterNode)
        {
            if (filterNode is not ICSTNode.NonTerminal filterNTNode)
                throw new ArgumentException($"Invalid node: {filterNode?.GetType().ToString() ?? "null"}");

            return NodeFilter.Of(
                ToNodeType(filterNTNode.Nodes.First(n => NodeName_Filter_Type.Equals(n.Name))),
                ToSymbolName(filterNTNode.Nodes.FirstOrDefault(n => NodeName_Symbol_Name.Equals(n.Name))),
                ToTokens(filterNTNode.Nodes.FirstOrDefault(n => NodeName_Tokens.Equals(n.Name))));
        }

        internal static NodeType ToNodeType(ICSTNode filterTypeNode)
        {
            ArgumentNullException.ThrowIfNull(filterTypeNode);

            if (filterTypeNode.Tokens.IsEmpty)
                return NodeType.Unspecified;

            else return filterTypeNode.Tokens[1] switch
            {
                'u' => NodeType.Unspecified,
                't' => NodeType.Terminal,
                'n' => NodeType.NonTerminal,
                _ => throw new InvalidOperationException(
                    $"Invalid filter type character: '{filterTypeNode.Tokens[1]}'")
            };
        }

        internal static string? ToSymbolName(ICSTNode? symbolNameNode)
        {
            if (symbolNameNode is null)
                return null;

            var tokens = symbolNameNode.Tokens;
            return tokens[0] == ':'
                ? tokens[1..].ToString()
                : tokens.ToString();
        }

        internal static string? ToTokens(ICSTNode? tokenNode)
        {
            if (tokenNode is null)
                return null;

            return tokenNode.Tokens[1..^1].ToString();
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
                            tokens = tokens.Join(token);
                            isEscaping = true;
                        }
                        else if ('>'.Equals(token[0]))
                        {
                            reader.Back(1);
                            break;
                        }
                        else tokens = tokens.Join(token);
                    }
                    else
                    {
                        if ('\\'.Equals(token[0]) || '>'.Equals(token[0]))
                        {
                            tokens = tokens.Join(token);
                            isEscaping = false;
                        }
                        else
                        {
                            reader.Reset(position);
                            result = Result.Of<ICSTNode>(new PartiallyRecognizedTokens(
                                tokensPath,
                                position,
                                openDelim.Join(tokens)));
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
                        openDelim.Join(tokens)));
                    return false;
                }
                #endregion

                result = openDelim
                    .Join(tokens)
                    .Join(closeDelim)
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
                if (!reader.TryGetPattern(IProduction.SymbolPattern, out var symbolName))
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
                    .Join(symbolName)
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
                    .Join(typeChar)
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
                    if (symbolName.AsError().ActualCause() is PartiallyRecognizedTokens)
                    {
                        reader.Reset(tempPosition);
                        result = symbolName;
                        return false;
                    }
                    symbolName = Result.Of((ICSTNode)null!);
                }

                tempPosition = reader.Position;
                if (!TryRecognizeTokens(reader, filterPath, out var tokens))
                {
                    if (tokens.AsError().ActualCause() is PartiallyRecognizedTokens)
                    {
                        reader.Reset(position);
                        result = tokens;
                        return false;
                    }
                    tokens = Result.Of((ICSTNode)null!);
                }

                result = filterType
                    .Combine(symbolName, (type, name) => (type, name))
                    .Combine(tokens, (tuple, tokens) => ArrayUtil
                        .Of(tuple.type, tuple.name, tokens)
                        .Where(n => n is not null))
                    .Map(nodes => ICSTNode.Of(filterPath.Name, nodes.ToArray()));
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
    
        private static FormatException ToFormatException(IResult<ICSTNode>.ErrorResult errorResult)
        {
            ArgumentNullException.ThrowIfNull(errorResult);

            return errorResult.ActualCause() switch
            {
                INodeError ne => new FormatException(
                    $"Invalid path format: error detected at position {ne.Position}"),
                _ => new FormatException($"Invalid path format: unknown error")
            };
        }
    }
}
