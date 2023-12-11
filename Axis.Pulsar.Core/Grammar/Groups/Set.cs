using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Grammar.Groups
{
    /// <summary>
    /// 
    /// </summary>
    public class Set : IGroup
    {
        public ImmutableArray<IGroupElement> Elements { get; }

        public Cardinality Cardinality { get; }

        /// <summary>
        /// Minimum number of recognized items that can exist for this group to be deemed recognized.
        /// Default value is <see cref="Set.Rules.Length"/>.
        /// </summary>
        public int MinRecognitionCount { get; }


        public Set(Cardinality cardinality, int minRecognitionCount, params IGroupElement[] elements)
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
            params IGroupElement[] elements)
            => new(cardinality, minRecognitionCount, elements);

        public static Set Of(
            Cardinality cardinality,
            params IGroupElement[] elements)
            => new(cardinality, elements.Length, elements);

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath parentPath,
            ILanguageContext context,
            out IRecognitionResult<INodeSequence> result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(parentPath);

            var position = reader.Position;
            var elementList = Elements.Reverse().ToList();
            var results = new List<IRecognitionResult<INodeSequence>>();
            bool isElementConsumed = false;
            do
            {
                isElementConsumed = false;
                var stepPosition = reader.Position;
                for (int index = elementList.Count - 1; index >= 0; index--)
                {
                    var elt = elementList[index];
                    if (elt.Cardinality.TryRepeat(reader, parentPath, context, elt, out var groupResult))
                    {
                        results.Add(groupResult);
                        elementList.RemoveAt(index);
                        isElementConsumed = true;
                        break;
                    }
                    else if (groupResult.IsError(out GroupRecognitionError gre)
                        && gre.Cause is FailedRecognitionError)
                    {
                        reader.Reset(stepPosition);
                        continue;
                    }
                    else
                    {
                        reader.Reset(stepPosition);
                        result = groupResult;
                        return false;
                    }
                }
            }
            while (isElementConsumed);

            if (results.Count == Elements.Length || results.Count >= MinRecognitionCount)
            {
                result = results.Fold((acc, next) => acc.Append(next));
                return true;
            }
            else if (results.Count == 0)
            {
                result = FailedRecognitionError
                    .Of(parentPath, position)
                    .ApplyTo(GroupRecognitionError.Of)
                    .ApplyTo(error => RecognitionResult.Of<INodeSequence>(error));
                return false;
            }
            else
            {
                result = PartialRecognitionError
                    .Of(parentPath, position, reader.Position - position)
                    .ApplyTo(fre => GroupRecognitionError.Of(fre, results.Count))
                    .ApplyTo(error => RecognitionResult.Of<INodeSequence>(error));
                return false;
            }
        }
    }
}
