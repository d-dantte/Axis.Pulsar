using Axis.Luna.Common.Results;
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

        internal static IResult<TData> MapError<TData, TError>(this
            IResult<TData> result,
            Func<TError, TData> errorMapper)
            where TError : Exception
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(errorMapper);

            if (result.IsErrorResult<TData, TError>(out _))
                return result.MapError(err => errorMapper.Invoke((err as TError)!));

            else return result;
        }

        internal static TItems AddItem<TItems, TItem>(this TItems items, TItem item)
        where TItems : ICollection<TItem>
        {
            ArgumentNullException.ThrowIfNull(items);

            items.Add(item);
            return items;
        }
    }
}
