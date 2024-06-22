using Axis.Luna.Unions;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Grammar.Results
{
    public interface INodeRecognitionResult<TResult, TSelf> :
        IUnion<TResult, FailedRecognitionError, PartialRecognitionError, TSelf>
        where TSelf : INodeRecognitionResult<TResult, TSelf>
    {
    }
}
