using Axis.Luna.Unions;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Grammar.Results
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <typeparam name="TSelf"></typeparam>
    public interface INodeRecognitionResultBase<TResult, TSelf> :
        IUnion<TResult, FailedRecognitionError, PartialRecognitionError, TSelf>
        where TSelf : INodeRecognitionResultBase<TResult, TSelf>
    {
    }
}
