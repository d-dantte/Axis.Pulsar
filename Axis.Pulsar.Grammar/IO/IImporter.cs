using System.IO;
using System.Threading.Tasks;

namespace Axis.Pulsar.Grammar.IO
{
    /// <summary>
    /// 
    /// </summary>
    public interface IImporter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        Language.Grammar ImportGrammar(Stream inputStream);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        Task<Language.Grammar> ImportGrammarAsync(Stream inputStream);
    }
}
