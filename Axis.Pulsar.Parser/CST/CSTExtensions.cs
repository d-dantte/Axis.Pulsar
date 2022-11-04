using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.CST
{
    public static class CSTExtensions
    {
        public static int NodeCount(this ICSTNode node)
        {
            return node switch
            {
                ICSTNode.LeafNode => -1,
                ICSTNode.BranchNode branch => branch.NodeCount,
                _ => throw new ArgumentException($"Invalid node type: {node?.GetType()}")
            };
        }

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
        public static bool TryFindNode(this ICSTNode source, string path, out ICSTNode node)
        {
            if (source.TryFindNodes(path, out var children))
            {
                node = children[0];
                return true;
            }

            node = null;
            return false;
        }

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
        public static bool TryFindNodes(this ICSTNode source, string path, out ICSTNode[] nodes)
        {
            nodes = null;
            return source switch
            {
                ICSTNode.LeafNode leaf => false,

                ICSTNode.BranchNode branch => (nodes = path
                    .ThrowIf(string.IsNullOrWhiteSpace, new ArgumentException($"Invalid path: {path}"))
                    .Split('.', StringSplitOptions.RemoveEmptyEntries)
                    .Aggregate(source.Enumerate(), GetChildren)
                    .ToArray())
                    .Length > 0,

                _ => throw new ArgumentException($"Invalid node type: {source?.GetType()}")
            };
        }

        /// <summary>
        /// Recursively search the entire Node tree for all occurence of nodes whose names matches the given symbol name.
        /// </summary>
        /// <param name="symbolName">The symbol name to match</param>
        /// <param name="nodes">All of the nodes found</param>
        /// <returns>Indicating if nodes were found, or not</returns>
        public static bool TryFindAllNodes(this ICSTNode source, string symbolName, out ICSTNode[] nodes)
        {
            nodes = null;
            return source switch
            {
                ICSTNode.LeafNode => false,

                ICSTNode.BranchNode => (nodes = source
                    .FindAllNodes(symbolName)
                    .ToArray())
                    .Length > 0,

                _ => throw new ArgumentException($"Invalid node type: {source?.GetType()}")
            };
        }

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
        public static IEnumerable<ICSTNode> FindNodes(this ICSTNode source, string path)
        {
            return source switch
            {
                ICSTNode.LeafNode => Enumerable.Empty<ICSTNode>(),

                ICSTNode.BranchNode => source.TryFindNodes(path, out var result) ? result : result,

                _ => throw new ArgumentException($"Invalid node type: {source?.GetType()}")
            };
        }

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
        public static ICSTNode FindNode(this ICSTNode source, string path)
        {
            return source switch
            {
                ICSTNode.LeafNode => null,

                ICSTNode.BranchNode => source.TryFindNode(path, out var result) ? result : result,

                _ => throw new ArgumentException($"Invalid node type: {source?.GetType()}")
            };
        }

        /// <summary>
        /// Recursively search and the entire tree and return all nodes whose name matches the given symbol name.
        /// </summary>
        /// <param name="symbolName">The symbol name to match</param>
        /// <returns>The matching nodes</returns>
        public static IEnumerable<ICSTNode> FindAllNodes(this ICSTNode source, string symbolName)
        {
            return source switch
            {
                ICSTNode.LeafNode => Enumerable.Empty<ICSTNode>(),

                ICSTNode.BranchNode branch => branch.Nodes
                    .Aggregate(
                        Enumerable.Empty<ICSTNode>(),
                        (list, node) =>
                        {
                            if (node.SymbolName.Equals(symbolName))
                                list.Concat(node);

                            return node
                                .AllChildNodes()
                                .SelectMany(n => n.FindAllNodes(symbolName))
                                .Map(list.Concat);
                        }),

                _ => throw new ArgumentException($"Invalid node type: {source?.GetType()}")
            };
        }

        /// <summary>
        /// Returns the first inner node, or null if empty.
        /// </summary>
        public static ICSTNode FirstNode(this ICSTNode source)
        {
            return source switch
            {
                ICSTNode.LeafNode => null,
                ICSTNode.BranchNode branch => branch.Nodes.IsEmpty() ? null : branch.Nodes[0],
                _ => throw new ArgumentException($"Invalid node type: {source?.GetType()}")
            };
        }

        /// <summary>
        /// Returns the last inner node, or null if empty.
        /// </summary>
        /// <returns></returns>
        public static ICSTNode LastNode(this ICSTNode source)
        {
            return source switch
            {
                ICSTNode.LeafNode => null,
                ICSTNode.BranchNode branch => branch.Nodes.IsEmpty() ? null : branch.Nodes[^1],
                _ => throw new ArgumentException($"Invalid node type: {source?.GetType()}")
            };
        }

        /// <summary>
        /// Get the node at the given index.
        /// </summary>
        /// <param name="index">zero-based index</param>
        public static ICSTNode NodeAt(this ICSTNode source, int index)
        {
            return source switch
            {
                ICSTNode.LeafNode => null,
                ICSTNode.BranchNode branch => branch.Nodes.IsEmpty() ? null : branch.Nodes[index],
                _ => throw new ArgumentException($"Invalid node type: {source?.GetType()}")
            };
        }

        /// <summary>
        /// Gets all the immediate child nodes of the current node
        /// </summary>
        public static IEnumerable<ICSTNode> AllChildNodes(this ICSTNode source)
        {
            return source switch
            {
                ICSTNode.LeafNode => Enumerable.Empty<ICSTNode>(),
                ICSTNode.BranchNode branch => branch.Nodes,
                _ => throw new ArgumentException($"Invalid node type: {source?.GetType()}")
            };
        }

        /// <summary>
        /// Gets the token value of the node
        /// </summary>
        /// <param name="node">The node</param>
        /// <returns>The token value</returns>
        /// <exception cref="ArgumentException">if the node is neither a <see cref="ICSTNode.LeafNode"/> or a <see cref="ICSTNode.BranchNode"/>.</exception>
        public static string TokenValue(this ICSTNode node) => node switch
        {
            ICSTNode.LeafNode leaf => leaf.Tokens,

            ICSTNode.BranchNode branch => branch.AggregateTokens,

            _ => throw new ArgumentException($"Invalid node type: {node?.GetType()}")
        };


        private static IEnumerable<ICSTNode> GetChildren(
            IEnumerable<ICSTNode> nodes,
            string name)
            => nodes.SelectMany(node => node switch
            {
                ICSTNode.LeafNode => new ICSTNode[0],
                ICSTNode.BranchNode branch => branch.GetChildren(name),
                _ => throw new ArgumentException($"Invalid {nameof(ICSTNode)} union-type: {(node.GetType())}")
            });

        private static IEnumerable<ICSTNode> GetChildren(this ICSTNode.BranchNode node, string name)
        {
            var nameSet = name
                .Split('|')
                .Map(names => new HashSet<string>(names));
            return node.Nodes.Where(node => nameSet.Contains(node.SymbolName));
        }

    }
}
