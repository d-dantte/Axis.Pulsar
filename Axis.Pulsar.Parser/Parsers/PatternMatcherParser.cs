using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Grammar;
using System;
using System.Text;
using Axis.Pulsar.Parser.CST;

namespace Axis.Pulsar.Parser.Parsers
{
    public class PatternMatcherParser : IParser
    {
        private readonly PatternRule _terminal;

        /// <inheritdoc/>
        public string SymbolName { get; }

        /// <inheritdoc/>
        public int? RecognitionThreshold => null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="symbolName"></param>
        /// <param name="terminal"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PatternMatcherParser(string symbolName, PatternRule terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            SymbolName = symbolName.ThrowIf(
                string.IsNullOrWhiteSpace,
                _ => new ArgumentException("Invalid symbol name"));
        }

        /// <inheritdoc/>
        public bool TryParse(BufferedTokenReader tokenReader, out IResult result)
        {
            var position = tokenReader.Position;
            try
            {
                if(_terminal.MatchType is IPatternMatchType.Open open)
                {
                    // match each pulled character, and only fail when the number of non-matches is equal to MatchCardinality.MinOccurences
                    var sbuffer = new StringBuilder();
                    var mismatchCount = 0;
                    while (tokenReader.TryNextToken(out var token))
                    {
                        if (!_terminal.Value.IsMatch(sbuffer.Append(token).ToString()))
                        {
                            if (++mismatchCount == open.MaxMismatch)
                                break;
                        }
                        else mismatchCount = 0;
                    }

                    // walk back any mis-matches
                    tokenReader.Back(mismatchCount);
                    sbuffer.RemoveLast(mismatchCount);

                    if (sbuffer.Length == 0 && !open.MatchesEmptyTokens)
                    {
                        result = new IResult.FailedRecognition(SymbolName, position + 1);
                        tokenReader.Reset(position);
                        return false;
                    }

                    var node = ICSTNode.Of(SymbolName, sbuffer.ToString());
                    if(_terminal.RuleValidator?.IsValidCSTNode(_terminal, node) == false)
                    {
                        result = new IResult.FailedRecognition(SymbolName, position + 1);
                        tokenReader.Reset(position);
                        return false;
                    }

                    result = new IResult.Success(node);
                    return true;
                }
                else if(_terminal.MatchType is IPatternMatchType.Closed closed) //close ended
                {
                    _ = tokenReader.TryNextTokens(closed.MaxMatch, false, out var tokens);
                                        
                    var lim = tokens?.Length ?? 0 - closed.MinMatch;
                    string symbolValue = null;
                    for (int index = 0; index < lim; index++)
                    {
                        var subtokens = tokens[..^index];
                        if (_terminal.Value.IsMatch(new string(subtokens)))
                        {
                            symbolValue = new(subtokens);
                            break;
                        }
                    }

                    //no match at all
                    if (symbolValue == null)
                    {
                        result = new IResult.FailedRecognition(SymbolName, position + 1);
                        tokenReader.Reset(position);
                        return false;
                    }

                    var node = ICSTNode.Of(SymbolName, symbolValue);
                    if(_terminal.RuleValidator?.IsValidCSTNode(_terminal, node) == false)
                    {
                        result = new IResult.FailedRecognition(SymbolName, position + 1);
                        tokenReader.Reset(position);
                        return false;
                    }

                    result = new IResult.Success(node);
                    return true;
                }
                else
                {
                    // error
                    result = new IResult.Exception(
                        new InvalidCastException($"Invaid match-type: {_terminal.MatchType}"),
                        position + 1);
                    tokenReader.Reset(position);
                    return false;
                }
            }
            catch(Exception ex)
            {
                result = new IResult.Exception(ex, position + 1);
                tokenReader.Reset(position);
                return false;
            }
        }

        public IResult Parse(BufferedTokenReader tokenReader)
        {
            _ = TryParse(tokenReader, out var result);
            return result;
        }

        public override string ToString() => $"/{_terminal.Value}/";

    }
}
