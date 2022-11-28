using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Axis.Pulsar.Parser.Grammar
{

    public abstract class SymbolGroup: ISymbolExpression
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
        protected SymbolGroup(
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
            if (!Expressions.ExactlyAll(TerminatesAtSymbolRefOrEOF))
                throw new ArgumentException($"Some expressions do not terminate in {nameof(ProductionRef)}");
        }

        /// <summary>
        /// Returns true if the given expression has all it's branches terminating in <see cref="ProductionRef"/> instances.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        private bool TerminatesAtSymbolRefOrEOF(ISymbolExpression expression)
        {
            return expression switch
            {
                SymbolGroup group => group.Expressions.ExactlyAll(TerminatesAtSymbolRefOrEOF),
                ProductionRef => true,
                EOF => true,
                _ => false
            };
        }


        #region nested types

        /// <summary>
        /// Represents an unordered set of expressions, each of which MUST be recognized successfully.
        /// Sets do not accept optional recognitions as a success.
        /// </summary>
        public class Set : SymbolGroup
        {
            /// <summary>
            /// Minimum number of recognized items that can exist for this group to be deemed recognized.
            /// Default value is 1
            /// </summary>
            public int? MinContentCount { get; }

            public Set(
                Cardinality cardinality,
                int? minContentCount,
                params ISymbolExpression[] expressions)
                : base(GroupingMode.Set, cardinality, expressions)
            {
                MinContentCount = minContentCount.ThrowIf(
                    v => v < 1,
                    new ArgumentException($"Invalid content count: {minContentCount}"));
            }

            public override bool Equals(object obj)
            {
                return obj is Set other
                    && other.Cardinality == Cardinality
                    && other.MinContentCount == MinContentCount
                    && other.Expressions.SequenceEqual(Expressions);
            }

            public override int GetHashCode()
            {
                return Expressions.Aggregate(
                    HashCode.Combine(Mode, Cardinality, MinContentCount),
                    (code, expression) => HashCode.Combine(code, expression));
            }
        }

        /// <summary>
        /// Represents an ordered list of expressions, each of which MUST be recognized successfully.
        /// </summary>
        public class Sequence : SymbolGroup
        {
            public Sequence(
                Cardinality cardinality,
                params ISymbolExpression[] expressions)
                : base(GroupingMode.Sequence, cardinality, expressions)
            {
            }

            public override bool Equals(object obj)
            {
                return obj is Sequence other
                    && other.Cardinality == Cardinality
                    && other.Expressions.SequenceEqual(Expressions);
            }

            public override int GetHashCode()
            {
                return Expressions.Aggregate(
                    HashCode.Combine(Mode, Cardinality),
                    (code, expression) => HashCode.Combine(code, expression));
            }
        }

        /// <summary>
        /// Represents an ordered list of choices of expressions, only one of which MUST be recognied successfully.
        /// </summary>
        public class Choice : SymbolGroup
        {
            public Choice(
                Cardinality cardinality,
                params ISymbolExpression[] expressions)
                : base(GroupingMode.Choice, cardinality, expressions)
            {
            }

            public override bool Equals(object obj)
            {
                return obj is Choice other
                    && other.Cardinality == Cardinality
                    && other.Expressions.SequenceEqual(Expressions);
            }

            public override int GetHashCode()
            {
                return Expressions.Aggregate(
                    HashCode.Combine(Mode, Cardinality),
                    (code, expression) => HashCode.Combine(code, expression));
            }
        }
        #endregion
    }
}
