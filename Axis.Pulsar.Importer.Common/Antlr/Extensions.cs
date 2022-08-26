namespace Axis.Pulsar.Importer.Common.Antlr
{
    static internal class Extensions
    {
        internal static string UnescapeSensitive(this string input)
            => input.Replace("&quot;", "\"").Replace("&amp;", "&");
        internal static string UnescapeInsensitive(this string input)
            => input.Replace("&apos;", "'").Replace("&amp;", "&");
        internal static string UnescapePattern(this string input)
            => input.Replace("&sol;", "/").Replace("&amp;", "&");
    }
}
