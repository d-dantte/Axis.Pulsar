using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Builder
{
    public class ChoiceParser : ProductionParser
    {
        public static readonly string PSEUDO_NAME = "#Choice";


        public ChoiceParser(IParser parser)
            : this(Cardinality.OccursOnlyOnce(), parser)
        { }

        public ChoiceParser(Cardinality cardinality, params IParser[] children)
            :base(cardinality, children)
        {
        }

        public override bool TryParse(BufferedTokenReader tokenReader, out ParseResult result)
        {
            var position = tokenReader.Position;
            try
            {
                var results = new List<ParseResult>();
                int cycleCount = 0;
                int length = Children.Length;
                var children = Children.ToArray();
                ParseResult choice = null;
                do
                {
                    foreach (var parser in children)
                    {
                        if (parser.TryParse(tokenReader, out choice))
                        {
                            results.Add(choice);
                            break;
                        }
                    }
                }
                while (choice.Succeeded && CanRepeat(++cycleCount));


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
                    result = new(new ParseError(PSEUDO_NAME, position + 1, choice.Error));
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
