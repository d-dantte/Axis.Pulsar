using Axis.Pulsar.Core.XBNF.Definitions;
using Axis.Pulsar.Core.XBNF.Lang;

namespace Axis.Pulsar.Core.XBNF.Tests
{
    public class LanguageMetadataBuilder
    {
        private readonly List<AtomicRuleDefinition> _atomicRuleDefinitions = new();
        private readonly Dictionary<string, ProductionValidatorDefinition> _productionValidatorMap = new();

        public LanguageMetadataBuilder()
        {
        }

        public static LanguageMetadataBuilder NewBuilder() => new();

        #region AtomicFactory

        public LanguageMetadataBuilder WithAtomicRuleDefinition(AtomicRuleDefinition ruleDefinition)
        {
            ArgumentNullException.ThrowIfNull(ruleDefinition);

            _atomicRuleDefinitions.Add(ruleDefinition);
            return this;
        }

        public LanguageMetadataBuilder WithDefaultAtomicRuleDefinitions()
        {
            return this
                .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.EOF)
                .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.Literal)
                .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.Pattern)
                .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.CharacterRanges);
        }

        #endregion

        #region Production Validator
        public LanguageMetadataBuilder WithProductionValidator(ProductionValidatorDefinition validatorDefinition)
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
                _atomicRuleDefinitions,
                _productionValidatorMap.Values);
        }
    }
}
