using System;

namespace Axis.Pulsar.Parser.Exceptions
{
    public class ParseException: Exception
    {
        public ParseError ParseError { get; }

        public ParseException(ParseError error, string message = null)
            : base(message ?? "Parse Error(s) were encountered")
        {
            ParseError = error;
        }
    }
}
