using Axis.Pulsar.Core.CST;

namespace Axis.Pulsar.Core.Grammar.Errors
{
    internal class GroupError : Exception
    {
        /// <summary>
        /// Source Recognition error
        /// </summary>
        internal INodeError NodeError => (INodeError)InnerException!;

        /// <summary>
        /// Recognized nodes
        /// </summary>
        internal NodeSequence Nodes { get; }


        internal GroupError(INodeError error, NodeSequence nodes)
        : base("", error as Exception)
        {
            ArgumentNullException.ThrowIfNull(error);
            ArgumentNullException.ThrowIfNull(nodes);

            if (error is not Exception)
                throw new ArgumentException("Invalid error: not an exception");

            Nodes = nodes;
        }

        internal static GroupError Of(
            INodeError error,
            NodeSequence nodes)
            => new(error, nodes);

        internal static GroupError Of(
            INodeError error)
            => new(error, NodeSequence.Empty);

        public GroupError Prepend(NodeSequence nodes)
        {
            ArgumentNullException.ThrowIfNull(nodes);

            return new GroupError(NodeError, Nodes.Prepend(nodes));
        }

        public GroupError Append(NodeSequence nodes)
        {
            ArgumentNullException.ThrowIfNull(nodes);

            return new GroupError(NodeError, Nodes.Append(nodes));
        }
    }
}
