using Axis.Pulsar.Parser;

namespace Axis.Pulsar.Importer.Common.xBNF
{
    static internal class Extensions
    {
        internal static string ApplyPatternEscape(this string input) => input.Replace("//", "/");

    }
}
