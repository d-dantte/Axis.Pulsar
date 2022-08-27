using Axis.Pulsar.Parser.Grammar;
using Axis.Pulsar.Parser.Utils;
using System;
using System.Collections.Generic;

namespace Axis.Pulsar.Parser.Builders
{
    /// <summary>
    /// Expression List builder instance. Encapsulates a list of <see cref="ISymbolExpression"/> instances that make up a 
    /// <see cref="SymbolGroup"/>.
    /// </summary>
    public class ExpressionListBuilder : IBuilder<ISymbolExpression[]>
    {
        private List<ISymbolExpression> _expressions = new List<ISymbolExpression>();

        /// <summary>
        /// Adds a symbol ref to the list
        /// </summary>
        public ExpressionListBuilder WithRef(string symbolRef, Cardinality? cardinality = null)
        {
            _expressions.Add(new ProductionRef(
                symbolRef,
                cardinality ?? Cardinality.OccursOnlyOnce()));
            return this;
        }

        /// <summary>
        /// Adds an array of expressions, encapsulated within a <see cref="SymbolGroup"/> of the given mode, into the list.
        /// </summary>
        public ExpressionListBuilder With(SymbolGroup.GroupingMode mode, params ISymbolExpression[] expressions)
        {
            _expressions.Add(mode switch
            {
                SymbolGroup.GroupingMode.Choice => SymbolGroup.Choice(expressions),
                SymbolGroup.GroupingMode.Sequence => SymbolGroup.Sequence(expressions),
                SymbolGroup.GroupingMode.Set => SymbolGroup.Set(expressions),
                _ => throw new ArgumentException($"Invalid group mode: {mode}")
            });

            return this;
        }

        /// <summary>
        /// Adds the list built from the given builder into a <see cref="SymbolGroup.GroupingMode.Sequence"/> instance, and adds that to the list.
        /// </summary>
        public ExpressionListBuilder WithSequence(
            Action<ExpressionListBuilder> sequenceBuilderAction,
            Cardinality? cardinality = null)
        {
            cardinality ??= Cardinality.OccursOnlyOnce();
            new ExpressionListBuilder()
                .Use(sequenceBuilderAction.Invoke)
                .Build()
                .Map(expressions => SymbolGroup.Sequence(cardinality.Value, expressions))
                .Consume(_expressions.Add);
            return this;
        }

        /// <summary>
        /// Adds the list built from the given builder into a <see cref="SymbolGroup.GroupingMode.Choice"/> instance, and adds that to the list.
        /// </summary>
        public ExpressionListBuilder WithChoice(
            Action<ExpressionListBuilder> choiceBuilderAction,
            Cardinality? cardinality = null)
        {
            cardinality ??= Cardinality.OccursOnlyOnce();
            new ExpressionListBuilder()
                .Use(choiceBuilderAction.Invoke)
                .Build()
                .Map(expressions => SymbolGroup.Choice(cardinality.Value, expressions))
                .Consume(_expressions.Add);
            return this;
        }

        /// <summary>
        /// Adds the list built from the given builder into a <see cref="SymbolGroup.GroupingMode.Set"/> instance, and adds that to the list.
        /// </summary>
        public ExpressionListBuilder WithSet(
            Action<ExpressionListBuilder> setBuilderAction,
            Cardinality? cardinality = null)
        {
            cardinality ??= Cardinality.OccursOnlyOnce();
            new ExpressionListBuilder()
                .Use(setBuilderAction.Invoke)
                .Build()
                .Map(expressions => SymbolGroup.Set(cardinality.Value, expressions))
                .Consume(_expressions.Add);
            return this;
        }

        /// <summary>
        /// Clears the underlying list
        /// </summary>
        public ExpressionListBuilder Clear()
        {
            _expressions.Clear();
            return this;
        }


        /// <inheritdoc/>
        public ISymbolExpression[] Build() =>  _expressions.ToArray();
    }
}
