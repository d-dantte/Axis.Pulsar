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

        public SymbolExpressionRule(
            ISymbolExpression expression,
            int? recognitionThreshold = null)
        {
            Value = expression;
            RecognitionThreshold = recognitionThreshold.ThrowIf(
                v => v <= 0,
                new ArgumentException($"{nameof(recognitionThreshold)} cannot be <= 0"));
        }
    }
}
