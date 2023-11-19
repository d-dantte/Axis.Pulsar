using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Groups
{
    public class ProductionRef : IRuleRef<string>
    {
        public Cardinality Cardinality { get; }

        /// <summary>
        /// The production symbol
        /// </summary>
        public string Ref { get; }

        public ProductionRef(Cardinality cardinality, string productionSymbol)
        {
            Cardinality = cardinality.ThrowIfDefault(new ArgumentException($"Invalid {nameof(cardinality)}: default"));
            Ref = productionSymbol
                .ThrowIfNot(
                    IProduction.SymbolPattern.IsMatch,
                    new ArgumentException($"Invalid {nameof(productionSymbol)}: '{productionSymbol}'"));
        }

        public static ProductionRef Of(
            Cardinality cardinality,
            string productionSymbol)
            => new(cardinality, productionSymbol);

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath parentPath,
            ILanguageContext context,
            out IResult<NodeSequence> result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(parentPath);

            var position = reader.Position;
            var production = context.Grammar.GetProduction(Ref);
            if (!production.TryProcessRule(reader, parentPath, context, out var refResult))
            {
                reader.Reset(position);

                result = refResult.AsError().MapGroupError(
                    ute => GroupError.Of(ute, NodeSequence.Empty),
                    pte => GroupError.Of(pte, NodeSequence.Empty));

                return false;
            }

            result = refResult.Map(node => NodeSequence.Of(node));
            return true;
        }
    }
}
