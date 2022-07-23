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

                        if (!setResult.Succeeded)
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

        public IResult Recognize(BufferedTokenReader tokenReader)
        {
            var position = tokenReader.Position;
            try
            {
                IResult setResult = null;
                List<IResult.Success>
                    results = new(),
                    setResults = null;

                int
                    cycleCount = 0,
                    setPosition = 0;
                do
                {
                    setPosition = tokenReader.Position;
                    var tempChildren = _recognizers.ToList();
                    var index = -1;
                    setResults = new List<IResult.Success>();
                    while (tempChildren.Count > 0)
                    {
                        (setResult, index) = tempChildren
                            .Select((recognizer, index) => (Result: recognizer.Recognize(tokenReader), Index: index))
                            .Where(result => result.Result is IResult.Success)
                            .FirstOrDefault();

                        if (setResult is not IResult.Success)
                        {
                            tokenReader.Reset(setPosition);
                            break;
                        }

                        setResults.Add(setResult as IResult.Success);
                        tempChildren.RemoveAt(index);
                    }

                    if (setResult is IResult.Success)
                        results.AddRange(setResults);

                    else break;
                }
                while (Cardinality.CanRepeat(++cycleCount));

                #region success
                var cycles = results.Count / _recognizers.Length;
                if (Cardinality.MinOccurence == 0
                    && results.Count == 0
                    && setResults.Count < RecognitionThreshold)
                    return new IResult.Success();

                else if (Cardinality.IsValidRange(cycles)
                    && setResults.Count < RecognitionThreshold)
                {
                    return results
                        .SelectMany(result => result.Symbols)
                        .Map(symbols => new IResult.Success(symbols));
                }
                #endregion

                #region Partial
                else if (Helper.SymbolCount(results.AsEnumerable().Concat(setResults)) >= RecognitionThreshold)
                {
                    _ = tokenReader.Reset(position);
                    return setResult switch
                    {
                        IResult.PartialRecognition partial => new IResult.PartialRecognition(
                            expectedSymbol: partial.ExpectedSymbol,
                            inputPosition: partial.InputPosition,
                            recognizedSymbols: results
                                .AsEnumerable()
                                .Concat(setResults)
                                .SelectMany(result => result.Symbols)
                                .Concat(partial.RecognizedSymbols)),

                        IResult.FailedRecognition failed => new IResult.PartialRecognition(
                            expectedSymbol: failed.SymbolName,
                            inputPosition: failed.InputPosition,
                            recognizedSymbols: results
                                .AsEnumerable()
                                .Concat(setResults)
                                .SelectMany(result => result.Symbols)),

                        IResult.Exception exception => new IResult.Exception(exception.Error, exception.InputPosition),

                        _ => new IResult.Exception(
                            new Exception($"invaid result type: {setResult.GetType()}"),
                            position)
                    };
                }
                #endregion

                #region Failed
                // Not enough symbols were recognized; this was a failed attempt
                var currentPosition = tokenReader.Position + 1;
                _ = tokenReader.Reset(position);

                return new IResult.FailedRecognition(PSEUDO_NAME, currentPosition);
                #endregion
            }
            catch (Exception e)
            {
                _ = tokenReader.Reset(position);
                return new IResult.Exception(e, position + 1);
            }
        }

        public override string ToString() => Helper.AsString(Grammar.SymbolGroup.GroupingMode.Set, Cardinality, _recognizers);
    }
}
