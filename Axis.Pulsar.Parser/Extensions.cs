using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace Axis.Pulsar.Parser
{
    internal static class Extensions
    {
        public static bool IsNull<T>(this T obj) => obj == null;
        public static bool IsEmpty<T>(this ICollection<T> collection) => collection.Count == 0;
        public static bool IsNullOrEmpty<T>(this T[] array) => array == null || array.IsEmpty();

        public static IEnumerable<T> Concat<T>(
            this IEnumerable<T> initialValues,
            IEnumerable<T> otherValue)
            => Enumerable.Concat(initialValues, otherValue);

        public static IEnumerable<T> Concat<T>(
            this IEnumerable<T> initialValues,
            T addendum)
            => Enumerable.Concat(initialValues, new[] { addendum });

        public static IEnumerable<T> Enumerate<T>(this T value) => new[] { value };

        public static T ThrowIf<T>(this
            T value,
            Func<T, bool> predicate,
            Func<T, Exception> exception = null)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            else if (predicate.Invoke(value))
            {
                var ex = exception?.Invoke(value) ?? new Exception("An exception occured");

                if (ex.StackTrace == null)
                    throw ex;

                else
                    ExceptionDispatchInfo.Capture(ex).Throw();
            }
            return value;
        }

        public static T ThrowIf<T>(this
            T value,
            Func<T, bool> predicate,
            Exception exception)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            else if (predicate.Invoke(value))
            {
                var ex = exception ?? new Exception("An exception occured");

                if (ex.StackTrace == null)
                    throw ex;

                else
                    ExceptionDispatchInfo.Capture(ex).Throw();
            }
            return value;
        }

        public static bool ContainsNull<T>(this IEnumerable<T> enm) => enm.Any(t => t == null);

        public static bool IsNegative(this int value) => value < 0;

        public static bool IsNegative(this int? value) => value < 0;


        public static void ForAll<T>(this IEnumerable<T> @enum, Action<T> action)
        {
            foreach (var t in @enum)
                action.Invoke(t);
        }

        public static bool NullOrEquals<T>(this T operand1, T operand2)
        where T : class
        {
            if (operand1 == null && operand2 == null)
                return true;

            return operand1 != null
                && operand2 != null
                && operand1.Equals(operand2);
        }

        public static TOut Map<TIn, TOut>(this TIn @in, Func<TIn, TOut> mapper)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));

            return mapper.Invoke(@in);
        }

        public static TOut MapIf<TIn, TOut>(this
            TIn @in,
            Func<TIn, bool> predicate,
            Func<TIn, TOut> mapper,
            Func<TOut> failureProducer = null)
        {
            if (failureProducer == null)
                failureProducer = () => default;

            if (predicate.Invoke(@in))
                return mapper.Invoke(@in);

            else return failureProducer.Invoke();
        }

        public static TIn Use<TIn>(this TIn @in, Action<TIn> action)
        {
            action.Invoke(@in);
            return @in;
        }

        public static void Consume<TIn>(this TIn @in, Action<TIn> consumer) => consumer.Invoke(@in);

        public static bool ExactlyAll<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            var called = false;
            return enumerable
                .All(t => called = predicate.Invoke(t))
                && called;
        }
    }
}
