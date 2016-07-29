using System;

namespace Axis.Pulsar
{
    public class PulsarException : Exception 
    { }

    public class AbortParseException: PulsarException
    {
    }
    public class SyntaxException : PulsarException
    { 
    }
    public class SemanticException: PulsarException
    {
    }
}
