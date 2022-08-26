using Axis.Pulsar.Parser.Exceptions;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Axis.Pulsar.Parser.Grammar
{

    public class SymbolGroup: ISymbolExpression
    {
        /// <summary>
        /// Represents types of non-terminal groupings.
        /// </summary>
        public enum GroupingMode
        {
            /// <summary>
            /// Given a group of rules, each rule is tried in the order they were presented, and the first rule that passes signifies this grouping-expression is satisfied.
            /// </summary>
            Choice,

            /// <summary>
            /// Given a group of unique rules, all individual rules from the group must pass once for this grouping-expression to be satisfied
            /// </summary>
            Set,

            /// <summary>
            /// Given a gorup of rules, all individual rules must pass, in the provided order, for this grouping-expression to be satisfied.
            /// </summary>
            Sequence
        }

        /// <summary>
        /// The grouping mode for this instance.
        /// </summary>
        public GroupingMode Mode { get; }

        /// <inheritdoc />
        public Cardinality Cardinality { get; }

        /// <summary>
        /// The expressions this instance encapsulates.
        /// </summary>
        public IReadOnlyCollection<ISymbolExpression> Expressions { get; }

        /// <summary>
        /// Returns all refs that are "leaf-nodes" for the tree starting at the current <see cref="ISymbolExpression"/> instance.
        /// </summary>
        public IReadOnlyCollection<ProductionRef> SymbolRefs => Expressions
            .SelectMany(expression => expression switch
            {
                ProductionRef sr => new [] {sr},
                SymbolGroup sg => sg.SymbolRefs,
                _ => Enumerable.Empty<ProductionRef>()
            })
            .ToList()
            .AsReadOnly();

        /// <summary>
        /// Constructor. Note that if the grouping mode is <see cref="GroupingMode.Set"/>, duplicate values in the <paramref name="expressions"/> array will be discarded.
        /// </summary>
        /// <param name="mode">The <see cref="GroupingMode"/> applied on this Rule</param>
        /// <param name="cardinality">The cardinality for this rule</param>
        /// <param name="expressions">The symbol-refs</param>
        private SymbolGroup(
            GroupingMode mode,
            Cardinality cardinality,
            params ISymbolExpression[] expressions)
        {
            if (expressions == null || expressions.Length == 0)
                throw new ArgumentException($"Invalid {expressions} array");

            Mode = mode;
            Cardinality = cardinality;

            if (Mode == GroupingMode.Set)
                Expressions = expressions
                     .Distinct()
                     .ToList()
                     .Map(list => new ReadOnlyCollection<ISymbolExpression>(list));

            else
                Expressions = Array.AsReadOnly(expressions);

            //ensure that the expressions all terminate in Proeuction-Refs
            if (!Expressions.ExactlyAll(TerminatesAtSymbolRef))
                throw new ArgumentException($"Some expressions do not terminate in {nameof(ProductionRef)}");
        }

        /// <summary>
        /// Returns true if the given expression has all it's branches terminating in <see cref="ProductionRef"/> instances.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        private bool TerminatesAtSymbolRef(ISymbolExpression expression)
        {
            return expression switch
            {
                SymbolGroup group => group.Expressions.ExactlyAll(TerminatesAtSymbolRef),
                ProductionRef @ref => true,
                _ => false
            };
        }

        public override bool Equals(object obj)
        {
            return obj is SymbolGroup sg
                && sg.Mode == Mode
                && sg.Cardinality == Cardinality
                && sg.Expressions.SequenceEqual(Expressions);
        }

        public override int GetHashCode()
        {
            return Expressions.Aggregate(
                HashCode.Combine(Mode, Cardinality),
                (code, expression) => HashCode.Combine(code, expression));
        }

        #region Set

        /// <summary>
        /// Creates a set-expression. Note that duplicates will be discarded from the <paramref name="symbolExpressions"/> array.
        /// </summary>
        /// <param name="cardinality">The cardinality for this rule</param>
        /// <param name="symbolExpressions">The symbol-refs</param>
        public static SymbolGroup Set(
            Cardinality cardinality,
            params ISymbolExpression[] symbolExpressions)
        {
            return new(GroupingMode.Set, cardinality, symbolExpressions);
        }

        /// <summary>
        /// Creates a set-expression. Note that duplicates will be discarded from the <paramref name="symbolExpressions"/> array.
        /// </summary>
        /// <param name="symbolExpressions">The symbol-refs</param>
        public static SymbolGroup Set(params ISymbolExpression[] rules)
        {
            return new(GroupingMode.Set, Cardinality.OccursOnlyOnce(), rules);
        }

        #endregion

        #region Sequence

        /// <summary>
        /// Creates a sequence-expression.
        /// </summary>
        /// <param name="cardinality">The cardinality for this rule</param>
        /// <param name="symbolExpressions">The symbol-refs</param>
        public static SymbolGroup Sequence(Cardinality cardinality, params ISymbolExpression[] symbolExpressions)
        {
            return new(GroupingMode.Sequence, cardinality, symbolExpressions);
        }

        /// <summary>
        /// Creates a sequence-expression.
        /// </summary>
        /// <param name="symbolExpressions">The symbol-refs</param>
        public static SymbolGroup Sequence(params ISymbolExpression[] symbolExpressions)
        {
            return new(GroupingMode.Sequence, Cardinality.OccursOnlyOnce(), symbolExpressions);
        }

        #endregion

        #region Choice

        /// <summary>
        /// Creates a choice-expression
        /// </summary>
        /// <param name="cardinality">The cardinality for this rule</param>
        /// <param name="symbolExpressions">The symbol-refs</param>
        public static SymbolGroup Choice(
            Cardinality cardinality,
            params ISymbolExpression[] symbolExpressions)
        {
            return new(GroupingMode.Choice, cardinality, symbolExpressions);
        }

        /// <summary>
        /// Creates a choice-expression
        /// </summary>
        /// <param name="symbolExpressions">The symbol-refs</param>
        public static SymbolGroup Choice(params ISymbolExpression[] symbolExpressions)
        {
            return new(GroupingMode.Choice, Cardinality.OccursOnlyOnce(), symbolExpressions);
        }

        #endregion
    }
}
