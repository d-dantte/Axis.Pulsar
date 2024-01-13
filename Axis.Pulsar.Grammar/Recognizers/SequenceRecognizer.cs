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
    public class SequenceRecognizer: IRecognizer
    {
        /// <summary>
        /// The name given to this node in the Concrete Syntax Tree node. (<see  cref="CST.CSTNode"/>)
        /// </summary>
        public const string PSEUDO_NAME = "@Sequence";

        private readonly IRecognizer[] _recognizers;
        private readonly Sequence _rule;

        /// <inheritdoc/>
        public IRule Rule => _rule;

        /// <inheritdoc/>
        public Language.Grammar Grammar { get; }

        public SequenceRecognizer(Sequence sequence, Language.Grammar grammar)
        {
            _rule = sequence.ThrowIfDefault(_ => new ArgumentException($"Invalid {nameof(sequence)}: {sequence}"));
            Grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            _recognizers = sequence.Rules
                .Select(rule => rule.ToRecognizer(Grammar))
                .ToArray();
        }

        public override string ToString() => $"Recognizer({_rule})";

        /// <summary>
        /// 
        /// <para>
        /// Syntax tree structure:
        /// <list type="bullet">
        ///     <item>@Repeatable --> @Sequence*</item>
        ///     <item>@Sequence --> ...*</item>
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
                int
                    cycleCount = 0,
                    tempPosition = 0;
                do
                {
                    tempPosition = tokenReader.Position;
                    cycleResults = new List<SuccessResult>();
                    foreach (var recognizer in _recognizers)
                    {
                        currentResult = recognizer.Recognize(tokenReader);
                        if (currentResult is SuccessResult success)
                            cycleResults.Add(success);

                        else
                        {
                            tokenReader.Reset(tempPosition);
                            break;
                        }
                    }

                    if (currentResult is SuccessResult)
                        results.Add(cycleResults.ToArray());

                    else break;
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
                if (_rule.Cardinality.IsValidRange(cycleCount)) //verify that cycleCount is always equal to results.Count
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
                IReason reason = currentResult is FailureResult failureResult 
                    ? failureResult.Reason
                    : null;

                CSTNode[] passingSymbols = results
                    .Append(cycleResults.ToArray())
                    .SelectMany()
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
