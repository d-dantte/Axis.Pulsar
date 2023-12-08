using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Utils;
using System.Collections;

namespace Axis.Pulsar.Core.CST
{
    /// <summary>
    /// A sequence of nodes
    /// </summary>
    public class NodeSequence : IEnumerable<ICSTNode>
    {
        private readonly ICollection<ICSTNode> _nodes;
        private readonly NodeSequence? _parent;
        private readonly Lazy<Tokens> _tokens;

        /// <summary>
        /// The parent node sequence
        /// </summary>
        public NodeSequence? Parent => _parent;

        /// <summary>
        /// The number of nodes in this sequence, including that of the parent if available.
        /// </summary>
        public int Count => _nodes.Count + (_parent?.Count ?? 0);

        /// <summary>
        /// Concatenated tokens of all contained nodes.
        /// </summary>
        public Tokens Tokens => _tokens.Value;

        /// <summary>
        /// Returns an empty node sequence
        /// </summary>
        public static NodeSequence Empty { get; } = new NodeSequence();

        public NodeSequence()
        {
            _nodes = Array.Empty<ICSTNode>();
            _tokens = new Lazy<Tokens>(Tokens.Empty);
        }

        public NodeSequence(ICollection<ICSTNode> nodes, NodeSequence? parent = null)
        {
            ArgumentNullException.ThrowIfNull(nodes);

            _nodes = nodes;
            _parent = parent;
            _tokens = new Lazy<Tokens>(() => this.Select(node => node.Tokens).Join());
        }

        public static NodeSequence Of(
            IEnumerable<ICSTNode> nodes)
            => new NodeSequence(nodes.ToArray(), null);

        public static NodeSequence Of(
            params ICSTNode[] nodes)
            => new NodeSequence(nodes, null);

        public static NodeSequence Of(
            ICollection<ICSTNode> nodes)
            => new NodeSequence(nodes, null);

        public static NodeSequence Of(
            NodeSequence parent,
            ICollection<ICSTNode> nodes)
            => new NodeSequence(nodes, parent);

        public static NodeSequence Of(
            NodeSequence parent,
            NodeSequence child)
            => new NodeSequence(child.ToArray(), parent);

        public static implicit operator NodeSequence(List<ICSTNode> nodes) => new(nodes);
        public static implicit operator NodeSequence(ICSTNode[] nodes) => new(nodes);

        #region IEnumerable
        public IEnumerator<ICSTNode> GetEnumerator() => Enumerate().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        /// <summary>
        /// Creates an enumerable to enumerate the <see cref="NodeSequence"/>
        /// </summary>
        public IEnumerable<ICSTNode> Enumerate()
        {
            var prev = _parent?.Enumerate() ?? Enumerable.Empty<ICSTNode>();
            return prev.Concat(_nodes);
        }

        /// <summary>
        /// Prepends the given sequence to this sequence in a new instance.
        /// </summary>
        /// <param name="parent">The sequence to prepend</param>
        /// <returns>The new node sequence</returns>
        public NodeSequence Prepend(NodeSequence parent)
        {
            return NodeSequence.Of(parent, this);
        }
        public NodeSequence Prepend__(NodeSequence parent)
        {
            return NodeSequence
                .Flatten(this)
                .Select(node => node._nodes)
                .Aggregate(parent, NodeSequence.Of);
        }

        /// <summary>
        /// Appends the given sequence to this sequence in a new instance
        /// </summary>
        /// <param name="nodes">The sequence to append</param>
        /// <returns>The new sequence</returns>
        public NodeSequence Append(NodeSequence nodes)
        {
            return NodeSequence.Of(this, nodes);
        }

        private static IEnumerable<NodeSequence> Flatten(NodeSequence nodeSequence)
        {
            var list = new List<NodeSequence>();
            var seq = nodeSequence;
            do
            {
                list.Add(seq);
                seq = seq.Parent;
            }
            while (seq is not null);

            return list.AsEnumerable().Reverse();
        }
    }

    internal static class NodeSequenceExtensions
    {
        public static NodeSequence Fold(this IEnumerable<NodeSequence> nodes)
        {
            return nodes
                .ThrowIfNull(new ArgumentNullException(nameof(nodes)))
                .Aggregate(NodeSequence.Empty, NodeSequence.Of);
        }
    }
}
