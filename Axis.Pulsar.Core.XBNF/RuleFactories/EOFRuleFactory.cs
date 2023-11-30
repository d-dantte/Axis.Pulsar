using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.XBNF.Lang;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.XBNF.RuleFactories
{
    public class EOFRuleFactory : IAtomicRuleFactory
    {
        public IAtomicRule NewRule(
            string ruleId,
            LanguageMetadata context,
            ImmutableDictionary<IAtomicRuleFactory.Argument, string> arguments)
        {
            return new EOF(ruleId);
        }
    }
}
