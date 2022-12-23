using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Axis.Pulsar.Grammar.IO
{
    /// <summary>
    /// 
    /// </summary>
    public interface IExporter
    {
        /// <summary>
        /// Writes the grammar to the given output stream, formatted specifically for the language supported by the exporter.
        /// <para>
        /// Note that the <paramref name="outputStream"/> is only flushed, but not closed/disposed.
        /// </para>
        /// </summary>
        /// <param name="grammar">The grammar</param>
        /// <param name="outputStream">The output stream</param>
        void ExportGrammar(Language.Grammar grammar, Stream outputStream);

        /// <summary>
        /// Asynchroniously Writes the grammar to the given output stream, formatted specifically for the language supported by the exporter.
        /// <para>
        /// Note that the <paramref name="outputStream"/> is only flushed, but not closed/disposed.
        /// </para>
        /// </summary>
        /// <param name="grammar">The grammar</param>
        /// <param name="outputStream">The output stream</param>
        /// <param name="token">The cancellation token</param>
        Task ExportGrammarAsync(Language.Grammar grammar, Stream outputStream, CancellationToken? token = null);
    }
}
