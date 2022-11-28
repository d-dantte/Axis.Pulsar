using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Recognizers
{
    /// <summary>
    /// Recognizes tokens for a given <see cref="Grammar.SymbolGroup"/> that is configured with <see cref="Grammar.SymbolGroup.GroupingMode.Set"/>.
    /// <para>
    /// NOTE: If any given expression is recognized successfully, but it's <see cref="Parser.CST.ICSTNode"/> array is empty - 
    /// meaning the expression was effectively optional, the set recognizes it as a failed recognition.
    /// </para>
    /// <para>
    /// The bottom line is, DO NOT place optional expressions inside sets
    /// </para>
    /// </summary>
    public class SetRecognizer : IRecognizer
    {
        /// <summary>
        /// The name given to this node in the Concrete Syntax Tree node. (<see  cref="CST.CSTNode"/>)
        /// </summary>
        public static readonly string PSEUDO_NAME = "#Set";

        private readonly IRecognizer[] _recognizers;
        private readonly SymbolGroup.Set _set;

        ///<inheritdoc/>
        public Cardinality Cardinality => _set.Cardinality;

        /// <summary>
        /// See <see cref="SymbolGroup.Set.MinContentCount"/>
        /// </summary>
        public int? MinContentCount => _set.MinContentCount;

        public SetRecognizer(SymbolGroup.Set set, params IRecognizer[] recognizers)
        {
            _set = set ?? throw new ArgumentNullException(nameof(set));

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
                    cyclePosition = 0;
                do
                {
                    cyclePosition = tokenReader.Position;
                    var tempList = _recognizers.ToList();
                    var index = -1;
                    setResults = new List<IResult.Success>();
                    while (tempList.Count > 0)
                    {
                        foreach(var tuple in tempList.Select((recognizer, _index) => (recognizer.Recognize(tokenReader), _index)))
                        {
                            (setResult, index) = tuple;

                            if (setResult is IResult.FailedRecognition)
                                continue;

                            else if (setResult is IResult.Success success && success.IsOptionalRecognition)
                                continue;

                            else break;
                        }

                        if (setResult is not IResult.Success)
                            break;

                        else
                        {
                            setResults.Add(setResult as IResult.Success);
                            tempList.RemoveAt(index);
                        }
                    }

                    if (setResults.Count >= (MinContentCount ?? _recognizers.Length))
                        results.AddRange(setResults);

                    else
                    {
                        tokenReader.Reset(cyclePosition);
                        break;
                    }
                }
                while (Cardinality.CanRepeat(++cycleCount));

                #region success
                if (Cardinality.IsValidRange(cycleCount))
                {
                    var recognizedSymbols = results
                        .SelectMany(result => result.Symbols)
                        .ToArray();
                    result = new IResult.Success(recognizedSymbols);
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
        public override string ToString() => $"#{MinContentCount}{Helper.AsString(SymbolGroup.GroupingMode.Set, Cardinality, _recognizers)}";
    }
}
