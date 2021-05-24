using Axis.Pulsar.Parser.Syntax;
using System;

namespace Axis.Pulsar.Parser
{
    public class ParseResult
    {
        public bool Succeeded { get; }

        public Symbol Symbol { get; }

        public ParseError Error { get; }


        public ParseResult(Symbol symbol)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Succeeded = true;
        }

        public ParseResult(ParseError error)
        {
            Error = error ?? throw new ArgumentNullException(nameof(error));
            Succeeded = false;
        }

    }
}
