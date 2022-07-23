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

        public int RecognitionThreshold { get; }

        public ChoiceRecognizer(int recognitionThreshold, Cardinality cardinality, params IRecognizer[] recognizers)
        {
            RecognitionThreshold = recognitionThreshold;
            Cardinality = cardinality;
            _recognizers = recognizers
                .ThrowIf(Extensions.IsNull, _ => new ArgumentNullException(nameof(recognizers)))
                .ThrowIf(Extensions.IsEmpty, _ => new ArgumentException("empty recognizer array supplied"));
        }

        public ChoiceRecognizer(
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

        public IResult Recognize(BufferedTokenReader tokenReader)
        {
            var position = tokenReader.Position;
            try
            {
                IResult choice = null;
                List<IResult.Success>
                    results = new(),
                    cycleResults = null;
                int cycleCount = 0;
                do
                {
                    choice = _recognizers
                        .Select(recognizer => recognizer.Recognize(tokenReader))
                        .Where(result => result is IResult.Success)
                        .FirstOrDefault();

                    if (choice is IResult.Success success)
                        results.Add(success);

                    else break;
                }
                while (Cardinality.CanRepeat(++cycleCount));

                #region success
                var cycles = results.Count;
                if (Cardinality.MinOccurence == 0
                    && results.Count == 0
                    && cycleResults.Count < RecognitionThreshold)
                    return new IResult.Success();

                else if (Cardinality.IsValidRange(cycles)
                    && cycleResults.Count < RecognitionThreshold)
                {
                    return results
                        .SelectMany(result => result.Symbols)
                        .Map(symbols => new IResult.Success(symbols));
                }
                #endregion

                #region Partial
                else if ((results.Count + cycleResults.Count) >= RecognitionThreshold)
                {
                    _ = tokenReader.Reset(position);
                    return choice switch
                    {
                        IResult.PartialRecognition partial => new IResult.PartialRecognition(
                            expectedSymbol: partial.ExpectedSymbol,
                            inputPosition: partial.InputPosition,
                            recognizedSymbols: results
                                .AsEnumerable()
                                .Concat(cycleResults)
                                .SelectMany(result => result.Symbols)
                                .Concat(partial.RecognizedSymbols)),

                        IResult.FailedRecognition failed => new IResult.PartialRecognition(
                            expectedSymbol: failed.SymbolName,
                            inputPosition: failed.InputPosition,
                            recognizedSymbols: results
                                .AsEnumerable()
                                .Concat(cycleResults)
                                .SelectMany(result => result.Symbols)),

                        IResult.Exception exception => new IResult.Exception(exception.Error, exception.InputPosition),

                        _ => new IResult.Exception(
                            new Exception($"invaid result type: {choice.GetType()}"),
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

        public override string ToString() => Helper.AsString(Grammar.SymbolGroup.GroupingMode.Choice, Cardinality, _recognizers);
    }
}
