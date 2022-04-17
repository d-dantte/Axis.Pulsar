using Axis.Pulsar.Parser.Input;

namespace Axis.Pulsar.Parser.Parsers
{
    /// <summary>
    /// 
    /// </summary>
    public interface IParser
    {
        /// <summary>
        /// 
        /// </summary>
        string SymbolName { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenReader"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool TryParse(BufferedTokenReader tokenReader, out ParseResult result);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenReader"></param>
        /// <returns></returns>
        Result Parse(BufferedTokenReader tokenReader);
    }
}
