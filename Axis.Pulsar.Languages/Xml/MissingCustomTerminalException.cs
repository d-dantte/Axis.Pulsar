using System;

namespace Axis.Pulsar.Languages.Xml
{
    /// <summary>
    /// Used to indicate that a custom terminal is expected, but it wasn't found in the importer
    /// </summary>
    public class MissingCustomTerminalException: Exception
    {
        public string CustomTerminalSymbol { get; }

        public MissingCustomTerminalException(string customTerminalSymbol)
            :base ($"The '{customTerminalSymbol}' symbol was not found in the importer")
        {
            CustomTerminalSymbol = customTerminalSymbol;
        }
    }
}
