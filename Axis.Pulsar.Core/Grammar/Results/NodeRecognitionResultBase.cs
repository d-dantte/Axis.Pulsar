using Axis.Luna.Common.Unions;

namespace Axis.Pulsar.Core.Grammar.Results
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <typeparam name="TSelf"></typeparam>
    public abstract class NodeRecognitionResultBase<TResult, TSelf> :
        RefUnion<TResult, FailedRecognitionError, PartialRecognitionError, TSelf>
        where TSelf : NodeRecognitionResultBase<TResult, TSelf>
    {
        protected NodeRecognitionResultBase(object value) : base(value)
        { }
    }
}
