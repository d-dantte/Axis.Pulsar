using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Language;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Builder
{
    public class SingleSymbolParser : ProductionParser
    {
        public static readonly string PSEUDO_NAME = "#Single";

        public IParser Parser { get; }


        public SingleSymbolParser(Cardinality cardinality, IParser parser)
            :base(cardinality, new[] { parser })
        {
            if (parser == null)
                throw new ArgumentNullException(nameof(parser));

            else
            {
                Parser = parser;
            }
        }

        public override bool TryParse(BufferedTokenReader tokenReader, out ParseResult result)
        {
            var position = tokenReader.Position;
            try
            {
                var results = new List<ParseResult>();
                var cycleCount = 0;
                ParseResult cycleResult = null;
                do
                {
                    if (Parser.TryParse(tokenReader, out cycleResult))
                        results.Add(cycleResult);
                }
                while (CanRepeat(++cycleCount));


                var cycles = results.Count;
                if ((cycles == 0 && Cardinality.MinOccurence == 0)
                    || (cycles >= Cardinality.MinOccurence && (Cardinality.MaxOccurence == null || cycles <= Cardinality.MaxOccurence)))
                {
                    result = new(new Syntax.Symbol(
                        PSEUDO_NAME,
                        results.Select(r => r.Symbol).ToArray()));

                    return true;
                }
                else
                {
                    tokenReader.Reset(position);
                    result = new(new ParseError(PSEUDO_NAME, position + 1, cycleResult.Error));
                    return false;
                }
            }
            catch
            {
                tokenReader.Reset(position);
                result = new(new ParseError(PSEUDO_NAME, position + 1));
                return false;
            }
        }
    }
}
