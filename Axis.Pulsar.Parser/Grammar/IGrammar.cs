using Axis.Pulsar.Parser.Parsers;
using System.Collections.Generic;
using Axis.Pulsar.Parser.Exceptions;

namespace Axis.Pulsar.Parser.Grammar
{
    /// <summary>
    /// A grammar encapsulates productions that together make up a language.
    /// </summary>
    public interface IGrammar
    {
        /// <summary>
        /// Returns the parser for the root symbol.
        /// </summary>
        IParser RootParser();

        /// <summary>
        /// Get the parser for the symbol specified in the argument,  Throws <see cref="SymbolNotFoundException"/> if the symbol is absent.
        /// </summary>
        /// <param name="symbolName">The symbol name</param>
        IParser GetParser(string symbolName);

        /// <summary>
        /// Get the production for the root symbol.
        /// </summary>
        Production RootProduction();

        /// <summary>
        /// Indicates the a production exists for the given symbol name.
        /// </summary>
        /// <param name="symbolName">The symbol name</param>
        bool HasProduction(string symbolName);

        /// <summary>
        /// Returns the production for the given symbol.  Throws <see cref="SymbolNotFoundException"/> if the root symbol is absent.
        /// </summary>
        /// <param name="symbolName">The symbol name</param>
        Production GetProduction(string symbolName);

        /// <summary>
        /// Get all the available productions, in no particular order
        /// </summary>
        IEnumerable<Production> Productions { get; }

        /// <summary>
        /// Get all parsers for all available productions, in no particular order
        /// </summary>
        IEnumerable<IParser> Parsers { get; }

        /// <summary>
        /// Gets the root symbol for this grammar
        /// </summary>
        string RootSymbol { get; }
    }
}
