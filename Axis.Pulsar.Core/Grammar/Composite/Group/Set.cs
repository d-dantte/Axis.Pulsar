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
    public class Set : IGroup
    {
        public ImmutableArray<IGroupRule> Elements { get; }

        public Cardinality Cardinality { get; }

        /// <summary>
        /// Minimum number of recognized items that can exist for this group to be deemed recognized.
        /// Default value is <see cref="Set.Rules.Length"/>.
        /// </summary>
        public int MinRecognitionCount { get; }


        public Set(Cardinality cardinality, int minRecognitionCount, params IGroupRule[] elements)
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
            params IGroupRule[] elements)
            => new(cardinality, minRecognitionCount, elements);

        public static Set Of(
            Cardinality cardinality,
            params IGroupRule[] elements)
            => new(cardinality, elements.Length, elements);

        public bool TryRecognize(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            out GroupRecognitionResult result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(symbolPath);

            var position = reader.Position;
            var elementList = Elements.Reverse().ToList();
            var nodeSequence = INodeSequence.Empty;
            bool isElementConsumed = false;
            do
            {
                isElementConsumed = false;
                var stepPosition = reader.Position;
                for (int index = elementList.Count - 1; index >= 0; index--)
                {
                    var elt = elementList[index];
                    if (elt.Cardinality.TryRepeat(reader, symbolPath, context, elt, out var elementResult))
                    {
                        if (!elementResult.Is(out INodeSequence elementSequence))
                            throw new InvalidOperationException(
                                $"Invalid result: Expected sequence, found - {elementResult}");

                        nodeSequence = nodeSequence.Append(elementSequence);
                        elementList.RemoveAt(index);
                        isElementConsumed = true;
                        break;
                    }
                    else if (elementResult.Is(out GroupRecognitionError gre)
                        && gre.Cause is FailedRecognitionError)
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
            while (isElementConsumed);

            if (nodeSequence.Count == Elements.Length || nodeSequence.Count >= MinRecognitionCount)
            {
                result = GroupRecognitionResult.Of(nodeSequence);
                return true;
            }
            else if (nodeSequence.Count == 0)
            {
                result = FailedRecognitionError
                    .Of(symbolPath, position)
                    .ApplyTo(fre => GroupRecognitionError.Of(fre))
                    .ApplyTo(GroupRecognitionResult.Of);
                return false;
            }
            else
            {
                result = PartialRecognitionError
                    .Of(symbolPath, position, reader.Position - position)
                    .ApplyTo(pre => GroupRecognitionError.Of(pre, nodeSequence.Count))
                    .ApplyTo(GroupRecognitionResult.Of);
                return false;
            }
        }
    }
}
