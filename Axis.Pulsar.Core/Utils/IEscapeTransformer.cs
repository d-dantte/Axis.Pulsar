namespace Axis.Pulsar.Core.Utils
{
    public interface IEscapeTransformer
    {
        /// <summary>
        /// Given a raw sequence of characters, convert it into the escaped format
        /// </summary>
        /// <param name="rawSequence">The raw sequence of characters to encode</param>
        /// <returns>The encoded sequence of characters</returns>
        Tokens Encode(Tokens rawSequence);

        /// <summary>
        /// Given a sequence of encoded characters, converts it into the raw format
        /// </summary>
        /// <param name="escapeSequence">The encoded sequence of characters to decode</param>
        /// <returns>The raw sequence of characters</returns>
        Tokens Decode(Tokens escapeSequence);
    }
}
