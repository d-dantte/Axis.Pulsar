using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
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

        public IResult<ICSTNode> Recognize(string inputTokens)
        {
            _ = Grammar[Grammar.Root].TryProcessRule(inputTokens, null, this, out var result);

            return result;
        }
    }
}
