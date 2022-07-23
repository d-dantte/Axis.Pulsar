using Axis.Pulsar.Parser.Input;

namespace Axis.Pulsar.Parser.Parsers
{
    /// <summary>
    /// An entity that, given a token reader, reads a finite amount of tokens that satisfy a given condition.
    /// </summary>
    public interface IParser
    {
        /// <summary>
        /// The name of the symbol to which this parse is bound
        /// </summary>
        string SymbolName { get; }

        /// <summary>
        /// Try to read the least amount of tokens that satisfy the encapsulated condition. Returns false for
        /// every situation where the passing-condition is not met.
        /// </summary>
        /// <param name="tokenReader">the token reader</param>
        /// <param name="result">the result of the parse operation. Null if the parse fails</param>
        /// <returns>true if parse succeeds, false otherwise</returns>
        bool TryParse(BufferedTokenReader tokenReader, out ParseResult result);

        /// <summary>
        /// Attempts to parse tokens from the reader based on the underlying conditions.
        /// </summary>
        /// <param name="tokenReader">the token reader</param>
        /// <returns>the parse result</returns>
        IResult Parse(BufferedTokenReader tokenReader);
    }
}
