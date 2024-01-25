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
    public class Choice : IGroup
    {
        public ImmutableArray<IGroupRule> Elements { get; }

        public Cardinality Cardinality { get; }


        public Choice(Cardinality cardinality, params IGroupRule[] elements)
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
            foreach (var element in Elements)
            {
                if (element.Cardinality.TryRepeat(reader, symbolPath, context, element, out var grr)
                    && grr.Is(out INodeSequence nodeSequence))
                {
                    if (Cardinality.IsZeroMinOccurence)
                        nodeSequence = !nodeSequence.IsOptional
                            ? INodeSequence.Of(nodeSequence, true)
                            : nodeSequence;

                    result = GroupRecognitionResult.Of(nodeSequence);
                    return true;
                }

                reader.Reset(position);

                if (grr.Is(out GroupRecognitionError gre)
                    && gre.Cause is FailedRecognitionError)
                    continue;

                else
                {
                    result = grr;
                    return false;
                }
            }

            reader.Reset(position);
            result = FailedRecognitionError
                .Of(symbolPath, position)
                .ApplyTo(error => GroupRecognitionError.Of(error))
                .ApplyTo(GroupRecognitionResult.Of);

            return false;
        }
    }
}
