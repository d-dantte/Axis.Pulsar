using Axis.Pulsar.Core.Grammar.Nodes;
using Axis.Pulsar.Core.XBNF.Lang;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.XBNF.RuleFactories
{
    public class EOFRuleFactory : IAtomicRuleFactory
    {
        public IAtomicRule NewRule(
            string ruleId,
            LanguageMetadata context,
            ImmutableDictionary<IAtomicRuleFactory.IArgument, string> arguments)
        {
            return new EOF(ruleId);
        }
    }
}
