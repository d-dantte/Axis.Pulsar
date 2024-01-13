using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using System;

namespace Axis.Pulsar.Grammar.Builders
{
    /// <summary>
    /// Production builder instance
    /// </summary>
    public class ProductionBuilder: AbstractBuiler<Production>
    {
        private IRule _rule;
        private string _symbol;
        private int? _recognitionThreshold;
        private IProductionValidator _validator;

        public static ProductionBuilder NewBuilder() => new();

        /// <summary>
        /// Updates the symbol name.
        /// </summary>
        /// <param name="symbol">The symbol</param>
        public ProductionBuilder WithSymbol(string symbol)
        {
            AssertNotBuilt();

            _symbol = symbol.ThrowIf(
                string.IsNullOrWhiteSpace,
                _ => new ArgumentException($"Invalid {nameof(symbol)}: {symbol}"));

            return this;
        }

        /// <summary>
        /// Updates the validator
        /// </summary>
        /// <param name="validator">The validator</param>
        public ProductionBuilder WithValidator(IProductionValidator validator)
        {
            _validator = validator;
            return this;
        }

        /// <summary>
        /// Updates the encapsulated rule.
        /// </summary>
        /// <param name="ruleBuilderAction">An action that customizes the given <see cref="RuleListBuilder"/>instance</param>
        public ProductionBuilder WithRule(Action<RuleBuilder> ruleBuilderAction)
        {
            AssertNotBuilt();

            _rule = RuleBuilder
                .NewBuilder()
                .With(ruleBuilderAction.Invoke)
                .Build();

            return this;
        }

        /// <summary>
        /// Updates the encapsulated rule
        /// </summary>
        /// <param name="rule">The rule</param>
        public ProductionBuilder WithRule(IRule rule)
        {
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));

            return this;
        }

        /// <summary>
        /// Updates the recognition threshold
        /// </summary>
        /// <param name="recognitionThreshold">the threshold value</param>
        public ProductionBuilder WithRecognitionThreshold(int? recognitionThreshold)
        {
            AssertNotBuilt();

            _recognitionThreshold = recognitionThreshold.ThrowIf(
                v => v < 1,
                _ => new ArgumentException($"Invalid {nameof(recognitionThreshold)}: {recognitionThreshold}"));

            return this;
        }

        protected override void ValidateTarget()
        {
            // relies on the production struct to validate the parameters
            _ = new Production(new ProductionRule(_symbol, _recognitionThreshold, _rule, _validator));
        }

        protected override Production BuildTarget() => new(new ProductionRule(_symbol, _recognitionThreshold, _rule, _validator));
    }
}
