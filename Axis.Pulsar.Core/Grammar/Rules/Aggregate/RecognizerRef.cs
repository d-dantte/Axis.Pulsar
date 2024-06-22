using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Rules.Aggregate
{
    public abstract class RecognizerRef : IAggregationElement
    {
        public abstract AggregationType Type { get; }

        internal protected abstract IRecognizer<NodeRecognitionResult> Recognizer(ILanguageContext context);

        public bool TryRecognize(
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context,
            out NodeAggregationResult result)
        {
            ArgumentNullException.ThrowIfNull(reader);

            var position = reader.Position;
            var recognizer = Recognizer(context);

            if (!recognizer.TryRecognize(reader, symbolPath, context, out var ruleResult))
                reader.Reset(position);

            result = ruleResult.MapMatch(

                // data
                node => NodeAggregationResult.Of(node),

                // FailedRecognitionError
                fre => fre
                    .ApplyTo(err => AggregateRecognitionError.Of(err))
                    .ApplyTo(NodeAggregationResult.Of),

                // PartialRecognitionError
                pre => pre
                    .ApplyTo(err => AggregateRecognitionError.Of(err))
                    .ApplyTo(NodeAggregationResult.Of));

            return result.Is(out ISymbolNode _);
        }
    }

    public class AtomicRuleRef : RecognizerRef
    {
        public IAtomicRule Ref { get; }

        public override AggregationType Type => AggregationType.Unit;

        public AtomicRuleRef(IAtomicRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);
            this.Ref = rule;
        }

        public static AtomicRuleRef Of(IAtomicRule rule) => new(rule);

        internal protected override IRecognizer<NodeRecognitionResult> Recognizer(ILanguageContext context) => Ref;
    }

    public class ProductionRef : RecognizerRef
    {
        public override AggregationType Type => AggregationType.Unit;

        /// <summary>
        /// The production symbol
        /// </summary>
        public string Ref { get; }

        public ProductionRef(string productionSymbol)
        {
            Ref = productionSymbol.ThrowIfNot(
                Production.SymbolPattern.IsMatch,
                _ => new ArgumentException($"Invalid {nameof(productionSymbol)}: '{productionSymbol}'"));
        }

        public static ProductionRef Of(string productionSymbol) => new(productionSymbol);

        internal protected override IRecognizer<NodeRecognitionResult> Recognizer(
            ILanguageContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return context.Grammar.GetProduction(Ref);
        }
    }
}
