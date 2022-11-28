using System.IO;
using System.Threading.Tasks;

namespace Axis.Pulsar.Grammar.IO
{
    /// <summary>
    /// 
    /// </summary>
    public interface IExporter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="grammar"></param>
        /// <param name="outputStream"></param>
        void ExportGrammar(Language.Grammar grammar, Stream outputStream);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="grammar"></param>
        /// <param name="outputStream"></param>
        /// <returns></returns>
        Task ExportGrammarAsync(Language.Grammar grammar, Stream outputStream);
    }
}
