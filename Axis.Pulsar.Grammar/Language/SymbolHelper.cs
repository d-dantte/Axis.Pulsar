using System.Text.RegularExpressions;

namespace Axis.Pulsar.Grammar.Language
{
    public static class SymbolHelper
    {
        public static readonly Regex SymbolPattern = new(@"^[a-zA-A_][-\w\d]*$");

        public static readonly Regex SymbolRefPattern = new(@"^@(?<symbol>[a-zA-A_][-\w\d]*)\.Ref$");

        public static bool IsValidSymbolName(this string symbolName)
        {
            return !string.IsNullOrWhiteSpace(symbolName)
                && SymbolPattern.IsMatch(symbolName);
        }
    }
}
