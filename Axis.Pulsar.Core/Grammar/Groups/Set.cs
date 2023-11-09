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
    public class Set : IGroup
    {
        public ImmutableArray<IGroupElement> Elements { get; }

        public Cardinality Cardinality { get; }

        /// <summary>
        /// Minimum number of recognized items that can exist for this group to be deemed recognized.
        /// Default value is <see cref="Set.Rules.Length"/>.
        /// </summary>
        public int? MinRecognitionCount { get;


        public Set(Cardinality cardinality, params IGroupElement[] elements)
        {
            Cardinality = cardinality;
            Elements = elements
                .ThrowIfNull(new ArgumentNullException(nameof(elements)))
                .ThrowIfAny(e => e is null, new ArgumentException($"Invalid element: null"))
                .ApplyTo(ImmutableArray.CreateRange);
        }

        public static Set Of(
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
            var elementList = new List<IGroupElement>(Elements);
            var results = new List<IResult<NodeSequence>>();
            bool isElementConsumed = false;
            do
            {
                isElementConsumed = false;
                foreach (var elt in elementList)
                {
                    if (elt.Cardinality.TryRepeat(reader, parentPath, context, elt, out var groupResult))
                    {
                        results.Add(groupResult);
                        elementList.Remove(elt);
                        isElementConsumed = true;
                        break;
                    }
                    else if (groupResult.IsErrorResult(out GroupError ge))
                    {
                        if (ge.NodeError is UnrecognizedTokens)
                            continue;

                        else if (ge.NodeError is PartiallyRecognizedTokens)
                        {
                            result = groupResult;
                            return false;
                        }
                        else
                        {
                            result = RecognitionRuntimeError
                                .Of((Exception)ge.NodeError)
                                .ApplyTo(Result.Of<NodeSequence>);
                            return false;
                        }
                    }
                    else
                    {
                        var error = !groupResult.IsErrorResult(out RecognitionRuntimeError rre)
                            ? RecognitionRuntimeError.Of(groupResult.AsError().ActualCause())
                            : rre;
                        result = Result.Of<NodeSequence>(error);
                        return false;
                    }
                }
            }
            while (isElementConsumed);

            if (results.Count == Elements.Length)
            {
                result = results.FoldInto(_results => _results.Fold());
                return true;
            }
            else
            {
                var partialSequence = results
                    .FoldInto(_results => _results.Fold())
                    .Resolve();

                INodeError nodeError = results.Count <= 0
                    ? UnrecognizedTokens.Of(parentPath, position)
                    : PartiallyRecognizedTokens.Of(parentPath, position, partialSequence.Tokens);

                result = GroupError
                    .Of(nodeError, partialSequence)
                    .ApplyTo(Result.Of<NodeSequence>);
                return false;
            }
        }
    }
}
