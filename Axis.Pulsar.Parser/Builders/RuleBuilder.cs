using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Parser.Builders
{
    /// <summary>
    /// Rule builder instance
    /// </summary>
    public class RuleBuilder : IBuilder<IRule>
    {
        private IRule _rule;

        /// <inheritdoc/>
        public IRule Build() => _rule;

        /// <summary>
        /// Replaces the underlying rule with a literal rule
        /// </summary>
        public RuleBuilder HavingLiteralRule(
            string stringLiteral,
            bool isCaseSensitive,
            IRuleValidator<LiteralRule> validator = null)
        {
            _rule = new LiteralRule(stringLiteral, isCaseSensitive, validator);
            return this;
        }

        /// <summary>
        /// Replaces the underlying rule with a pattern rule
        /// </summary>
        public RuleBuilder HavingPatternRule(
            string regexPattern,
            IPatternMatchType matchType = null,
            IRuleValidator<PatternRule> validator = null)
        {
            _rule = new PatternRule(
                new Regex(regexPattern),
                matchType ?? new IPatternMatchType.Open(1),
                validator);
            return this;
        }

        /// <summary>
        /// Replaces the underlying rule with a symbol expression rule encapsulating a sequence
        /// </summary>
        public RuleBuilder HavingSequence(
            Action<ExpressionListBuilder> builder,
            Cardinality? cardinality = null,
            IRuleValidator<SymbolExpressionRule> validator = null)
        {
            _rule = new SymbolExpressionRule(
                new SymbolGroup.Sequence(
                    cardinality ?? Cardinality.OccursOnlyOnce(),
                    new ExpressionListBuilder()
                        .Use(builder.Invoke)
                        .Build()),
                null,
                validator);

            return this;
        }

        /// <summary>
        /// Replaces the underlying rule with a symbol expression rule encapsulating a choice
        /// </summary>
        public RuleBuilder HavingChoice(
            Action<ExpressionListBuilder> builder,
            Cardinality? cardinality = null,
            IRuleValidator<SymbolExpressionRule> validator = null)
        {
            _rule = new SymbolExpressionRule(
                new SymbolGroup.Choice(
                    cardinality ?? Cardinality.OccursOnlyOnce(),
                    new ExpressionListBuilder()
                        .Use(builder.Invoke)
                        .Build()),
                null,
                validator);

            return this;
        }

        /// <summary>
        /// Replaces the underlying rule with a symbol expression rule encapsulating a set
        /// </summary>
        public RuleBuilder HavingSet(
            Action<ExpressionListBuilder> builder,
            int? minContentCount,
            Cardinality? cardinality = null,
            IRuleValidator<SymbolExpressionRule> validator = null)
        {
            _rule = new SymbolExpressionRule(
                new SymbolGroup.Set(
                    cardinality ?? Cardinality.OccursOnlyOnce(),
                    minContentCount,
                    new ExpressionListBuilder()
                        .Use(builder.Invoke)
                        .Build()),
                null,
                validator);

            return this;
        }

        /// <summary>
        /// Replaces the underlying rule with a symbol expression rule encapsulating a production ref
        /// </summary>
        public RuleBuilder HavingRef(
            string symbolRef,
            Cardinality? cardinality = null,
            IRuleValidator<SymbolExpressionRule> validator = null)
        {
            _rule = new SymbolExpressionRule(
                new ProductionRef(symbolRef, cardinality ?? Cardinality.OccursOnlyOnce()),
                null,
                validator);

            return this;
        }
    }
}
