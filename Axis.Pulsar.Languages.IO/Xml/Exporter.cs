using Axis.Pulsar.Grammar.IO;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Axis.Pulsar.Languages.Xml
{
    public class Exporter : IExporter
    {
        /// <inheritdoc/>
        public void ExportGrammar(Grammar.Language.Grammar grammar, Stream outputStream)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ExportGrammarAsync(Grammar.Language.Grammar grammar, Stream outputStream)
        {
            throw new NotImplementedException();
        }
    }
}
