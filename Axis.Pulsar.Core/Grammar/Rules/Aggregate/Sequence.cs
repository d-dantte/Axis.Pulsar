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
    public class Sequence : IAggregation
    {
        public ImmutableArray<IAggregationElement> Elements { get; }

        public AggregationType Type => AggregationType.Sequence;

        public Sequence(params IAggregationElement[] elements)
        {
            Elements = elements
                .ThrowIfNull(() => new ArgumentNullException(nameof(elements)))
                .ThrowIf(items => items.IsEmpty(), _ => new ArgumentException("Invalid elements: empty"))
                .ThrowIfAny(e => e is null, _ => new InvalidOperationException($"Invalid element: null"))
                .ApplyTo(ImmutableArray.CreateRange);
        }

        public static Sequence Of(
            params IAggregationElement[] elements)
            => new(elements);

        public bool TryRecognize(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            out NodeAggregationResult result)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var position = reader.Position;
            var tempPosition = position;
            var sequence = new List<ISymbolNode>();
            NodeAggregationResult elementResult = default;

            foreach (var element in Elements)
            {
                tempPosition = reader.Position;
                if (element.TryRecognize(reader, symbolPath, context, out elementResult))
                    elementResult.Consume<ISymbolNode>(sequence.Add);

                else
                {
                    reader.Reset(position);
                    break;
                }
            }

            result = elementResult.MapMatch(

                // last element was recognized
                _ => NodeAggregationResult.Of(
                    new ISymbolNode.Aggregate(AggregationType.Sequence, false, sequence)),

                // GroupRecognitionError
                gre => gre.Cause switch
                {
                    PartialRecognitionError => elementResult,

                    _ => AggregateRecognitionError
                        .Of(gre.Cause, sequence.AddItems(gre.RecognizedNodes))
                        .ApplyTo(NodeAggregationResult.Of),
                });

            return result.Is(out ISymbolNode _);
        }
    }
}
