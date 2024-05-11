using Axis.Luna.Common;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;

namespace Axis.Pulsar.Core.Grammar.Composite.Group
{
    public interface ISymbolNodeAggregation
    {
        public static ISymbolNodeAggregation Of(
            ISymbolNode node)
            => new Unit(node);

        public static ISymbolNodeAggregation Of(
            bool isOptional = false,
            params ISymbolNodeAggregation[] aggregations)
            => new Sequence(
                isOptional, aggregations.ThrowIfNull(() => new ArgumentNullException(nameof(aggregations))));

        public static ISymbolNodeAggregation Of(
            bool isOptional = false,
            params ISymbolNode[] symbolNodes)
            => new Sequence(
                isOptional,
                symbolNodes
                    .ThrowIfNull(() => new ArgumentNullException(nameof(symbolNodes)))
                    .Select(Of)
                    .ToArray());

        public static ISymbolNodeAggregation Of(
            IEnumerable<ISymbolNodeAggregation> aggregations,
            bool isOptional = false)
            => new Sequence(isOptional, aggregations.ToArray());

        #region Nested Types

        internal class Sequence:
            ISymbolNodeAggregation,
            IEquatable<Sequence>
        {
            private readonly List<ISymbolNodeAggregation> aggregations;

            public bool IsOptional { get; }

            public int Count => aggregations.Count;

            public IEnumerable<ISymbolNodeAggregation> Aggregations => aggregations;

            public Sequence(bool isOptional = false, params ISymbolNodeAggregation[] aggregations)
            {
                IsOptional = isOptional;
                this.aggregations = aggregations
                    .ThrowIfNull(() => new ArgumentNullException(nameof(aggregations)))
                    .ThrowIfAny(
                        a => a is null,
                        _ => new InvalidOperationException("Invalid aggregation: null"))
                    .ToList();
            }

            public static Sequence Empty(bool isOptional = false) => new(isOptional);

            public Sequence AddItems(params ISymbolNodeAggregation[] aggregations)
            {
                ArgumentNullException.ThrowIfNull(aggregations);

                for (int index = 0; index < aggregations.Length; index++)
                {
                    if (this.Equals(aggregations[index]))
                        throw new InvalidOperationException($"Invalid aggregation: cannot add self");

                    this.aggregations.Add(aggregations[index]);
                }

                return this;
            }

            public void AddAll(params
                ISymbolNodeAggregation[] aggregations)
                => AddItems(aggregations);

            public Sequence AddItem(ISymbolNodeAggregation aggregation)
            {
                aggregations.Add(aggregation
                    .ThrowIfNull(
                        () => new ArgumentNullException(nameof(aggregation)))
                    .ThrowIf(
                        agg => agg == this,
                        _ => new InvalidOperationException($"Invalid aggregation: cannot add self")));
                return this;
            }

            public void Add(
                ISymbolNodeAggregation aggregation)
                => AddItem(aggregation);

            public bool Equals(Sequence? other)
            {
                return
                    other is Sequence seq
                    && IsOptional == other.IsOptional
                    && Count == seq.Count
                    && aggregations.SequenceEqual(other.aggregations);
            }

            public override bool Equals(object? obj)
            {
                return obj is Sequence other && Equals(other);
            }

            public override int GetHashCode()
            {
                return aggregations.Aggregate(IsOptional ? 0 : 1, HashCode.Combine);
            }
        }

        internal struct Unit:
            ISymbolNodeAggregation,
            IEquatable<Unit>,
            IDefaultValueProvider<Unit>
        {
            public static Unit Default => default;


            public ISymbolNode Node;

            public bool IsDefault => Node is null;

            public Unit(ISymbolNode node)
            {
                ArgumentNullException.ThrowIfNull(node);

                Node = node;
            }

            public bool Equals(Unit other)
            {
                return EqualityComparer<ISymbolNode>.Default.Equals(Node, other.Node);
            }

            public override bool Equals(object? obj)
            {
                return obj is Unit other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Node);
            }
        }

        #endregion
    }

    public static class SymbolNodeAggregationExtension
    {
        public static int RequiredNodeCount(this ISymbolNodeAggregation aggregation)
        {
            ArgumentNullException.ThrowIfNull(aggregation);

            return aggregation switch
            {
                ISymbolNodeAggregation.Unit => 1,
                ISymbolNodeAggregation.Sequence seq => seq.IsOptional switch
                {
                    true => 0,
                    false => seq.Aggregations.Sum(RequiredNodeCount)
                },
                _ => throw new InvalidOperationException(
                    $"Invalid {nameof(aggregation)}: {aggregation.GetType()}")
            };
        }

        public static int NodeCount(this ISymbolNodeAggregation aggregation)
        {
            ArgumentNullException.ThrowIfNull(aggregation);

            return aggregation switch
            {
                ISymbolNodeAggregation.Unit => 1,
                ISymbolNodeAggregation.Sequence seq => seq
                    .Aggregations
                    .Sum(NodeCount),
                _ => throw new InvalidOperationException(
                    $"Invalid {nameof(aggregation)}: {aggregation.GetType()}")
            };
        }

        public static ISymbolNode[] AllNodes(this ISymbolNodeAggregation aggregation)
        {
            return aggregation.AllNodesInner().ToArray();
        }

        // This algorithm is not efficient with respect to allocations. Review it.
        private static IEnumerable<ISymbolNode> AllNodesInner(this ISymbolNodeAggregation aggregation)
        {
            ArgumentNullException.ThrowIfNull(aggregation);

            return aggregation switch
            {
                ISymbolNodeAggregation.Unit unit => new[] { unit.Node },
                ISymbolNodeAggregation.Sequence seq => seq
                    .Aggregations
                    .SelectMany(AllNodesInner),
                _ => throw new InvalidOperationException(
                    $"Invalid {nameof(aggregation)}: {aggregation.GetType()}")
            };
        }
    }
}
