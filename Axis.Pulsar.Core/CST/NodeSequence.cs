using Axis.Luna.Extensions;
using System.Collections;

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
        /// An empty node sequence
        /// </summary>
        public static INodeSequence Empty { get; } = new CollectionNodeSequence(Array.Empty<ICSTNode>());

        public static INodeSequence Of(
            ICSTNode singleNode)
            => new CollectionNodeSequence(new[] { singleNode });

        public static INodeSequence Of(
            ICollection<ICSTNode> collection)
            => new CollectionNodeSequence(collection);

        public static INodeSequence Of(
            INodeSequence first,
            INodeSequence second)
            => new ConcatenatedNodeSequence(first, second);

        public static INodeSequence Of(params INodeSequence[] sequences)
        {
            ArgumentNullException.ThrowIfNull(sequences);

            return sequences.Length switch
            {
                1 => sequences[0],
                > 1 => sequences.Aggregate(
                    (prev, next) => new ConcatenatedNodeSequence(prev, next)),

                _ => throw new InvalidOperationException(
                    $"Invalid argument length: {sequences.Length}"),
            };
        }


        #region Nested types
        /// <summary>
        /// 
        /// </summary>
        private class CollectionNodeSequence: INodeSequence
        {
            private readonly ICollection<ICSTNode> _nodes;

            public int Count => _nodes.Count;

            internal CollectionNodeSequence(ICollection<ICSTNode> nodes)
            {
                _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            }

            public IEnumerator<ICSTNode> GetEnumerator() => _nodes.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        private class ConcatenatedNodeSequence: INodeSequence
        {
            private readonly IEnumerable<ICSTNode> _nodes;

            public int Count { get; private set; }

            internal ConcatenatedNodeSequence(INodeSequence first, INodeSequence second)
            {
                ArgumentNullException.ThrowIfNull(first);
                ArgumentNullException.ThrowIfNull(second);

                _nodes = first.Concat(second);
                Count = first.Count + second.Count;
            }

            public IEnumerator<ICSTNode> GetEnumerator() => _nodes.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        #endregion
    }

    internal static class NodeSequenceExtensions
    {
        public static INodeSequence Fold(
            this IEnumerable<INodeSequence> nodes)
        {
            return nodes
                .ThrowIfNull(() => new ArgumentNullException(nameof(nodes)))
                .Aggregate(INodeSequence.Empty, INodeSequence.Of);
        }

        public static INodeSequence Prepend(this
            INodeSequence successor,
            INodeSequence predecessor)
            => predecessor.Append(successor);

        public static INodeSequence Append(this
            INodeSequence predecessor,
            INodeSequence successor)
            => INodeSequence.Of(predecessor, successor);
    }
}
