using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Exceptions;
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


        public Set(Cardinality cardinality, params IGroupElement[] elements)
        {
            Cardinality = cardinality;
            Elements = elements
                .ThrowIfNull(new ArgumentNullException(nameof(elements)))
                .ThrowIfAny(e => e is null, new ArgumentException($"Invalid element: null"))
                .ApplyTo(ImmutableArray.CreateRange);
        }

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath parentPath,
            out IResult<NodeSequence> result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(parentPath);

            var position = reader.Position;
            var elementList = new List<IGroupElement>(Elements);
            var results = new List<IResult<NodeSequence>>();
            for (int cnt = 0; cnt < elementList.Count; cnt++)
            {
                var element = elementList[cnt];
                if (element.Cardinality.TryRecognize(reader, parentPath, element, out var elementResult))
                {
                    results.Add(elementResult);
                    elementList.RemoveAt(cnt);
                    cnt = 0;
                    continue;
                }

                var error = elementResult.AsError().ActualCause();
                if (error is GroupError groupError)
                {
                    if (groupError.RecognitionError is Errors.UnrecognizedTokens)
                        continue;

                    else
                    {
                        reader.Reset(position);
                        var prev = results.FoldInto(v => v.Fold()).Resolve();
                        groupError = groupError.Prepend(prev);
                        result = Result.Of<NodeSequence>(groupError);
                        return false;
                    }
                }
                else
                {
                    reader.Reset(position);
                    result = Errors.RuntimeError
                        .Of(parentPath, error)
                        .ApplyTo(ire => (ire, NodeSequence.Empty))
                        .ApplyTo(GroupError.Of)
                        .ApplyTo(Result.Of<NodeSequence>);
                    return false;
                }
            }

            if (results.Count == elementList.Count)
            {
                result = results.FoldInto(_results => _results.Fold());
                return true;
            }
            else
            {
                var nodeSequence = results
                    .FoldInto(_results => _results.Fold())
                    .Resolve();

                result = Errors.UnrecognizedTokens
                    .Of(parentPath, position)
                    .ApplyTo(ire => (ire, nodeSequence))
                    .ApplyTo(GroupError.Of)
                    .ApplyTo(Result.Of<NodeSequence>);

                reader.Reset(position);
                return false;
            }
        }
    }
}
