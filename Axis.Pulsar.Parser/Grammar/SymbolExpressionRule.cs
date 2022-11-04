using System;

namespace Axis.Pulsar.Parser.Grammar
{

    /// <summary>
    /// Represents a grouping of other symbols.
    /// </summary>
    public record SymbolExpressionRule : INonTerminal
    {
        /// <inheritdoc/>
        public ISymbolExpression Value { get; }

        /// <inheritdoc/>
        public int? RecognitionThreshold { get; }

        /// <summary>
        /// When present, is called by the corresponding parse after it has parsed a token, but before it reports the parse as successful.
        /// </summary>
        public IRuleValidator<SymbolExpressionRule> RuleValidator { get; }

        public SymbolExpressionRule(
            ISymbolExpression expression,
            int? recognitionThreshold = null,
            IRuleValidator<SymbolExpressionRule> ruleValidator = null)
        {
            Value = expression;
            RuleValidator = ruleValidator;
            RecognitionThreshold = recognitionThreshold.ThrowIf(
                v => v <= 0,
                new ArgumentException($"{nameof(recognitionThreshold)} cannot be <= 0"));
        }
    }
}
