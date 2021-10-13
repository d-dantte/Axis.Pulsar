using Axis.Pulsar.Parser.Input;
using System;
using System.Diagnostics;

namespace Axis.Pulsar.Parser.Parsers
{
    public class ProductionParser : IParser
    {
        private readonly string _name;

        private readonly RuleParser _ruleParser;

        public ProductionParser(string name, RuleParser parser)
        {
            _ruleParser = parser ?? throw new ArgumentNullException(nameof(parser));
            _name = name.ThrowIf(
                string.IsNullOrWhiteSpace,
                n => new ArgumentException("Invalid name"));
        }

        [DebuggerStepThrough]
        public bool TryParse(BufferedTokenReader tokenReader, out ParseResult result)
        {
            var position = tokenReader.Position;
            if (_ruleParser.TryParse(tokenReader, out var presult))
            {
                result = new ParseResult(new Syntax.Symbol(
                    _name,
                    presult.Symbol.FlattenProduction()));

                return true;
            }
            else
            {
                result = new(new ParseError(
                    _name,
                    position + 1,
                    presult.Error));
                return false;
            }
        }
    }
}
