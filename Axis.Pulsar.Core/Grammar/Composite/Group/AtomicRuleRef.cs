using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Atomic;
using Axis.Pulsar.Core.Utils;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Grammar.Composite.Group
{
    public class AtomicRuleRef : INodeRef<IAtomicRule>
    {
        public Cardinality Cardinality { get; }

        public IAtomicRule Ref { get; }

        public AtomicRuleRef(Cardinality cardinality, IAtomicRule rule)
        {
            Cardinality = cardinality.ThrowIfDefault(
                _ => new ArgumentException($"Invalid {nameof(cardinality)}: default"));
            Ref = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public static AtomicRuleRef Of(
            Cardinality cardinality,
            IAtomicRule rule)
            => new(cardinality, rule);

        public bool TryRecognize(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            out GroupRecognitionResult result)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(symbolPath);

            var position = reader.Position;
            if (!Ref.TryRecognize(reader, symbolPath, context, out var ruleResult))
                reader.Reset(position);

            result = ruleResult.MapMatch(

                // data
                data => INodeSequence
                    .Of(data, Cardinality.IsZeroMinOccurence)
                    .ApplyTo(GroupRecognitionResult.Of),

                // FailedRecognitionError
                fre => GroupRecognitionError
                    .Of(fre)
                    .ApplyTo(GroupRecognitionResult.Of),

                // PartialREcognitionError
                pre => GroupRecognitionError
                    .Of(pre)
                    .ApplyTo(GroupRecognitionResult.Of));

            return result.Is(out INodeSequence _);
        }
    }
}
