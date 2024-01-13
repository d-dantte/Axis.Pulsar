using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.CST;
using Axis.Pulsar.Grammar.Exceptions;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Recognizers.Results;
using System;
using System.Linq;
using static Axis.Pulsar.Grammar.Language.Rules.ProductionValidationResult;

namespace Axis.Pulsar.Grammar.Recognizers
{
    public class ProductionRuleRecognizer : IRecognizer
    {
        private readonly ProductionRule _rule;
        private readonly IRecognizer _recognizer;

        /// <inheritdoc/>
        public IRule Rule => _rule;

        /// <inheritdoc/>
        public Language.Grammar Grammar { get; }

        public ProductionRuleRecognizer(ProductionRule rule, Language.Grammar grammar)
        {
            _rule = rule.ThrowIfDefault(_ => new ArgumentException($"Invalid {nameof(rule)}: default"));
            Grammar = grammar;
            _recognizer = rule.Rule.ToRecognizer(Grammar);
        }

        /// <summary>
        /// 
        /// <para>
        /// Syntax tree structure:
        /// <list type="bullet">
        ///     <item>[Production Symbol] --> EOF, @Literal, @Pattern, ...*</item>
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
                var recognizerResult = _recognizer.Recognize(tokenReader);
                result = recognizerResult switch
                {
                    SuccessResult success => IsValidatedCST(success.Symbol, out var validationError)
                        // success, and node is validated
                        ? new SuccessResult(
                            symbol: CSTNode.Of(_rule.SymbolName, Flatten(success.Symbol)),
                            inputPosition: position + 1)
                        // success, and non-validated node, report partial recognition
                        : new ErrorResult(
                            inputPosition: position + 1,
                            exception: new PartialRecognitionException(
                                _rule.SymbolName,
                                position + 1,
                                IReason.Of(validationError),
                                success.Symbol)),

                    FailureResult failure => IsPastRecognitionThreshold(failure)
                        // failed, with aggretation failure reason, and past threshold
                        ? new ErrorResult(
                            position + 1,
                            new PartialRecognitionException(
                                _rule.SymbolName,
                                position + 1,
                                failure.Reason,
                                (failure.Reason as IReason.AggregationFailure).PassingSymbols))
                        // every other failure
                        : failure,

                    ErrorResult exception => exception,

                    _ => tokenReader
                        .Reset(position)
                        .Map(_ => new ErrorResult(
                            exception: new InvalidOperationException($"Invalid result type: {recognizerResult?.GetType()}"),
                            inputPosition: position + 1))
                };

                return result is SuccessResult;
            }
            catch (Exception ex)
            {
                #region Error
                _ = tokenReader.Reset(position);
                result = new ErrorResult(position + 1, ex);
                return false;
                #endregion
            }
        }

        /// <inheritdoc/>
        public virtual IRecognitionResult Recognize(BufferedTokenReader tokenReader)
        {
            _ = TryRecognize(tokenReader, out var result);
            return result;
        }

        public override string ToString() => _recognizer.ToString();

        private bool IsValidatedCST(CSTNode node, out Error error)
        {
            var result = _rule.Validator?.ValidateCSTNode(_rule, CSTNode.Of(_rule.SymbolName, Flatten(node)));

            if (result is Error validationResult)
            {
                error = validationResult;
                return false;
            }

            error = null;
            return true;
        }

        private bool IsPastRecognitionThreshold(FailureResult result)
        {
            if(result.Reason is IReason.AggregationFailure aggregationFailure)
            {
                var productionTerminals = aggregationFailure.PassingSymbols
                    .SelectMany(Flatten)
                    .ToArray();

                return productionTerminals.Length >= _rule.RecognitionThreshold;
            }

            return false;
        }

        /// <summary>
        /// Traverses the symbol tree till either a <see cref="Literal.SymbolName"/>, <see cref="Pattern.SymbolName"/>,
        /// or a <see cref="ProductionRef.SymbolName"/> is found, returning a list of all found nodes.
        /// </summary>
        /// <param name="node">The root node from which the search starts</param>
        /// <returns>The flattened list of found nodes</returns>
        private static CSTNode[] Flatten(CSTNode node)
        {
            return node switch
            {
                #region Leaf nodes - for now, only EOF, Pattern, and Lietral are expected here.
                //CSTNode.LeafNode leaf
                //when leaf.SymbolName.Equals(Literal.LiteralSymbolName) => new[] { leaf },

                //CSTNode.LeafNode leaf
                //when leaf.SymbolName.Equals(Pattern.PatternSymbolName) => new[] { leaf },

                //CSTNode.LeafNode leaf
                //when leaf.SymbolName.Equals(EOF.EOFSymbolName) => new[] { leaf },

                //CSTNode.LeafNode leaf
                //when leaf.SymbolName.StartsWith("@") => new[] { leaf },

                CSTNode.LeafNode leaf => new[] { leaf },
                #endregion

                #region Branch nodes - ProductionRef is given special treatment.
                CSTNode.BranchNode branch
                when branch.SymbolName.EndsWith(ProductionRef.SymbolSuffix) => branch
                    .AllChildNodes()
                    .ToArray(),

                CSTNode.BranchNode branch => branch
                    .AllChildNodes()
                    .SelectMany(Flatten)
                    .ToArray(),
                #endregion

                _ => throw new ArgumentException($"Invalid node: {node}")
            };
        }
    }
}
