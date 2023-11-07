namespace Axis.Pulsar.Core.Utils
{
    public interface IEscapeTransformer
    {
        /// <summary>
        /// Given a string of raw characters, encode every occurence of escapable characters
        /// </summary>
        /// <param name="rawString">The raw string to encode</param>
        /// <returns>The encoded string of characters</returns>
        string Encode(string rawString);

        /// <summary>
        /// Given a string containing escaped characters, decode every occurence of the escaped characters
        /// </summary>
        /// <param name="escapedString">The encoded string of characters</param>
        /// <returns>The raw string of characters</returns>
        string Decode(string escapedString);
    }
}
