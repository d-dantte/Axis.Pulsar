using Axis.Pulsar.Parser.Utils;
using System;
using System.Linq;

namespace Axis.Pulsar.Parser.Grammar
{
    public class SymbolRef: ISymbolExpression
    {
        public string ProductionSymbol { get; }

        public Cardinality Cardinality { get; }


        public SymbolRef(string productionSymbol, Cardinality cardinality)
        {
            Cardinality = cardinality;
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

        public static SymbolRef[] Create(params string[] refs) => refs.Select(@ref => (SymbolRef)@ref).ToArray();

        public static implicit operator SymbolRef(string symbolName) => new(symbolName, Cardinality.OccursOnlyOnce());
    }
}
