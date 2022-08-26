using Axis.Pulsar.Parser.Utils;

namespace Axis.Pulsar.Parser.Grammar
{

    /// <summary>
    /// A symbol expression represents an ordering of <see cref="Parser.Grammar.ProductionRef"/> or <see cref="Parser.Grammar.SymbolGroup"/> instances that are intended to be evaluated to yield
    /// a boolean result, based on the individual grouping where needed.
    /// <para>
    /// A <see cref="ISymbolExpression"/> represents the discriminated union of either a <see cref="Parser.Grammar.SymbolGroup"/> or a <see cref="Parser.Grammar.ProductionRef"/>.
    /// </para>
    /// </summary>
    public interface ISymbolExpression
    {
        /// <summary>
        /// The cardinality of this expression
        /// </summary>
        Cardinality Cardinality { get; }
    }
}
