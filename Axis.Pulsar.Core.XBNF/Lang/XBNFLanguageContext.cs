using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Results;
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
            ArgumentNullException.ThrowIfNull(grammar);

            Grammar = grammar;
            Metadata = context.Metadata;

            AtomicRuleArguments = context.AtomicRuleArguments
                .ThrowIfAny(
                    kvp => string.IsNullOrEmpty(kvp.Key),
                    _ => new InvalidOperationException("Invalid atomicRuleArgument key: null/empty"))
                .ToImmutableDictionary();

            ProductionValidators = context.Metadata.ProductionValidatorDefinitionMap
                .ToImmutableDictionary(
                    def => def.Key,
                    def => def.Value.Validator);
        }

        public NodeRecognitionResult Recognize(string inputTokens)
        {
            _ = Grammar[Grammar.Root].TryRecognize(inputTokens, SymbolPath.Default, this, out var result);

            return result;
        }
    }
}
