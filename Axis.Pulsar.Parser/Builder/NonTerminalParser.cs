using Axis.Pulsar.Parser.Input;
using System;

namespace Axis.Pulsar.Parser.Builder
{
    public class NonTerminalParser : IParser
    {
        private readonly Language.NonTerminal _nonTerminal;

        private readonly ProductionParser productionParser;

        public NonTerminalParser(Language.NonTerminal nonTerminal)
        {
            _nonTerminal = nonTerminal ?? throw new ArgumentNullException(nameof(nonTerminal));

            productionParser = ProductionParserBuilder.BuildParser(_nonTerminal.Production);
        }

        public bool TryParse(BufferedTokenReader tokenReader, out ParseResult result)
        {
            if(productionParser.TryParse(tokenReader, out var presult))
            {
                result = new ParseResult(new Syntax.Symbol(
                    _nonTerminal.Name,
                    presult.Symbol.FlattenProduction()));

                return true;
            }
            else
            {
                result = presult;
                return false;
            }
        }
    }
}
