using System;

namespace Axis.Pulsar.Parser.Grammar
{

    /// <summary>
    /// 
    /// </summary>
    public class SymbolExpressionRule : INonTerminal
    {
        public ISymbolExpression Value { get; }

        public int RecognitionThreshold { get; }

        public SymbolExpressionRule(
            int recognitionThreshold,
            ISymbolExpression expression)
        {
            Value = expression;
            RecognitionThreshold = recognitionThreshold.ThrowIf(
                value => value <= 0,
                _ => new ArgumentException($"{nameof(recognitionThreshold)} must be >= 1"));
        }
    }
}
