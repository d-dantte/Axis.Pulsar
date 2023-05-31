using Axis.Luna.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Grammar.CST
{
    public static class CSTExtensions
    {
        public static int NodeCount(this CSTNode node)
        {
            return node switch
            {
                CSTNode.LeafNode => -1,
                CSTNode.BranchNode branch => branch.NodeCount,
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
        public static bool TryFindNode(this CSTNode source, string path, out CSTNode node)
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
        /// Gets all symbols found by searching corresponding child-symbols that match the path given, starting from the source node
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
        public static bool TryFindNodes(this CSTNode source, string path, out CSTNode[] nodes)
        {
            nodes = Array.Empty<CSTNode>();
            return source switch
            {
                CSTNode.LeafNode leaf => false,

                CSTNode.BranchNode branch => (nodes = path
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
        public static bool TryFindAllNodes(this CSTNode source, string symbolName, out CSTNode[] nodes)
        {
            nodes = Array.Empty<CSTNode>();
            return source switch
            {
                CSTNode.LeafNode => false,

                CSTNode.BranchNode => (nodes = source
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
        public static IEnumerable<CSTNode> FindNodes(this CSTNode source, string path)
        {
            return source switch
            {
                CSTNode.LeafNode => Array.Empty<CSTNode>(),

                CSTNode.BranchNode => source.TryFindNodes(path, out var result) ? result : result,

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
        public static CSTNode FindNode(this CSTNode source, string path)
        {
            return source switch
            {
                CSTNode.LeafNode => null,

                CSTNode.BranchNode => source.TryFindNode(path, out var result) ? result : result,

                _ => throw new ArgumentException($"Invalid node type: {source?.GetType()}")
            };
        }

        /// <summary>
        /// Recursively search and the entire tree and return all nodes whose name matches the given symbol name.
        /// </summary>
        /// <param name="symbolName">The symbol name to match</param>
        /// <returns>The matching nodes</returns>
        public static IEnumerable<CSTNode> FindAllNodes(this CSTNode source, string symbolName)
        {
            return source switch
            {
                CSTNode.LeafNode => Array.Empty<CSTNode>(),

                CSTNode.BranchNode branch => branch.Nodes
                    .Aggregate(
                        new List<CSTNode>(),
                        (list, node) =>
                        {
                            if (node.SymbolName.Equals(symbolName))
                                list.Add(node);

                            list.AddRange(node
                                .AllChildNodes()
                                .SelectMany(n => n.FindAllNodes(symbolName)));

                            return list;
                        }),

                _ => throw new ArgumentException($"Invalid node type: {source?.GetType()}")
            };
        }

        /// <summary>
        /// Returns the first inner node, or null if empty.
        /// </summary>
        public static CSTNode FirstNode(this CSTNode source)
        {
            return source switch
            {
                CSTNode.LeafNode => null,
                CSTNode.BranchNode branch => branch.Nodes.IsEmpty() ? null : branch.Nodes[0],
                _ => throw new ArgumentException($"Invalid node type: {source?.GetType()}")
            };
        }

        /// <summary>
        /// Returns the last inner node, or null if empty.
        /// </summary>
        /// <returns></returns>
        public static CSTNode LastNode(this CSTNode source)
        {
            return source switch
            {
                CSTNode.LeafNode => null,
                CSTNode.BranchNode branch => branch.Nodes.IsEmpty() ? null : branch.Nodes[^1],
                _ => throw new ArgumentException($"Invalid node type: {source?.GetType()}")
            };
        }

        /// <summary>
        /// Get the node at the given index.
        /// </summary>
        /// <param name="index">zero-based index</param>
        public static CSTNode NodeAt(this CSTNode source, int index)
        {
            return source switch
            {
                CSTNode.LeafNode => null,
                CSTNode.BranchNode branch => branch.Nodes.IsEmpty() ? null : branch.Nodes[index],
                _ => throw new ArgumentException($"Invalid node type: {source?.GetType()}")
            };
        }

        /// <summary>
        /// Gets all the immediate child nodes of the current node
        /// </summary>
        public static IEnumerable<CSTNode> AllChildNodes(this CSTNode source)
        {
            return source switch
            {
                CSTNode.LeafNode => Array.Empty<CSTNode>(),
                CSTNode.BranchNode branch => branch.Nodes,
                _ => throw new ArgumentException($"Invalid node type: {source?.GetType()}")
            };
        }

        /// <summary>
        /// Gets the token value of the node
        /// </summary>
        /// <param name="node">The node</param>
        /// <returns>The token value</returns>
        /// <exception cref="ArgumentException">if the node is neither a <see cref="CSTNode.LeafNode"/> or a <see cref="CSTNode.BranchNode"/>.</exception>
        public static string TokenValue(this CSTNode node) => node switch
        {
            CSTNode.LeafNode leaf => leaf.Tokens,

            CSTNode.BranchNode branch => branch.AggregateTokens,

            _ => throw new ArgumentException($"Invalid node type: {node?.GetType()}")
        };


        private static IEnumerable<CSTNode> GetChildren(
            IEnumerable<CSTNode> nodes,
            string name)
            => nodes.SelectMany(node => node switch
            {
                CSTNode.LeafNode => Array.Empty<CSTNode>(),
                CSTNode.BranchNode branch => branch.GetChildren(name),
                _ => throw new ArgumentException($"Invalid {nameof(CSTNode)} union-type: {(node.GetType())}")
            });

        private static IEnumerable<CSTNode> GetChildren(this CSTNode.BranchNode node, string name)
        {
            var nameSet = name
                .Split('|')
                .Map(names => new HashSet<string>(names));
            return node.Nodes.Where(node => nameSet.Contains(node.SymbolName));
        }

    }
}
