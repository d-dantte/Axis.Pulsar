using Axis.Pulsar.Core.Grammar.Results;

namespace Axis.Pulsar.Core.Grammar.Composite.Group
{
    public interface IGroupRule : IRecognizer<GroupRecognitionResult>
    {
        /// <summary>
        /// The cardinality of the element
        /// </summary>
        Cardinality Cardinality { get; }
    }
}
