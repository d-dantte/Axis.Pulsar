using Axis.Pulsar.Grammar.Language;

namespace Axis.Pulsar.Grammar.Recognizers
{
    public interface IRecognizer
    {
        /// <summary>
        /// The grammar to which this recognizer belongs
        /// </summary>
        Language.Grammar Grammar { get; }

        /// <summary>
        /// The unerlying rule for this recognizer
        /// </summary>
        IRule Rule { get; }

        /// <summary>
        /// Attempts a recognition operation on the <paramref name="tokenReader"/>
        /// </summary>
        /// <param name="tokenReader">The token reader</param>
        /// <param name="result">The result</param>
        /// <returns><c>true</c> if recognition was successful, <c>false</c> otherwise.</returns>
        bool TryRecognize(BufferedTokenReader tokenReader, out IRecognitionResult result);

        /// <summary>
        /// Returns the result of performing a recognition operation on the <paramref name="tokenReader"/>.
        /// </summary>
        /// <param name="tokenReader">The token reader</param>
        /// <returns>The result of the operation</returns>
        IRecognitionResult Recognize(BufferedTokenReader tokenReader);
    }
}
