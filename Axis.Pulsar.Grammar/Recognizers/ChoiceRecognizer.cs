using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Recognizers.Results;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Grammar.Recognizers
{
    public class ChoiceRecognizer: IRecognizer
    {
        private readonly IRecognizer[] _recognizers;
        private readonly Choice _rule;

        /// <inheritdoc/>
        public IRule Rule => _rule;

        /// <inheritdoc/>
        public Language.Grammar Grammar { get; }


        public ChoiceRecognizer(Choice choice, Language.Grammar grammar)
        {
            _rule = choice.ThrowIfDefault(new ArgumentException($"Invalid {nameof(choice)}: {choice}"));
            Grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            _recognizers = choice.Rules
                .Select(rule => rule.ToRecognizer(Grammar))
                .ToArray();
        }

        public override string ToString() => $"Recognizer({_rule})";

        /// <summary>
        /// Performs the recognition operation based on the contents of the configuration of the choice <see cref="Rule"/>.
        /// Successfully recognizing this rule yields a <see cref="CST.CSTNode.BranchNode"/> with <see cref="Choice.SymbolName"/> as its symbol name,
        /// and each successfully recognized rule within the choice-list. 
        /// <para>
        /// Syntax tree structure:
        /// <list type="bullet">
        ///     <item>@Choice --> ...*</item>
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
                var results = new List<SuccessResult>();
                IReason.AggregationFailure mostSignificantAggregationFailure; 

                do
                {
                    mostSignificantAggregationFailure = null;
                    foreach (var recognizer in _recognizers)
                    {
                        currentResult = recognizer.Recognize(tokenReader);

                        if (currentResult is not FailureResult failure)
                            break; // break for Exception or Success or Partial

                        if (failure.Reason is IReason.AggregationFailure aggregation
                            && aggregation.AggregationCount > (mostSignificantAggregationFailure?.AggregationCount ?? 0))
                            mostSignificantAggregationFailure = aggregation;
                    }

                    if (currentResult is SuccessResult success)
                        results.Add(success);

                    else break;
                }
                while (_rule.Cardinality.CanRepeat(results.Count));

                #region abort
                if (currentResult is ErrorResult)
                {
                    _ = tokenReader.Reset(position);
                    result = currentResult;
                    return false;
                }
                #endregion

                #region success
                if (_rule.Cardinality.IsValidRange(results.Count))
                {
                    result = results
                        .Select(result => result.Symbol)
                        .Map(symbols => CSTNode.Of(Rule.SymbolName, symbols.ToArray()))
                        .Map(node => new SuccessResult(position + 1, node));

                    return true;
                }
                #endregion

                #region Failure
                IReason reason =
                    mostSignificantAggregationFailure != null ? mostSignificantAggregationFailure :
                    currentResult is FailureResult failureResult ? failureResult.Reason :
                    null;

                CSTNode[] passingSymbols = results
                    .Select(r => r.Symbol)
                    .ToArray();

                result = new FailureResult(
                    tokenReader.Position + 1,
                    IReason.Of(reason, passingSymbols));

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
