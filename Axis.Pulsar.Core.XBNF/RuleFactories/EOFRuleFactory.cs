using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Rules;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.XBNF.RuleFactories
{
    public class EOFRuleFactory : IAtomicRuleFactory
    {
        public IAtomicRule NewRule(
            MetaContext context,
            ImmutableDictionary<IAtomicRuleFactory.Argument, string> arguments)
        {
            return EOF.Instance;
        }
    }
}
