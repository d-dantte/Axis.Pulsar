using Axis.Luna.Common;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Axis.Pulsar.Core.Grammar.Composite.Group;

namespace Axis.Pulsar.Core.CST
{
    /// <summary>
    /// Defines a sequence of <see cref="ICSTNode"/> instances
    /// </summary>
    public interface INodeSequence: IEnumerable<ICSTNode>
    {
        /// <summary>
        /// Number of elements in the sequence
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Number of non-optional elements in the sequence
        /// </summary>
        int RequiredNodeCount { get; }

        /// <summary>
        /// Indicates if this node sequence was reocgnized by a group element with <see cref="Cardinality.IsZeroMinOccurence"/>.
        /// </summary>
        bool IsOptional { get; }

        /// <summary>
        /// An empty node sequence
        /// </summary>
        public static INodeSequence Empty { get; } = new CollectionNodeSequence(Array.Empty<ICSTNode>());

        public static INodeSequence Of(
            ICSTNode singleNode,
            bool isOptional = false)
            => new CollectionNodeSequence(new[] { singleNode }, isOptional);

        public static INodeSequence Of(
            ICollection<ICSTNode> collection,
            bool isOptional = false)
            => new CollectionNodeSequence(collection, isOptional);

        public static INodeSequence Of(
            INodeSequence sequence,
            bool isOptional)
            => new ConcatenatedNodeSequence(Empty, sequence, isOptional);

        public static INodeSequence Of(
            INodeSequence first,
            INodeSequence second,
            bool isOptional = false)
            => new ConcatenatedNodeSequence(first, second, isOptional);


        #region Nested types
        /// <summary>
        /// make these structs?
        /// </summary>
        private readonly struct CollectionNodeSequence :
            INodeSequence,
            IEquatable<CollectionNodeSequence>,
            IDefaultValueProvider<CollectionNodeSequence>
        {
            private readonly ICollection<ICSTNode> _nodes;
            private readonly bool _isOptional;

            public int Count => _nodes?.Count ?? 0;

            public int RequiredNodeCount => _isOptional ? 0 : Count;

            public bool IsOptional => _isOptional;

            public bool IsDefault => _nodes is null;

            public static CollectionNodeSequence Default => default;

            /// <summary>
            /// Creates a new instance of this type.
            /// <para/>
            /// Within a production, nodes-sequences having a "zero or more", or "zero or one" cardinality are considered
            /// optional, and as such do not participate in "recognition threshold" counting.
            /// </summary>
            /// <param name="nodes">The node sequence</param>
            /// <param name="isOptional">Indicating if the nodes were recognized from an "optional" cardinaltiy</param>
            /// <exception cref="ArgumentNullException"></exception>
            internal CollectionNodeSequence(
                ICollection<ICSTNode> nodes,
                bool isOptional = false)
            {
                _isOptional = isOptional;
                _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            }

            public IEnumerator<ICSTNode> GetEnumerator()
                => _nodes?.GetEnumerator() ?? Enumerable.Empty<ICSTNode>().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public override bool Equals(
                [NotNullWhen(true)] object? obj)
                => obj is CollectionNodeSequence other && Equals(other);

            public bool Equals(
                CollectionNodeSequence other)
            {
                if (IsDefault && other.IsDefault)
                    return true;

                if (IsDefault ^ other.IsDefault)
                    return false;

                return _isOptional == other._isOptional
                    && _nodes.SequenceEqual(other._nodes);
            }

            public override int GetHashCode()
            {
                if (IsDefault)
                    return 0;

                return _nodes.Aggregate(RequiredNodeCount, HashCode.Combine);
            }

            public static bool operator ==(
                CollectionNodeSequence left,
                CollectionNodeSequence right)
                => left.Equals(right);

            public static bool operator !=(
                CollectionNodeSequence left,
                CollectionNodeSequence right)
                => !left.Equals(right);
        }

        /// <summary>
        /// make these structs?
        /// </summary>
        private readonly struct ConcatenatedNodeSequence:
            INodeSequence,
            IEquatable<ConcatenatedNodeSequence>,
            IDefaultValueProvider<ConcatenatedNodeSequence>
        {
            private readonly IEnumerable<ICSTNode> _nodes;
            private readonly bool _isOptional;

            public int Count { get; }

            public int RequiredNodeCount { get; }

            public bool IsOptional => _isOptional;

            public bool IsDefault => _nodes is null;

            public static ConcatenatedNodeSequence Default => default;

            internal ConcatenatedNodeSequence(
                INodeSequence first,
                INodeSequence second,
                bool isOptional = false)
            {
                ArgumentNullException.ThrowIfNull(first);
                ArgumentNullException.ThrowIfNull(second);

                _isOptional = isOptional;
                _nodes = Enumerable.Concat(first, second);
                Count = first.Count + second.Count;
                RequiredNodeCount = !isOptional
                    ? first.RequiredNodeCount + second.RequiredNodeCount
                    : 0;
            }

            public IEnumerator<ICSTNode> GetEnumerator() =>
                _nodes?.GetEnumerator() ?? Enumerable.Empty<ICSTNode>().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public override bool Equals(
                [NotNullWhen(true)] object? obj)
                => obj is ConcatenatedNodeSequence other && Equals(other);

            public bool Equals(
                ConcatenatedNodeSequence other)
            {
                if (IsDefault && other.IsDefault)
                    return true;

                if (IsDefault ^ other.IsDefault)
                    return false;

                return RequiredNodeCount == other.RequiredNodeCount
                    && _nodes.SequenceEqual(other._nodes);
            }

            public override int GetHashCode()
            {
                if (IsDefault)
                    return 0;

                return _nodes.Aggregate(RequiredNodeCount, HashCode.Combine);
            }

            public static bool operator ==(
                ConcatenatedNodeSequence left,
                ConcatenatedNodeSequence right)
                => left.Equals(right);

            public static bool operator !=(
                ConcatenatedNodeSequence left,
                ConcatenatedNodeSequence right)
                => !left.Equals(right);
        }
        #endregion
    }

    internal static class NodeSequenceExtensions
    {
        public static INodeSequence ConcatSequence(this
            INodeSequence predecessor,
            INodeSequence successor,
            bool isOptional = false)
            => INodeSequence.Of(predecessor, successor, isOptional);
    }
}
