using Axis.Pulsar.Grammar.Language;
using System;
using System.Collections.Generic;

namespace Axis.Pulsar.Grammar.Exceptions
{
    /// <summary>
    /// Exception thrown when grammar validation fails.
    /// </summary>
    public class GrammarValidationException: Exception
    {
        private static readonly string ErrorMessageTemplate = "Validation errors found in the Grammar. Unreferenced-Productions: {0}, Orphaned-SymbolRefs: {1}, Non-Terminaling-Symbols: {2}";

        public IReadOnlyCollection<Production> Productions { get; }

        /// <summary>
        /// Collection of symbols that cannot be traced back to the root.
        /// </summary>
        public IReadOnlyCollection<string> UnreferencedProductionSymbols { get; }

        /// <summary>
        /// Collection of symbols that have no corresponding production.
        /// </summary>
        public IReadOnlyCollection<string> OrphanedSymbols { get; }

        /// <summary>
        /// Collection of symbols that do not terminate in terminals
        /// </summary>
        public IReadOnlyCollection<string> NonTerminatingSymbols { get; }

        /// <summary>
        /// No terminal symbol detected
        /// </summary>
        public bool TerminalsAreAbsent { get; }

        public GrammarValidationException(
            string[] unreferencedProductionSymbols,
            string[] orphanedSymbols,
            string[] nonTerminatingSymbols,
            Production[] productions)
            :base(string.Format(
                ErrorMessageTemplate,
                unreferencedProductionSymbols?.Length ?? 0,
                orphanedSymbols?.Length ?? 0,
                nonTerminatingSymbols?.Length ?? 0))
        {
            UnreferencedProductionSymbols = Array.AsReadOnly(unreferencedProductionSymbols ?? Array.Empty<string>());
            OrphanedSymbols = Array.AsReadOnly(orphanedSymbols ?? Array.Empty<string>());
            Productions = Array.AsReadOnly(productions ?? Array.Empty<Production>());
            NonTerminatingSymbols = Array.AsReadOnly(nonTerminatingSymbols ?? Array.Empty<string>());
        }
    }
}
