using System;

namespace Axis.Pulsar.Parser.Exceptions
{
    public class ParseException: Exception
    {
        public ParseError ParseError { get; }

        public ParseException(ParseError parseError, Exception otherException = null)
            :base(parseError.SymbolName, otherException)
        {
            ParseError = parseError;
        }
    }
}
