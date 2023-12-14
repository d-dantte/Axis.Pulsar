using Axis.Luna.Common.Unions;
using Axis.Pulsar.Core.CST;

namespace Axis.Pulsar.Core.Grammar.Results
{
    public class GroupRecognitionResult :
        RefUnion<INodeSequence, GroupRecognitionError, GroupRecognitionResult>,
        IUnionOf<INodeSequence, GroupRecognitionError, GroupRecognitionResult>
    {
        public GroupRecognitionResult(object value)
        : base(value)
        {
        }

        /// <summary>
        /// Rejects null sequences
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static GroupRecognitionResult Of(INodeSequence value) => value switch
        {
            null => throw new ArgumentNullException(nameof(value)),
            _ => new(value)
        };

        public static GroupRecognitionResult Of(GroupRecognitionError value) => new(value);
    }
}
