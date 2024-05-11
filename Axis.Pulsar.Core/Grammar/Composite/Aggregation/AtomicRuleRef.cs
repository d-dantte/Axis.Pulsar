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

            Ref = rule.ThrowIfNull(
                () => throw new ArgumentNullException(nameof(rule)));
        }

        public static AtomicRuleRef Of(
            Cardinality cardinality,
            IAtomicRule rule)
            => new(cardinality, rule);

        public bool TryRecognize(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            out SymbolAggregationResult result)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var position = reader.Position;

            if (!Ref.TryRecognize(reader, symbolPath, context, out var ruleResult))
                reader.Reset(position);

            result = ruleResult.MapMatch(

                // data
                node => SymbolAggregationResult.Of(
                    new ISymbolNodeAggregation.Unit(node)),

                // FailedRecognitionError
                fre => fre
                    .ApplyTo(err => SymbolAggregationError.Of(err, 0))
                    .ApplyTo(SymbolAggregationResult.Of),

                // PartialRecognitionError
                pre => pre
                    .ApplyTo(err => SymbolAggregationError.Of(err, 0))
                    .ApplyTo(SymbolAggregationResult.Of));

            return result.Is(out ISymbolNodeAggregation _);
        }
    }
}
