using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Recognizers
{
    /// <summary>
    /// Recognizes tokens for a given <see cref="Grammar.SymbolGroup"/> that is configured with <see cref="Grammar.SymbolGroup.GroupingMode.Set"/>.
    /// </summary>
    public class SetRecognizer : IRecognizer
    {
        /// <summary>
        /// The name given to this node in the Concrete Syntax Tree node. (<see  cref="CST.CSTNode"/>)
        /// </summary>
        public static readonly string PSEUDO_NAME = "#Set";

        private readonly IRecognizer[] _recognizers;

        ///<inheritdoc/>
        public Cardinality Cardinality { get; }

        public SetRecognizer(Cardinality cardinality, params IRecognizer[] recognizers)
        {
            Cardinality = cardinality;

            _recognizers = recognizers
                .ThrowIf(Extensions.IsNull, new ArgumentNullException(nameof(recognizers)))
                .ThrowIf(Extensions.IsEmpty, new ArgumentException("Empty recognizer array supplied"))
                .ThrowIf(Extensions.ContainsNull, new ArgumentException("Recognizer array must not contain nulls"));
        }

        ///<inheritdoc/>
        public bool TryRecognize(BufferedTokenReader tokenReader, out IResult result)
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
                    var tempList = _recognizers.ToList();
                    var index = -1;
                    setResults = new List<IResult.Success>();
                    while (tempList.Count > 0)
                    {
                        foreach(var tuple in tempList.Select((recognizer, index) => (recognizer.Recognize(tokenReader), index)))
                        {
                            (setResult, index) = tuple;
                            if (setResult is not IResult.FailedRecognition)
                                break;
                        }

                        if (setResult is not IResult.Success)
                        {
                            tokenReader.Reset(setPosition);
                            break;
                        }

                        setResults.Add(setResult as IResult.Success);
                        tempList.RemoveAt(index);
                    }

                    if (setResult is IResult.Success)
                        results.AddRange(setResults);

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
                // Not enough symbols were recognized; this was a failed attempt
                var currentPosition = tokenReader.Position + 1;
                _ = tokenReader.Reset(position);
                result = setResult switch
                {
                    // null because we previously filered out only Success and Exception results.
                    IResult.FailedRecognition failed => new IResult.FailedRecognition(
                        results.Count + setResults?.Count ?? 0,
                        currentPosition,
                        failed.Reason),

                    IResult.Exception exception => exception,

                    _ => new IResult.Exception(
                        new InvalidOperationException($"Invalid result type: {setResult?.GetType()}"),
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
        public override string ToString() => Helper.AsString(Grammar.SymbolGroup.GroupingMode.Set, Cardinality, _recognizers);
    }
}
