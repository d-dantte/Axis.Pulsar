using Axis.Pulsar.Parser.Input;

namespace Axis.Pulsar.Parser
{
    public interface IParser
    {
        bool TryParse(BufferedTokenReader tokenReader, out ParseResult result);
    }
}
