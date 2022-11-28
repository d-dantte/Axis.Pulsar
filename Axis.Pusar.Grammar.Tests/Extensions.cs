namespace Axis.Pusar.Grammar.Tests
{
    public static class Extensions
    {
        public static string JoinUsing(this IEnumerable<string> strings, string separator)
        {
            if(strings is null)
                throw new ArgumentNullException(nameof(strings));

            return string.Join(separator, strings);
        }
    }
}
