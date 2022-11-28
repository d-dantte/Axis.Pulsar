namespace Axis.Pulsar.Importer.Common.Antlr
{
    static internal class Extensions
    {
        internal static string ApplyPatternEscape(this string input) => input.Replace("//", "/");
    }
}
