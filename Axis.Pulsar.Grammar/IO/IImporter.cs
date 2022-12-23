using System.IO;
using System.Threading.Tasks;

namespace Axis.Pulsar.Grammar.IO
{
    /// <summary>
    /// An importer is a type that receives a stream of tokens, formatted in a specific language
    /// (e.g Antlr, bnf), and outputs the (new) grammar represented by the tokens.
    /// </summary>
    public interface IImporter
    {
        /// <summary>
        /// Imports a grammar synchroniously
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        Language.Grammar ImportGrammar(Stream inputStream);

        /// <summary>
        /// Imports a grammar asynchroniously
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        Task<Language.Grammar> ImportGrammarAsync(Stream inputStream);
    }
}
