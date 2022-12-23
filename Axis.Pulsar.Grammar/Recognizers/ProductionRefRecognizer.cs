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
    public class ProductionRefRecognizer: IRecognizer
    {
        private readonly ProductionRef _rule;

        /// <inheritdoc/>
        public IRule Rule => _rule;

        /// <inheritdoc/?
        public Language.Grammar Grammar { get; }

        public ProductionRefRecognizer(
            ProductionRef @ref,
            Language.Grammar grammar)
        {
            _rule = @ref.ThrowIfDefault(new ArgumentException(nameof(@ref)));
            Grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
        }

        /// <summary>
        /// Performs the recognition operation on the encapsulated <see cref="Rule"/>, and using the configured <see cref="IRepeatable.Cardinality"/>
        /// Successfully recognizing this rule yields a <see cref="CST.CSTNode.BranchNode"/> with <see cref="ProductionRef.SymbolName"/> as its symbol name,
        /// and each successfully recognized repetition of its encapsulated rule.
        /// <para>
        /// Syntax tree structure:
        /// <list type="bullet">
        ///     <item>@[Production Symbol].Ref --> [Production Symbol]*</item>
        ///     <item>[Production Symbol] --> ...</item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="tokenReader"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public virtual bool TryRecognize(BufferedTokenReader tokenReader, out IRecognitionResult result)
        {
            var position = tokenReader.Position;
            var _recognizer = Grammar
                .GetRecognizer(_rule.ProductionSymbol) 
                ?? throw new InvalidOperationException($"This recognizer represents an invalid symbol-ref: {_rule.SymbolName}");

            try
            {
                var results = new List<SuccessResult>();
                IRecognitionResult currentResult;
                int cycleCount = 0;
                do
                {
                    currentResult = _recognizer.Recognize(tokenReader);

                    if (currentResult is SuccessResult success)
                        results.Add(success);

                    else break;
                }
                while (_rule.Cardinality.CanRepeat(++cycleCount));

                // abort
                if (currentResult is ErrorResult)
                {
                    _ = tokenReader.Reset(position);
                    result = currentResult;
                    return false;
                }

                #region Success
                if (_rule.Cardinality.IsValidRange(results.Count))
                {
                    result = results
                        .Select(r => r.Symbol)
                        .Map(symbols => CSTNode.Of(Rule.SymbolName, symbols.ToArray()))
                        .Map(node => new SuccessResult(position + 1, node));
                    return true;
                }
                #endregion

                #region Failed
                // aggregation failure: not enough successful recognitions of encapsulated rule
                var reason = currentResult is FailureResult failure ? failure.Reason : null;
                result = new FailureResult(
                    tokenReader.Position + 1,
                    IReason.Of(
                        failureReason: reason,
                        passingSymbols: results
                            .Select(r => r.Symbol)
                            .ToArray())); 
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
