using System;

namespace Axis.Pulsar.Parser.Grammar
{
    public class SymbolRef: ISymbolExpression
    {
        public string ProductionSymbol { get; }


        public SymbolRef(string productionSymbol)
        {
            ProductionSymbol = !string.IsNullOrWhiteSpace(productionSymbol)
                ? productionSymbol
                : throw new ArgumentException($"Invalid argument: {nameof(productionSymbol)}");
        }

        public override string ToString() => ProductionSymbol;

        public override bool Equals(object obj)
        {
            return obj is SymbolRef other
                && ProductionSymbol.Equals(other.ProductionSymbol);
        }

        public override int GetHashCode() => ProductionSymbol.GetHashCode();

    }
}
