using Axis.Luna.Extensions;

namespace Axis.Pulsar.Core.Utils
{
    internal static class Extensions
    {
        internal static Tokens Join(this IEnumerable<Tokens> segmentTokens) => Tokens.Join(segmentTokens);

        internal static IEnumerable<CharRange> NormalizeRanges(this IEnumerable<CharRange> ranges) => CharRange.NormalizeRanges(ranges);

        internal static IEnumerable<TItem> ThrowIfContainsNull<TItem>(
            this IEnumerable<TItem> items,
            Exception ex)
            => items.ThrowIfAny(item => item is null, ex);

        internal static IEnumerable<TItem> ThrowIfContainsDefault<TItem>(
            this IEnumerable<TItem> items,
            Exception ex)
            => items.ThrowIfAny(item => EqualityComparer<TItem>.Default.Equals(default, item), ex);

        internal static IEnumerable<TItem> ThrowIfContainsNull<TItem>(
            this IEnumerable<TItem> items,
            Func<TItem, Exception> exceptionMapper)
            => items.ThrowIfAny(item => item is null, exceptionMapper);

        internal static bool Intersects(
            this (int lower, int upper) first,
            (int lower, int upper) second)
        {
            (var less, var greater) = first.lower <= second.lower
                ? (first, second)
                : (second, first);

            return less.upper >= greater.lower;
        }
    }
}
