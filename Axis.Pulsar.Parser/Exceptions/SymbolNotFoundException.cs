using System;

namespace Axis.Pulsar.Parser.Exceptions
{
    public class SymbolNotFoundException: Exception
    {
        public string SymbolName { get; }

        public SymbolNotFoundException(string symbolName)
        : base($"symbol '{symbolName}' not found")
        {
            SymbolName = symbolName;
        }
    }
}
