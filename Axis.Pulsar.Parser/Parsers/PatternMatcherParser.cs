using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Grammar;
using System;
using System.Text;

namespace Axis.Pulsar.Parser.Parsers
{
    public class PatternMatcherParser : RuleParser
    {
        private readonly PatternRule _terminal;
        private readonly string _ruleName = "$pattern";

        public PatternMatcherParser(PatternRule terminal)
            :base(Utils.Cardinality.OccursOnlyOnce())
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        public override bool TryParse(BufferedTokenReader tokenReader, out ParseResult result)
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
                        result = new ParseResult(new ParseError(_ruleName, position + 1));
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
                        result = new ParseResult(new ParseError(_ruleName, position + 1));
                        tokenReader.Reset(position);
                        return false;
                    }
                }

                result = new ParseResult(new Syntax.Symbol(_ruleName, symbolValue));
                return true;
            }
            catch
            {
                result = new ParseResult(new ParseError(_ruleName, position + 1));
                tokenReader.Reset(position);
                return false;
            }
        }

        public override string ToString() => $"Pattern[{_terminal.Regex}]";
    }
}
