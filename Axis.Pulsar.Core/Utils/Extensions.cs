using Axis.Luna.Result;

namespace Axis.Pulsar.Core.Utils
{
    internal static class Extensions
    {
        internal static IEnumerable<CharRange> NormalizeRanges(this IEnumerable<CharRange> ranges) => CharRange.NormalizeRanges(ranges);

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

        internal static int IndexOf(this
            ReadOnlySpan<char> span,
            ReadOnlySpan<char> pattern,
            int offset,
            StringComparison comparison)
        {
            if (offset < 0 || offset >= span.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return span[offset..].IndexOf(pattern, comparison);
        }

        internal static bool TryNextIndexOf(this
            ReadOnlySpan<char> span,
            ReadOnlySpan<char> pattern,
            int offset,
            StringComparison comparison,
            out int index)
        {
            index = span.IndexOf(pattern, offset, comparison);
            return index != -1;
        }

        internal static TList InsertItem<TList, TItem>(this
            TList list, Index index, TItem item)
            where TList: IList<TItem>
        {
            ArgumentNullException.ThrowIfNull(list);

            list[index] = item;
            return list;
        }

        internal static void NoOp<TIn>(TIn @in) { }

        internal static TOut DefaultOp<TIn, TOut>(TIn _) => default!;
    }
}
