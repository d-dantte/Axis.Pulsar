using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Grammar.Results
{
    internal readonly struct SuccessResult<TValue>: IRecognitionResult<TValue>
    {
        internal TValue Value { get; }

        public SuccessResult(TValue value)
        {
            Value = value;
        }

        public IRecognitionResult<TOut> MapAs<TOut>()
        {
            return new SuccessResult<TOut>(Value.As<TOut>());
        }
    }
}
