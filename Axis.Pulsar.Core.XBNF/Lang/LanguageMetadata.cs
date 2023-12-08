using System.Collections.Immutable;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.XBNF.Definitions;
using static Axis.Pulsar.Core.XBNF.IAtomicRuleFactory;

namespace Axis.Pulsar.Core.XBNF.Lang;

public class LanguageMetadata
{

    public ImmutableDictionary<string, AtomicRuleDefinition> AtomicRuleDefinitionMap { get; }

    public ImmutableDictionary<ContentArgumentDelimiter, string> AtomicContentTypeMap { get; }

    public ImmutableDictionary<string, ProductionValidatorDefinition> ProductionValidatorDefinitionMap { get; }


    internal LanguageMetadata(
        IEnumerable<AtomicRuleDefinition> atomicRules,
        IEnumerable<ProductionValidatorDefinition> validators)
    {
        AtomicRuleDefinitionMap = atomicRules
            .ThrowIfNull(new ArgumentNullException(nameof(atomicRules)))
            .ThrowIfAny(
                item => item is null,
                new ArgumentException($"Invalid factory definition: null"))
            .ToImmutableDictionary(
                item => item.Id,
                item => item);

        AtomicContentTypeMap = atomicRules
            .Where(def => def.ContentDelimiterType != ContentArgumentDelimiter.None)
            .ToImmutableDictionary(
                item => item.ContentDelimiterType,
                item => item.Id);

        ProductionValidatorDefinitionMap = validators
            .ThrowIfNull(new ArgumentNullException(nameof(validators)))
            .ThrowIfAny(
                item => item is null,
                new ArgumentException($"Invalid validator definition: null"))
            .ToImmutableDictionary(
                item => item.Symbol,
                item => item);
    }
}
