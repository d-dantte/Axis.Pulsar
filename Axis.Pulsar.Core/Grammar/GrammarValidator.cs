using Axis.Pulsar.Core.Grammar.Composite;
using Axis.Pulsar.Core.Grammar.Atomic;
using System.Collections.Immutable;
using Axis.Pulsar.Core.Grammar.Composite.Group;
using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Grammar
{
    public static class GrammarValidator
    {
        public static ValidationResult Validate(IGrammar grammar)
        {
            var context = new TraversalContext(grammar);
            Traverse(grammar.Root, context, out _);
            return new ValidationResult(
                grammar,
                context.HealthyRefs,
                context.OrphanedRefs,
                context.UnresolvableProductions);
        }

        private static void Traverse(string symbol, TraversalContext context, out bool isLeftTerminated)
        {
            isLeftTerminated = context.HealthyRefs.Contains(symbol);

            // Has the production been traversed already?
            if (isLeftTerminated
                || context.OrphanedRefs.Contains(symbol)
                || context.UnresolvableProductions.Contains(symbol))
                return;

            // Does the ref point to any production?
            if (!context.Grammar.ContainsProduction(symbol))
            {
                context.OrphanedRefs.Add(symbol);
                return;
            }

            context.HealthyRefs.Add(symbol);
            var production = context.Grammar[symbol];
            TraverseRule(production.Rule, context, out isLeftTerminated);

            if (!isLeftTerminated)
                context.UnresolvableProductions.Add(symbol);
        }

        private static void TraverseRule(
            IRule rule,
            TraversalContext context,
            out bool isLeftTerminated)
        {
            if (rule is IAtomicRule)
                isLeftTerminated = true;

            else if (rule is ICompositeRule composite)
                TraverseElement(composite.Element, context, out isLeftTerminated);

            else throw new InvalidOperationException(
                $"Invalid production rule: '{rule?.GetType()}'");
        }

        private static void TraverseElement(
            IGroupRule element,
            TraversalContext context,
            out bool isLeftTerminated)
        {
            if (element is Choice choice)
                TraverseChoice(choice, context, out isLeftTerminated);

            else if (element is Sequence sequence)
                TraverseSequence(sequence, context, out isLeftTerminated);

            else if (element is Set set)
                TraverseSet(set, context, out isLeftTerminated);

            else if (element is ProductionRef pref)
                Traverse(pref.Ref, context, out isLeftTerminated);

            else if (element is AtomicRuleRef)
                isLeftTerminated = true;

            else throw new InvalidOperationException(
                $"Invalid production element: {element?.GetType()}");
        }

        private static void TraverseChoice(Choice choice, TraversalContext context, out bool isLeftTerminated)
        {
            ArgumentNullException.ThrowIfNull(nameof(choice));

            if (choice.Elements.IsEmpty)
                throw new InvalidOperationException($"Invalid choice: empty");

            // All elements must be left-terminated for a choice to be left-terminated
            isLeftTerminated = choice.Elements
                .Aggregate(true, (terminated, elt) =>
                {
                    TraverseElement(elt, context, out var lternimated);
                    terminated &= lternimated;
                    return terminated;
                });
        }

        private static void TraverseSequence(Sequence sequence, TraversalContext context, out bool isLeftTerminated)
        {
            ArgumentNullException.ThrowIfNull(nameof(sequence));

            if (sequence.Elements.IsEmpty)
                throw new InvalidOperationException($"Invalid sequence: empty");

            // Only the first element of the sequene must be left-terminated for a sequence to 
            // be considered left-terminated
            isLeftTerminated = sequence.Elements
                .Aggregate(default(bool?), (terminated, elt) =>
                {
                    TraverseElement(elt, context, out var lternimated);
                    terminated ??= lternimated;
                    return terminated;
                })
                !.Value;
        }

        private static void TraverseSet(Set set, TraversalContext context, out bool isLeftTerminated)
        {
            ArgumentNullException.ThrowIfNull(nameof(set));

            if (set.Elements.IsEmpty)
                throw new InvalidOperationException($"Invalid set: empty");

            // All elements must be left-terminated for a set to be left-terminated
            isLeftTerminated = set.Elements.All(elt =>
            {
                TraverseElement(elt, context, out var terminated);
                return terminated;
            });
        }

        #region Nested types

        internal class TraversalContext
        {
            /// <summary>
            /// Refs that do not point to any production in the grammar
            /// </summary>
            internal HashSet<string> OrphanedRefs { get; } = new HashSet<string>();

            /// <summary>
            /// Refs that point to productions in the grammar
            /// </summary>
            internal HashSet<string> HealthyRefs { get; } = new HashSet<string>();

            /// <summary>
            /// Productions that do no resolve to an atomic rule in their left-most component
            /// </summary>
            internal HashSet<string> UnresolvableProductions { get; } = new HashSet<string>();

            /// <summary>
            /// 
            /// </summary>
            internal IGrammar Grammar { get; }

            internal TraversalContext(IGrammar grammar)
            {
                Grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            }
        }

        public class ValidationResult
        {
            /// <summary>
            /// Refs that do not point ot any production in the grammar
            /// </summary>
            public ImmutableHashSet<string> OrphanedRefs { get; }

            /// <summary>
            /// Refs that point to productions in the grammar
            /// </summary>
            public ImmutableHashSet<string> HealthyRefs { get; }

            /// <summary>
            /// Productions that do no resolve to an atomic rule in their left-most component
            /// </summary>
            public ImmutableHashSet<string> UnresolvableProductions { get; }

            /// <summary>
            /// Productions that do not have any references, or whose references cannot be traced back
            /// to the root.
            /// </summary>
            public ImmutableHashSet<string> UnreferencedProductions { get; }

            /// <summary>
            /// 
            /// </summary>
            public IGrammar Grammar { get; }


            internal ValidationResult(
                IGrammar grammar,
                IEnumerable<string> healthyRefs,
                IEnumerable<string> orphanedRefs,
                IEnumerable<string> undelimitedProductions)
            {
                Grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));

                HealthyRefs = healthyRefs
                    .ThrowIfNull(() => new ArgumentNullException(nameof(healthyRefs)))
                    .ToImmutableHashSet();

                OrphanedRefs = orphanedRefs
                    .ThrowIfNull(() => new ArgumentNullException(nameof(orphanedRefs)))
                    .ToImmutableHashSet();

                UnresolvableProductions = undelimitedProductions
                    .ThrowIfNull(() => new ArgumentNullException(nameof(undelimitedProductions)))
                    .ToImmutableHashSet();

                UnreferencedProductions = Grammar.ProductionSymbols
                    .Except(HealthyRefs)
                    .ToImmutableHashSet();
            }

            public bool IsValidGrammar =>
                HealthyRefs.Count == Grammar.ProductionCount
                && OrphanedRefs.IsEmpty
                && UnresolvableProductions.IsEmpty
                && UnreferencedProductions.IsEmpty;
        }

        #endregion
    }
}
