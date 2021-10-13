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
    }
}
