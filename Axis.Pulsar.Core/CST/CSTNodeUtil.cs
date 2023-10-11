using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.CST
{
    public static class CSTNodeUtil
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchPath"></param>
        /// <returns></returns>
        public static IEnumerable<ICSTNode> FindNodes(this ICSTNode root, Path searchPath)
        {
            return searchPath.Segments
                .Aggregate(NodeSequence.Of(root), (seq, segment) =>
                {
                    return seq
                        .SelectMany(node => node switch
                        {
                            ICSTNode.NonTerminal ntn => ntn.Nodes,
                            _ => NodeSequence.Empty
                        })
                        .Where(segment.Matches)
                        .ApplyTo(NodeSequence.Of);
                });
        }


        /// <summary>
        /// Finds all Nodes having the given name. Note that not all nodes have a "name". 
        /// These are excluded from the search
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        public static IEnumerable<ICSTNode> FindAllNodes(this ICSTNode root, string symbolName)
        {
            var nodes = new List<ICSTNode>();
            FindAllChildNodes(root, symbolName, nodes);
            return nodes;
        }

        private static void FindAllChildNodes(ICSTNode node, string name, List<ICSTNode> nodes)
        {
            if (node is ICSTNode.NonTerminal nt)
            {
                foreach (var child in nt.Nodes)
                {
                    if (child is ICSTNode.NonTerminal ntchild && ntchild.Name.Equals(name))
                        nodes.Add(child);

                    if (child is ICSTNode.CustomTerminal ctchild && ctchild.Name.Equals(name))
                        nodes.Add(child);

                    FindAllChildNodes(child, name, nodes);
                }
            }
        }
    }
}
