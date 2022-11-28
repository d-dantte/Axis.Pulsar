using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Grammar.Builders
{
    /// <summary>
    /// Rule builder instance
    /// </summary>
    public class RuleListBuilder: AbstractBuiler<IRule[]>
    {
        private List<IRule> _rules = new List<IRule>();

        public static RuleListBuilder NewBuilder() => new();


        protected override IRule[] BuildTarget() => _rules.ToArray();

        protected override void ValidateTarget()
        {
            if (_rules.IsEmpty())
                throw new InvalidOperationException($"No rule building action has occured");
        }

        /// <summary>
        /// Appends to the underlying rule with a literal rule
        /// </summary>
        public RuleListBuilder HavingLiteral(
            string stringLiteral,
            bool isCaseSensitive)
        {
            AssertNotBuilt();
            _rules.Add(new Literal(stringLiteral, isCaseSensitive));
            return this;
        }

        /// <summary>
        /// Appends to the underlying rule with a pattern rule
        /// </summary>
        public RuleListBuilder HavingPattern(
            Regex regex,
            MatchType matchType = null)
        {
            AssertNotBuilt();
            _rules.Add(
                new Pattern(
                    regex,
                    matchType ?? new MatchType.Open(1)));
            return this;
        }

        /// <summary>
        /// Appends to the underlying rule with a pattern rule
        /// </summary>
        public RuleListBuilder HavingEOF()
        {
            AssertNotBuilt();
            _rules.Add(new EOF());
            return this;
        }

        /// <summary>
        /// Appends to the underlying rule with a symbol expression rule encapsulating a production ref
        /// </summary>
        public RuleListBuilder HavingRef(
            string symbolRef,
            Cardinality? cardinality = null)
        {
            AssertNotBuilt();
            _rules.Add(
                new ProductionRef(
                    symbolRef,
                    cardinality));

            return this;
        }

        /// <summary>
        /// Appends to the underlying rule with a symbol expression rule encapsulating a sequence
        /// </summary>
        public RuleListBuilder HavingSequence(
            Action<RuleListBuilder> ruleListBuilderAction,
            Cardinality? cardinality = null)
        {
            AssertNotBuilt();
            _rules.Add(
                new Sequence(
                    cardinality ?? Cardinality.OccursOnlyOnce(),
                    RuleListBuilder
                        .NewBuilder()
                        .With(ruleListBuilderAction.Invoke)
                        .Build()));

            return this;
        }

        /// <summary>
        /// Appends to the underlying rule with a symbol expression rule encapsulating a choice
        /// </summary>
        public RuleListBuilder HavingChoice(
            Action<RuleListBuilder> ruleListBuilderAction,
            Cardinality? cardinality = null)
        {
            AssertNotBuilt();
            _rules.Add(
                new Choice(
                    cardinality ?? Cardinality.OccursOnlyOnce(),
                    RuleListBuilder
                        .NewBuilder()
                        .With(ruleListBuilderAction.Invoke)
                        .Build()));

            return this;
        }

        /// <summary>
        /// Appends to the underlying rule with a symbol expression rule encapsulating a set
        /// </summary>
        public RuleListBuilder HavingSet(
            Action<RuleListBuilder> ruleListBuilderAction,
            int? minRecognitionCount = null,
            Cardinality? cardinality = null)
        {
            AssertNotBuilt();
            _rules.Add(
                new Set(
                    cardinality ?? Cardinality.OccursOnlyOnce(),
                    minRecognitionCount,
                    RuleListBuilder
                        .NewBuilder()
                        .With(ruleListBuilderAction.Invoke)
                        .Build()));

            return this;
        }

        /// <summary>
        /// Appends a rule. This method doesn't do any checks on the type of rule that is added,
        /// beyond a null-check
        /// </summary>
        /// <param name="rule">The rule</param>
        /// <exception cref="ArgumentNullException">If the given <paramref name="rule"/> is null</exception>
        public RuleListBuilder HavingRule(IRule rule)
        {
            _rules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));

            return this;
        }

    }
}
