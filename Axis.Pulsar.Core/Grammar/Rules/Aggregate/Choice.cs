using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Grammar.Rules.Aggregate
{
    /// <summary>
    /// NOTE: Must not accept any Element having an MinOccurence cardinality of 0.
    /// <para/>
    /// Also, choices do not produce their own <see cref="CST.ISymbolNode.Aggregate"/> instances because they simply return
    /// the first passing result they encounter.
    /// </summary>
    public class Choice : IAggregation
    {
        public ImmutableArray<IAggregationElement> Elements { get; }

        public AggregationType Type => AggregationType.Choice;

        public Choice(params IAggregationElement[] elements)
        {
            Elements = elements
                .ThrowIfNull(
                    () => new ArgumentNullException(nameof(elements)))
                .ThrowIf(
                    items => items.IsEmpty(),
                    _ => new ArgumentException("Invalid elements: empty"))
                .ThrowIfAny(
                    e => e is null,
                    _ => new InvalidOperationException($"Invalid element: null"))
                .ThrowIfAny(
                    element => element is Repetition r && r.Cardinality.MinOccurence == 0,
                    _ => new InvalidOperationException($"Invalid repetition-element: choices cannot have repetitions with 0 MinOccurence"))
                .ApplyTo(ImmutableArray.CreateRange);
        }

        public static Choice Of(
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
            foreach (var element in Elements)
            {
                if (element.TryRecognize(reader, symbolPath, context, out result))
                    return true;

                else
                {
                    reader.Reset(position);

                    if (result.Get<AggregateRecognitionError>().Cause is PartialRecognitionError)
                        return false;

                    continue;
                }
            }

            reader.Reset(position);
            result = FailedRecognitionError
                .Of(symbolPath, position)
                .ApplyTo(error => AggregateRecognitionError.Of(error))
                .ApplyTo(NodeAggregationResult.Of);

            return false;
        }
    }
}
