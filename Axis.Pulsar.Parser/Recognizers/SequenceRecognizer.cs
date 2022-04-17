using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Recognizers
{
    public class SequenceRecognizer : IRecognizer
    {
        public static readonly string PSEUDO_NAME = "#Sequence";

        private readonly IRecognizer[] _recognizers;

        public Cardinality Cardinality { get; }

        public SequenceRecognizer(Cardinality cardinality, params IRecognizer[] recognizers)
        {
            Cardinality = cardinality;
            _recognizers = recognizers
                .ThrowIf(Extensions.IsNull, _ => new ArgumentNullException(nameof(recognizers)))
                .ThrowIf(Extensions.IsEmpty, _ => new ArgumentException("empty recognizer array supplied"));
        }

        public bool TryRecognize(BufferedTokenReader tokenReader, out RecognizerResult result)
        {
            var position = tokenReader.Position;
            try
            {
                var results = new List<RecognizerResult>();
                RecognizerResult current = null;
                int cycleCount = 0;
                do
                {
                    int tempPosition = tokenReader.Position;
                    var cycleResults = new List<RecognizerResult>();
                    foreach(var recognizer in _recognizers)
                    {
                        if (recognizer.TryRecognize(tokenReader, out current))
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
                while (current.Succeeded && Cardinality.CanRepeat(++cycleCount));

                var cycles = results.Count / _recognizers.Length;
                if (cycles == 0 && Cardinality.MinOccurence == 0)
                {
                    result = new RecognizerResult(new Syntax.Symbol(PSEUDO_NAME, ""));
                    return true;
                }
                else if (cycles >= Cardinality.MinOccurence && (Cardinality.MaxOccurence == null || cycles <= Cardinality.MaxOccurence))
                {
                    result = results
                        .SelectMany(result => result.Symbols)
                        .ToArray()
                        .Map(symbols => new RecognizerResult(symbols));

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

        public override string ToString() => Helper.AsString(Grammar.SymbolGroup.GroupingMode.Sequence, Cardinality, _recognizers);
    }
}
