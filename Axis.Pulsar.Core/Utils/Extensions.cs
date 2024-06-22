using System.Collections.Immutable;

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

        internal static bool DefaultOrSequenceEqual<TItem>(this
            ImmutableArray<TItem> first,
            ImmutableArray<TItem> second)
        {
            return (first.IsDefault, second.IsDefault) switch
            {
                (true, true) => true,
                (false, false) => first.SequenceEqual(second),
                _ => false
            };
        }

        internal static (ImmutableHashSet<T> distinctLeft, ImmutableHashSet<T> intersection, ImmutableHashSet<T> distinctRight) SplitSets<T>(
            this HashSet<T> left,
            HashSet<T> right)
        {
            ArgumentNullException.ThrowIfNull(left);
            ArgumentNullException.ThrowIfNull(right);

            return (
                left.Except(right).ToImmutableHashSet(),
                left.Intersect(right).ToImmutableHashSet(),
                right.Except(left).ToImmutableHashSet());
        }
    }
}
