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
    public class Sequence : IGroup
    {
        public ImmutableArray<IGroupRule> Elements { get; }

        public Cardinality Cardinality { get; }


        public Sequence(Cardinality cardinality, params IGroupRule[] elements)
        {
            Cardinality = cardinality;
            Elements = elements
                .ThrowIfNull(() => new ArgumentNullException(nameof(elements)))
                .ThrowIf(items => items.IsEmpty(), _ => new ArgumentException("Invalid elements: empty"))
                .ThrowIfAny(e => e is null, _ => new ArgumentException($"Invalid element: null"))
                .ApplyTo(ImmutableArray.CreateRange);
        }

        public static Sequence Of(
            Cardinality cardinality,
            params IGroupRule[] elements)
            => new(cardinality, elements);

        public bool TryRecognize(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            out GroupRecognitionResult result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(symbolPath);

            var position = reader.Position;
            var nodeSequence = INodeSequence.Empty;
            var elementResult = GroupRecognitionResult.Of(INodeSequence.Empty);
            foreach (var element in Elements)
            {
                if (element.Cardinality.TryRepeat(reader, symbolPath, context, element, out elementResult))
                {
                    if (!elementResult.Is(out INodeSequence elementSequence))
                        throw new InvalidOperationException(
                            $"Invalid result: Expected sequence, found - {elementResult}");

                    nodeSequence = nodeSequence.ConcatSequence(elementSequence);
                }
                else
                {
                    reader.Reset(position);
                    break;
                }
            }

            result = elementResult.MapMatch(

                // last element was recognized
                _ => GroupRecognitionResult.Of(Cardinality.IsZeroMinOccurence switch
                {
                    false => nodeSequence,
                    true => !nodeSequence.IsOptional
                        ? INodeSequence.Of(nodeSequence, true)
                        : nodeSequence
                }),

                // GroupRecognitionError
                gre => gre.Cause switch
                {
                    FailedRecognitionError => GroupRecognitionError
                        .Of(gre.Cause, nodeSequence.RequiredNodeCount + gre.ElementCount)
                        .ApplyTo(GroupRecognitionResult.Of),

                    PartialRecognitionError => GroupRecognitionError
                        .Of(gre.Cause, nodeSequence.Count + gre.ElementCount)
                        .ApplyTo(GroupRecognitionResult.Of),

                    _ => throw new InvalidOperationException(
                        $"Invalid cause: '{gre.Cause?.GetType()}'")
                });

            return result.Is(out INodeSequence _);
        }
    }
}
