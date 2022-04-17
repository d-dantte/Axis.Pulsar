using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Recognizers
{
    public class SymbolRefRecognizer : IRecognizer
    {
        public static readonly string PSEUDO_NAME = "#Ref";

        private readonly Grammar.Grammar _grammar;

        public Cardinality Cardinality { get; }

        public int RecognitionThreshold { get; }

        public string SymbolRef { get; }

        public SymbolRefRecognizer(string symbolRef, Grammar.Grammar grammar)
            :this(symbolRef, 1, Cardinality.OccursOnlyOnce(), grammar)
        {
        }

        public SymbolRefRecognizer(
            string symbolRef,
            int recognitionThreshold,
            Cardinality cardinality,
            Grammar.Grammar grammar)
        {
            RecognitionThreshold = recognitionThreshold;
            Cardinality = cardinality;
            _grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            SymbolRef = symbolRef.ThrowIf(
                string.IsNullOrWhiteSpace,
                _ => new ArgumentNullException(nameof(symbolRef)));
        }

        public bool TryRecognize(BufferedTokenReader tokenReader, out RecognizerResult result)
        {
            var position = tokenReader.Position;
            var _parser = _grammar.GetParser(SymbolRef) ?? throw new InvalidOperationException($"This recognizer represents an invalid symbol-ref: {SymbolRef}");

            try
            {
                var results = new List<ParseResult>();
                ParseResult current = null;
                int cycleCount = 0;
                do
                {
                    if (_parser.TryParse(tokenReader, out current))
                        results.Add(current);
                }
                while (current.Succeeded && Cardinality.CanRepeat(++cycleCount));

                var cycles = results.Count;
                if (cycles == 0 && Cardinality.MinOccurence == 0)
                {
                    result = new RecognizerResult(new Syntax.Symbol(ToString(), ""));
                    return true;
                }
                else if (cycles >= Cardinality.MinOccurence && (Cardinality.MaxOccurence == null || cycles <= Cardinality.MaxOccurence))
                {
                    result = results
                        .Select(result => result.Symbol)
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
            catch(Exception e)
            {
                tokenReader.Reset(position);
                result = new(new ParseError(PSEUDO_NAME, position + 1, e.Message));
                return false;
            }
        }

        public Result Recognize(BufferedTokenReader tokenReader)
        {
            var position = tokenReader.Position;
            var _parser = _grammar.GetParser(SymbolRef) ?? throw new InvalidOperationException($"This recognizer represents an invalid symbol-ref: {SymbolRef}");

            try
            {
                var results = new List<Parsers.Result.Success>();
                Parsers.Result currentResult;
                int cycleCount = 0;
                do
                {
                    currentResult = _parser.Parse(tokenReader);
                    if (currentResult is Parsers.Result.Success success)
                        results.Add(success);

                    else break;
                }
                while (Cardinality.CanRepeat(++cycleCount));

                #region Success
                if (results.Count == 0 && Cardinality.MinOccurence == 0)
                    return new Result.Success(new Syntax.Symbol(SymbolRef, ""));

                else if (Cardinality.IsValidRange(results.Count))
                {
                    return results
                        .Select(result => result.Symbol)
                        .Map(symbols => new Result.Success(symbols));
                }
                #endregion

                // If we got this far, currentResult is definitely not 'Success', and results.Count is definitely < Cardinality.MinOccurence

                #region Partial
                // If we have a minimum of the threshold symbols recognized, we need to report a partial result
                if (results.Count >= RecognitionThreshold)
                {
                    var recognizedSymbols = results
                        .Select(result => result.Symbol)
                        .Map(symbols => new Result.Success(symbols));
                    _ = tokenReader.Reset(position);

                    return currentResult switch
                    {
                        Parsers.Result.PartialRecognition partial => new Result.PartialRecognition(
                            expectedSymbol: partial.ExpectedSymbol,
                            inputPosition: partial.InputPosition,
                            recognizedSymbols: results
                                .Select(result => result.Symbol)
                                .Concat(partial.PartialSymbol)),

                        Parsers.Result.FailedRecognition failed => new Result.PartialRecognition(
                            expectedSymbol: failed.SymbolName,
                            inputPosition: failed.InputPosition,
                            recognizedSymbols: results.Select(result => result.Symbol)),

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

                return new Result.FailedRecognition(SymbolRef, currentPosition);
                #endregion
            }
            catch (Exception e)
            {
                _ = tokenReader.Reset(position);
                return new Result.Exception(e, position + 1);
            }
        }

        public override string ToString() => $"${SymbolRef}";
    }
}
