using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axis.Pulsar.Parser.CST
{
    /// <summary>
    /// Concrete Syntax Tree node
    /// </summary>
    public interface ICSTNode
    {
        #region Of

        /// <summary>
        /// Creates a <see cref="LeafNode"/> type of <see cref="ICSTNode"/>
        /// </summary>
        /// <param name="symbolName">The symbol name of this node</param>
        /// <param name="tokens">The recognized tokens</param>
        public static ICSTNode Of(string symbolName, string tokens) => new LeafNode(symbolName, tokens);

        /// <summary>
        /// Creates a <see cref="BranchNode"/> type of <see cref="ICSTNode"/>
        /// </summary>
        /// <param name="symbolName">The symbol name of this node</param>
        /// <param name="nodes">The recognized nodes</param>
        /// <returns></returns>
        public static ICSTNode Of(string symbolName, params ICSTNode[] nodes) => new BranchNode(symbolName, nodes);

        #endregion

        #region Members
        /// <summary>
        /// The symbol name.
        /// </summary>
        string SymbolName { get; }

        #endregion

        /// <summary>
        /// Represents a leaf-node. This node accepts as tokens, anything except <c>null</c>.
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
        /// Represents a branch-node. This node consists of zero or more <see cref="ICSTNode"/>s.
        /// </summary>
        public record BranchNode : ICSTNode
        {
            private readonly ICSTNode[] _nodes;
            private readonly Lazy<string> _aggregatedTokens;

            /// <inheritdoc/>
            public string SymbolName { get; }

            /// <summary>
            /// Aggregated tokens from all child-nodes.
            /// </summary>
            public string AggregateTokens => _aggregatedTokens.Value;

            /// <summary>
            /// Child-nodes.
            /// </summary>
            public ICSTNode[] Nodes => _nodes.ToArray();

            /// <summary>
            /// Count of the present nodes.
            /// </summary>
            public int NodeCount => _nodes.Length;

            /// <summary>
            /// Indicates if the Node has inner/children nodes or not.
            /// </summary>
            public bool IsEmpty => _nodes.IsEmpty();

            internal BranchNode(string symbolName, params ICSTNode[] nodes)
            {
                SymbolName = symbolName.ThrowIf(
                    string.IsNullOrWhiteSpace,
                    new ArgumentException("Invalid symbol name"));

                _nodes = nodes
                    .ThrowIf(Extensions.IsNull, new ArgumentNullException(nameof(nodes)))
                    .ThrowIf(Extensions.ContainsNull, new ArgumentException("Symbol array must not contain null elements"))
                    .ThrowIf(ContainsInvalidNodeType, new ArgumentException($"Symbol array contains an invalid node type (neither '{nameof(LeafNode)}' nor '{nameof(BranchNode)}')"))
                    .ToArray();

                _aggregatedTokens = new Lazy<string>(() => nodes
                    .Aggregate(
                        new StringBuilder(),
                        (acc, next) => acc.Append(
                            next switch
                            {
                                LeafNode leaf => leaf.Tokens,
                                BranchNode branch => branch.AggregateTokens,
                                _ => throw new ArgumentException($"Invalid {nameof(ICSTNode)} union-type: {(next.GetType())}")
                            }))
                .ToString());
            }

            private bool ContainsInvalidNodeType(ICSTNode[] nodes)
            {
                return nodes.Any(node => node switch
                {
                    LeafNode => false,
                    BranchNode => false,
                    _ => true
                });
            }
        }
    }
}
