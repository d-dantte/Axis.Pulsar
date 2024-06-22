using Axis.Pulsar.Core.Grammar.Results;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Grammar.Rules.Aggregate
{
    /// <summary>
    /// Represents a group of aggregation elements whose recognition must be carried out according to some well-established
    /// rule.
    /// </summary>
    public interface IAggregation : IAggregationElement
    {
        /// <summary>
        /// Elements within this aggregation
        /// </summary>
        ImmutableArray<IAggregationElement> Elements { get; }
    }

    /// <summary>
    /// An aggregation element represents a unit that participates in recognition aggregation. Each element implements
    /// the concept of Cardinality - representing the number of repetitions that validly denote a successful recognition
    /// of the element.
    /// </summary>
    public interface IAggregationElement : IRecognizer<NodeAggregationResult>
    {
        /// <summary>
        /// The aggregation type
        /// </summary>
        AggregationType Type { get; }
    }
}
