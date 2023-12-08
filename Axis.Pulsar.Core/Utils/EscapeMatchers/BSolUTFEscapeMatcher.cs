using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using Axis.Luna.Extensions;
using static Axis.Pulsar.Core.Grammar.Rules.DelimitedString;

namespace Axis.Pulsar.Core.Utils.EscapeMatchers
{
    public class BSolUTFEscapeMatcher :
        IEscapeTransformer
    {
        internal static Regex EscapeSequencePattern = new(
            "\\\\u[a-fA-F0-9]{4}",
            RegexOptions.Compiled);

        public string EscapeDelimiter => "\\u";

        #region Escape Transformer

        /// <summary>
        /// Transforms Ascii unprintable xters, and all xter > 255 into the \uffff format.
        /// </summary>
        /// <param name="rawString"></param>
        public string Encode(string rawString)
        {
            if (rawString is null)
                return rawString!;

            var substrings = new List<Tokens>();
            var offset = 0;
            for (int index = 0; index < rawString.Length; index++)
            {
                if (BSolAsciiEscapeMatcher.UnprintableAsciiCharCodes.Contains(rawString[index])
                    || rawString[index] > 255)
                {
                    var prev = Tokens.Of(rawString, offset, index - offset);
                    if (!prev.IsEmpty)
                        substrings.Add(prev);

                    offset = index + 1;
                    substrings.Add(Tokens.Of($"\\u{(int)rawString[index]:x4}"));
                }
            }

            return substrings
                .Append(Tokens.Of(rawString, offset))
                .Select(s => s.ToString())
                .JoinUsing("");
        }

        /// <summary>
        /// Transforms sequences in the \uffff format into the actual utf characters
        /// </summary>
        /// <param name="escapedString"></param>
        public string Decode(string escapedString)
        {
            return EscapeSequencePattern.Replace(escapedString, match =>
            {
                var utfCode = short.Parse(
                    match.Value.AsSpan(2),
                    NumberStyles.HexNumber);
                var @char = (char)utfCode;

                return @char.ToString();
            });
        }
        #endregion
    }
}
