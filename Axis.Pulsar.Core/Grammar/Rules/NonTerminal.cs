using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Groups;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Rules
{
    public class NonTerminal : ICompositeRule
    {
        public IGroupElement Element { get; }

        public uint RecognitionThreshold { get; }

        public NonTerminal(uint recognitionThreshold, IGroupElement ruleGroup)
        {
            Element = ruleGroup ?? throw new ArgumentNullException(nameof(ruleGroup));
            RecognitionThreshold = recognitionThreshold;
        }

        public static NonTerminal Of(
            uint recognitionThreshold,
            IGroupElement element)
            => new(recognitionThreshold, element);

        public static NonTerminal Of(
            IGroupElement element)
            => new(1, element);

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath productionPath,
            ILanguageContext context,
            out IResult<ICSTNode> result)
        {
            ArgumentNullException.ThrowIfNull(nameof(reader));
            ArgumentNullException.ThrowIfNull(nameof(productionPath));

            var position = reader.Position;
            if (!Element.Cardinality.TryRepeat(
                reader,
                productionPath,
                context,
                Element,
                out var groupResult))
            {
                result = groupResult
                    .TransformError((GroupRecognitionError gre) =>
                    {
                        if (gre.Cause is FailedRecognitionError fre
                            && gre.ElementCount >= RecognitionThreshold)
                            return PartialRecognitionError
                                .Of(productionPath,
                                    position,
                                    gre.Cause.TokenSegment.EndOffset - position - 1)
                                .As<Exception>();

                        else return (Exception)gre.Cause;
                    })
                    .MapAs<ICSTNode>();
                return false;
            }

            result = groupResult.Map(nodes => ICSTNode.Of(productionPath.Name, nodes));
            return true;
        }
    }
}
