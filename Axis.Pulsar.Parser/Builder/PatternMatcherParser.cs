using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Language;
using System;
using System.Text;

namespace Axis.Pulsar.Parser.Builder
{
    public class PatternMatcherParser : IParser
    {
        private readonly PatternTerminal _terminal;

        public PatternMatcherParser(PatternTerminal terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        public bool TryParse(BufferedTokenReader tokenReader, out ParseResult result)
        {
            var position = tokenReader.Position;
            try
            {
                string symbolValue = null;
                if(_terminal.CharacterCardinality.MaxOccurence == null) //open ended
                {
                    //pull the min number of characters from the source, then keep pulling and matching till a false match is encountered
                    if (!tokenReader.TryNextTokens(_terminal.CharacterCardinality.MinOccurence, out var tokens))
                        throw new System.IO.EndOfStreamException();

                    else if (!_terminal.Value.IsMatch(new string(tokens)))
                    {
                        result = new ParseResult(new ParseError(_terminal.Name, position + 1));
                        tokenReader.Reset(position);
                        return false;
                    }

                    var sbuffer = new StringBuilder(new string(tokens));
                    while (tokenReader.TryNextToken(out var token))
                    {
                        if(!_terminal.Value.IsMatch(sbuffer.Append(token).ToString()))
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
                    for (int charCount = _terminal.CharacterCardinality.MaxOccurence.Value;
                        charCount >= _terminal.CharacterCardinality.MinOccurence;
                        charCount--)
                    {
                        if (!tokenReader.TryNextTokens(charCount, out var tokens))
                            continue;

                        else if (!_terminal.Value.IsMatch(new string(tokens)))
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
                        result = new ParseResult(new ParseError(_terminal.Name, position + 1));
                        tokenReader.Reset(position);
                        return false;
                    }
                }

                result = new ParseResult(new Syntax.Symbol(_terminal.Name, symbolValue));
                return true;
            }
            catch
            {
                result = new ParseResult(new ParseError(_terminal.Name, position + 1));
                tokenReader.Reset(position);
                return false;
            }
        }
    }
}
