using Axis.Pulsar.Parser.Grammar;
using System.IO;
using System.Threading.Tasks;

namespace Axis.Pulsar.Parser.Input
{
    public interface IGrammarImporter
    {
        Grammar.Grammar ImportGrammar(Stream inputStream);

        Task<Grammar.Grammar> ImportGrammarAsync(Stream inputStream);
    }
}
