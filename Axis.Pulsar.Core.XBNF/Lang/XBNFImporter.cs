using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar;
using Axis.Pulsar.Core.Grammar.Errors;
using Axis.Pulsar.Core.Lang;
using Axis.Pulsar.Core.XBNF.Definitions;
using Axis.Pulsar.Core.XBNF.Parsers;
using System.Collections.Immutable;

namespace Axis.Pulsar.Core.XBNF.Lang
{
    public class XBNFImporter : ILanguageImporter
    {
        private readonly LanguageMetadata _metadata;

        public static Builder NewBuilder() => new();

        private XBNFImporter(LanguageMetadata metaContext)
        {
            _metadata = metaContext ?? throw new ArgumentNullException(nameof(metaContext));
        }

        public ILanguageContext ImportLanguage(string inputTokens)
        {
            var context = new ParserContext(_metadata);
            _ = GrammarParser.TryParseGrammar(inputTokens, context, out var grammarResult);

            if (grammarResult.Is(out IGrammar grammar))
            {
                // validate the grammar
                GrammarValidator
                    .Validate(grammar)
                    .ThrowIf(
                        r => !r.IsValidGrammar,
                        r => new GrammarValidationException(r));

                // create the language context from the grammar
                return new XBNFLanguageContext(grammar, context);
            }
            else if (grammarResult.Is(out FailedRecognitionError fre))
                throw new RecognitionFormatException(
                    fre.TokenSegment.Offset,
                    fre.TokenSegment.Count,
                    inputTokens);

            else if (grammarResult.Is(out PartialRecognitionError pre))
                throw new RecognitionFormatException(
                    pre.TokenSegment.Offset,
                    pre.TokenSegment.Count,
                    inputTokens);

            else throw new InvalidOperationException(
                $"Invalid grammar parser result: {grammarResult}");
        }

        #region Nested types

        public class Builder
        {
            private readonly List<AtomicRuleDefinition> _atomicRuleDefinitions = new();
            private readonly Dictionary<string, ProductionValidatorDefinition> _productionValidatorMap = new();

            public ImmutableArray<AtomicRuleDefinition> AtomicDefinitions => _atomicRuleDefinitions.ToImmutableArray();

            public Builder()
            {
            }

            #region AtomicFactory

            public Builder WithAtomicRuleDefinition(AtomicRuleDefinition ruleDefinition)
            {
                ArgumentNullException.ThrowIfNull(ruleDefinition);

                _atomicRuleDefinitions.Add(ruleDefinition);
                return this;
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
                    _atomicRuleDefinitions,
                    _productionValidatorMap.Values));
            }
        }

        #endregion
    }
}
