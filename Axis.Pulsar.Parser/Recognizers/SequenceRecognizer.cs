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

        public int RecognitionThreshold { get; }

        public SequenceRecognizer(int recognitionThreshold, Cardinality cardinality, params IRecognizer[] recognizers)
        {
            RecognitionThreshold = recognitionThreshold;
            Cardinality = cardinality;
            _recognizers = recognizers
                .ThrowIf(Extensions.IsNull, _ => new ArgumentNullException(nameof(recognizers)))
                .ThrowIf(Extensions.IsEmpty, _ => new ArgumentException("empty recognizer array supplied"));
        }

        public SequenceRecognizer(
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

        public Result Recognize(BufferedTokenReader tokenReader)
        {
            var position = tokenReader.Position;
            try
            {
                Result currentResult = null;
                List<Result.Success> 
                    results = new(),
                    cycleResults = null;
                int 
                    cycleCount = 0,
                    tempPosition = 0;
                do
                {
                    tempPosition = tokenReader.Position;
                    cycleResults = new List<Result.Success>();
                    foreach (var recognizer in _recognizers)
                    {
                        currentResult = recognizer.Recognize(tokenReader);
                        if (currentResult is Result.Success success)
                            cycleResults.Add(success);

                        else
                        {
                            tokenReader.Reset(tempPosition);
                            break;
                        }
                    }

                    if (currentResult is Result.Success)
                        results.AddRange(cycleResults);

                    else break;
                }
                while (Cardinality.CanRepeat(++cycleCount));

                #region success
                var cycles = results.Count / _recognizers.Length;
                if (Cardinality.MinOccurence == 0
                    && results.Count == 0
                    && cycleResults.Count < RecognitionThreshold)
                    return new Result.Success();

                else if (Cardinality.IsValidRange(cycles)
                    && cycleResults.Count < RecognitionThreshold)
                {
                    return results
                        .SelectMany(result => result.Symbols)
                        .Map(symbols => new Result.Success(symbols));
                }
                #endregion

                #region Prtial
                else if((results.Count + cycleResults.Count) >= RecognitionThreshold)
                {
                    return currentResult switch
                    {
                        Parsers.Result.PartialRecognition partial => new Result.PartialRecognition(
                            expectedSymbol: partial.ExpectedSymbol,
                            inputPosition: partial.InputPosition,
                            recognizedSymbols: results
                                .Concat(cycleResults)
                                .SelectMany(result => result)
                                .SelectMany(result => result.Symbols)
                                .Concat(partial.PartialSymbol)),

                        Parsers.Result.FailedRecognition failed => new Result.PartialRecognition(
                            expectedSymbol: failed.SymbolName,
                            inputPosition: failed.InputPosition,
                            recognizedSymbols: results
                                .Concat(cycleResults)
                                .SelectMany(result => result)
                                .SelectMany(result => result.Symbols)),

                        Parsers.Result.Exception exception => new Result.Exception(exception.Error, exception.InputPosition),

                        _ => new Result.Exception(
                            new Exception($"invaid result type: {currentResult.GetType()}"),
                            position)
                    };
                }
                #endregion

                #region Failed
                // Not enough symbols were recognized; this was a failed attempt
                var currentPosition = tokenReader.Position + 1;
                _ = tokenReader.Reset(position);

                return new Result.FailedRecognition(PSEUDO_NAME, currentPosition);
                #endregion
            }
            catch (Exception e)
            {
                _ = tokenReader.Reset(position);
                return new Result.Exception(e, position + 1);
            }
        }

        public override string ToString() => Helper.AsString(Grammar.SymbolGroup.GroupingMode.Sequence, Cardinality, _recognizers);
    }
}
