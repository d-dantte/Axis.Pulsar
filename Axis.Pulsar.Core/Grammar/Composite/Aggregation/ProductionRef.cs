using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Composite.Group
{
    public class ProductionRef : INodeRef<string>
    {
        public Cardinality Cardinality { get; }

        /// <summary>
        /// The production symbol
        /// </summary>
        public string Ref { get; }

        public ProductionRef(Cardinality cardinality, string productionSymbol)
        {
            Cardinality = cardinality.ThrowIfDefault(
                _ => new ArgumentException($"Invalid {nameof(cardinality)}: default"));
            Ref = productionSymbol
                .ThrowIfNot(
                    Production.SymbolPattern.IsMatch,
                    _ => new ArgumentException($"Invalid {nameof(productionSymbol)}: '{productionSymbol}'"));
        }

        public static ProductionRef Of(
            Cardinality cardinality,
            string productionSymbol)
            => new(cardinality, productionSymbol);

        public bool TryRecognize(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            out SymbolAggregationResult result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(symbolPath);

            var position = reader.Position;
            var production = context.Grammar.GetProduction(Ref);

            if (!production.TryRecognize(reader, symbolPath, context, out var refResult))
                reader.Reset(position);

            result = refResult.MapMatch(

                // data
                node => SymbolAggregationResult.Of(
                    new ISymbolNodeAggregation.Unit(node)),

                // FailedRecognitionError
                fre => fre
                    .ApplyTo(err => SymbolAggregationError.Of(err, 0))
                    .ApplyTo(SymbolAggregationResult.Of),

                // PartialRecognitionError
                pre => pre
                    .ApplyTo(err => SymbolAggregationError.Of(err, 0))
                    .ApplyTo(SymbolAggregationResult.Of));

            return result.Is(out ISymbolNodeAggregation _);
        }
    }
}
