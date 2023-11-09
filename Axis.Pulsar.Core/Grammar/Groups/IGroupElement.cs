using Axis.Pulsar.Core.CST;

namespace Axis.Pulsar.Core.Grammar.Groups
{
    public interface IGroupElement: IRecognizer<NodeSequence>
    {
        /// <summary>
        /// The cardinality of the element
        /// </summary>
        Cardinality Cardinality { get; }
    }
}
