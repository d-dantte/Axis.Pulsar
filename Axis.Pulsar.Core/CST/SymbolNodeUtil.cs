using Axis.Luna.Common;
using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.CST
{
    public static class SymbolNodeUtil
    {
        /// <summary>
        /// Find all nodes using the search path
        /// </summary>
        /// <param name="searchPath">The search path</param>
        /// <returns>A collection of nodes found at the given path, or an empty enumerable</returns>
        public static IEnumerable<ISymbolNode> FindNodes(this ISymbolNode root, NodePath searchPath)
        {
            var prime = ArrayUtil.Of(root).AsEnumerable();
            return searchPath.Segments.Aggregate(prime, (seq, segment) =>
            {
                return seq
                    .SelectMany(node => node switch
                    {
                        ISymbolNode.Composite ntn => ntn.Nodes,
                        _ => Enumerable.Empty<ISymbolNode>()
                    })
                    .Where(segment.Matches);
            });
        }

        /// <summary>
        /// Finds all Nodes having the given name. Note that not all nodes have a "name". 
        /// These are excluded from the search
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns>A collection of nodes found in the node tree, having the given name, or an empty enumerable</returns>
        public static IEnumerable<ISymbolNode> FindAllNodes(this ISymbolNode root, string symbolName)
        {
            var nodes = new List<ISymbolNode>();
            FindAllChildNodes(root, symbolName, nodes);
            return nodes;
        }

        private static void FindAllChildNodes(ISymbolNode node, string name, List<ISymbolNode> nodes)
        {
            if (node is ISymbolNode.Composite nt)
            {
                foreach (var child in nt.Nodes)
                {
                    if (child is ISymbolNode.Composite cchild && cchild.Symbol.Equals(name))
                        nodes.Add(child);

                    else if (child is ISymbolNode.Atom achild && achild.Symbol.Equals(name))
                        nodes.Add(child);

                    FindAllChildNodes(child, name, nodes);
                }
            }
        }
    }
}
