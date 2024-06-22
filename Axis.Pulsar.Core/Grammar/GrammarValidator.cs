using System.Collections.Immutable;
using System.Data;
using System.Text;
using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Rules;
using Axis.Pulsar.Core.Grammar.Rules.Aggregate;
using Axis.Pulsar.Core.Grammar.Rules.Atomic;
using Axis.Pulsar.Core.Grammar.Rules.Composite;
using Axis.Pulsar.Core.Utils;

namespace Axis.Pulsar.Core.Grammar
{
    public static class GrammarValidator__old
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
            Production.IRule rule,
            TraversalContext context,
            out bool isLeftTerminated)
        {
            if (rule is IAtomicRule)
                isLeftTerminated = true;

            else if (rule is CompositeRule composite)
                TraverseElement(composite.Element, context, out isLeftTerminated);

            else throw new InvalidOperationException(
                $"Invalid production rule: '{rule?.GetType()}'");
        }

        private static void TraverseElement(
            IAggregationElement element,
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

    public static class GrammarValidator
    {
        public static ValidationResult ValidateGrammar(IGrammar grammar)
        {
            var context = new ValidationContext(grammar);

            TraverseProduction(grammar.GetProduction(grammar.Root), context);

            var splitSets = grammar.ProductionSymbols
                .ToHashSet()
                .SplitSets(context.Symbols);

            return new ValidationResult(
                splitSets.intersection,
                splitSets.distinctLeft,
                context.UnresolvableProductions,
                splitSets.distinctRight);
        }

        public static void TraverseProduction(Production production, ValidationContext context)
        {
            ArgumentNullException.ThrowIfNull(production);
            ArgumentNullException.ThrowIfNull(context);

            // Check for infinite recursion: for every production ref, ensure that the production node for the ref isn't already in the stack
            if (HasNonHaltingProductionLoop(production, context))
            {
                // report infinite loop validation error, and traverse no further
                context.UnresolvableProductions.Add(NodeStack.ProductionRefKeyFor(production.Symbol));
                return;
            }

            // No infinite recursion detected.
            else
            {
                context.TraversalStack.Push(new ProductionNode(production));
                context.Symbols.Add(NodeStack.ProductionKeyFor(production.Symbol));

                if (production.Rule is IAtomicRule atomicRule)
                    TraverseAtomicRule(atomicRule, context);

                else if (production.Rule is CompositeRule compositeRule)
                    TraverseAggregationElementRule(0, compositeRule.Element, context);

                else throw new InvalidOperationException(
                    $"Invalid rule [type: {production.Rule!.GetType()}, path: {context.TraversalStack.SymbolPath}]");

                context.TraversalStack.Pop();
            }
        }

        public static void TraverseAtomicRule(IAtomicRule rule, ValidationContext context)
        {
            ArgumentNullException.ThrowIfNull(rule);
            ArgumentNullException.ThrowIfNull(context);

            context.Symbols.Add(NodeStack.AtomicKeyFor(rule.Id));
        }

        public static void TraverseAggregationElementRule(int index, IAggregationElement rule, ValidationContext context)
        {
            ArgumentNullException.ThrowIfNull(rule);
            ArgumentNullException.ThrowIfNull(context);

            context.TraversalStack.Push(new AggregateRuleNode(index, rule));

            if (rule is ProductionRef productionRef)
                TraverseProduction(context.Grammar.GetProduction(productionRef.Ref), context);

            else if (rule is AtomicRuleRef atomicRef)
                TraverseAtomicRule(atomicRef.Ref, context);

            else if (rule is Repetition repetition)
                TraverseAggregationElementRule(0, repetition.Element, context);

            else TraverseAggregationRule(rule.As<IAggregation>(), context);

            context.TraversalStack.Pop();
        }

        public static void TraverseAggregationRule(IAggregation rule, ValidationContext context)
        {
            ArgumentNullException.ThrowIfNull(rule);
            ArgumentNullException.ThrowIfNull(context);

            rule.Elements.ForEvery((index, element) =>
            {
                TraverseAggregationElementRule((int)index, element, context);
            });
        }

        /// <summary>
        /// Detects infinite recursion in symbol-graph
        /// </summary>
        public static bool HasNonHaltingProductionLoop(Production production, ValidationContext context)
        {
            var haltingLoopDetected = false;

            // 1. Find any previous productions nodes with the same symbol
            var productionKey = NodeStack.ProductionKeyFor(production.Symbol);
            if (context.TraversalStack.TryPeek(productionKey, out var productionIndices))
            {
                // indicates that a loop halting condition is detected
                var lastProductionIndex = productionIndices[^1];

                // 2. Find any choice after the last production, whose child has an index >= 1
                if (context.TraversalStack.TryPeek(NodeStack.ChoiceAggregation, out var choiceIndices))
                {
                    foreach(var choiceIndex in choiceIndices)
                    {
                        if (choiceIndex < lastProductionIndex)
                            continue;

                        if (context.TraversalStack.Peek(choiceIndex + 1).Index > 0)
                        {
                            // infinite loop averted
                            haltingLoopDetected = true;
                            break;
                        }
                    }
                }

                // 3. Find any repetition after the last production, that is optional
                if (!haltingLoopDetected
                    && context.TraversalStack.TryPeek(NodeStack.RepetitionAggregation, out var repetitionIndices))
                {
                    foreach (var repetitionIndex in repetitionIndices)
                    {
                        if (repetitionIndex < lastProductionIndex)
                            continue;

                        var repetition = context.TraversalStack
                            .Peek(repetitionIndex)
                            .As<AggregateRuleNode>().Rule
                            .As<Repetition>();

                        if (repetition.Cardinality.IsZeroMinOccurence)
                        {
                            // infinite loop averted
                            haltingLoopDetected = true;
                            break;
                        }
                    }
                }

                return !haltingLoopDetected;
            }

            return false;
        }

        #region Nested types
        public class ValidationContext
        {
            /// <summary>
            /// All refs encountered in the grammar
            /// </summary>
            public HashSet<string> Symbols { get; } = [];

            /// <summary>
            /// Productions for whom resolution results in an infinite loop.
            /// </summary>
            public HashSet<string> UnresolvableProductions { get; } = [];

            public NodeStack TraversalStack { get; } = new();

            public IGrammar Grammar { get; }

            public ValidationContext(IGrammar grammar)
            {
                ArgumentNullException.ThrowIfNull(grammar);

                Grammar = grammar;
            }

            internal ValidationContext Clear()
            {
                Symbols.Clear();
                UnresolvableProductions.Clear();
                TraversalStack.Clear();
                return this;
            }
        }

        public class ValidationResult
        {
            public ImmutableHashSet<string> HealthyRefs { get; }

            /// <summary>
            /// Productions that have no refs pointing to them that can be resolved from the root symbol.
            /// </summary>
            public ImmutableHashSet<string> UnreferencedProductions { get; }

            /// <summary>
            /// Productions for whom resolution results in an infinite loop.
            /// </summary>
            public ImmutableHashSet<string> UnresolvableProductions { get; }

            /// <summary>
            /// Production Refs that point to no production in the grammar
            /// </summary>
            public ImmutableHashSet<string> UnresolvedSymbolRefs { get; }

            public bool IsValid =>
                UnreferencedProductions.IsEmpty
                && UnresolvedSymbolRefs.IsEmpty
                && UnresolvableProductions.IsEmpty;

            public ValidationResult(
                IEnumerable<string> healthyRefs,
                IEnumerable<string> unreferencedProductions,
                IEnumerable<string> unresolvableProductions,
                IEnumerable<string> unresolvedSymbolRefs)
            {
                ArgumentNullException.ThrowIfNull(healthyRefs);
                ArgumentNullException.ThrowIfNull(unreferencedProductions);
                ArgumentNullException.ThrowIfNull(unresolvableProductions);
                ArgumentNullException.ThrowIfNull(unresolvedSymbolRefs);

                HealthyRefs = healthyRefs.ToImmutableHashSet();
                UnreferencedProductions = unreferencedProductions.ToImmutableHashSet();
                UnresolvableProductions = unresolvableProductions.ToImmutableHashSet();
                UnresolvedSymbolRefs = unresolvedSymbolRefs.ToImmutableHashSet();
            }
        }

        public interface INode
        {
            int Index { get; }
        }

        public class ProductionNode : INode
        {
            public int Index => 0;

            public Production Production { get; }

            public ProductionNode(Production production)
            {
                ArgumentNullException.ThrowIfNull(production);
                Production = production;
            }
        }

        public class ProductionRuleNode: INode
        {
            public int Index => 0;

            public Production.IRule Rule { get; }

            public ProductionRuleNode(Production.IRule rule)
            {
                ArgumentNullException.ThrowIfNull(rule);

                Rule = rule;
            }
        }

        public class AggregateRuleNode: INode
        {
            public int Index { get; }

            public IAggregationElement Rule { get; }

            public AggregateRuleNode(int index, IAggregationElement rule)
            {
                ArgumentNullException.ThrowIfNull(rule);

                Rule = rule;
                Index = index.ThrowIf(
                    i => i < 0,
                    _ => new ArgumentOutOfRangeException(nameof(index)));
            }
        }

        public class NodeStack
        {
            private readonly List<INode> nodes = [];
            private readonly Dictionary<string, List<int>> symbolIndexMap = [];

            public static readonly string ProductionPrefix = "$";
            public static readonly string RefPrefix = "#";
            public static readonly string AtomicPrefix = "@";
            public static readonly string ChoiceAggregation = "Choice";
            public static readonly string SequenceAggregation = "Sequence";
            public static readonly string SetAggregation = "Set";
            public static readonly string RepetitionAggregation = "Repetition";

            public int Count => nodes.Count;
            public string SymbolPath
            {
                get
                {
                    return nodes
                        .Aggregate(
                            new StringBuilder(),
                            (sb, node) => sb.Append('/').Append(NodeStack.NodeString(node)))
                        .ToString();
                }
            }

            public NodeStack Push(INode node)
            {
                ArgumentNullException.ThrowIfNull(node);

                nodes.Add(node);

                var key = EvaluateNodeKey(node);
                var list = symbolIndexMap.GetOrAdd(key, _ => []);
                list.Add(nodes.Count - 1);

                return this;
            }

            public NodeStack Pop()
            {
                if (nodes.Count == 0)
                    return this;

                var key = EvaluateNodeKey(nodes[^1]);
                var list = symbolIndexMap[key];
                list.RemoveAt(list.Count - 1);
                if (list.IsEmpty())
                    symbolIndexMap.Remove(key);

                nodes.RemoveAt(nodes.Count - 1);

                return this;
            }

            public NodeStack Clear()
            {
                nodes.Clear();
                symbolIndexMap.Clear();
                return this;
            }

            public INode Peek(int index)
            {
                return nodes[index];
            }

            public ImmutableArray<int>? Peek(string nodeKey)
            {
                if (symbolIndexMap.TryGetValue(nodeKey, out var list))
                    return [.. list];

                else return null;
            }

            public bool TryPeek(string nodeKey, out ImmutableArray<int> indices)
            {
                var result = Peek(nodeKey);

                if (result is null)
                {
                    indices = default;
                    return false;
                }
                else
                {
                    indices = result.Value;
                    return true;
                }
            }


            public static string EvaluateNodeKey(INode node)
            {
                return node switch
                {
                    ProductionNode pnode => ProductionKeyFor(pnode.Production.Symbol),
                    ProductionRuleNode prnode => AtomicRefKeyFor(prnode.Rule.As<IAtomicRule>().Id),
                    AggregateRuleNode arnode => arnode.Rule switch
                    {
                        Set => SetAggregation,
                        Choice => ChoiceAggregation,
                        Sequence => SequenceAggregation,
                        Repetition => RepetitionAggregation,

                        ProductionRef pref => ProductionRefKeyFor(pref.Ref),
                        AtomicRuleRef arref => AtomicRefKeyFor(arref.Ref.Id),

                        _ => throw new InvalidOperationException(
                            $"Invalid aggregation: {arnode.Rule.GetType()}")
                    },
                    _ => throw new InvalidOperationException(
                        $"Invalid node: {node.GetType()}")
                };
            }

            public static string ProductionRefKeyFor(
                string symbol)
                => $"{RefPrefix}{ProductionPrefix}{symbol}";

            public static string AtomicRefKeyFor(
                string symbol)
                => $"{RefPrefix}{AtomicPrefix}{symbol}";

            public static string AtomicKeyFor(
                string symbol)
                => $"{AtomicPrefix}{symbol}";

            public static string ProductionKeyFor(
                string symbol)
                => $"{ProductionPrefix}{symbol}";

            public static string NodeString(
                INode node)
                => $"{EvaluateNodeKey(node)}{{{node.Index}}}";
        }
        #endregion
    }
}
