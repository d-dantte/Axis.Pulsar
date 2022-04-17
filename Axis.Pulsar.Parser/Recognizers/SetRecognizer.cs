using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Recognizers
{
    public class SetRecognizer : IRecognizer
    {
        public static readonly string PSEUDO_NAME = "#Set";

        private readonly IRecognizer[] _recognizers;

        public Cardinality Cardinality { get; }

        public int RecognitionThreshold { get; }

        public SetRecognizer(int recognitionThreshold, Cardinality cardinality, params IRecognizer[] recognizers)
        {
            RecognitionThreshold = recognitionThreshold;
            Cardinality = cardinality;
            _recognizers = recognizers
                .ThrowIf(Extensions.IsNull, _ => new ArgumentNullException(nameof(recognizers)))
                .ThrowIf(Extensions.IsEmpty, _ => new ArgumentException("empty recognizer array supplied"));
        }

        public SetRecognizer(
            Cardinality cardinality,
            params IRecognizer[] recognizers)
            : this(1, cardinality, recognizers)
        { }

        public bool TryRecognize(BufferedTokenReader tokenReader, out RecognizerResult result)
        {
            var position = tokenReader.Position;
            try
            {
                var results = new List<RecognizerResult>();
                int cycleCount = 0;
                int length = _recognizers.Length;
                RecognizerResult setResult = null;
                do
                {
                    var setPosition = tokenReader.Position;
                    var tempChildren = _recognizers.ToList();
                    var setResults = new List<RecognizerResult>();
                    while (tempChildren.Count > 0)
                    {
                        for (int cnt = 0; cnt < tempChildren.Count; cnt++)
                        {
                            var recognizer = tempChildren[cnt];
                            if (recognizer.TryRecognize(tokenReader, out setResult))
                            {
                                setResults.Add(setResult);
                                tempChildren.Remove(recognizer);
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
                while (setResult.Succeeded && Cardinality.CanRepeat(++cycleCount));


                var cycles = results.Count / length;

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

        public Result Recognize(BufferedTokenReader tokenReader)
        {

        }

        public override string ToString() => Helper.AsString(Grammar.SymbolGroup.GroupingMode.Set, Cardinality, _recognizers);
    }
}
