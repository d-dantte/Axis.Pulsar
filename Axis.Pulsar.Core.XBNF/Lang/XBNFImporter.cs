using Axis.Luna.Common.Results;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.IO;
using Axis.Pulsar.Core.XBNF.Definitions;
using Axis.Pulsar.Core.XBNF.Parsers;

namespace Axis.Pulsar.Core.XBNF.Lang
{
    public class XBNFImporter : ILanguageImporter
    {
        private readonly LanguageMetadata _metadata;

        private XBNFImporter(LanguageMetadata metaContext)
        {
            _metadata = metaContext ?? throw new ArgumentNullException(nameof(metaContext));
        }

        public ILanguageContext ImportLanguage(string inputTokens)
        {
            var context = new ParserContext(_metadata);
            _ = GrammarParser.TryParseGrammar(inputTokens, context, out var grammarResult);

            return grammarResult

                // validate the grammar
                .WithData(grammar => GrammarValidator
                    .Validate(grammar)
                    .ThrowIf(
                        r => !r.IsValidGrammar,
                        r => new GrammarValidationException(r)))

                // create the language context from the grammar
                .Map(grammar => new XBNFLanguageContext(grammar, context))

                // convert IRecognitionErrors to FormatException
                .TransformError(err => err switch { 
                    IRecognitionError rerror => RecognitionFormatException.Of(rerror, inputTokens),
                    _ => err
                })

                // get the result
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

                _atomicFactoryMap[ruleDefinition.Id] = ruleDefinition;
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
                    .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.CharacterRanges)
                    .WithAtomicRuleDefinition(DefaultAtomicRuleDefinitions.DelimitedString);
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
