using System;

namespace Axis.Pulsar.Parser.Grammar
{

    /// <summary>
    /// Represents a grouping of other symbols.
    /// </summary>
    public class SymbolExpressionRule : INonTerminal
    {
        /// <inheritdoc/>
        public ISymbolExpression Value { get; }

        /// <inheritdoc/>
        public int? RecognitionThreshold { get; }

        public SymbolExpressionRule(
            int recognitionThreshold,
            ISymbolExpression expression)
        {
            Value = expression;
            RecognitionThreshold = recognitionThreshold.ThrowIf(
                v => v <= 0,
                new ArgumentException($"{nameof(recognitionThreshold)} cannot be <= 0"));
        }
    }
}
