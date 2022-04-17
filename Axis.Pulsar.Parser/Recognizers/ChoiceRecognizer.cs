using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Recognizers
{
    public class ChoiceRecognizer : IRecognizer
    {
        public static readonly string PSEUDO_NAME = "#Choice";

        private readonly IRecognizer[] _recognizers;

        public Cardinality Cardinality { get; }

        public ChoiceRecognizer(Cardinality cardinality, params IRecognizer[] recognizers)
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
                int cycleCount = 0;
                int length = _recognizers.Length;
                RecognizerResult choice = null;
                do
                {
                    foreach (var recognizer in _recognizers)
                    {
                        if (recognizer.TryRecognize(tokenReader, out choice))
                        {
                            results.Add(choice);
                            break;
                        }
                    }
                }
                while (choice.Succeeded && Cardinality.CanRepeat(++cycleCount));


                var cycles = results.Count;
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

        public override string ToString() => Helper.AsString(Grammar.SymbolGroup.GroupingMode.Choice, Cardinality, _recognizers);
    }
}
