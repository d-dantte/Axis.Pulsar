using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Grammar.Rules.Aggregate
{
    /// <summary>
    /// 
    /// </summary>
    public class Set : IAggregation
    {
        /// <summary>
        /// Minimum number of recognized items that can exist for this group to be deemed recognized.
        /// Default value is <see cref="Set.Rules.Length"/>.
        /// </summary>
        public int MinRecognitionCount { get; }

        public ImmutableArray<IAggregationElement> Elements { get; }

        public AggregationType Type => AggregationType.Set;

        public Set(params IAggregationElement[] elements)
            : this(elements?.Length ?? 0, elements!)
        {
        }

        public Set(
            int minRecognitionCount,
            params IAggregationElement[] elements)
        {
            Elements = elements
                .ThrowIfNull(() => new ArgumentNullException(nameof(elements)))
                .ThrowIf(items => items.IsEmpty(), _ => new ArgumentException("Invalid elements: empty"))
                .ThrowIfAny(e => e is null, _ => new InvalidOperationException($"Invalid element: null"))
                .ApplyTo(ImmutableArray.CreateRange);

            MinRecognitionCount = minRecognitionCount.ThrowIf(
                value => value < 1 || value > Elements.Length,
                _ => new ArgumentOutOfRangeException(
                    nameof(minRecognitionCount),
                    $"Invalid recognition-count (1 <= x <= Elements.Count): {minRecognitionCount}"));
        }

        public static Set Of(
            params IAggregationElement[] elements)
            => new(elements);

        public static Set Of(
            int minRecognitionCount,
            params IAggregationElement[] elements)
            => new(minRecognitionCount, elements);

        public bool TryRecognize(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            out NodeAggregationResult result)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var position = reader.Position;
            var elementList = Elements.Reverse().ToList();
            var set = new List<ISymbolNode>();
            NodeAggregationResult elementResult = default;
            bool isElementConsumed = false;

            do
            {
                isElementConsumed = false;
                var stepPosition = reader.Position;
                for (int index = elementList.Count - 1; index >= 0; index--)
                {
                    var element = elementList[index];

                    if (element.TryRecognize(reader, symbolPath, context, out elementResult))
                    {
                        elementResult.Consume<ISymbolNode>(set.Add);
                        elementList.RemoveAt(index);
                        isElementConsumed = true;
                        break;
                    }
                    else
                    {
                        reader.Reset(stepPosition);
                        var are = elementResult.Get<AggregateRecognitionError>();

                        if (are.Cause is FailedRecognitionError)
                            continue;

                        else
                        {
                            result = elementResult;
                            return false;
                        }
                    }
                }
            }
            while (isElementConsumed);

            result = IsValidCount(set.Count) switch
            {
                true => NodeAggregationResult.Of(
                    new ISymbolNode.Aggregate(AggregationType.Set, false, set)),

                false => FailedRecognitionError
                    .Of(symbolPath, position)
                    .ApplyTo(fre => AggregateRecognitionError.Of(fre, set))
                    .ApplyTo(NodeAggregationResult.Of)
            };

            return result.Is(out ISymbolNode _);
        }

        private bool IsValidCount(int setCount)
        {
            return setCount == Elements.Length || setCount >= MinRecognitionCount;
        }
    }
}
