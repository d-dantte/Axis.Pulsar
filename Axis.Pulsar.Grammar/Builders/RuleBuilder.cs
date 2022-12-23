using Axis.Luna.Extensions;
using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using Axis.Pulsar.Grammar.Language.Rules.CustomTerminals;
using System;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Grammar.Builders
{
    /// <summary>
    /// Rule builder instance
    /// </summary>
    public class RuleBuilder: AbstractBuiler<IRule>
    {
        private IRule _rule;

        public static RuleBuilder NewBuilder() => new();


        protected override IRule BuildTarget() => _rule;

        protected override void ValidateTarget()
        {
            if (_rule is null)
                throw new InvalidOperationException($"No rule building action has occured");
        }

        /// <summary>
        /// Replaces the underlying rule with a literal rule
        /// </summary>
        public RuleBuilder WithLiteral(
            string stringLiteral,
            bool isCaseSensitive)
        {
            AssertNotBuilt();
            _rule = new Literal(stringLiteral, isCaseSensitive);
            return this;
        }

        /// <summary>
        /// Replaces the underlying rule with a pattern rule
        /// </summary>
        public RuleBuilder WithPattern(
            Regex regex,
            MatchType matchType = null)
        {
            AssertNotBuilt();
            _rule = new Pattern(
                regex,
                matchType ?? new MatchType.Open(1));
            return this;
        }

        /// <summary>
        /// Replaces the underlying rule with a pattern rule
        /// </summary>
        public RuleBuilder WithEOF()
        {
            AssertNotBuilt();
            _rule = new EOF();
            return this;
        }

        /// <summary>
        /// Replaces the underlying rule with a symbol expression rule encapsulating a production ref
        /// </summary>
        public RuleBuilder WithRef(
            string symbolRef,
            Cardinality? cardinality = null)
        {
            AssertNotBuilt();
            _rule = new ProductionRef(
                    symbolRef,
                    cardinality);

            return this;
        }

        /// <summary>
        /// Replaces the underlying rule with a symbol expression rule encapsulating a sequence
        /// </summary>
        public RuleBuilder WithSequence(
            Action<RuleListBuilder> ruleListBuilderAction,
            Cardinality? cardinality = null)
        {
            AssertNotBuilt();
            _rule = new Sequence(
                cardinality ?? Cardinality.OccursOnlyOnce(),
                RuleListBuilder
                    .NewBuilder()
                    .With(ruleListBuilderAction.Invoke)
                    .Build());

            return this;
        }

        /// <summary>
        /// Replaces the underlying rule with a symbol expression rule encapsulating a choice
        /// </summary>
        public RuleBuilder WithChoice(
            Action<RuleListBuilder> ruleListBuilderAction,
            Cardinality? cardinality = null)
        {
            AssertNotBuilt();
            _rule = new Choice(
                cardinality ?? Cardinality.OccursOnlyOnce(),
                RuleListBuilder
                    .NewBuilder()
                    .With(ruleListBuilderAction.Invoke)
                    .Build());

            return this;
        }

        /// <summary>
        /// Replaces the underlying rule with a symbol expression rule encapsulating a set
        /// </summary>
        public RuleBuilder WithSet(
            Action<RuleListBuilder> ruleListBuilderAction,
            int? minRecognitionCount = null,
            Cardinality? cardinality = null)
        {
            AssertNotBuilt();
            _rule = new Set(
                cardinality ?? Cardinality.OccursOnlyOnce(),
                minRecognitionCount,
                RuleListBuilder
                    .NewBuilder()
                    .With(ruleListBuilderAction.Invoke)
                    .Build());

            return this;
        }

        /// <summary>
        /// Replaces the underlying rule with the supplied instance
        /// </summary>
        /// <param name="rule">The rule</param>
        public RuleBuilder WithRule(IRule rule)
        {
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));

            return this;
        }

        /// <summary>
        /// Replaces the underlying rule with the supplied instance
        /// </summary>
        /// <param name="customTerminal">The rule</param>
        public RuleBuilder WithCustomTerminal(ICustomTerminal customTerminal)
        {
            _rule = customTerminal ?? throw new ArgumentNullException(nameof(customTerminal));

            return this;
        }
    }
}
