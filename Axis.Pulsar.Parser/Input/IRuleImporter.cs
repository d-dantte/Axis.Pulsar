using Axis.Pulsar.Parser.Language;
using System.IO;
using System.Threading.Tasks;

namespace Axis.Pulsar.Parser.Input
{
    public interface IRuleImporter
    {
        IRule ImportRule(Stream inputStream);

        Task<IRule> ImportRuleAsync(Stream inputStream);
    }
}
