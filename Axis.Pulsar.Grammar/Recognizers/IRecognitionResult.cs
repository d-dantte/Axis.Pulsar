namespace Axis.Pulsar.Grammar.Recognizers
{
    /// <summary>
    /// Represents the result of a recognition attempt
    /// </summary>
    public interface IRecognitionResult
    {
        /// <summary>
        /// The position in the <see cref="BufferedTokenReader"/> where the recognition attempt began
        /// </summary>
        int Position { get; }
    }
}
