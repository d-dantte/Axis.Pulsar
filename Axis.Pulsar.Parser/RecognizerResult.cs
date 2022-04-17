using Axis.Pulsar.Parser.Syntax;
using System;

namespace Axis.Pulsar.Parser
{
    public class RecognizerResult
    {
        public bool Succeeded { get; }

        public Symbol[] Symbols { get; }

        public ParseError Error { get; }

        public RecognizerResult(params Symbol[] symbols)
        {
            Symbols = symbols.IsNullOrEmpty()
                ? throw new ArgumentNullException(nameof(symbols))
                : symbols;
            Succeeded = true;
        }

        public RecognizerResult(ParseError error)
        {
            Error = error ?? throw new ArgumentNullException(nameof(error));
            Succeeded = false;
        }
    }
}
