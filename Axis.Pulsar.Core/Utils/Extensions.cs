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
    }
}
