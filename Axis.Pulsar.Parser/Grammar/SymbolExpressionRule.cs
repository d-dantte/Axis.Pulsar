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

        public static implicit operator SymbolExpressionRule(SymbolGroup group) => new(group);

        public static implicit operator SymbolExpressionRule(SymbolRef @ref) => new(@ref);
    }
}
