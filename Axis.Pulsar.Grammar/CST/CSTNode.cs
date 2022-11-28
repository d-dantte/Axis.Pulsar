using System;
using System.Linq;
using System.Text;

namespace Axis.Pulsar.Grammar.CST
{
    /// <summary>
    /// Concrete Syntax Tree node
    /// </summary>
    public interface CSTNode
    {
        #region Of

        /// <summary>
        /// Creates a <see cref="LeafNode"/> type of <see cref="CSTNode"/>
        /// </summary>
        /// <param name="symbolName">The symbol name of this node</param>
        /// <param name="tokens">The recognized tokens</param>
        public static CSTNode Of(string symbolName, string tokens) => new LeafNode(symbolName, tokens);

        /// <summary>
        /// Creates a <see cref="BranchNode"/> type of <see cref="CSTNode"/>
        /// </summary>
        /// <param name="symbolName">The symbol name of this node</param>
        /// <param name="nodes">The recognized nodes</param>
        /// <returns></returns>
        public static CSTNode Of(string symbolName, params CSTNode[] nodes) => new BranchNode(symbolName, nodes);

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
        public record LeafNode : CSTNode
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

            public override string ToString() => $"{{{SymbolName}::{Tokens}}}";
        }

        /// <summary>
        /// Represents a branch-node. This node consists of zero or more <see cref="CSTNode"/>s.
        /// </summary>
        public record BranchNode : CSTNode
        {
            private readonly CSTNode[] _nodes;
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
            public CSTNode[] Nodes => _nodes.ToArray();

            /// <summary>
            /// Count of the present nodes.
            /// </summary>
            public int NodeCount => _nodes.Length;

            /// <summary>
            /// Indicates if the Node has inner/children nodes or not.
            /// </summary>
            public bool IsEmpty => _nodes.IsEmpty();

            internal BranchNode(
                string symbolName,
                params CSTNode[] nodes)
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
                                _ => throw new ArgumentException($"Invalid {nameof(CSTNode)} union-type: {(next.GetType().FullName)}")
                            }))
                .ToString());
            }

            private bool ContainsInvalidNodeType(CSTNode[] nodes)
            {
                return nodes.Any(node => node switch
                {
                    LeafNode => false,
                    BranchNode => false,
                    _ => true
                });
            }

            public override string ToString()
            {
                return _nodes
                    .Select(node => node.ToString())
                    .Map(strings => string.Join(", ", strings))
                    .Map(@string => $"{{{SymbolName}::[{@string}]}}");
            }
        }
    }
}
