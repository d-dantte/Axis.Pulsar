using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Lang;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.XBNF.Lang
{
    public class XBNFLanguageContext : ILanguageContext
    {
        public IGrammar Grammar { get; }

        public ImmutableDictionary<string, IProductionValidator> ProductionValidators { get; }

        public XBNFLanguageContext(
            IGrammar grammar,
            MetaContext metaContext)
        {
            Grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            ProductionValidators = metaContext
                .ThrowIfNull(new ArgumentNullException(nameof(metaContext)))
                .ProductionValidatorMap
                .ToImmutableDictionary(
                    def => def.Key,
                    def => def.Value.Validator);
        }
    }
}
