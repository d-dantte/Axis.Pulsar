using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace Axis.Pulsar.Parser
{
    internal static class Extensions
    {
        public static IEnumerable<T> Concat<T>(this T value, T otherValue)
        {
            yield return value;
            yield return otherValue;
        }

        public static IEnumerable<T> Concat<T>(this T value, IEnumerable<T> values)
        {
            yield return value;
            foreach (var t in values)
            {
                yield return t;
            }
        }

        public static IEnumerable<T> Concat<T>(this T value, params T[] values)
        {
            yield return value;
            foreach (var t in values)
            {
                yield return t;
            }
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> initialValues, T otherValue)
        {
            foreach (var t in initialValues)
            {
                yield return t;
            }
            yield return otherValue;
        }

        public static IEnumerable<T> Concat<T>(
            this IEnumerable<T> initialValues,
            params T[] otherValue)
            => Enumerable.Concat(initialValues, otherValue);

        public static IEnumerable<T> Concat<T>(
            this IEnumerable<T> initialValues,
            IEnumerable<T> otherValue)
            => Enumerable.Concat(initialValues, otherValue);

        public static T ThrowIf<T>(this 
            T value, Func<T, bool> predicate,
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

        public static bool IsNull<T>(this T value) where T : class => value == null;

        public static bool IsNotNull<T>(this T value) where T : class => value != null;

        public static bool IsDefault<T>(this T value)
        {
            if (typeof(T).IsClass)
                return value == null;

            else return default(T).Equals(value);
        }

        public static bool ContainsNull<T>(this IEnumerable<T> enm) => enm.Any(t => t == null);

        public static Func<IEnumerable<T>, bool> Contains<T>(T value)
        {
            return enm => enm.Any(t => value.Equals(t));
        }

        public static bool ContainsAny<T>(this IEnumerable<T> enm, params T[] values)
        {
            return enm.Any(t => values.Contains(t));
        }

        public static bool IsNegative(this int value) => value < 0;

        public static bool IsZero(this int value) => value == 0;

        public static bool IsPositive(this int value) => value > 0;

        public static Syntax.Symbol[] FlattenProduction(this Syntax.Symbol symbol)
        {
            //basically, if this isn't a production generated symbol, return it
            if (!symbol.Name.StartsWith("#"))
                return new[] { symbol };

            else
            {
                return symbol.Children
                    .Select(FlattenProduction)
                    .Aggregate(Enumerable.Empty<Syntax.Symbol>(), (enm, next) => enm.Concat(next))
                    .ToArray();
            }
        }
    }
}
