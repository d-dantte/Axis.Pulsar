using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Grammar
{

    /// <summary>
    /// A symbol expression represents an ordering of <see cref="Parser.Grammar.SymbolRef"/> or <see cref="Parser.Grammar.SymbolGroup"/> instances that are intended to be evaluated to yield
    /// a boolean result, based on the individual grouping where needed.
    /// <para>
    /// A <see cref="SymbolExpression"/> represents the discriminated union of either a <see cref="Parser.Grammar.SymbolGroup"/> or a <see cref="Parser.Grammar.SymbolRef"/>.
    /// </para>
    /// <para>
    /// Ps: This is a poor man's discriminated union. :D
    /// </para>
    /// </summary>
    public interface ISymbolExpression
    {
    }
}
