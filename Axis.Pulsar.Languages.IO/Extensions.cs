using Axis.Pulsar.Grammar.Utils;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Axis.Pulsar.Languages
{
    public static class Extensions
    {
        public static IEnumerable<T> GetFlags<T>(this T input)
        where T: struct, Enum
        {
            foreach (T value in Enum.GetValues(input.GetType()))
            {
                if (input.HasFlag(value))
                    yield return value;
            }
        }

        public static TOut Map<TIn, TOut>(this TIn @in, Func<TIn, TOut> func)
        {
            return func.Invoke(@in);
        }

        internal static string ApplyPatternEscape(this string input) => input.Replace("//", "/");

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

        public static IEnumerable<T> Enumerate<T>(params T[] values) => values;
    }
}
