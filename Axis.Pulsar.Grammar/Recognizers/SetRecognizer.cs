using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Recognizers.Results;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Grammar.Recognizers
{
    /// <summary>
    /// Set recognizer.
    /// <para>
    /// Note: when optional recognizers are present within a set recognizer, they get added to the cycle-result last, in reverse order,
    /// for each one that recognized optionally.
    /// </para>
    /// </summary>
    public class SetRecognizer: IRecognizer
    {
        /// <summary>
        /// The name given to this node in the Concrete Syntax Tree node. (<see  cref="CST.CSTNode"/>)
        /// </summary>
        public const string PSEUDO_NAME = "@Set";

        private readonly IRecognizer[] _recognizers;
        private readonly Set _rule;

        /// <inheritdoc/>
        public IRule Rule => _rule;

        /// <inheritdoc/>
        public Language.Grammar Grammar { get; }

        public SetRecognizer(Set set, Language.Grammar grammar)
        {
            _rule = set.ThrowIfDefault(_ => new ArgumentException($"Invalid {nameof(set)}: {set}"));
            Grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            _recognizers = set.Rules
                .Select(rule => rule.ToRecognizer(Grammar))
                .ToArray();
        }

        public override string ToString() => $"Recognizer({_rule})";

        /// <summary>
        /// 
        /// <para>
        /// Syntax tree structure:
        /// <list type="bullet">
        ///     <item>@Repeatable --> @Set*</item>
        ///     <item>@Set --> ...*</item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="tokenReader"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public virtual bool TryRecognize(BufferedTokenReader tokenReader, out IRecognitionResult result)
        {
            var position = tokenReader.Position;
            try
            {
                IRecognitionResult currentResult = null;
                List<SuccessResult[]> results = new();
                List<SuccessResult> cycleResults = null;
                IReason.AggregationFailure mostSignificantAggregationFailure;

                int
                    cycleCount = 0,
                    cyclePosition = 0;
                do
                {
                    cyclePosition = tokenReader.Position;
                    var tempList = _recognizers.ToList();
                    var index = -1;
                    cycleResults = new List<SuccessResult>();
                    mostSignificantAggregationFailure = null;
                    while (tempList.Count > 0)
                    {
                        foreach (var tuple in tempList.Select((recognizer, _index) => (recognizer.Recognize(tokenReader), _index)))
                        {
                            (currentResult, index) = tuple;

                            if (currentResult is FailureResult failure)
                            {
                                if (failure.Reason is IReason.AggregationFailure aggregation
                                    && aggregation.AggregationCount > (mostSignificantAggregationFailure?.AggregationCount ?? 0))
                                    mostSignificantAggregationFailure = aggregation;

                                continue;
                            }

                            else if (currentResult is SuccessResult success && success.IsOptionalRecognition)
                                continue;

                            else break;
                        }

                        if (currentResult is not SuccessResult)
                            break;

                        // Note that even 'success.IsOptionalRecognition' results (correctly) end up in here.
                        else
                        {
                            cycleResults.Add(currentResult as SuccessResult);
                            tempList.RemoveAt(index);
                        }
                    }

                    if (cycleResults.Count >= (_rule.MinRecognitionCount ?? _recognizers.Length))
                        results.Add(cycleResults.ToArray());

                    else
                    {
                        tokenReader.Reset(cyclePosition);
                        break;
                    }
                }
                while (_rule.Cardinality.CanRepeat(++cycleCount));

                #region abort
                if (currentResult is ErrorResult)
                {
                    _ = tokenReader.Reset(position);
                    result = currentResult;
                    return false;
                }
                #endregion

                #region success
                if (_rule.Cardinality.IsValidRange(cycleCount))
                {
                    result = results
                        .Select(successes => successes.Select(r => r.Symbol))
                        .Select(nodes => CSTNode.Of(Rule.SymbolName, nodes.ToArray()))
                        .Map(symbols => CSTNode.Of(IRepeatable.SymbolName, symbols.ToArray()))
                        .Map(node => new SuccessResult(position + 1, node));

                    return true;
                }
                #endregion

                #region Failure
                IReason reason =
                    mostSignificantAggregationFailure != null ? mostSignificantAggregationFailure :
                    currentResult is FailureResult failureResult ? failureResult.Reason :
                    null;

                CSTNode[] passingResults = results
                    .Append(cycleResults.ToArray())
                    .SelectMany()
                    .Select(r => r.Symbol)
                    .ToArray();

                result = new FailureResult(
                    tokenReader.Position + 1,
                    IReason.Of(reason, passingResults));

                _ = tokenReader.Reset(position);

                return false;
                #endregion
            }
            catch (Exception e)
            {
                #region Error
                _ = tokenReader.Reset(position);
                result = new ErrorResult(position + 1, e);
                return false;
                #endregion
            }
        }

        ///<inheritdoc/>
        public virtual IRecognitionResult Recognize(BufferedTokenReader tokenReader)
        {
            _ = TryRecognize(tokenReader, out var result);
            return result;
        }
    }
}
