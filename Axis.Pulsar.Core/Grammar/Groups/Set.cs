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
                var tempPosition = reader.Position;

                if (!element.Cardinality.TryRecognize(reader, parentPath, element, out var elementResult))
                {
                    reader.Reset(tempPosition);

                    if (elementResult.IsErrorResult(out GroupError error, ge => ge.NodeError is UnrecognizedTokens))
                        continue;

                    else
                    {
                        reader.Reset(position);
                        result = elementResult.AsError().MapGroupError(
                            (ge, ute) => throw new InvalidOperationException("Unknown Error"),
                            (ge, pte) => results
                                .FoldInto(v => v.Fold())
                                .Map(ns => ge.Prepend(ns))
                                .Resolve());

                        return false;
                    }
                }

                results.Add(elementResult);
                elementList.RemoveAt(cnt);
                cnt = 0;
                continue;
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

                result = UnrecognizedTokens
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
