using Axis.Luna.Extensions;
using Axis.Pulsar.Core.Grammar.Errors;

namespace Axis.Pulsar.Core.Grammar.Results
{
    public interface IRecognitionResult<TValue>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <returns></returns>
        IRecognitionResult<TOut> MapAs<TOut>();
    }

    public static class RecognitionResult
    {
        #region Of
        public static IRecognitionResult<TValue> Of<TValue>(TValue value)
        {
            return new SuccessResult<TValue>(value);
        }

        public static IRecognitionResult<TValue> Of<TValue>(IRecognitionError error)
        {
            return new FailureResult<TValue>(error);
        }

        public static IRecognitionResult<TValue> Of<TValue, TError>(
            TError error)
            where TError : IRecognitionError
        {
            return new FailureResult<TValue>(error);
        }
        #endregion

        #region IS

        public static bool IsSuccess<TValue>(this
            IRecognitionResult<TValue> result,
            out TValue value)
        {
            if (result is SuccessResult<TValue> success)
            {
                value = success.Value;
                return true;
            }

            value = default!;
            return false;
        }

        public static bool IsSuccess<TValue>(this
            IRecognitionResult<TValue> result)
            => result.IsSuccess(out _);

        public static bool IsError<TValue>(this
            IRecognitionResult<TValue> result,
            out IRecognitionError error)
        {
            if (result is FailureResult<TValue> failure)
            {
                error = failure.Error;
                return true;
            }

            error = default!;
            return false;
        }

        public static bool IsError<TValue, TError>(this
            IRecognitionResult<TValue> result,
            out TError error)
            where TError : IRecognitionError
        {
            if (result is FailureResult<TValue> failure
                && failure.Error is TError terror)
            {
                error = terror;
                return true;
            }

            error = default!;
            return false;
        }

        public static bool IsError<TValue>(this
            IRecognitionResult<TValue> result)
            => result.IsError(out IRecognitionError _);

        #endregion

        #region Map

        public static IRecognitionResult<TOut> Map<TIn, TOut>(this
            IRecognitionResult<TIn> result,
            Func<TIn, TOut> map)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(map);

            if (result is SuccessResult<TIn> success)
                return new SuccessResult<TOut>(map.Invoke(success.Value));

            return new FailureResult<TOut>(((FailureResult<TIn>)result).Error);
        }

        public static IRecognitionResult<TOut> MapAs<TIn, TOut>(this
            IRecognitionResult<TIn> result)
            => result.Map(value => value.As<TOut>());

        public static IRecognitionResult<TValue> MapError<TValue>(
            this IRecognitionResult<TValue> result,
            Func<IRecognitionError, TValue> map)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(map);

            if (result is FailureResult<TValue> failure)
                return new SuccessResult<TValue>(map.Invoke(failure.Error));

            return result;
        }

        public static IRecognitionResult<TValue> MapError<TError, TValue>(
            this IRecognitionResult<TValue> result,
            Func<TError, TValue> errorMapper)
            where TError: IRecognitionError
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(errorMapper);

            if (result.IsError(out TError error))
                return new SuccessResult<TValue>(errorMapper.Invoke(error));

            return result;
        }

        #endregion

        #region Resolve

        public static TValue Resolve<TValue>(this IRecognitionResult<TValue> result)
        {
            return result switch
            {
                SuccessResult<TValue> success => success.Value,
                FailureResult<TValue> failure => throw new RecognitionException(failure.Error),
                _ => throw new InvalidOperationException(
                    $"Invalid result type: '{result?.GetType()}'")
            };
        }

        #endregion

        #region Consume

        public static void Consume<TValue>(this
            IRecognitionResult<TValue> result,
            Action<TValue> consumer)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(consumer);

            if (result is SuccessResult<TValue> success)
                consumer.Invoke(success.Value);
        }

        public static void ConsumeError<TValue>(
            this IRecognitionResult<TValue> result,
            Action<IRecognitionError> errorConsumer)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(errorConsumer);

            if (result is FailureResult<TValue> failure)
                errorConsumer.Invoke(failure.Error);
        }

        public static void ConsumeError<TError, TValue>(
            this IRecognitionResult<TValue> result,
            Action<TError> errorConsumer)
            where TError : IRecognitionError
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(errorConsumer);

            if (result.IsError(out TError error))
                errorConsumer.Invoke(error);
        }

        #endregion

        #region With

        public static IRecognitionResult<TValue> WithResult<TValue>(this
            IRecognitionResult<TValue> result,
            Action<TValue> consumer)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(consumer);

            if (result.IsSuccess(out var value))
                consumer.Invoke(value);

            return result;
        }

        public static IRecognitionResult<TValue> WithError<TValue, TError>(this
            IRecognitionResult<TValue> result,
            Action<IRecognitionError> errorConsumer)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(errorConsumer);

            if (result.IsError(out var value))
                errorConsumer.Invoke(value);

            return result;
        }

        public static IRecognitionResult<TValue> WithError<TValue, TError>(this
            IRecognitionResult<TValue> result,
            Action<TError> errorConsumer)
            where TError : IRecognitionError
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(errorConsumer);

            if (result.IsError(out TError value))
                errorConsumer.Invoke(value);

            return result;
        }

        #endregion

        #region Transform Error

        public static IRecognitionResult<TValue> TransformError<TValue>(
            this IRecognitionResult<TValue> result,
            Func<IRecognitionError, IRecognitionError> errorTransformer)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(errorTransformer);

            if (result is FailureResult<TValue> failure)
                return new FailureResult<TValue>(errorTransformer.Invoke(failure.Error));

            return result;
        }

        public static IRecognitionResult<TValue> TransformError<TError, TValue>(
            this IRecognitionResult<TValue> result,
            Func<TError, IRecognitionError> errorTransformer)
            where TError : IRecognitionError
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(errorTransformer);

            if (result.IsError(out TError error))
                return new FailureResult<TValue>(errorTransformer.Invoke(error));

            return result;
        }

        #endregion

        #region Fold

        public static IRecognitionResult<TIn> Fold<TIn>(this
            IEnumerable<IRecognitionResult<TIn>> results,
            Func<TIn, TIn, TIn> aggregator)
        {
            ArgumentNullException.ThrowIfNull(results);
            ArgumentNullException.ThrowIfNull(aggregator);

            TIn prev = default!;
            var isPrimed = false;
            foreach (var r in results)
            {
                if (!isPrimed)
                {
                    if (r.IsSuccess(out var _V))
                    {
                        prev = _V;
                        isPrimed = true;
                        continue;
                    }

                    else throw new InvalidOperationException(
                        $"Invalid parse result: failure-result");
                }

                if (r.IsSuccess(out var value))
                    prev = aggregator.Invoke(prev, value);

                else throw new InvalidOperationException(
                    $"Invalid parse result: failure-result");
            }

            return new SuccessResult<TIn>(prev);
        }

        public static IRecognitionResult<TOut> Fold<TIn, TOut>(this
            TOut seed,
            IEnumerable<IRecognitionResult<TIn>> results,
            Func<TOut, TIn, TOut> aggregator)
        {
            ArgumentNullException.ThrowIfNull(results);
            ArgumentNullException.ThrowIfNull(aggregator);

            var accumulator = seed;
            foreach(var result in results)
            {
                if (result.IsSuccess(out var value))
                    accumulator = aggregator.Invoke(accumulator, value);

                else throw new RecognitionException(result.As<FailureResult<TIn>>().Error);
            }

            return new SuccessResult<TOut>(accumulator);
        }

        #endregion
    }
}
