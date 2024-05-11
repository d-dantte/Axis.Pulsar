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
        IEnumerable<AtomicRuleDefinition> atomicRuleDefinitions,
        IEnumerable<ProductionValidatorDefinition> validators)
    {
        AtomicRuleDefinitionMap = atomicRuleDefinitions
            .ThrowIfNull(() => new ArgumentNullException(nameof(atomicRuleDefinitions)))
            .ThrowIfAny(
                item => item is null,
                _ => new ArgumentException($"Invalid {nameof(atomicRuleDefinitions)}: null"))
            .SelectMany(item => item.Symbols.Select(symbol => (Symbol: symbol, Def: item)))
            .Aggregate(ImmutableDictionary.CreateBuilder<string, AtomicRuleDefinition>(), (builder, item) =>
            {
                if (builder.TryAdd(item.Symbol, item.Def))
                    return builder;

                throw new InvalidOperationException(
                    $"Invalid symbol: duplicate value '{item.Symbol}'");
            })
            .ToImmutable();

        AtomicContentTypeMap = atomicRuleDefinitions
            .Where(def => def.ContentDelimiterType != ContentArgumentDelimiter.None)
            .ToImmutableDictionary(
                item => item.ContentDelimiterType,
                item => item.Symbols.First());

        ProductionValidatorDefinitionMap = validators
            .ThrowIfNull(() => new ArgumentNullException(nameof(validators)))
            .ThrowIfAny(
                item => item is null,
                _ => new ArgumentException($"Invalid validator definition: null"))
            .ToImmutableDictionary(
                item => item.Symbol,
                item => item);
    }
}
