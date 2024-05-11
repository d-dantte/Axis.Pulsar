using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Grammar.Composite.Group
{
    /// <summary>
    /// NOTE: Must not accept any Element having an MinOccurence cardinality of 0
    /// </summary>
    public class Choice : IAggregationRule
    {
        public ImmutableArray<IAggregationElementRule> Elements { get; }

        public Cardinality Cardinality { get; }


        public Choice(Cardinality cardinality, params IAggregationElementRule[] elements)
        {
            Cardinality = cardinality;
            Elements = elements
                .ThrowIfNull(() => new ArgumentNullException(nameof(elements)))
                .ThrowIf(items => items.IsEmpty(), _ => new ArgumentException("Invalid elements: empty"))
                .ThrowIfAny(e => e is null, _ => new ArgumentException($"Invalid element: null"))
                .ApplyTo(ImmutableArray.CreateRange);
        }

        public static Choice Of(
            Cardinality cardinality,
            params IAggregationElementRule[] elements)
            => new(cardinality, elements);

        public bool TryRecognize(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            out SymbolAggregationResult result)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var position = reader.Position;
            foreach (var element in Elements)
            {
                if (element.Cardinality.TryRepeat(reader, symbolPath, context, element, out result))
                    return true;

                else
                {
                    reader.Reset(position);

                    if (!result.Map<SymbolAggregationError, bool>(sae => sae.Cause is FailedRecognitionError))
                        return false;

                    continue;
                }
            }

            reader.Reset(position);
            result = FailedRecognitionError
                .Of(symbolPath, position)
                .ApplyTo(error => SymbolAggregationError.Of(error, 0))
                .ApplyTo(SymbolAggregationResult.Of);

            return false;
        }
    }
}
