using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.CST;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Validation;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.XBNF.Parsers;
using System.Collections.Immutable;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF.Lang
{
    public class XBNFLanguageContext : ILanguageContext
    {
        public IGrammar Grammar { get; }

        public LanguageMetadata Metadata { get; }

        public ImmutableDictionary<string, Parameter[]> AtomicRuleArguments { get; }

        public ImmutableDictionary<string, IProductionValidator> ProductionValidators { get; }

        internal XBNFLanguageContext(
            IGrammar grammar,
            ParserContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            Grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            Metadata = context.Metadata;

            AtomicRuleArguments = context.AtomicRuleArguments
                .ThrowIfAny(
                    kvp => string.IsNullOrEmpty(kvp.Key),
                    _ => new ArgumentException("Invalid atomicRuleArgument key: null/empty"))
                .ToImmutableDictionary();

            ProductionValidators = context.Metadata.ProductionValidatorDefinitionMap
                .ThrowIfAny(
                    kvp => string.IsNullOrEmpty(kvp.Key),
                    _ => new ArgumentException("Invalid production symbol: null/empty"))
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
