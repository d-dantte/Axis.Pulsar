using System;
using System.Collections.Generic;

namespace Axis.Pulsar.Parser.Exceptions
{
    public class GrammarValidatoinException: Exception
    {
        private static readonly string ErrorMessageTemplate = "Validation errors found in the Grammar. Unreferenced-Productions: {0}, Orphaned-SymbolRefs: {1}, No-Terminals-Found: {2}";

        public IReadOnlyCollection<string> UnreferencedProductionSymbols { get; }
        public IReadOnlyCollection<string> OrphanedSymbols { get; }
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
