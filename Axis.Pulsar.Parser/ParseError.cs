using System;

namespace Axis.Pulsar.Parser
{
    public class ParseError
    {
        public ParseError Cause { get; }
        
        public string SymbolName { get; }

        public int CharacterIndex { get; }

        public string Message { get; }

        public ParseError(string symbolName, int characterIndex, ParseError cause = null)
        {
            Cause = cause;

            CharacterIndex = characterIndex.ThrowIf(
                Extensions.IsNegative,
                n => new ArgumentException($"Invalid {nameof(characterIndex)}"));

            SymbolName = symbolName.ThrowIf(
                string.IsNullOrWhiteSpace,
                n => new ArgumentException($"Invalid {nameof(symbolName)}"));

            Message = $"{SymbolName} was not found at stream index: {CharacterIndex}"
                + (Cause == null ? "" : $" because [{Cause.Message}]");
        }
    }
}
