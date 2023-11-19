using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Groups
{
    public class AtomicRuleRef : IRuleRef<IAtomicRule>
    {
        public Cardinality Cardinality { get; }

        public IAtomicRule Ref { get; }

        public AtomicRuleRef(Cardinality cardinality, IAtomicRule rule)
        {
            Cardinality = cardinality.ThrowIfDefault(new ArgumentException($"Invalid {nameof(cardinality)}: default"));
            Ref = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public static AtomicRuleRef Of(
            Cardinality cardinality,
            IAtomicRule rule)
            => new(cardinality, rule);

        public bool TryRecognize(
            TokenReader reader,
            ProductionPath parentPath,
            ILanguageContext context,
            out IResult<NodeSequence> result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(parentPath);

            var position = reader.Position;
            if (!Ref.TryRecognize(reader, parentPath, context, out var ruleResult))
            {
                reader.Reset(position);

                result = ruleResult.AsError().MapGroupError(
                    ute => GroupError.Of(ute, NodeSequence.Empty),
                    pte => GroupError.Of(pte, NodeSequence.Empty));

                return false;
            }

            result = ruleResult.Map(node => NodeSequence.Of(node));
            return true;
        }
    }
}
