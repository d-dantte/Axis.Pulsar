using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using Axis.Luna.Extensions;
using static Axis.Pulsar.Core.Grammar.Rules.DelimitedString;

namespace Axis.Pulsar.Core.Utils.EscapeMatchers
{
    public class BSolAsciiEscapeMatcher :
        IEscapeSequenceMatcher,
        IEscapeTransformer
    {
        internal static Regex EscapeSequencePattern = new(
            "^\\\\x[a-fA-F0-9]{2}\\z",
            RegexOptions.Compiled);

        public readonly static ImmutableHashSet<int> UnprintableAsciiCharCodes = ImmutableHashSet.Create(
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
            21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
            31, 129, 143, 144, 157, 160, 173);

        public string EscapeDelimiter => "\\x";

        public bool TryMatchEscapeArgument(TokenReader reader, out Tokens tokens)
        {
            if (!reader.TryGetTokens(2, out tokens))
                return false;

            if (!byte.TryParse(tokens.AsSpan(), NumberStyles.HexNumber, null, out _))
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
                if (UnprintableAsciiCharCodes.Contains(rawString[index]))
                {
                    var prev = Tokens.Of(rawString, offset, index - offset);
                    if (!prev.IsEmpty)
                        substrings.Add(prev);

                    offset = index + 1;
                    substrings.Add(Tokens.Of($"\\x{(int)rawString[index]:x2}"));
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
