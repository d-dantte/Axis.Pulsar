using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.Language;
using System;
using System.Collections.Generic;
using System.Text;

namespace Axis.Pulsar.Grammar.CST
{
    public static class PathParser
    {
        private static readonly HashSet<char> FilterTypeCharacters = new HashSet<char>
        {
            'l', 'r', 'p', 'c', 'n'
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IResult<Path> Parse(string path)
        {
            try
            {
                BufferedTokenReader reader = path;
                _ = TryParsePath(reader, out var pathResult);

                if (pathResult.IsDataResult() && !reader.IsConsumed)
                    return Result.Of<Path>(new Errors.PartiallyRecognizedTokens(
                        0,
                        reader.Position,
                        pathResult.Resolve().ToString()));

                return pathResult;
            }
            catch(Exception e)
            {
                return Result.Of<Path>(e switch
                {
                    Errors.RuntimeError => e,
                    _ => new Errors.RuntimeError(e)
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParse(
            string path,
            out IResult<Path> result)
            => (result = Parse(path)).IsDataResult();


        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static bool TryParsePath(
            BufferedTokenReader reader,
            out IResult<Path> path)
        {
            var position = reader.Position;
            var segments = new List<Segment>();
            char? delimiterChar = null;

            try
            {
                // parse the first segment
                if (TryParseSegment(reader, out var segment))
                    segments.Add(segment.Resolve());

                else
                {
                    path = segment.MapAs<Path>();
                    return false;
                }

                while(true)
                {
                    #region Delimiter
                    // read the delimiter
                    if (!reader.TryNextToken(out var token))
                        break;

                    // delimiter character is invalid
                    if ((delimiterChar is not null && token != delimiterChar)
                        || (delimiterChar is null && token != '.' && token != '/'))
                    {
                        reader.Back(1);
                        break;
                    }

                    delimiterChar ??= token;
                    #endregion

                    #region Segments
                    // read subsequent segments
                    if (TryParseSegment(reader, out segment))
                        segments.Add(segment.Resolve());

                    else if(segment.IsErrorResult(out var error))
                    {
                        path = error switch
                        {
                            Errors.RuntimeError e => Result.Of<Path>(e),
                            Errors.PartiallyRecognizedTokens
                            or Errors.UnrecognizedTokens
                            or Errors.EndOfStream => Result.Of<Path>(
                                new Errors.PartiallyRecognizedTokens(
                                    position,
                                    reader.Position - position)),
                            _ => throw new InvalidOperationException(
                                $"Invalid Error type: '{error?.GetType()}'")
                        };

                        return false;
                    }
                    #endregion
                }

                path = Path
                    .Of(segments)
                    .ApplyTo(Result.Of);

                return true;
            }
            catch(Exception e)
            {
                path = Result.Of<Path>(new Errors.RuntimeError(e));
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="segment"></param>
        /// <returns></returns>
        internal static bool TryParseSegment(
            BufferedTokenReader reader,
            out IResult<Segment> segment)
        {
            var position = reader.Position;
            var filters = new List<NodeFilter>();

            try
            {
                // parse the first filter
                if (TryParseFilter(reader, out var filter))
                    filters.Add(filter.Resolve());

                else
                {
                    segment = filter.MapAs<Segment>();
                    return false;
                }

                while (true)
                {
                    #region Delimiter
                    // cannot read the delimiter
                    if (!reader.TryNextToken(out var token))
                        break;

                    // delimiter character is invalid
                    if (token != '|')
                    {
                        reader.Back(1);
                        break;
                    }
                    #endregion

                    #region Filters
                    // read subsequent filters
                    if (TryParseFilter(reader, out filter))
                        filters.Add(filter.Resolve());

                    else if (filter.IsErrorResult(out var error))
                    {
                        segment = error switch
                        {
                            Errors.RuntimeError e => Result.Of<Segment>(e),
                            Errors.PartiallyRecognizedTokens
                            or Errors.UnrecognizedTokens
                            or Errors.EndOfStream => Result.Of<Segment>(
                                new Errors.PartiallyRecognizedTokens(
                                    position,
                                    reader.Position - position)),
                            _ => throw new InvalidOperationException(
                                $"Invalid Error type: '{error?.GetType()}'")
                        };

                        return false;
                    }
                    #endregion
                }

                segment = Segment
                    .Of(filters)
                    .ApplyTo(Result.Of);

                return true;
            }
            catch (Exception e)
            {
                segment = Result.Of<Segment>(new Errors.RuntimeError(e));
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        internal static bool TryParseFilter(
            BufferedTokenReader reader,
            out IResult<NodeFilter> filter)
        {
            var position = reader.Position;

            try
            {
                if (!TryParseFilterType(reader, out var filterType)
                    && filterType.IsErrorResult(out var error)
                    && error is not Errors.UnrecognizedTokens
                    && error is not Errors.EndOfStream)
                {
                    filter = filterType.MapAs<NodeFilter>();
                    return false;
                }

                if (!TryParseSymbolName(reader, out var symbolName)
                    && symbolName.IsErrorResult(out error)
                    && error is not Errors.UnrecognizedTokens
                    && error is not Errors.EndOfStream)
                {
                    filter = symbolName.MapAs<NodeFilter>();
                    return false;
                }

                if (!TryParseTokens(reader, out var tokens)
                    && tokens.IsErrorResult(out error)
                    && error is not Errors.UnrecognizedTokens
                    && error is not Errors.EndOfStream)
                {
                    filter = tokens.MapAs<NodeFilter>();
                    return false;
                }

                // nothing was recognized
                if (filterType.IsErrorResult()
                    && symbolName.IsErrorResult()
                    && tokens.IsErrorResult())
                {
                    reader.Reset(position);
                    filter = Result.Of<NodeFilter>(new Errors.UnrecognizedTokens());
                    return false;
                }

                filter = filterType
                    .Combine(symbolName.MapError(_ => null), (type, name) => (type, name))
                    .Combine(tokens.MapError(_ => null), (tuple, tokens) => new NodeFilter(
                        tuple.type,
                        tuple.name,
                        tokens));
                return true;
            }
            catch (Exception e)
            {
                filter = Result.Of<NodeFilter>(new Errors.RuntimeError(e));
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="symbolType"></param>
        /// <returns></returns>
        internal static bool TryParseFilterType(
            BufferedTokenReader reader,
            out IResult<NodeType> filterType)
        {
            var position = reader.Position;

            try
            {
                #region delimiter
                if (!reader.TryNextToken(out var token))
                {
                    filterType = Result.Of(NodeType.None);
                    return true;
                }

                if (!'@'.Equals(token))
                {
                    reader.Back();
                    filterType = Result.Of(NodeType.None);
                    return true;
                }
                #endregion

                #region type
                if (!reader.TryNextToken(out var typeChar)
                    || !FilterTypeCharacters.Contains(char.ToLower(typeChar)))
                {
                    filterType = Result.Of<NodeType>(
                        new Errors.PartiallyRecognizedTokens(position, 1));
                    return false;
                }
                #endregion

                filterType = char.ToLower(typeChar) switch
                {
                    'l' => Result.Of(NodeType.Literal),
                    'r' => Result.Of(NodeType.Ref),
                    'c' => Result.Of(NodeType.Custom),
                    'p' => Result.Of(NodeType.Pattern),
                    'n' => Result.Of(NodeType.None),
                    _ => Result.Of<NodeType>(new Errors.PartiallyRecognizedTokens(position, 1))
                };
                return true;
            }
            catch (Exception e)
            {
                filterType = Result.Of<NodeType>(new Errors.RuntimeError(e));
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="symbolName"></param>
        /// <returns></returns>
        internal static bool TryParseSymbolName(
            BufferedTokenReader reader,
            out IResult<string> symbolName)
        {
            var position = reader.Position;

            try
            {
                #region delimiter
                var hasDelimiter = false;

                if (!reader.TryNextToken(out var token))
                {
                    symbolName = Result.Of<string>(new Errors.EndOfStream());
                    return false;
                }

                else if (char.IsAsciiLetter(token))
                    reader.Back();

                else if (!':'.Equals(token))
                {
                    reader.Reset(position);
                    symbolName = Result.Of<string>(new Errors.UnrecognizedTokens());
                    return false;
                }

                else hasDelimiter = true;
                #endregion

                #region Name
                if (!reader.TryNextPattern(IRule.SymbolNamePattern, out var tokens))
                {
                    if (!hasDelimiter)
                    {
                        reader.Reset(position);
                        symbolName = Result.Of<string>(new Errors.UnrecognizedTokens());
                    }
                    else
                    {
                        symbolName = Result.Of<string>(
                            new Errors.PartiallyRecognizedTokens(
                                position,
                                1));
                    }
                    return false;
                }
                #endregion

                symbolName = Result.Of(tokens);
                return true;
            }
            catch (Exception e)
            {
                symbolName = Result.Of<string>(new Errors.RuntimeError(e));
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="tokens"></param>
        /// <returns></returns>
        internal static bool TryParseTokens(
            BufferedTokenReader reader,
            out IResult<string> tokens)
        {
            var position = reader.Position;

            try
            {
                #region open delimiter
                if (!reader.TryNextToken(out var token))
                {
                    tokens = Result.Of<string>(new Errors.EndOfStream());
                    return false;
                }

                if (!'<'.Equals(token))
                {
                    reader.Reset(position);
                    tokens = Result.Of<string>(new Errors.UnrecognizedTokens());
                    return false;
                }
                #endregion

                #region tokens
                var isEscaping = false;
                var sb = new StringBuilder();
                while(reader.TryNextToken(out token))
                {
                    if (!isEscaping)
                    {
                        if ('\\'.Equals(token))
                            isEscaping = true;

                        else if ('>'.Equals(token))
                        {
                            reader.Back(1);
                            break;
                        }

                        else sb.Append(token);
                    }
                    else
                    {
                        if ('\\'.Equals(token) || '>'.Equals(token))
                        {
                            sb.Append(token);
                            isEscaping = false;
                        }
                        else
                        {
                            tokens = Result.Of<string>(new Errors.PartiallyRecognizedTokens(
                                position,
                                reader.Position - position));
                            return false;
                        }
                    }
                }
                #endregion

                #region close delimiter
                if (!reader.TryNextToken(out token)
                    || !'>'.Equals(token))
                {
                    tokens = Result.Of<string>(new Errors.PartiallyRecognizedTokens(
                        position,
                        reader.Position - position));
                    return false;
                }
                #endregion

                tokens = Result.Of(sb.ToString());
                return true;
            }
            catch (Exception e)
            {
                tokens = Result.Of<string>(new Errors.RuntimeError(e));
                return false;
            }
        }
    }
}
