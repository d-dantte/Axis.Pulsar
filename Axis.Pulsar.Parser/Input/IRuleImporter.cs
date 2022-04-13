using Axis.Pulsar.Parser.Grammar;
using System.IO;
using System.Threading.Tasks;

namespace Axis.Pulsar.Parser.Input
{
    public interface IRuleImporter
    {
        Grammar.Grammar ImportRule(Stream inputStream);

        Task<Grammar.Grammar> ImportRuleAsync(Stream inputStream);
    }
}
