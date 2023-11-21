using System.Collections.Immutable;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.XBNF.Definitions;
using Axis.Pulsar.Core.XBNF.RuleFactories;

namespace Axis.Pulsar.Core.XBNF;

internal class MetaContext
{
    public ImmutableDictionary<string, AtomicRuleDefinition> AtomicFactoryMap { get; }

    public ImmutableDictionary<AtomicContentDelimiterType, string> AtomicContentTypeMap { get; }

    public ImmutableDictionary<string, ProductionValidatorDefinition> ProductionValidatorMap { get; }

    internal MetaContext(
        IEnumerable<AtomicRuleDefinition> atomicRules,
        IEnumerable<ProductionValidatorDefinition> validators)
    {
        AtomicFactoryMap = atomicRules
            .ThrowIfNull(new ArgumentNullException(nameof(atomicRules)))
            .ThrowIfAny(
                item => item is null,
                new ArgumentException($"Invalid factory definition: null"))
            .ToImmutableDictionary(
                item => item.Symbol,
                item => item);

        AtomicContentTypeMap = atomicRules
            .Where(def => def.ContentDelimiterType != AtomicContentDelimiterType.None)
            .ToImmutableDictionary(
                item => item.ContentDelimiterType,
                item => item.Symbol);

        ProductionValidatorMap = validators
            .ThrowIfNull(new ArgumentNullException(nameof(validators)))
            .ThrowIfAny(
                item => item is null,
                new ArgumentException($"Invalid validator definition: null"))
            .ToImmutableDictionary(
                item => item.Symbol,
                item => item);
    }
}
