using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Parsers
{
    public class SetParser : RuleParser
    {
        public static readonly string PSEUDO_NAME = "#Set";



        public SetParser(IParser parser)
            : this(Cardinality.OccursOnlyOnce(), parser)
        { }

        public SetParser(Cardinality cardinality, params IParser[] children)
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
                ParseResult setResult = null;
                do
                {
                    var setPosition = tokenReader.Position;
                    var tempChildren = Children.ToList();
                    var setResults = new List<ParseResult>();
                    while (tempChildren.Count > 0)
                    {
                        for (int cnt = 0; cnt < tempChildren.Count; cnt++)
                        {
                            var parser = tempChildren[cnt];
                            if (parser.TryParse(tokenReader, out setResult))
                            {
                                setResults.Add(setResult);
                                tempChildren.Remove(parser);
                                break;
                            }
                        }

                        if(!setResult.Succeeded)
                        {
                            tokenReader.Reset(setPosition);
                            break;
                        }
                    }

                    if (setResult.Succeeded == true)
                        results.AddRange(setResults);
                }
                while (setResult.Succeeded && CanRepeat(++cycleCount));


                var cycles = results.Count / length;
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
                    result = new(new ParseError(PSEUDO_NAME, position + 1, setResult.Error));
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

        public override string ToString() => $"Set[{Children.Length}]";
    }
}
