using Axis.Pulsar.Parser.Grammar;
using System.IO;
using System.Threading.Tasks;

namespace Axis.Pulsar.Parser.Input
{
    /// <summary>
    /// Represents an entity that extracts a grammar from a stream.
    /// </summary>
    public interface IGrammarImporter
    {
        /// <summary>
        /// Import a grammar instance from the given stream
        /// </summary>
        /// <param name="inputStream">The input stream</param>
        IGrammar ImportGrammar(Stream inputStream);

        /// <summary>
        /// Import a grammar instance from the given stream, asynchroniously
        /// </summary>
        /// <param name="inputStream">The input stream</param>
        Task<IGrammar> ImportGrammarAsync(Stream inputStream);
    }
}
