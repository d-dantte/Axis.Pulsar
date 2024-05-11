using Axis.Pulsar.Core.Grammar.Results;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Composite.Group
{
    /// <summary>
    /// An aggregation element represents a unit that participates in recognition aggregation. Each element implements
    /// the concept of Cardinality - representing the number of repetitions that validly denote a successful recognition
    /// of the element.
    /// </summary>
    public interface IAggregationElementRule : IRecognizer<SymbolAggregationResult>
    {
        /// <summary>
        /// The cardinality of the element
        /// </summary>
        Cardinality Cardinality { get; }
    }

    public static class AggregationElementRuleExtensions
    {
        public static SymbolAggregationResult Recognize(this
            IAggregationElementRule rule,
            TokenReader reader,
            SymbolPath symbolPath,
            ILanguageContext context)
        {
            _ = rule.TryRecognize(reader, symbolPath, context, out var result);
            return result;
        }
    }
}
