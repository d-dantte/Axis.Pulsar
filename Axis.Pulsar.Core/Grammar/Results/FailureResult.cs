namespace Axis.Pulsar.Core.Grammar.Results
{
    internal readonly struct FailureResult<TValue>: IRecognitionResult<TValue>
    {
        internal IRecognitionError Error { get; }

        internal FailureResult(IRecognitionError error)
        {
            ArgumentNullException.ThrowIfNull(error);
            Error = error;
        }

        public IRecognitionResult<TOut> MapAs<TOut>()
        {
            return new FailureResult<TOut>(Error);
        }
    }
}
