using System.Collections.Immutable;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.XBNF.RuleFactories;

namespace Axis.Pulsar.Core.XBNF;

public class LanguageContext
{
    public ImmutableDictionary<string, AtomicRuleDefinition> AtomicFactoryMap { get; }

    public ImmutableDictionary<string, EscapeMatcherDefinition> EscapeMatcherMap { get; }

    private LanguageContext(
        IEnumerable<AtomicRuleDefinition> factoryMap,
        IEnumerable<EscapeMatcherDefinition> matcherMap)
    {
        AtomicFactoryMap = factoryMap
            .ThrowIfNull(new ArgumentNullException(nameof(factoryMap)))
            .ThrowIfAny(
                item => item is null,
                new ArgumentException($"Invalid factory definition: null"))
            .ToImmutableDictionary(
                item => item.Symbol,
                item => item);

        EscapeMatcherMap = matcherMap
            .ThrowIfNull(new ArgumentNullException(nameof(matcherMap)))
            .ThrowIfAny(
                item => item is null,
                new ArgumentException($"Invalid matcher definition: null"))
            .ToImmutableDictionary(
                item => item.Name,
                item => item);
    }

    public class Builder
    {
        private readonly Dictionary<string, AtomicRuleDefinition> _atomicFactoryMap = new();
        private readonly Dictionary<string, EscapeMatcherDefinition> _matcherMap = new();

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

        public LanguageContext Build()
        {
            return new LanguageContext(
                _atomicFactoryMap.Values,
                _matcherMap.Values);
        }
    }
}
