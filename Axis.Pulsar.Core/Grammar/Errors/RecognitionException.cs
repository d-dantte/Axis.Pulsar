namespace Axis.Pulsar.Core.Grammar.Errors
{
    public class RecognitionException: Exception
    {
        public IRecognitionError Error { get; }

        public RecognitionException(IRecognitionError error)
        : base("An unhandled recognition error occured")
        {
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }
    }
}
