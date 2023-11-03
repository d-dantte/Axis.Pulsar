using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Utils;
using System.Collections;

namespace Axis.Pulsar.Core.CST
{
    /// <summary>
    /// 
    /// </summary>
    public class NodeSequence : IEnumerable<ICSTNode>
    {
        private ICollection<ICSTNode> _nodes;
        private NodeSequence? _parent;
        private Lazy<Tokens> _tokens;

        /// <summary>
        /// 
        /// </summary>
        public NodeSequence? Parent => _parent;

        /// <summary>
        /// 
        /// </summary>
        public int Count => _nodes.Count + (_parent?.Count ?? 0);

        /// <summary>
        /// 
        /// </summary>
        public Tokens Tokens => _tokens.Value;

        /// <summary>
        /// 
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
            => new NodeSequence(child._nodes, parent);

        public static implicit operator NodeSequence(List<ICSTNode> nodes) => new(nodes);
        public static implicit operator NodeSequence(ICSTNode[] nodes) => new(nodes);

        #region IEnumerable
        public IEnumerator<ICSTNode> GetEnumerator() => Enumerate().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ICSTNode> Enumerate()
        {
            var prev = _parent?.AsEnumerable() ?? Enumerable.Empty<ICSTNode>();
            return prev.Concat(_nodes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public NodeSequence Prepend(NodeSequence parent)
        {
            return NodeSequence
                .Flatten(this)
                .Select(node => node._nodes)
                .Aggregate(parent, NodeSequence.Of);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public NodeSequence Append(NodeSequence nodes)
        {
            return NodeSequence.Of(this, nodes._nodes);
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

    public static class NodeSequenceExtensions
    {
        public static NodeSequence Fold(this IEnumerable<NodeSequence> nodes)
        {
            return nodes
                .ThrowIfNull(new ArgumentNullException(nameof(nodes)))
                .Aggregate(NodeSequence.Empty, NodeSequence.Of);
        }
    }
}
