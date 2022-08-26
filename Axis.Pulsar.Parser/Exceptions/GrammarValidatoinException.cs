using System;
using System.Collections.Generic;

namespace Axis.Pulsar.Parser.Exceptions
{
    /// <summary>
    /// Exception thrown when grammar validation fails.
    /// </summary>
    public class GrammarValidatoinException: Exception
    {
        private static readonly string ErrorMessageTemplate = "Validation errors found in the Grammar. Unreferenced-Productions: {0}, Orphaned-SymbolRefs: {1}, No-Terminals-Found: {2}";

        /// <summary>
        /// Collection of symbols that cannot be traced back to the root.
        /// </summary>
        public IReadOnlyCollection<string> UnreferencedProductionSymbols { get; }

        /// <summary>
        /// Collection of symbols that have no corresponding production.
        /// </summary>
        public IReadOnlyCollection<string> OrphanedSymbols { get; }

        /// <summary>
        /// No terminal symbol detected
        /// </summary>
        public bool TerminalsAreAbsent { get; }

        public GrammarValidatoinException(
            string[] unreferencedProductionSymbols,
            string[] orphanedSymbols,
            bool terminalsAreAbsent)
            :base(string.Format(
                ErrorMessageTemplate,
                unreferencedProductionSymbols?.Length ?? 0,
                orphanedSymbols?.Length ?? 0,
                terminalsAreAbsent))
        {
            UnreferencedProductionSymbols = Array.AsReadOnly(unreferencedProductionSymbols ?? Array.Empty<string>());
            OrphanedSymbols = Array.AsReadOnly(orphanedSymbols ?? Array.Empty<string>());
            TerminalsAreAbsent = terminalsAreAbsent;
        }
    }
}
