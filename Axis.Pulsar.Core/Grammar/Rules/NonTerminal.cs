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
            if (!RuleGroup.Cardinality.TryRecognize(reader, productionPath, RuleGroup, out var groupResult))
            {
                result = groupResult.AsError().MapNodeError(
                    (ge, ute) => MapUnrecognizedTokensError(ge, ute, productionPath, RecognitionThreshold),
                    (ge, pte) => pte);

                return false;
            }

            result = groupResult.Map(nodes => ICSTNode.Of(productionPath.Name, nodes));
            return true;
        }

        private static INodeError MapUnrecognizedTokensError(
            GroupError groupError,
            UnrecognizedTokens unrecognizedTokens,
            ProductionPath productionPath,
            uint threshold)
        {
            if (groupError.Nodes.Count >= threshold)
                return PartiallyRecognizedTokens.Of(
                    productionPath,
                    unrecognizedTokens.Position,
                    groupError.Nodes.Tokens);

            else return UnrecognizedTokens.Of(
                productionPath,
                unrecognizedTokens.Position);
        }
    }
}
