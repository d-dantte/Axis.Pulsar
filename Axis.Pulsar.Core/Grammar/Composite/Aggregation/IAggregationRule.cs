using System.Collections.Immutable;

namespace Axis.Pulsar.Core.Grammar.Composite.Group
{
    /// <summary>
    /// Represents a group of aggregation elements whose recognition must be carried out according to some well-established
    /// rule.
    /// </summary>
    public interface IAggregationRule : IAggregationElementRule
    {
        /// <summary>
        /// Elements within this aggregation
        /// </summary>
        ImmutableArray<IAggregationElementRule> Elements { get; }
    }
}
