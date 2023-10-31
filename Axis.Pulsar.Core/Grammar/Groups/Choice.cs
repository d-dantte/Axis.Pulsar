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
    public class Choice : IGroup
    {
        public ImmutableArray<IGroupElement> Elements { get; }

        public Cardinality Cardinality { get; }


        public Choice(Cardinality cardinality, params IGroupElement[] elements)
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
            foreach(var element in Elements)
            {
                if (element.Cardinality.TryRecognize(reader, parentPath, element, out result))
                    return true;

                reader.Reset(position);

                if (result.IsErrorResult(out RecognitionRuntimeError _))
                    return false;
                
                else if (result.IsErrorResult(out GroupError ge))
                {
                    if (ge.NodeError is UnrecognizedTokens)
                        continue;

                    else return false;
                }
                else
                {
                    result = RecognitionRuntimeError
                        .Of(result.AsError().ActualCause())
                        .ApplyTo(Result.Of<NodeSequence>);
                    return false;
                }
            }

            reader.Reset(position);
            result = UnrecognizedTokens
                .Of(parentPath, position)
                .ApplyTo(ire => (ire, NodeSequence.Empty))
                .ApplyTo(GroupError.Of)
                .ApplyTo(Result.Of<NodeSequence>);

            return false;
        }
    }
}
