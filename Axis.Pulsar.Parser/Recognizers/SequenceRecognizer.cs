using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Recognizers
{
    /// <summary>
    /// Recognizes tokens for a given <see cref="Grammar.SymbolGroup"/> that is configured with <see cref="Grammar.SymbolGroup.GroupingMode.Sequence"/>.
    /// </summary>
    public class SequenceRecognizer : IRecognizer
    {
        /// <summary>
        /// The name given to this node in the Concrete Syntax Tree node. (<see  cref="CST.CSTNode"/>)
        /// </summary>
        public static readonly string PSEUDO_NAME = "#Sequence";

        private readonly IRecognizer[] _recognizers;

        ///<inheritdoc/>
        public Cardinality Cardinality { get; }

        public SequenceRecognizer(Cardinality cardinality, params IRecognizer[] recognizers)
        {
            Cardinality = cardinality;
            _recognizers = recognizers
                .ThrowIf(Extensions.IsNull, _ => new ArgumentNullException(nameof(recognizers)))
                .ThrowIf(Extensions.IsEmpty, _ => new ArgumentException("Empty recognizer array supplied"))
                .ThrowIf(Extensions.ContainsNull, new ArgumentException("Recognizer array must not contain nulls"));
        }

        ///<inheritdoc/>
        public bool TryRecognize(BufferedTokenReader tokenReader, out IResult result)
        {
            var position = tokenReader.Position;
            try
            {
                IResult currentResult = null;
                List<IResult.Success>
                    results = new(),
                    cycleResults = null;
                int
                    cycleCount = 0,
                    tempPosition = 0;
                do
                {
                    tempPosition = tokenReader.Position;
                    cycleResults = new List<IResult.Success>();
                    foreach (var recognizer in _recognizers)
                    {
                        currentResult = recognizer.Recognize(tokenReader);
                        if (currentResult is IResult.Success success)
                            cycleResults.Add(success);

                        else
                        {
                            tokenReader.Reset(tempPosition);
                            break;
                        }
                    }

                    if (currentResult is IResult.Success)
                        results.AddRange(cycleResults);

                    else break;
                }
                while (Cardinality.CanRepeat(++cycleCount));

                #region success
                var cycles = results.Count / _recognizers.Length;
                if (Cardinality.IsValidRange(cycles))
                {
                    result = results
                        .SelectMany(result => result.Symbols)
                        .Map(symbols => new IResult.Success(symbols));
                    return true;
                }
                #endregion

                #region Failed
                // else - Not enough symbols were recognized; this was a failed attempt
                var currentPosition = tokenReader.Position + 1;
                _ = tokenReader.Reset(position);
                result = currentResult switch
                {
                    IResult.FailedRecognition failed => new IResult.FailedRecognition(
                        failed.ExpectedSymbolName, // or should the SymbolRef of the current recognizer be used?
                        results.Count,
                        currentPosition),

                    IResult.Exception exception => exception,

                    _ => new IResult.Exception(
                        new InvalidOperationException($"Invalid result type: {currentResult?.GetType()}"),
                        currentPosition)
                };
                return false;
                #endregion
            }
            catch (Exception e)
            {
                #region Fatal
                _ = tokenReader.Reset(position);
                result = new IResult.Exception(e, position + 1);
                return false;
                #endregion
            }
        }

        ///<inheritdoc/>
        public IResult Recognize(BufferedTokenReader tokenReader)
        {
            _ = TryRecognize(tokenReader, out var result);
            return result;
        }

        ///<inheritdoc/>
        public override string ToString() => Helper.AsString(Grammar.SymbolGroup.GroupingMode.Sequence, Cardinality, _recognizers);
    }
}
