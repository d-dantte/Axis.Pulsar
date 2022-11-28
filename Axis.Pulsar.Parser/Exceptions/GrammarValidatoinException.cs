using Axis.Pulsar.Parser.Grammar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Axis.Pulsar.Parser.Exceptions
{
    /// <summary>
    /// Exception thrown when grammar validation fails.
    /// </summary>
    public class GrammarValidationException: Exception
    {
        private static readonly string ErrorMessageTemplate = "Validation errors found in the Grammar. Unreferenced-Productions: {0}, Orphaned-SymbolRefs: {1}, No-Terminals-Found: {2}";

        public ReadOnlyDictionary<string, IRule> Productions { get; }

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

        public GrammarValidationException(
            string[] unreferencedProductionSymbols,
            string[] orphanedSymbols,
            bool terminalsAreAbsent,
            Dictionary<string, IRule> grammar)
            :base(string.Format(
                ErrorMessageTemplate,
                unreferencedProductionSymbols?.Length ?? 0,
                orphanedSymbols?.Length ?? 0,
                terminalsAreAbsent))
        {
            UnreferencedProductionSymbols = Array.AsReadOnly(unreferencedProductionSymbols ?? Array.Empty<string>());
            OrphanedSymbols = Array.AsReadOnly(orphanedSymbols ?? Array.Empty<string>());
            TerminalsAreAbsent = terminalsAreAbsent;
            Productions = new ReadOnlyDictionary<string, IRule>(grammar);
        }
    }
}
