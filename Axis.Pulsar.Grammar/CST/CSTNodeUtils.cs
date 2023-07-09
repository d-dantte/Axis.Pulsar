using System;
using System.Linq;

namespace Axis.Pulsar.Grammar.CST
{
    public static class CSTNodeUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="searchPath"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static CSTNode[] FindNodes(this CSTNode node, Path searchPath)
        {
            return searchPath.Segments.Aggregate(new[] { node }, (nodes, segment) =>
            {
                return nodes
                    .SelectMany(node => node switch
                    {
                        CSTNode.LeafNode => Array.Empty<CSTNode>(),
                        CSTNode.BranchNode branch => branch.Nodes
                            .Where(segment.Matches)
                            .ToArray(),
                        _ => throw new InvalidOperationException($"Invalid node type: '{node?.GetType()}'")
                    })
                    .ToArray();
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="searchPath"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public static bool TryFindNodes(this CSTNode node, Path searchPath, out CSTNode[] nodes)
        {
            nodes = node.FindNodes(searchPath);
            return nodes.Length > 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="symbolName"></param>
        /// <returns></returns>
        public static CSTNode[] FindAllNodes(this CSTNode node, string symbolName)
        {
            var foundNodes = node switch
            {
                CSTNode.LeafNode => Array.Empty<CSTNode>(),
                CSTNode.BranchNode branch => branch.Nodes.Where(n => n.SymbolName == symbolName),
                _ => throw new InvalidOperationException($"Invalid node type: '{node?.GetType()}'")
            };

            return foundNodes
                .Concat(foundNodes.SelectMany(n => FindAllNodes(n, symbolName)))
                .ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="symbolName"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        public static bool TryFindAllNodes(this CSTNode node, string symbolName, out CSTNode[] nodes)
        {
            nodes = CSTNodeUtils.FindAllNodes(node, symbolName);
            return nodes.Length > 0;
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

        /// <summary>
        /// Get the node count
        /// </summary>
        /// <param name="node">The node</param>
        /// <exception cref="ArgumentException"></exception>
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
        public static CSTNode[] AllChildNodes(this CSTNode source)
        {
            return source switch
            {
                CSTNode.LeafNode => Array.Empty<CSTNode>(),
                CSTNode.BranchNode branch => branch.Nodes,
                _ => throw new ArgumentException($"Invalid node type: {source?.GetType()}")
            };
        }
    }
}
