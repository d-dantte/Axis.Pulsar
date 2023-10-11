using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar.Groups
{
    public interface IGroupElement
    {
        /// <summary>
        /// The cardinality of the element
        /// </summary>
        Cardinality Cardinality { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="parentPath"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool TryRecognize(
            TokenReader reader,
            ProductionPath parentPath,
            out IResult<NodeSequence> result);
    }
}
