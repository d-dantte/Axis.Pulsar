using System.Globalization;
using static Axis.Pulsar.Core.Grammar.Rules.DelimitedString;

namespace Axis.Pulsar.Core.Utils.EscapeMatchers
{
    public class BSolUTFEscapeMatcher :
        IEscapeSequenceMatcher,
        IEscapeTransformer
    {
        public string EscapeDelimiter => "\\u";

        public bool TryMatchEscapeArgument(TokenReader reader, out Tokens tokens)
        {
            if (!reader.TryGetTokens(4, out tokens))
                return false;

            if (!ushort.TryParse(tokens.AsSpan(), NumberStyles.HexNumber, null, out _))
                reader.Back();

            return true;
        }

        #region Escape Transformer
        public Tokens Decode(Tokens escapeSequence)
        {
            if (escapeSequence.Count != 6
                || !escapeSequence[0..2].Equals("\\u"))
                throw new FormatException($"Invalid ascii escape sequence: '{escapeSequence}'");

            if (ushort.TryParse(
                escapeSequence[2..].AsSpan(),
                NumberStyles.HexNumber,
                null, out var @charByte))
                return Tokens.Of(((char)charByte).ToString());

            throw new FormatException($"Invalid utf-16 escape sequence: '{escapeSequence}'");
        }

        public Tokens Encode(Tokens rawSequence)
        {
            if (rawSequence.Count != 1)
                throw new FormatException("Invalid Ascii sequence: must be a single character");

            var @byte = (ushort)rawSequence[0];
            return Tokens.Of($"\\u{@byte:x4}"); // <-- make sure the string encodes to 2 digits
        }
        #endregion
    }
}
