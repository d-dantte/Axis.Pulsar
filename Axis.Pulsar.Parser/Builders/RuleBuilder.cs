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
        public RuleBuilder HavingLiteralRule(string stringLiteral, bool isCaseSensitive)
        {
            _rule = new LiteralRule(stringLiteral, isCaseSensitive);
            return this;
        }

        /// <summary>
        /// Replaces the underlying rule with a pattern rule
        /// </summary>
        public RuleBuilder HavingPatternRule(string regexPattern, Cardinality? matchCardinality = null)
        {
            _rule = new PatternRule(
                new Regex(regexPattern),
                matchCardinality ?? Cardinality.OccursAtLeastOnce());
            return this;
        }

        /// <summary>
        /// Replaces the underlying rule with a symbol expression rule encapsulating a sequence
        /// </summary>
        public RuleBuilder HavingSequence(
            Action<ExpressionListBuilder> builder,
            Cardinality? cardinality = null)
            => HavingGroup(
                SymbolGroup.GroupingMode.Sequence,
                cardinality ?? Cardinality.OccursOnlyOnce(),
                new ExpressionListBuilder()
                    .Use(builder.Invoke)
                    .Build());

        /// <summary>
        /// Replaces the underlying rule with a symbol expression rule encapsulating a choice
        /// </summary>
        public RuleBuilder HavingChoice(
            Action<ExpressionListBuilder> builder,
            Cardinality? cardinality = null)
            => HavingGroup(
                SymbolGroup.GroupingMode.Choice,
                cardinality ?? Cardinality.OccursOnlyOnce(),
                new ExpressionListBuilder()
                    .Use(builder.Invoke)
                    .Build());

        /// <summary>
        /// Replaces the underlying rule with a symbol expression rule encapsulating a set
        /// </summary>
        public RuleBuilder HavingSet(
            Action<ExpressionListBuilder> builder,
            Cardinality? cardinality = null)
            => HavingGroup(
                SymbolGroup.GroupingMode.Set,
                cardinality ?? Cardinality.OccursOnlyOnce(),
                new ExpressionListBuilder()
                    .Use(builder.Invoke)
                    .Build());

        /// <summary>
        /// Replaces the underlying rule with a symbol expression rule encapsulating a production ref
        /// </summary>
        public RuleBuilder HavingRef(string symbolRef, Cardinality? cardinality = null)
        {
            _rule = new SymbolExpressionRule(
                new ProductionRef(symbolRef, cardinality ?? Cardinality.OccursOnlyOnce()));

            return this;
        }

        private RuleBuilder HavingGroup(SymbolGroup.GroupingMode mode, Cardinality cardinality, ISymbolExpression[] expressions)
        {
            _rule = new SymbolExpressionRule(mode switch
            {
                SymbolGroup.GroupingMode.Choice => SymbolGroup.Choice(cardinality, expressions),
                SymbolGroup.GroupingMode.Sequence => SymbolGroup.Sequence(cardinality, expressions),
                SymbolGroup.GroupingMode.Set => SymbolGroup.Set(cardinality, expressions),
                _ => throw new ArgumentException($"Invalid group mode: {mode}")
            });

            return this;
        }
    }
}
