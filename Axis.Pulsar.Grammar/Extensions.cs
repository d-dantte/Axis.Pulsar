using Axis.Pulsar.Grammar.Language;
using Axis.Pulsar.Grammar.Language.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;

namespace Axis.Pulsar.Grammar
{
    public static class Extensions
    {
        #region General
        internal static bool IsNull<T>(this T obj) => obj == null;
        internal static bool IsEmpty<T>(this ICollection<T> collection) => collection.Count == 0;
        internal static bool IsNullOrEmpty<T>(this T[] array) => array == null || array.IsEmpty();
        internal static bool IsNegative(this long value) => value < 0;
        internal static bool IsNegative(this int value) => value < 0;
        internal static bool IsNegative(this long? value) => value < 0;
        internal static bool IsNegative(this int? value) => value < 0;
        internal static bool ExactlyAll<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            var called = false;
            return enumerable
                .All(t => called = predicate.Invoke(t))
                && called;
        }

        internal static bool Is<T>(this object value) => value is T;

        internal static bool IsNot<T>(this object value) => value is not T;

        internal static bool ContainsNull<T>(this IEnumerable<T> enumerable) => enumerable.Any(t => t is null);
        internal static bool Contains<T, U>(this IEnumerable<T> enumerable) => enumerable.Any(t => t is U);

        internal static IEnumerable<T> Enumerate<T>(this T value) => new[] { value };

        internal static StringBuilder RemoveLast(this StringBuilder sb, int count = 1)
        {
            if (sb == null)
                throw new ArgumentNullException(nameof(sb));

            else if (sb.Length < count)
                throw new IndexOutOfRangeException();

            return sb.Remove(sb.Length - count, count);
        }

        internal static TOut Map<TIn, TOut>(this TIn @in, Func<TIn, TOut> mapper)
        {
            if (mapper is null)
                throw new ArgumentNullException(nameof(mapper));

            return mapper.Invoke(@in);
        }

        internal static TOut As<TOut>(this object value) => (TOut)value;

        internal static void ForAll<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            foreach (var t in enumerable)
                action.Invoke(t);
        }

        internal static Dictionary<K, V> ToDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> kvps)
        {
            if (kvps is null)
                throw new ArgumentNullException(nameof(kvps));

            return kvps.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value);
        }

        internal static IEnumerable<T> SelectMany<T>(this IEnumerable<IEnumerable<T>> enumerable) => enumerable.SelectMany(e => e);

        internal static string AsRuleExpressionString(this IRuleExpression expression)
        {
            return expression.Rules
                .Select(rule => rule.ToString())
                .Map(strings => string.Join(' ', strings))
                .Map(@string => $"[{@string}]{expression.Cardinality}");
        }

        internal static string ToString(this StringBuilder builder, int index)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            if (index < 0)
                throw new IndexOutOfRangeException(index.ToString());

            return builder.ToString(index, builder.Length - index);
        }

        /// <summary>
        /// Akin to a decorator
        /// </summary>
        /// <typeparam name="T">The type of the target instance</typeparam>
        /// <param name="value">The target instance</param>
        /// <param name="consumer">The consumer/decorator action</param>
        /// <returns>The target instance</returns>
        /// <exception cref="ArgumentNullException">If the consumer action is null</exception>
        internal static T With<T>(this T value, Action<T> consumer)
        {
            if (consumer == null)
                throw new ArgumentNullException(nameof(consumer));

            consumer.Invoke(value);
            return value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="consumer"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static IEnumerable<T> WithEach<T>(this IEnumerable<T> enumerable, Action<T> consumer)
        {
            if (consumer is null)
                throw new ArgumentNullException(nameof(consumer));

            return enumerable.Select(item =>
            {
                consumer.Invoke(item);
                return item;
            });
        }

        /// <summary>
        /// Verifies that the rule terminates in a <see cref="ProductionRef"/>, <see cref="EOF"/>, <see cref="Literal"/>,
        /// or <see cref="Pattern"/>
        /// </summary>
        /// <param name="rule">The rule</param>
        internal static bool IsTerminal(this IRule rule)
        {
            if (rule is null)
                throw new ArgumentNullException(nameof(rule));

            return rule switch
            {
                ProductionRef => true,
                IAtomicRule => true,
                ICompositeRule composite => composite.Rule.IsTerminal(),
                IAggregateRule aggregate => aggregate.Rules.ExactlyAll(IsTerminal),
                _ => false
            };
        }

        internal static bool NullOrTrue<T>(this T first, T second, Func<T, T, bool> predicate)
        {
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            if (first is null && second is null)
                return true;

            if (first is not null && second is not null)
                return predicate.Invoke(first, second);

            return false;
        }
        #endregion

        #region Exceptions
        internal static T ThrowIf<T>(this T value, Func<T, bool> predicate, Exception exception)
        {
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            if (exception is null)
                throw new ArgumentNullException(nameof(exception));

            if (predicate.Invoke(value))
            {
                if (exception.StackTrace == null) throw exception;
                else ExceptionDispatchInfo.Capture(exception).Throw();
            }

            return value;
        }

        internal static T ThrowIfNot<T>(this T value, Func<T, bool> predicate, Exception exception)
        {
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            if (exception is null)
                throw new ArgumentNullException(nameof(exception));

            if (!predicate.Invoke(value))
            {
                if (exception.StackTrace == null) throw exception;
                else ExceptionDispatchInfo.Capture(exception).Throw();
            }

            return value;
        }

        internal static T ThrowIfNull<T>(this T value, Exception exception)
        {
            if (exception is null)
                throw new ArgumentNullException(nameof(exception));

            if (value is null)
            {
                if (exception.StackTrace == null) throw exception;
                else ExceptionDispatchInfo.Capture(exception).Throw();
            }

            return value;
        }

        internal static T ThrowIfDefault<T>(this T value, Exception exception)
        {
            if (exception is null)
                throw new ArgumentNullException(nameof(exception));

            if (EqualityComparer<T>.Default.Equals(default, value))
            {
                if (exception.StackTrace == null) throw exception;
                else ExceptionDispatchInfo.Capture(exception).Throw();
            }

            return value;
        }
        #endregion

    }
}
