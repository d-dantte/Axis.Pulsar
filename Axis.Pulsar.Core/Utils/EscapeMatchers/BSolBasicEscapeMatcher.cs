using System.Text.RegularExpressions;
using static Axis.Pulsar.Core.Grammar.Nodes.DelimitedString;

namespace Axis.Pulsar.Core.Utils.EscapeMatchers
{
    public class BSolBasicEscapeMatcher :
        IEscapeTransformer
    {
        internal static Regex EscapeSequencePattern = new Regex(
            "\\\\['\"\\\\nrfbtv0a]",
            RegexOptions.Compiled);

        internal static Regex RawSequencePattern = new Regex(
            "['\"\\\\\n\r\f\b\t\v\0\a]",
            RegexOptions.Compiled);

        public string EscapeDelimiter => "\\";

        #region Escape Transformer

        public string Encode(string rawString)
        {
            if (rawString is null)
                return rawString!;

            return RawSequencePattern.Replace(rawString, match => match.Value switch
            {
                "\'" => "\\\'",
                "\"" => "\\\"",
                "\\" => "\\\\",
                "\n" => "\\n",
                "\r" => "\\r",
                "\f" => "\\f",
                "\b" => "\\b",
                "\t" => "\\t",
                "\v" => "\\v",
                "\0" => "\\0",
                "\a" => "\\a",
                _ => throw new InvalidOperationException($"Invalid basic escapable sequence: '{match.ValueSpan}'")
            });
        }

        public string Decode(string escapedString)
        {
            if (escapedString is null)
                return escapedString!;

            return EscapeSequencePattern.Replace(escapedString, match => match.Value switch
            {
                "\\\'" => "\'",
                "\\\"" => "\"",
                "\\\\" => "\\",
                "\\n" => "\n",
                "\\r" => "\r",
                "\\f" => "\f",
                "\\b" => "\b",
                "\\t" => "\t",
                "\\v" => "\v",
                "\\0" => "\0",
                "\\a" => "\a",
                _ => throw new InvalidOperationException($"Invalid basic escapabled sequence: '{match.ValueSpan}'")
            });
        }

        #endregion
    }
}
