using Axis.Luna.Common.Results;
using static Axis.Pulsar.Core.Grammar.Rules.DelimitedString;

namespace Axis.Pulsar.Core.Utils.EscapeMatchers
{
    public class BSolBasicEscapeMatcher :
        IEscapeSequenceMatcher,
        IEscapeTransformer
    {
        public string EscapeDelimiter => "\\";

        public bool TryMatchEscapeArgument(TokenReader reader, out Tokens tokens)
        {
            if (!reader.TryGetTokens(1, out tokens))
                return false;

            var matches = tokens[0] switch
            {
                '\'' => true,
                '\"' => true,
                '\\' => true,
                'n' => true,
                'r' => true,
                'f' => true,
                'b' => true,
                't' => true,
                'v' => true,
                '0' => true,
                'a' => true,
                _ => false
            };

            if (!matches)
                reader.Back();

            return matches;
        }

        #region Escape Transformer
        public Tokens Decode(Tokens escapeSequence)
        {
            if (escapeSequence.Count != 2)
                throw new FormatException($"Invalid basic escape sequence: '{escapeSequence}'");

            if (escapeSequence.Equals("\\\\"))
                return "\\";

            if (escapeSequence.Equals("\\\""))
                return "\"";

            if (escapeSequence.Equals("\\\'"))
                return "\'";

            if (escapeSequence.Equals("\\n"))
                return "\n";

            if (escapeSequence.Equals("\\r"))
                return "\r";

            if (escapeSequence.Equals("\\f"))
                return "\f";

            if (escapeSequence.Equals("\\b"))
                return "\b";

            if (escapeSequence.Equals("\\t"))
                return "\t";

            if (escapeSequence.Equals("\\v"))
                return "\v";

            if (escapeSequence.Equals("\\0"))
                return "\0";

            if (escapeSequence.Equals("\\a"))
                return "\a";

            throw new FormatException($"Invalid basic escape sequence: '{escapeSequence}'");
        }

        public Tokens Encode(Tokens rawSequence)
        {
            if (rawSequence.Equals("\\"))
                return "\\\\";

            if (rawSequence.Equals("\""))
                return "\\\"";

            if (rawSequence.Equals("\'"))
                return "\\\'";

            if (rawSequence.Equals("\n"))
                return "\\\n";

            if (rawSequence.Equals("\r"))
                return "\\\r";

            if (rawSequence.Equals("\f"))
                return "\\\f";

            if (rawSequence.Equals("\b"))
                return "\\\b";

            if (rawSequence.Equals("\t"))
                return "\\\t";

            if (rawSequence.Equals("\v"))
                return "\\\v";

            if (rawSequence.Equals("\0"))
                return "\\\0";

            if (rawSequence.Equals("\a"))
                return "\\\a";

            throw new FormatException($"Invalid raw sequence: '{rawSequence}'");
        }
        #endregion
    }
}
