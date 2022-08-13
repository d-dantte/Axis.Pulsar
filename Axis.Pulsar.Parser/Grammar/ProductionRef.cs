using Axis.Pulsar.Parser.Utils;
using System;
using System.Linq;

namespace Axis.Pulsar.Parser.Grammar
{
    /// <summary>
    /// A <see cref="ProductionRef"/> represents a reference to a production.
    /// </summary>
    public class ProductionRef: ISymbolExpression
    {
        public string ProductionSymbol { get; }

        public Cardinality Cardinality { get; }

        public ProductionRef(string productionSymbol, Cardinality cardinality)
        {
            Cardinality = cardinality;
            ProductionSymbol = !string.IsNullOrWhiteSpace(productionSymbol)
                ? productionSymbol
                : throw new ArgumentException($"Invalid argument: {nameof(productionSymbol)}");
        }

        public override string ToString() => ProductionSymbol;

        public override bool Equals(object obj)
        {
            return obj is ProductionRef other
                && ProductionSymbol.Equals(other.ProductionSymbol);
        }

        public override int GetHashCode() => ProductionSymbol.GetHashCode();

        public static ProductionRef[] Create(params string[] refs) => refs.Select(@ref => (ProductionRef)@ref).ToArray();

        public static implicit operator ProductionRef(string symbolName) => new(symbolName, Cardinality.OccursOnlyOnce());
    }
}
