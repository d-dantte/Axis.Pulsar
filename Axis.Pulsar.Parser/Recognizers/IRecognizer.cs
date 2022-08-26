using Axis.Pulsar.Parser.Input;
using Axis.Pulsar.Parser.Utils;

namespace Axis.Pulsar.Parser.Recognizers
{
    /// <summary>
    /// Recognizes tokens fitting the <see cref="Grammar.ISymbolExpression"/> configuration.
    /// </summary>
    public interface IRecognizer
    {
        /// <summary>
        /// Cardinality of this recognizer
        /// </summary>
        Cardinality Cardinality { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenReader"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool TryRecognize(BufferedTokenReader tokenReader, out IResult result);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenReader"></param>
        /// <returns></returns>
        IResult Recognize(BufferedTokenReader tokenReader);
    }
}
