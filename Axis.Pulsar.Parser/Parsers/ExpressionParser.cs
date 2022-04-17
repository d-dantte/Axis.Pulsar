using Axis.Pulsar.Parser.Input;
using System;

namespace Axis.Pulsar.Parser.Parsers
{
    public class ExpressionParser : IParser
    {
        private readonly IRecognizer _recognizer;

        public string SymbolName { get; }

        public ExpressionParser(string symbolName, IRecognizer recognizer)
        {
            _recognizer = recognizer ?? throw new ArgumentNullException(nameof(recognizer));
            SymbolName = symbolName.ThrowIf(
                string.IsNullOrWhiteSpace,
                n => new ArgumentException("Invalid name"));
        }

        public bool TryParse(BufferedTokenReader tokenReader, out ParseResult result)
        {
            var position = tokenReader.Position;
            try
            {
                if (_recognizer.TryRecognize(tokenReader, out var presult))
                {
                    result = new ParseResult(new Syntax.Symbol(
                        SymbolName,
                        presult.Symbols));

                    return true;
                }
                else
                {
                    result = new(new ParseError(
                        SymbolName,
                        position + 1,
                        presult.Error));
                    return false;
                }
            }
            catch
            {
                //add relevant information into the parse error
                result = new ParseResult(new ParseError(SymbolName, position + 1));
                tokenReader.Reset(position);
                return false;
            }
        }

        public override string ToString() => _recognizer.ToString();
    }
}
