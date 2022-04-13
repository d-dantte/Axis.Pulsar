using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Grammar;
using System;

namespace Axis.Pulsar.Parser.Parsers
{
    public class LiteralParser: RuleParser
    {
        private readonly LiteralRule _terminal;
        private readonly string _symbolName;

        public LiteralParser(string symbolName, LiteralRule terminal)
            :base(Utils.Cardinality.OccursOnlyOnce())
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            _symbolName = symbolName.ThrowIf(
                string.IsNullOrWhiteSpace,
                _ => new ArgumentException("Invalid symbol name"));
        }

        public override bool TryParse(BufferedTokenReader tokenReader, out ParseResult result)
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
                            _symbolName,
                            new string(tokens)));

                    return true;
                }

                //add relevant information into the parse error
                result = new ParseResult(new ParseError(_symbolName, position + 1));
                tokenReader.Reset(position);
                return false;
            }
            catch
            {
                //add relevant information into the parse error
                result = new ParseResult(new ParseError(_symbolName, position + 1));
                tokenReader.Reset(position);
                return false;
            }
        }

        public override string ToString() => $"{_symbolName}[{_terminal.Value}]";
    }
}
