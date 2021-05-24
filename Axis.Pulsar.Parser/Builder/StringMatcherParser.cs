using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Language;
using System;

namespace Axis.Pulsar.Parser.Builder
{
    public class StringMatcherParser: IParser
    {
        private readonly StringTerminal _terminal;

        public StringMatcherParser(StringTerminal terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        public bool TryParse(BufferedTokenReader tokenReader, out ParseResult result)
        {
            var position = tokenReader.Position;
            try
            {
                if(tokenReader.TryNextTokens(_terminal.Value.Length, out var tokens)
                    && _terminal.Value.Equals(
                        new string(tokens),
                        _terminal.IsCaseSensitive
                            ? StringComparison.InvariantCulture
                            : StringComparison.InvariantCultureIgnoreCase))
                {
                    result = new ParseResult(
                        new Syntax.Symbol(
                            _terminal.Name,
                            new string(tokens)));

                    return true;
                }

                //add relevant information into the parse error
                result = new ParseResult(new ParseError(_terminal.Name, position + 1));
                tokenReader.Reset(position);
                return false;
            }
            catch
            {
                //add relevant information into the parse error
                result = new ParseResult(new ParseError(_terminal.Name, position + 1));
                tokenReader.Reset(position);
                return false;
            }
        }
    }
}
