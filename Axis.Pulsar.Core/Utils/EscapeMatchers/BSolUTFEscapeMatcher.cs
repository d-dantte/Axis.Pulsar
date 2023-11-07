using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using Axis.Luna.Extensions;
using static Axis.Pulsar.Core.Grammar.Rules.DelimitedString;

namespace Axis.Pulsar.Core.Utils.EscapeMatchers
{
    public class BSolUTFEscapeMatcher :
        IEscapeSequenceMatcher,
        IEscapeTransformer
    {
        internal static Regex EscapeSequencePattern = new Regex(
            "^\\\\u[a-fA-F0-9]{4}\\z",
            RegexOptions.Compiled);

        public string EscapeDelimiter => "\\u";

        public bool TryMatchEscapeArgument(TokenReader reader, out Tokens tokens)
        {
            if (!reader.TryGetTokens(4, out tokens))
                return false;

            if (!short.TryParse(tokens.AsSpan(), NumberStyles.HexNumber, null, out _))
                reader.Back();

            return true;
        }

        #region Escape Transformer
        public string Encode(string rawString)
        {
            if (rawString is null)
                return rawString!;


            var substrings = new List<Tokens>();
            var offset = 0;
            for (int index = 0; index < rawString.Length; index++)
            {
                if (BSolAsciiEscapeMatcher.UnprintableAsciiChars.Contains(rawString[index])
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
                .Select(s => s.ToString())
                .JoinUsing("");
        }

        public string Decode(string escapedString)
        {
            return EscapeSequencePattern.Replace(escapedString, match =>
            {
                    var asciiCode = short.Parse(
                        match.Value.AsSpan(2),
                        NumberStyles.HexNumber);
                    var @char = (char)asciiCode;
                    return @char.ToString();
            });
        }
        #endregion
    }
}
