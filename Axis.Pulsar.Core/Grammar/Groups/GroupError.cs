using Axis.Pulsar.Core.CST;
using static Axis.Pulsar.Core.Exceptions.Errors;

namespace Axis.Pulsar.Core.Grammar.Groups
{
    internal class GroupError: Exception
    {
        /// <summary>
        /// Source Recognition error
        /// </summary>
        internal IRecognitionError RecognitionError => (IRecognitionError) InnerException!; 

        /// <summary>
        /// Recognized nodes
        /// </summary>
        internal NodeSequence Nodes { get; }


        internal GroupError(IRecognitionError error, NodeSequence nodes)
        : base("", error as Exception)
        {
            ArgumentNullException.ThrowIfNull(error);
            ArgumentNullException.ThrowIfNull(nodes);

            Nodes = nodes;
        }

        internal static GroupError Of(
            IRecognitionError error,
            NodeSequence nodes) => new(error, nodes);

        public GroupError Prepend(NodeSequence nodes)
        {
            ArgumentNullException.ThrowIfNull(nodes);

            return new GroupError(RecognitionError, Nodes.Prepend(nodes));
        }

        public GroupError Append(NodeSequence nodes)
        {
            ArgumentNullException.ThrowIfNull(nodes);

            return new GroupError(RecognitionError, Nodes.Append(nodes));
        }
    }
}
