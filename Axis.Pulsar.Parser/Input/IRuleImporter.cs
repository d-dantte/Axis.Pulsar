using Axis.Pulsar.Parser.Grammar;
using System.IO;
using System.Threading.Tasks;

namespace Axis.Pulsar.Parser.Input
{
    public interface IRuleImporter
    {
        RuleMap ImportRule(Stream inputStream);

        Task<RuleMap> ImportRuleAsync(Stream inputStream);
    }
}
