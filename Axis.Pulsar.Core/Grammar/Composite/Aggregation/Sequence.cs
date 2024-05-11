using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Grammar.Composite.Group
{
    /// <summary>
    /// 
    /// </summary>
    public class Sequence : IAggregationRule
    {
        public ImmutableArray<IAggregationElementRule> Elements { get; }

        public Cardinality Cardinality { get; }


        public Sequence(Cardinality cardinality, params IAggregationElementRule[] elements)
        {
            Cardinality = cardinality;
            Elements = elements
                .ThrowIfNull(() => new ArgumentNullException(nameof(elements)))
                .ThrowIf(items => items.IsEmpty(), _ => new ArgumentException("Invalid elements: empty"))
                .ThrowIfAny(e => e is null, _ => new InvalidOperationException($"Invalid element: null"))
                .ApplyTo(ImmutableArray.CreateRange);
        }

        public static Sequence Of(
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
            var sequence = new ISymbolNodeAggregation.Sequence();
            SymbolAggregationResult elementResult = default;
            foreach (var element in Elements)
            {
                if (element.Cardinality.TryRepeat(reader, symbolPath, context, element, out elementResult))
                    elementResult.Consume<ISymbolNodeAggregation>(sequence.Add);

                else
                {
                    reader.Reset(position);
                    break;
                }
            }

            result = elementResult.MapMatch(

                // last element was recognized
                _ => SymbolAggregationResult.Of(sequence),

                // GroupRecognitionError
                gre => gre.Cause switch
                {
                    FailedRecognitionError => SymbolAggregationError
                        .Of(gre.Cause, sequence.RequiredNodeCount() + gre.ElementCount)
                        .ApplyTo(SymbolAggregationResult.Of),

                    PartialRecognitionError => SymbolAggregationError
                        .Of(gre.Cause, sequence.Count + gre.ElementCount)
                        .ApplyTo(SymbolAggregationResult.Of),

                    _ => throw new InvalidOperationException(
                        $"Invalid cause: '{gre.Cause?.GetType()}'")
                });

            return result.Is(out ISymbolNodeAggregation _);
        }
    }
}
