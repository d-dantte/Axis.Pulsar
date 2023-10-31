namespace Axis.Pulsar.Core.Exceptions
{
    internal class RecognitionRuntimeError: Exception
    {
        public RecognitionRuntimeError(Exception cause)
        :base("See inner exception", cause)
        {
            ArgumentNullException.ThrowIfNull(cause);
        }

        internal static RecognitionRuntimeError Of(Exception cause) => new(cause);
    }
}
