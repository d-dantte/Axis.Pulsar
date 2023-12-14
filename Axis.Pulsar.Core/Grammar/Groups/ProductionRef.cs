using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Nodes;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Groups
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
                    IProduction.SymbolPattern.IsMatch,
                    _ => new ArgumentException($"Invalid {nameof(productionSymbol)}: '{productionSymbol}'"));
        }

        public static ProductionRef Of(
            Cardinality cardinality,
            string productionSymbol)
            => new(cardinality, productionSymbol);

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath parentPath,
            ILanguageContext context,
            out IRecognitionResult<INodeSequence> result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(parentPath);

            var position = reader.Position;
            var production = context.Grammar.GetProduction(Ref);
            if (!production.TryProcessRule(reader, parentPath, context, out var refResult))
            {
                reader.Reset(position);
                result = refResult
                    .TransformError(err => err switch
                    {
                        FailedRecognitionError
                        or PartialRecognitionError => GroupRecognitionError.Of(err, 0),
                        _ => err
                    })
                    .MapAs<INodeSequence>();

                return false;
            }

            result = refResult.Map(node => INodeSequence.Of(node));
            return true;
        }
    }
}
