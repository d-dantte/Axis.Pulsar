using Axis.Pulsar.Parser.Grammar;
using System.Collections.Generic;
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
        /// <param name="validators">The rule-validator map</param>
        IGrammar ImportGrammar(Stream inputStream, Dictionary<string, IRuleValidator<IRule>> validators = null);

        /// <summary>
        /// Import a grammar instance from the given stream, asynchroniously
        /// </summary>
        /// <param name="inputStream">The input stream</param>
        /// <param name="validators">The rule-validator map</param>
        Task<IGrammar> ImportGrammarAsync(Stream inputStream, Dictionary<string, IRuleValidator<IRule>> validators = null);
    }
}
