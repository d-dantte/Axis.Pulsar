using System.Collections.Immutable;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.XBNF.Definitions;
using Axis.Pulsar.Core.XBNF.RuleFactories;

namespace Axis.Pulsar.Core.XBNF;

public class MetaContext
{
    public ImmutableDictionary<string, AtomicRuleDefinition> AtomicFactoryMap { get; }

    public ImmutableDictionary<string, EscapeMatcherDefinition> EscapeMatcherMap { get; }

    public ImmutableDictionary<string, ProductionValidatorDefinition> ProductionValidatorMap { get; }

    private MetaContext(
        IEnumerable<AtomicRuleDefinition> atomicRules,
        IEnumerable<EscapeMatcherDefinition> matchers,
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

        EscapeMatcherMap = matchers
            .ThrowIfNull(new ArgumentNullException(nameof(matchers)))
            .ThrowIfAny(
                item => item is null,
                new ArgumentException($"Invalid matcher definition: null"))
            .ToImmutableDictionary(
                item => item.Name,
                item => item);

        ProductionValidatorMap = validators
            .ThrowIfNull(new ArgumentNullException(nameof(validators)))
            .ThrowIfAny(
                item => item is null,
                new ArgumentException($"Invalid validator definition: null"))
            .ToImmutableDictionary(
                item => item.Symbol,
                item => item);
    }

    public class Builder
    {
        private readonly Dictionary<string, AtomicRuleDefinition> _atomicFactoryMap = new();
        private readonly Dictionary<string, EscapeMatcherDefinition> _matcherMap = new();
        private readonly Dictionary<string, ProductionValidatorDefinition> _productionValidatorMap = new();

        public Builder()
        {
        }

        public static Builder NewBuilder() => new();

        #region AtomicFactory

        public Builder WithAtomicRuleDefinition(AtomicRuleDefinition ruleDefinition)
        {
            ArgumentNullException.ThrowIfNull(ruleDefinition);

            _atomicFactoryMap[ruleDefinition.Symbol] = ruleDefinition;
            return this;
        }

        public bool ContainsRuleDefinitionFor(string productionSymbol)
        {
            return _atomicFactoryMap.ContainsKey(productionSymbol);
        }
        
        public Builder WithDefaultAtomicRuleDefinitions()
        {
            return this
                .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.Literal)
                .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.Pattern)
                .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.CharacterRanges);
        }
        
        #endregion

        #region EscapeMatcher

        public Builder WithEscapeMatcherDefinition(EscapeMatcherDefinition matcherDefinition)
        {
            ArgumentNullException.ThrowIfNull(matcherDefinition);

            _matcherMap[matcherDefinition.Name] = matcherDefinition;
            return this;
        }

        public bool ContainsEscapeMatcherDefinitionFor(string name)
        {
            return _matcherMap.ContainsKey(name);
        }
        
        public Builder WithDefaultEscapeMatcherDefinitions()
        {
            return this
                .WithEscapeMatcherDefinition(DefaultEscapeMatcherDefinitions.BSolBasic)
                .WithEscapeMatcherDefinition(DefaultEscapeMatcherDefinitions.BSolAscii)
                .WithEscapeMatcherDefinition(DefaultEscapeMatcherDefinitions.BSolUTF);
        }

        #endregion

        #region Production Validator
        public Builder WithProductionValidator(ProductionValidatorDefinition validatorDefinition)
        {
            ArgumentNullException.ThrowIfNull(validatorDefinition);

            _productionValidatorMap[validatorDefinition.Symbol] = validatorDefinition;
            return this;
        }

        public bool ContainsValidatorDefinitionFor(string productionSymbol)
        {
            return _productionValidatorMap.ContainsKey(productionSymbol);
        }
        #endregion

        public MetaContext Build()
        {
            return new MetaContext(
                _atomicFactoryMap.Values,
                _matcherMap.Values,
                _productionValidatorMap.Values);
        }
    }
}
