using Axis.Pulsar.Core.XBNF.Definitions;
using Axis.Pulsar.Core.XBNF.Lang;

namespace Axis.Pulsar.Core.XBNF.Tests
{
    public class MetaContextBuilder
    {
        private readonly Dictionary<string, AtomicRuleDefinition> _atomicFactoryMap = new();
        private readonly Dictionary<string, ProductionValidatorDefinition> _productionValidatorMap = new();

        public MetaContextBuilder()
        {
        }

        public static MetaContextBuilder NewBuilder() => new();

        #region AtomicFactory

        public MetaContextBuilder WithAtomicRuleDefinition(AtomicRuleDefinition ruleDefinition)
        {
            ArgumentNullException.ThrowIfNull(ruleDefinition);

            _atomicFactoryMap[ruleDefinition.Id] = ruleDefinition;
            return this;
        }

        public bool ContainsRuleDefinitionFor(string productionSymbol)
        {
            return _atomicFactoryMap.ContainsKey(productionSymbol);
        }

        public MetaContextBuilder WithDefaultAtomicRuleDefinitions()
        {
            return this
                .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.EOF)
                .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.Literal)
                .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.Pattern)
                .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.CharacterRanges);
        }

        #endregion

        #region Production Validator
        public MetaContextBuilder WithProductionValidator(ProductionValidatorDefinition validatorDefinition)
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

        public LanguageMetadata Build()
        {
            return new LanguageMetadata(
                _atomicFactoryMap.Values,
                _productionValidatorMap.Values);
        }
    }
}
