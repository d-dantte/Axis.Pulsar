using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Exceptions;
using Axis.Pulsar.Core.Grammar.Groups;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Rules
{
    public class NonTerminal : IAggregateRule
    {
        public IGroupElement RuleGroup { get; }

        public uint RecognitionThreshold { get; }

        public NonTerminal(uint recognitionThreshold, IGroupElement ruleGroup)
        {
            RuleGroup = ruleGroup ?? throw new ArgumentNullException(nameof(ruleGroup));
            RecognitionThreshold = recognitionThreshold;
        }

        public bool TryRecognize(TokenReader reader, ProductionPath productionPath, out IResult<ICSTNode> result)
        {
            ArgumentNullException.ThrowIfNull(nameof(reader));
            ArgumentNullException.ThrowIfNull(nameof(productionPath));

            var position = reader.Position;
            if (RuleGroup.Cardinality.TryRecognize(reader, productionPath, RuleGroup, out var groupResult))
            {
                result = groupResult.Map(nodes => ICSTNode.Of(productionPath.Name, nodes.ToArray()));
                return true;
            }

            var exception = groupResult.AsError().ActualCause();
            if (exception is GroupError groupError)
            {
                if (groupError.RecognitionError is Errors.UnrecognizedTokens)
                {
                    if (groupError.Nodes.Count >= RecognitionThreshold)
                        result = groupError.RecognitionError.MapPartiallyRecognizedTokens<ICSTNode>(
                            productionPath,
                            position,
                            groupError.Nodes.Select(node => node.Tokens));

                    else result = groupError.RecognitionError.MapUnrecognizedTokens<ICSTNode>(productionPath, position);
                }

                else if (groupError.RecognitionError is Errors.PartiallyRecognizedTokens
                    || groupError.RecognitionError is Errors.RuntimeError)
                    result = Result.Of<ICSTNode>((Exception)groupError.RecognitionError);

                else result = Result.Of<ICSTNode>(
                    new InvalidOperationException($"Invalid error: {groupError.RecognitionError}"));
            }
            else result = Result.Of<ICSTNode>(
                new InvalidOperationException($"Invalid error: {exception}"));

            return false;
        }
    }
}
