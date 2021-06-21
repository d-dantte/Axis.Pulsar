using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Builder
{
    public class SequenceParser : ProductionParser
    {
        public static readonly string PSEUDO_NAME = "#Sequence";



        public SequenceParser(IParser parser)
            : this(Cardinality.OccursOnlyOnce(), parser)
        { }

        public SequenceParser(Cardinality cardinality, params IParser[] children)
            :base(cardinality, children)
        {
        }

        public override bool TryParse(BufferedTokenReader tokenReader, out ParseResult result)
        {
            var position = tokenReader.Position;
            try
            {
                var results = new List<ParseResult>();
                ParseResult current = null;
                var children = Children.ToArray();
                int cycleCount = 0;
                do
                {
                    int tempPosition = position;
                    var cycleResults = new List<ParseResult>();
                    foreach(var parser in children)
                    {
                        if (parser.TryParse(tokenReader, out current))
                            cycleResults.Add(current);

                        else
                        {
                            tokenReader.Reset(tempPosition);
                            break;
                        }
                    }

                    if (current.Succeeded)
                        results.AddRange(cycleResults);
                }
                while (current.Succeeded && CanRepeat(++cycleCount));

                var cycles = results.Count / children.Length;
                if((cycles == 0 && Cardinality.MinOccurence == 0)
                    ||(cycles >= Cardinality.MinOccurence && (Cardinality.MaxOccurence == null || cycles <= Cardinality.MaxOccurence)))
                {
                    result = new (
                        new Syntax.Symbol(
                            PSEUDO_NAME,
                            results.Select(r => r.Symbol).ToArray()));

                    return true;
                }
                else
                {
                    tokenReader.Reset(position);
                    result = new(new ParseError(PSEUDO_NAME, position + 1, current.Error));
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
