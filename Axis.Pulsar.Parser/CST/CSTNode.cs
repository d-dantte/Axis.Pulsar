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
        /// <summary>
        /// The symbol name.
        /// </summary>
        string SymbolName { get; }

        #region Node accessors
        /// <summary>
        /// Get the first symbol found by searching corresponding child-symbols that match the path given.
        /// <para>
        /// The path is a '.' separated list of symbol names. e.g expression.operator.constant.
        /// </para>
        /// <para>
        /// Note that individual name segmens of paths can include alternative names to search, indicated by the '|' character. e.g
        /// <c>abc.def|ghi|qrs.klm|xyz.rst</c>
        /// </para>
        /// </summary>
        /// <param name="path">The path along which to search</param>
        /// <param name="node">The node, if found</param>
        public bool TryFindNode(string path, out ICSTNode node);

        /// <summary>
        /// Gets all symbols found by searching corresponding child-symbols that match the path given.
        /// <para>
        /// The path is a '.' separated list of symbol names. e.g expression.operator.constant.
        /// </para>
        /// <para>
        /// Note that individual name segmens of paths can include alternative names to search, indicated by the '|' character. e.g
        /// <c>abc.def|ghi|qrs.klm|xyz.rst</c>
        /// </para>
        /// </summary>
        /// <param name="path">The path along which to search</param>
        /// <param name="nodes">The nodes fitting the search criteria</param>
        public bool TryFindNodes(string path, out ICSTNode[] nodes);

        /// <summary>
        /// Recursively search the entire Node tree for all occurence of nodes whose names matches the given symbol name.
        /// </summary>
        /// <param name="symbolName">The symbol name to match</param>
        /// <param name="nodes">All of the nodes found</param>
        /// <returns>Indicating if nodes were found, or not</returns>
        public bool TryFindAllNodes(string symbolName, out ICSTNode[] nodes);

        /// <summary>
        /// Returns all nodes that fit the path parameter.
        /// <para>
        /// The path is a '.' separated list of symbol names. e.g expression.operator.constant.
        /// </para>
        /// <para>
        /// Note that individual name segmens of paths can include alternative names to search, indicated by the '|' character. e.g
        /// <c>abc.def|ghi|qrs.klm|xyz.rst</c>
        /// </para>
        /// </summary>
        /// <param name="path">The path along which to search</param>
        public IEnumerable<ICSTNode> FindNodes(string path);

        /// <summary>
        /// Recursively search and the entire tree and return all nodes whose name matches the given symbol name.
        /// </summary>
        /// <param name="symbolName">The symbol name to match</param>
        /// <returns>The matching nodes</returns>
        public IEnumerable<ICSTNode> FindAllNodes(string symbolName);

        /// <summary>
        /// Returns the first node that fits the path parameter.
        /// <para>
        /// The path is a '.' separated list of symbol names. e.g expression.operator.constant.
        /// </para>
        /// <para>
        /// Note that individual name segmens of paths can include alternative names to search, indicated by the '|' character. e.g
        /// <c>abc.def|ghi|qrs.klm|xyz.rst</c>
        /// </para>
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public ICSTNode FindNode(string path);

        /// <summary>
        /// Returns the first inner node, or null if empty.
        /// </summary>
        public ICSTNode FirstNode();

        /// <summary>
        /// Returns the last inner node, or null if empty.
        /// </summary>
        /// <returns></returns>
        public ICSTNode LastNode();

        /// <summary>
        /// Get the node at the given index.
        /// </summary>
        /// <param name="index">zero-based index</param>
        public ICSTNode NodeAt(int index);

        /// <summary>
        /// Gets all the immediate child nodes of the current node
        /// </summary>
        public IEnumerable<ICSTNode> AllChildNodes();
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


            #region Node Accessors
            /// <inheritdoc/>
            public bool TryFindNode(string path, out ICSTNode node)
            {
                node = null;
                return false;
            }

            /// <inheritdoc/>
            public bool TryFindNodes(string path, out ICSTNode[] nodes)
            {
                nodes = Array.Empty<ICSTNode>();
                return false;
            }

            /// <inheritdoc/>
            public bool TryFindAllNodes(string symbolName, out ICSTNode[] nodes)
            {
                nodes = Array.Empty<ICSTNode>();
                return false;
            }


            /// <inheritdoc/>
            public IEnumerable<ICSTNode> FindNodes(string path) => Enumerable.Empty<ICSTNode>();

            /// <inheritdoc/>
            public IEnumerable<ICSTNode> FindAllNodes(string symbolName) => Enumerable.Empty<ICSTNode>();

            /// <inheritdoc/>
            public ICSTNode FindNode(string path) => null;

            /// <inheritdoc/>
            public ICSTNode FirstNode() => null;

            /// <inheritdoc/>
            public ICSTNode LastNode() => null;

            /// <inheritdoc/>
            public ICSTNode NodeAt(int index) => Array.Empty<ICSTNode>()[index];

            /// <inheritdoc/>
            public IEnumerable<ICSTNode> AllChildNodes() => Enumerable.Empty<ICSTNode>();
            #endregion
        }

        /// <summary>
        /// Creates a <see cref="LeafNode"/> type of <see cref="ICSTNode"/>
        /// </summary>
        /// <param name="symbolName">The symbol name of this node</param>
        /// <param name="tokens">The recognized tokens</param>
        public static ICSTNode Of(string symbolName, string tokens) => new LeafNode(symbolName, tokens);


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
            public IEnumerable<ICSTNode> Nodes => _nodes.AsEnumerable();

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

            #region Node accessors
            /// <inheritdoc/>
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

            /// <inheritdoc/>
            public bool TryFindNodes(string path, out ICSTNode[] nodes)
            {
                var names = path.Split('.', StringSplitOptions.RemoveEmptyEntries);

                try
                {
                    nodes = names
                        .Aggregate(this.Enumerate().Cast<ICSTNode>(), GetChildren)
                        .ToArray();

                    if (nodes.Length > 0)
                        return true;

                    else
                    {
                        nodes = null;
                        return false;
                    }
                }
                catch
                {
                    nodes = null;
                    return false;
                }
            }

            /// <inheritdoc/>
            public bool TryFindAllNodes(string symbolName, out ICSTNode[] nodes)
            {
                nodes = this
                    .FindAllNodes(symbolName)
                    .ToArray();
                return nodes.Length > 0;
            }

            /// <inheritdoc/>
            public IEnumerable<ICSTNode> FindNodes(string path)
            {
                _ = TryFindNodes(path, out var result);
                return result;
            }

            /// <inheritdoc/>
            public ICSTNode FindNode(string path)
            {
                _ = TryFindNode(path, out var result);
                return result;
            }

            /// <inheritdoc/>
            public IEnumerable<ICSTNode> FindAllNodes(string symbolName)
            {
                return _nodes.Aggregate(
                    Enumerable.Empty<ICSTNode>(),
                    (list, node) =>
                    {
                        if (node.SymbolName.Equals(symbolName))
                            list.Concat(node);

                        return node
                            .AllChildNodes()
                            .SelectMany(n => n.FindAllNodes(symbolName))
                            .Map(list.Concat);
                    });
            }

            /// <inheritdoc/>
            public ICSTNode FirstNode() => _nodes.IsEmpty() ? null : _nodes[0];

            /// <inheritdoc/>
            public ICSTNode LastNode() => _nodes.IsEmpty() ? null : _nodes[^1];

            /// <inheritdoc/>
            public ICSTNode NodeAt(int index) => _nodes.IsEmpty() ? null : _nodes[index];

            /// <inheritdoc/>
            public IEnumerable<ICSTNode> AllChildNodes() => _nodes.AsEnumerable();
            #endregion

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
            {
                var nameSet = name
                    .Split('|')
                    .Map(names => new HashSet<string>(names));
                return _nodes.Where(node => nameSet.Contains(node.SymbolName));
            }

            private bool ContainsInvalidNodeType(ICSTNode[] nodes)
            {
                return nodes.Any(node => node switch
                {
                    ICSTNode.LeafNode => false,
                    ICSTNode.BranchNode => false,
                    _ => true
                });
            }

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
