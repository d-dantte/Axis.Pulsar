using Axis.Pulsar.Grammar.Recognizers;
using System;

namespace Axis.Pulsar.Grammar.Language.Rules
{
    /// <summary>
    /// Represents a reference to a production, using it's symbol name
    /// </summary>
    public struct ProductionRef: IAtomicRule, IRepeatable
    {
        public static readonly string SymbolSuffix = ".Ref";

        /// <summary>
        /// The name of the production symbol that this instance references
        /// </summary>
        public string ProductionSymbol { get; }

        /// <inheritdoc/>/>
        public string SymbolName => $"@{ProductionSymbol}{SymbolSuffix}";

        /// <inheritdoc />
        public Cardinality Cardinality { get; }

        public ProductionRef(
            string productionSymbolName,
            Cardinality? cardinality = null)
        {
            Cardinality = cardinality ?? Cardinality.OccursOnlyOnce();
            ProductionSymbol = productionSymbolName.ThrowIfNot(
                SymbolHelper.IsValidSymbolName,
                new ArgumentException($"Invalid symbol name: {productionSymbolName}"));
        }

        /// <inheritdoc />
        public override string ToString() => SymbolName;

        /// <inheritdoc/>
        public IRecognizer ToRecognizer(Grammar grammar) => new ProductionRefRecognizer(this, grammar);

        public override int GetHashCode() => HashCode.Combine(Cardinality, ProductionSymbol);

        public override bool Equals(object obj)
        {
            return obj is ProductionRef other
                && other.ProductionSymbol == ProductionSymbol
                && other.Cardinality.Equals(Cardinality);
        }

        public static bool operator ==(ProductionRef first, ProductionRef second) => first.Equals(second);
        public static bool operator !=(ProductionRef first, ProductionRef second) => !(first == second);
    }
}
