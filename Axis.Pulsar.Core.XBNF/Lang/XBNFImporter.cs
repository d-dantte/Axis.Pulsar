using Axis.Luna.Common.Results;
using Axis.Pulsar.Core.IO;
using Axis.Pulsar.Core.XBNF.Definitions;
using Axis.Pulsar.Core.XBNF.RuleFactories;

namespace Axis.Pulsar.Core.XBNF.Lang
{
    public class XBNFImporter : ILanguageImporter
    {
        private readonly MetaContext _metaContext;

        private XBNFImporter(MetaContext metaContext)
        {
            _metaContext = metaContext ?? throw new ArgumentNullException(nameof(metaContext));
        }

        public ILanguageContext ImportLanguage(string inputTokens)
        {
            _ = GrammarParser.TryParseGrammar(inputTokens, _metaContext, out var grammarResult);

            return grammarResult
                .Map(grammar => new XBNFLanguageContext(
                    grammar,
                    _metaContext))
                .Resolve();
        }

        #region Nested types


        public class Builder
        {
            private readonly Dictionary<string, AtomicRuleDefinition> _atomicFactoryMap = new();
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
                    .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.EOF)
                    .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.Literal)
                    .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.Pattern)
                    .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.CharacterRanges);
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

            public XBNFImporter Build()
            {
                return new XBNFImporter(new (
                    _atomicFactoryMap.Values,
                    _productionValidatorMap.Values));
            }
        }

        #endregion
    }
}
