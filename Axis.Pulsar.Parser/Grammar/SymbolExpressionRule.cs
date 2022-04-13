namespace Axis.Pulsar.Parser.Grammar
{

    /// <summary>
    /// 
    /// </summary>
    public class SymbolExpressionRule : INonTerminal
    {
        public ISymbolExpression Value { get; }

        public SymbolExpressionRule(ISymbolExpression expression)
        {
            Value = expression;
        }
    }
}
