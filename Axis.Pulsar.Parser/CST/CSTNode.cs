using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axis.Pulsar.Parser.CST
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICSTNode
    {
        /// <summary>
        /// The symbol name.
        /// </summary>
        string SymbolName { get; }



        /// <summary>
        /// 
        /// </summary>
        public record LeafNode: ICSTNode
        {
            /// <inheritdoc/>
            public string SymbolName { get; }

            /// <summary>
            /// The effective tokens this node consists of.
            /// </summary>
            public string Tokens { get; }

            internal LeafNode(string symbolName, string tokens)
            {
                SymbolName = symbolName.ThrowIf(
                    string.IsNullOrWhiteSpace,
                    new ArgumentException("Invalid symbol name"));

                Tokens = tokens.ThrowIf(
                    Extensions.IsNull,
                    new ArgumentException("Invalid tokens"));
            }
        }

        /// <summary>
        /// Creates a <see cref="LeafNode"/> type of <see cref="ICSTNode"/>
        /// </summary>
        /// <param name="symbolName">The symbol name of this node</param>
        /// <param name="tokens">The recognized tokens</param>
        public static ICSTNode Of(string symbolName, string tokens) => new LeafNode(symbolName, tokens);


        /// <summary>
        /// 
        /// </summary>
        public class BranchNode : ICSTNode
        {
            private readonly ICSTNode[] _nodes;
            private readonly Lazy<string> _aggregatedTokens;

            /// <inheritdoc/>
            public string SymbolName { get; }

            /// <summary>
            /// Aggregated tokens from all child-nodes.
            /// </summary>
            public string AggregatedTokens => _aggregatedTokens.Value;

            /// <summary>
            /// Child-nodes.
            /// </summary>
            public IEnumerable<ICSTNode> Nodes => _nodes.AsEnumerable();

            /// <summary>
            /// Count of the present nodes.
            /// </summary>
            public int NodeCount => _nodes.Length;

            internal BranchNode(string symbolName, params ICSTNode[] nodes)
            {
                SymbolName = symbolName.ThrowIf(
                    string.IsNullOrWhiteSpace,
                    new ArgumentException("Invalid symbol name"));

                _nodes = nodes
                    .ThrowIf(Extensions.IsNull, new ArgumentNullException(nameof(nodes)))
                    .ThrowIf(Extensions.ContainsNull, new ArgumentException("Symbol array must not contain null elements"))
                    .ToArray();

                _aggregatedTokens = new Lazy<string>(() => nodes
                .Aggregate(new StringBuilder(), (acc, next) => acc.Append(
                    next switch
                    {
                        LeafNode leaf => leaf.Tokens,
                        BranchNode branch => branch.AggregatedTokens,
                        _ => throw new ArgumentException($"Invalid {nameof(ICSTNode)} union-type: {(next.GetType())}")
                    }))
                .ToString());
            }

            /// <summary>
            /// Get the first symbol found by searching corresponding child-symbols that match the path given.
            /// The path is a '.' separated list of symbol names. e.g expression.operator.constant
            /// </summary>
            /// <param name="path"></param>
            /// <param name="node"></param>
            /// <returns></returns>
            public bool TryFindNode(string path, out ICSTNode node)
            {
                if (TryFindNodes(path, out var children))
                {
                    node = children[0];
                    return true;
                }
                else
                {
                    node = null;
                    return false;
                }
            }

            /// <summary>
            /// Gets all symbols found by searching corresponding child-symbols that match the path given.
            /// The path is a '.' separated list of symbol names. e.g expression.operator.constant
            /// </summary>
            /// <param name="path"></param>
            /// <param name="child"></param>
            /// <returns></returns>
            public bool TryFindNodes(string path, out ICSTNode[] children)
            {
                var names = path.Split('.', StringSplitOptions.RemoveEmptyEntries);

                try
                {
                    children = names
                        .Aggregate(this.Enumerate().Cast<ICSTNode>(), GetChildren)
                        .ToArray();

                    if (children.Length > 0)
                        return true;

                    else
                    {
                        children = null;
                        return false;
                    }
                }
                catch
                {
                    children = null;
                    return false;
                }
            }

            private static IEnumerable<ICSTNode> GetChildren(
                IEnumerable<ICSTNode> nodes,
                string name) 
                => nodes.SelectMany(node => node switch
                {
                    LeafNode => new ICSTNode[0],
                    BranchNode branch => branch.GetChildren(name),
                    _ => throw new ArgumentException($"Invalid {nameof(ICSTNode)} union-type: {(node.GetType())}")
                });

            private IEnumerable<ICSTNode> GetChildren(string name)
                => _nodes.Where(node => node.SymbolName.Equals(name, StringComparison.InvariantCulture));

        }

        /// <summary>
        /// Creates a <see cref="BranchNode"/> type of <see cref="ICSTNode"/>
        /// </summary>
        /// <param name="symbolName">The symbol name of this node</param>
        /// <param name="nodes">The recognized nodes</param>
        /// <returns></returns>
        public static ICSTNode Of(string symbolName, params ICSTNode[] nodes) => new BranchNode(symbolName, nodes);
    }
}
