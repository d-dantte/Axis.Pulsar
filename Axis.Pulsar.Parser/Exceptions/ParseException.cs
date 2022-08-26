using System;

namespace Axis.Pulsar.Parser.Exceptions
{
    public class ParseException: Exception
    {
        public Parsers.IResult Result { get; }

        public ParseException(Parsers.IResult result)
        : base("An exception occured. See the result for more details")
        {
            Result = result;
        }
    }
}
