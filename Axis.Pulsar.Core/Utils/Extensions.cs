using Axis.Luna.Common.Results;
using Axis.Misc.Pulsar.Utils;
using Axis.Pulsar.Core.Exceptions;

namespace Axis.Pulsar.Core.Utils
{
    internal static class Extensions
    {
        /// <summary>
        /// Calls <see cref="Tokens.CombineWith(Tokens)"/> on each consecutive items in the given sequence.
        /// </summary>
        /// <param name="segments">A sequence of consecutively related <see cref="Tokens"/> instances</param>
        /// <returns>A new instance that is a combination of all the given consecutive instances</returns>
        internal static Tokens Combine(this IEnumerable<Tokens> segments)
        {
            ArgumentNullException.ThrowIfNull(segments);

            return segments.Aggregate(
                Tokens.Empty,
                (segment, next) => segment.CombineWith(next));
        }



        /// <summary>
        /// TODO: remove this method when it gets released in the Axis.Luna.Common package
        /// </summary>
        public static IResult<TOut> FoldInto<TItem, TOut>(
            this IEnumerable<IResult<TItem>> results,
            Func<IEnumerable<TItem>, TOut> aggregator)
        {
            return results.Fold().Map(aggregator);
        }

        public static bool IsPartiallyRecognizedErrorResult<TOut>(this
            IResult<TOut> result,
            out Errors.PartiallyRecognizedTokens? error)
        {
            ArgumentNullException.ThrowIfNull(result);

            if (result.IsErrorResult()
                && result.AsError().ActualCause() is Errors.PartiallyRecognizedTokens partialError)
            {
                error = partialError;
                return true;
            }

            error = null;
            return false;
        }

        public static bool IsUnrecognizedErrorResult<TOut>(this
            IResult<TOut> result,
            out Errors.UnrecognizedTokens? error)
        {
            ArgumentNullException.ThrowIfNull(result);

            if (result.IsErrorResult()
                && result.AsError().ActualCause() is Errors.UnrecognizedTokens unrecognizedError)
            {
                error = unrecognizedError;
                return true;
            }

            error = null;
            return false;
        }

        public static bool IsRuntimeErrorResult<TOut>(this
            IResult<TOut> result,
            out Errors.RuntimeError? error)
        {
            ArgumentNullException.ThrowIfNull(result);

            if (result.IsErrorResult()
                && result.AsError().ActualCause() is Errors.RuntimeError runtimeError)
            {
                error = runtimeError;
                return true;
            }

            error = null;
            return false;
        }
    }
}
