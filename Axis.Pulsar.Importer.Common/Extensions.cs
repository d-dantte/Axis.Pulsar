using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Axis.Pulsar.Importer.Tests")]

namespace Axis.Pulsar.Importer.Common
{
    internal static class Extensions
    {
        public static void ForAll<T>(this IEnumerable<T> @enum, Action<T> action)
        {
            foreach (var t in @enum)
                action.Invoke(t);
        }

        public static TOut Map<TIn, TOut>(this TIn @in, Func<TIn, TOut> func)
        {
            return func.Invoke(@in);
        }
        public static T As<T>(this object value)
        {
            try
            {
                if (value is IConvertible
                    && typeof(IConvertible).IsAssignableFrom(typeof(T)))
                    return (T)Convert.ChangeType(value, typeof(T));

                else return (T)value;
            }
            catch
            {
                return default;
            }
        }

        public static TValue GetOrAdd<TKey, TValue>(this
            Dictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TKey, TValue> mapper)
        {
            if (dictionary.TryGetValue(key, out TValue value))
                return value;

            else return dictionary[key] = mapper.Invoke(key);
        }
    }
}
