using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Parser
{
    public static class Extensions
    {
        internal static bool IsNull<T>(this T obj) => obj == null;
        internal static bool IsEmpty<T>(this ICollection<T> collection) => collection.Count == 0;
        internal static bool IsNullOrEmpty<T>(this T[] array) => array == null || array.IsEmpty();

        internal static IEnumerable<T> Concat<T>(
            this IEnumerable<T> initialValues,
            IEnumerable<T> otherValue)
            => Enumerable.Concat(initialValues, otherValue);

        internal static IEnumerable<T> Concat<T>(
            this IEnumerable<T> initialValues,
            T addendum)
            => Enumerable.Concat(initialValues, new[] { addendum });

        internal static IEnumerable<T> Enumerate<T>(this T value) => new[] { value };

        internal static T ThrowIf<T>(this
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

        internal static T ThrowIf<T>(this
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

        internal static bool ContainsNull<T>(this IEnumerable<T> enm) => enm.Any(t => t == null);

        internal static bool IsNegative(this int value) => value < 0;

        internal static bool IsNegative(this int? value) => value < 0;


        internal static void ForAll<T>(this IEnumerable<T> @enum, Action<T> action)
        {
            foreach (var t in @enum)
                action.Invoke(t);
        }

        internal static bool NullOrEquals<T>(this T operand1, T operand2)
        where T : class
        {
            if (operand1 == null && operand2 == null)
                return true;

            return operand1 != null
                && operand2 != null
                && operand1.Equals(operand2);
        }

        internal static TOut Map<TIn, TOut>(this TIn @in, Func<TIn, TOut> mapper)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));

            return mapper.Invoke(@in);
        }

        internal static TOut MapIf<TIn, TOut>(this
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

        internal static TIn Use<TIn>(this TIn @in, Action<TIn> action)
        {
            action.Invoke(@in);
            return @in;
        }

        internal static void Consume<TIn>(this TIn @in, Action<TIn> consumer) => consumer.Invoke(@in);

        internal static bool ExactlyAll<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            var called = false;
            return enumerable
                .All(t => called = predicate.Invoke(t))
                && called;
        }

        internal static StringBuilder RemoveLast(this StringBuilder sb, int count)
        {
            if (sb == null)
                throw new ArgumentNullException(nameof(sb));

            else if (sb.Length < count)
                throw new IndexOutOfRangeException();

            return sb.Remove(sb.Length - count, count);
        }


        /// <summary>
        /// For every given <paramref name="escapeCharacters"/> flag, convert it's corresponding \{code} into the actual string value. e.g
        /// <code>
        /// var input = "abcd xyz \n another string";
        /// 
        /// if(escapeCharacters.HasFlag(EscapeCharacters.NewLine)) {
        ///     input = input.Replace("\\n","\n");
        /// }
        /// </code>
        /// </summary>
        /// <param name="input"></param>
        /// <param name="escapeCharacters"></param>
        /// <returns></returns>
        public static string ApplyEscape(this string input, EscapeCharacters escapeCharacters = EscapeCharacters.All)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var @string = input;

            if (escapeCharacters.HasFlag(EscapeCharacters.NewLine))
                @string = @string.Replace("\\n", "\n");

            if (escapeCharacters.HasFlag(EscapeCharacters.SingleQuote))
                @string = @string.Replace("\\'", "\'");

            if (escapeCharacters.HasFlag(EscapeCharacters.DoubleQuote))
                @string = @string.Replace("\\\"", "\"");

            if (escapeCharacters.HasFlag(EscapeCharacters.BackSlash))
                @string = @string.Replace("\\\\", "\\");

            if (escapeCharacters.HasFlag(EscapeCharacters.Null))
                @string = @string.Replace("\\0", "\0");

            if (escapeCharacters.HasFlag(EscapeCharacters.Backspace))
                @string = @string.Replace("\\b", "\b");

            if (escapeCharacters.HasFlag(EscapeCharacters.FormFeed))
                @string = @string.Replace("\\f", "\f");

            if (escapeCharacters.HasFlag(EscapeCharacters.CarriageReturn))
                @string = @string.Replace("\\r", "\r");

            if (escapeCharacters.HasFlag(EscapeCharacters.Alert))
                @string = @string.Replace("\\a", "\a");

            if (escapeCharacters.HasFlag(EscapeCharacters.HorizontalTab))
                @string = @string.Replace("\\t", "\t");

            if (escapeCharacters.HasFlag(EscapeCharacters.VerticalTab))
                @string = @string.Replace("\\v", "\v");

            if (escapeCharacters.HasFlag(EscapeCharacters.UTF16))
                @string = UTF16Pattern.Replace(@string, match =>
                {
                    var utf16Code = short.Parse(
                        match.Value[2..],
                        System.Globalization.NumberStyles.HexNumber);
                    var @char = (char)utf16Code;
                    return @char.ToString();
                });

            return @string;
        }

        private static readonly Regex UTF16Pattern = new Regex(@"\\u[0-9a-fA-F]{4}", RegexOptions.Compiled);

        /// <summary>
        /// see https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/strings/#string-escape-sequences
        /// </summary>
        [Flags]
        public enum EscapeCharacters
        {
            All = 4095,
            None = 0,

            NewLine = 1,
            SingleQuote = 2,
            DoubleQuote = 4,
            BackSlash = 8,
            Null = 16,
            Backspace = 32,
            FormFeed = 64,
            CarriageReturn = 128,
            Alert = 256,
            HorizontalTab = 512,
            VerticalTab = 1024,
            UTF16 = 2048,
        }
    }
}
