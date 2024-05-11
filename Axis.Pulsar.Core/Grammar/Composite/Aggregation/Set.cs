using Axis.Luna.Extensions;
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
    public class Set : IAggregationRule
    {
        public ImmutableArray<IAggregationElementRule> Elements { get; }

        public Cardinality Cardinality { get; }

        /// <summary>
        /// Minimum number of recognized items that can exist for this group to be deemed recognized.
        /// Default value is <see cref="Set.Rules.Length"/>.
        /// </summary>
        public int MinRecognitionCount { get; }


        public Set(Cardinality cardinality, int minRecognitionCount, params IAggregationElementRule[] elements)
        {
            Cardinality = cardinality;
            MinRecognitionCount = minRecognitionCount;
            Elements = elements
                .ThrowIfNull(() => new ArgumentNullException(nameof(elements)))
                .ThrowIf(items => items.IsEmpty(), _ => new ArgumentException("Invalid elements: empty"))
                .ThrowIfAny(e => e is null, _ => new ArgumentException($"Invalid element: null"))
                .ApplyTo(ImmutableArray.CreateRange);
        }

        public static Set Of(
            Cardinality cardinality,
            int minRecognitionCount,
            params IAggregationElementRule[] elements)
            => new(cardinality, minRecognitionCount, elements);

        public static Set Of(
            Cardinality cardinality,
            params IAggregationElementRule[] elements)
            => new(cardinality, elements.Length, elements);

        public bool TryRecognize(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            out SymbolAggregationResult result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(symbolPath);

            var position = reader.Position;
            var elementList = Elements.Reverse().ToList();
            var set = new ISymbolNodeAggregation.Sequence();
            bool isElementConsumed = false;
            do
            {
                isElementConsumed = false;
                var stepPosition = reader.Position;
                for (int index = elementList.Count - 1; index >= 0; index--)
                {
                    var elt = elementList[index];
                    var elementResult = elt.Cardinality.Repeat(reader, symbolPath, context, elt);

                    if (elementResult.Is(out ISymbolNodeAggregation agg))
                    {
                        set.Add(agg);
                        elementList.RemoveAt(index);
                        isElementConsumed = true;
                        break;
                    }
                    else
                    {
                        var sae = elementResult.Get<SymbolAggregationError>();
                        if (sae.Cause is FailedRecognitionError)
                        {
                            reader.Reset(stepPosition);
                            continue;
                        }
                        else
                        {
                            reader.Reset(stepPosition);
                            result = elementResult;
                            return false;
                        }
                    }
                }
            }
            while (isElementConsumed);

            if (set.Count == Elements.Length || set.Count >= MinRecognitionCount)
            {
                result = SymbolAggregationResult.Of(set);
                return true;
            }
            else
            {
                result = FailedRecognitionError
                    .Of(symbolPath, position)
                    .ApplyTo(fre => SymbolAggregationError.Of(fre, set))
                    .ApplyTo(SymbolAggregationResult.Of);
                return false;
            }
        }
    }
}
