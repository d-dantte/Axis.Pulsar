using System;
using System.Collections.Generic;
using System.Linq;

namespace Axis.Pulsar.Parser.Exceptions
{
    public class RuleMapValidatoinException: Exception
    {
        private readonly string[] _unreferencedProductions;
        private readonly string[] _orphanedSymbols;

        public IEnumerable<string> UnreferencedProductions => _unreferencedProductions.AsEnumerable();
        public IEnumerable<string> OrphanedSymbols => _orphanedSymbols.AsEnumerable();

        public RuleMapValidatoinException(
            string[] unreferencedProductions,
            string[] orphanedSymbols)
            :base($"Validation Errors found in the RuleMap. Unreferenced-Productions:{unreferencedProductions?.Length}, Orphaned-Symbols:{orphanedSymbols?.Length}")
        {
            _unreferencedProductions = unreferencedProductions ?? Array.Empty<string>();
            _orphanedSymbols = orphanedSymbols ?? Array.Empty<string>();
        }
    }
}
