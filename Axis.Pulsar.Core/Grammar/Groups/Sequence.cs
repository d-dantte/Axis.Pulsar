using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Utils;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Grammar.Groups
{
    /// <summary>
    /// 
    /// </summary>
    public class Sequence : IGroup
    {
        public ImmutableArray<IGroupElement> Elements { get; }

        public Cardinality Cardinality { get; }


        public Sequence(Cardinality cardinality, params IGroupElement[] elements)
        {
            Cardinality = cardinality;
            Elements = elements
                .ThrowIfNull(new ArgumentNullException(nameof(elements)))
                .ThrowIf(items => items.IsEmpty(), new ArgumentException("Invalid elements: empty"))
                .ThrowIfAny(e => e is null, new ArgumentException($"Invalid element: null"))
                .ApplyTo(ImmutableArray.CreateRange);
        }

        public static Sequence Of(
            Cardinality cardinality,
            params IGroupElement[] elements)
            => new(cardinality, elements);

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath parentPath,
            ILanguageContext context,
            out IResult<NodeSequence> result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(parentPath);

            var position = reader.Position;
            var nodes = new List<ICSTNode>();
            foreach (var element in Elements)
            {
                if (element.Cardinality.TryRepeat(reader, parentPath, context, element, out var elementResult))
                    nodes.AddRange(elementResult.Resolve());

                else
                {
                    reader.Reset(position);
                    result = elementResult.TransformError((GroupRecognitionError gre) => GroupRecognitionError.Of(
                        gre.Cause,
                        nodes.Count + gre.ElementCount));

                    return false;
                }
            }

            result = Result.Of<NodeSequence>(nodes);
            return true;
        }
    }
}
