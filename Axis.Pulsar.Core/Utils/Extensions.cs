using Axis.Luna.Extensions;
using Axis.Misc.Pulsar.Utils;

namespace Axis.Pulsar.Core.Utils
{
    internal static class Extensions
    {
        internal static Tokens Combine(this IEnumerable<Tokens> segmentTokens) => Tokens.Combine(segmentTokens);

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
    }
}
