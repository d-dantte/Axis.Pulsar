using Axis.Luna.Common.Unions;
using Axis.Pulsar.Core.CST;

namespace Axis.Pulsar.Core.Grammar.Results
{
    using FailedError = FailedRecognitionError;
    using PartialError = PartialRecognitionError;

    public class NodeRecognitionResult :
        NodeRecognitionResultBase<ICSTNode, NodeRecognitionResult>,
        IUnionOf<ICSTNode, FailedError, PartialError, NodeRecognitionResult>
    {
        private NodeRecognitionResult(object value)
        : base(value)
        {
        }

        /// <summary>
        /// Rejects null nodes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static NodeRecognitionResult Of(ICSTNode value) => value switch
        {
            null => throw new ArgumentNullException(nameof(value)),
            _ => new(value)
        };

        public static NodeRecognitionResult Of(FailedError value) => new(value);

        public static NodeRecognitionResult Of(PartialError value) => new(value);
    }
}
