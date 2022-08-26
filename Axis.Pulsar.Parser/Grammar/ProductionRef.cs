using Axis.Pulsar.Parser.Utils;
using System;
using System.Linq;

namespace Axis.Pulsar.Parser.Grammar
{
    /// <summary>
    /// A <see cref="ProductionRef"/> represents a reference to a production.
    /// This should be a struct
    /// </summary>
    public record ProductionRef: ISymbolExpression
    {
        /// <summary>
        /// The symbol for the production this instance refers to.
        /// </summary>
        public string ProductionSymbol { get; }

        /// <inheritdoc />
        public Cardinality Cardinality { get; }

        public ProductionRef(string productionSymbol, Cardinality cardinality)
        {
            Cardinality = cardinality;
            ProductionSymbol = !string.IsNullOrWhiteSpace(productionSymbol)
                ? productionSymbol
                : throw new ArgumentException($"Invalid argument: {nameof(productionSymbol)}");
        }

        /// <inheritdoc />
        public override string ToString() => ProductionSymbol;

        public static ProductionRef[] Create(params string[] refs) => refs.Select(@ref => (ProductionRef)@ref).ToArray();

        public static implicit operator ProductionRef(string symbolName) => new(symbolName, Cardinality.OccursOnlyOnce());
    }
}
