using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Grammar;
using System;
using System.Text;

namespace Axis.Pulsar.Parser.Parsers
{
    public class PatternMatcherParser : IParser
    {
        private readonly PatternRule _terminal;

        public string SymbolName { get; }

        public PatternMatcherParser(string symbolName, PatternRule terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            SymbolName = symbolName.ThrowIf(
                string.IsNullOrWhiteSpace,
                _ => new ArgumentException("Invalid symbol name"));
        }

        public bool TryParse(BufferedTokenReader tokenReader, out ParseResult result)
        {
            var position = tokenReader.Position;
            try
            {
                string symbolValue = null;
                if(_terminal.MatchCardinality.MaxOccurence == null) //open ended
                {
                    //pull the min number of characters from the source, then keep pulling and matching till a false match is encountered
                    if (!tokenReader.TryNextTokens(_terminal.MatchCardinality.MinOccurence, out var tokens))
                        throw new System.IO.EndOfStreamException();

                    else if (!_terminal.Regex.IsMatch(new string(tokens)))
                    {
                        result = new ParseResult(new ParseError(SymbolName, position + 1));
                        tokenReader.Reset(position);
                        return false;
                    }

                    var sbuffer = new StringBuilder(new string(tokens));
                    while (tokenReader.TryNextToken(out var token))
                    {
                        if(!_terminal.Regex.IsMatch(sbuffer.Append(token).ToString()))
                        {
                            tokenReader.Back();
                            sbuffer.Remove(sbuffer.Length - 1, 1);
                            break;
                        }
                    }

                    symbolValue = sbuffer.ToString();
                }
                else //close ended
                {
                    //pull the max number of characters from the source, then keep removing from the end till a positive match is encountered
                    for (int charCount = _terminal.MatchCardinality.MaxOccurence.Value;
                        charCount >= _terminal.MatchCardinality.MinOccurence;
                        charCount--)
                    {
                        if (!tokenReader.TryNextTokens(charCount, out var tokens))
                            continue;

                        else if (!_terminal.Regex.IsMatch(new string(tokens)))
                        {
                            tokenReader.Reset(position);
                            continue;
                        }
                        else
                        {
                            symbolValue = new(tokens);
                            break;
                        }
                    }

                    //no match at all
                    if (symbolValue == null)
                    {
                        result = new ParseResult(new ParseError(SymbolName, position + 1));
                        tokenReader.Reset(position);
                        return false;
                    }
                }

                result = new ParseResult(new Syntax.Symbol(SymbolName, symbolValue));
                return true;
            }
            catch
            {
                result = new ParseResult(new ParseError(SymbolName, position + 1));
                tokenReader.Reset(position);
                return false;
            }
        }

        public override string ToString() => $"/{_terminal.Regex}/";
    }
}
